using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Profiling;
using UnityEngine;
using Collider = Unity.Physics.Collider;

namespace RTS.DOTS
{
    public struct ExternalDamage
    {
        public int ExternalId, Amount;
        public Entity Source;
    }

    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial class SwarmSimulationSystem : SystemBase
    {
        NativeParallelMultiHashMap<int2, TargetSnapshot> m_Grid;
        NativeParallelHashMap<Entity, TargetSnapshot> m_Targets;
        NativeQueue<DamageEvent> m_Damage;
        NativeQueue<ProjectileRequest> m_Shots;
        NativeQueue<ExternalDamage> m_ExternalDamage;
        NativeParallelHashMap<Entity, int> m_DamageTotals;
        NativeParallelHashMap<Entity, Entity> m_DamageSources;
        NativeList<SwarmRenderData> m_RenderData;
        NativeArray<byte> m_Walkable;
        NativeArray<float2> m_Flow;
        NativeArray<int> m_Costs, m_FlowQueue;
        readonly Dictionary<float, BlobAssetReference<Collider>> m_Spheres = new();
        readonly Dictionary<Vector2, BlobAssetReference<Collider>> m_Boxes = new();
        BlobAssetReference<Collider> m_ProjectileShape;
        EntityArchetype m_AgentArchetype, m_ProjectileArchetype, m_ProxyArchetype;
        EntityQuery m_Agents, m_Projectiles, m_Bodies;
        float2 m_Origin, m_Goal;
        int2 m_Size, m_LastGoalCell;
        float m_CellSize;
        bool m_Configured, m_FlowDirty;
        Entity m_PrimaryTarget;
        static readonly ProfilerMarker TickMarker = new("RTS.DOTS.Tick");
        public bool Paused { get; set; }
        public double LastTickMilliseconds { get; private set; }
        public int AgentCount => m_Agents.CalculateEntityCount();
        public int ProjectileCount => m_Projectiles.CalculateEntityCount();
        public int TotalSpawned { get; private set; }
        public int TotalHits { get; private set; }
        public long TickCount { get; private set; }
        public int Generation { get; private set; }
        public bool IsConfigured => m_Configured;
        public NativeArray<SwarmRenderData> RenderData => m_RenderData.AsArray();
        public float2 Goal => m_Goal;

        protected override void OnCreate()
        {
            m_Grid = new NativeParallelMultiHashMap<int2, TargetSnapshot>(65536, Allocator.Persistent);
            m_Targets = new NativeParallelHashMap<Entity, TargetSnapshot>(16384, Allocator.Persistent);
            m_DamageTotals = new NativeParallelHashMap<Entity, int>(16384, Allocator.Persistent);
            m_DamageSources = new NativeParallelHashMap<Entity, Entity>(16384, Allocator.Persistent);
            m_Damage = new NativeQueue<DamageEvent>(Allocator.Persistent);
            m_Shots = new NativeQueue<ProjectileRequest>(Allocator.Persistent);
            m_ExternalDamage = new NativeQueue<ExternalDamage>(Allocator.Persistent);
            m_RenderData = new NativeList<SwarmRenderData>(32768, Allocator.Persistent);
            m_AgentArchetype = EntityManager.CreateArchetype(typeof(CombatBody), typeof(SwarmAgent));
            m_ProjectileArchetype = EntityManager.CreateArchetype(typeof(SwarmProjectile));
            m_ProxyArchetype = EntityManager.CreateArchetype(typeof(CombatBody));
            m_Agents = GetEntityQuery(typeof(SwarmAgent), typeof(CombatBody));
            m_Projectiles = GetEntityQuery(typeof(SwarmProjectile));
            m_Bodies = GetEntityQuery(typeof(CombatBody));
            m_ProjectileShape = CreateSphere(0.08f);
        }

        public void ConfigureGrid(float2 origin, int2 size, float cellSize, byte[] walkable)
        {
            if (size.x <= 0 || size.y <= 0 || cellSize <= 0 || walkable == null || walkable.Length != size.x * size.y)
                throw new ArgumentException("Invalid swarm navigation grid.");
            Dependency.Complete();
            DisposeGrid();
            m_Origin = origin; m_Size = size; m_CellSize = cellSize;
            m_Walkable = new NativeArray<byte>(walkable, Allocator.Persistent);
            m_Flow = new NativeArray<float2>(walkable.Length, Allocator.Persistent);
            m_Costs = new NativeArray<int>(walkable.Length, Allocator.Persistent);
            m_FlowQueue = new NativeArray<int>(walkable.Length, Allocator.Persistent);
            m_FlowDirty = true; m_Configured = true;
        }

        public void SetGoal(float2 position, Entity primaryTarget = default)
        {
            m_Goal = position; m_PrimaryTarget = primaryTarget;
            int2 cell = (int2)math.floor((position - m_Origin) / math.max(0.01f, m_CellSize));
            if (!math.all(cell == m_LastGoalCell)) { m_FlowDirty = true; m_LastGoalCell = cell; }
        }

        public Entity SpawnEnemy(float2 position, EnemySpawnSettings settings)
        {
            if (!m_Configured || !SwarmMath.Fits(position, math.max(0.05f, settings.Radius), m_Origin, m_CellSize, m_Size, m_Walkable)) return Entity.Null;
            Entity entity = EntityManager.CreateEntity(m_AgentArchetype);
            EntityManager.SetComponentData(entity, new CombatBody
            {
                Position = position, PreviousPosition = position, Radius = math.max(0.05f, settings.Radius),
                Health = math.max(1, settings.Health), Faction = 1, Targetable = 1,
                Collider = CreateSphere(math.max(0.05f, settings.Radius))
            });
            EntityManager.SetComponentData(entity, new SwarmAgent
            {
                Speed = math.clamp(settings.Speed, 0, 30), AttackRange = math.max(0.05f, settings.AttackRange),
                AttackInterval = math.max(0.1f, settings.AttackInterval), AttackDelay = math.max(0, settings.AttackDelay),
                DetectionRange = math.clamp(settings.DetectionRange, 1, 20), Damage = math.max(0, settings.Damage),
                VisualId = settings.VisualId, ProjectileVisualId = settings.ProjectileVisualId,
                Ranged = (byte)(settings.Ranged ? 1 : 0), Windup = -1, Target = m_PrimaryTarget,
                VisualScale = settings.VisualScale > 0 ? settings.VisualScale : 1,
                Projectile = settings.Projectile,
                ThinkTimer = (entity.Index % 13) * 0.02f
            });
            TotalSpawned++;
            return entity;
        }

        public bool TryResolveSpawnPosition(float2 requested, float radius, out float2 resolved)
        {
            resolved = requested;
            if (!m_Configured) return false;
            radius = math.max(0.05f, radius);
            resolved = GameplayMath.NearestWalkable(requested, radius, m_Origin, m_CellSize, m_Size, m_Walkable);
            return SwarmMath.Fits(resolved, radius, m_Origin, m_CellSize, m_Size, m_Walkable);
        }

        public Entity CreateProxy(int externalId, float2 position, float2 halfSize, float health, int faction = 0)
        {
            Entity entity = EntityManager.CreateEntity(m_ProxyArchetype);
            bool box = math.all(halfSize > 0);
            EntityManager.SetComponentData(entity, new CombatBody
            {
                ExternalId = externalId, Position = position, PreviousPosition = position,
                Radius = box ? math.length(halfSize) : 0.3f, Health = health, Faction = faction, Targetable = 1,
                Collider = box ? CreateBox(halfSize) : CreateSphere(0.3f)
            });
            return entity;
        }

        public Entity CreateActorBody(int externalId, float2 position, float radius, float health, int faction)
        {
            Entity entity = EntityManager.CreateEntity(m_ProxyArchetype);
            EntityManager.SetComponentData(entity, new CombatBody { ExternalId = externalId, Position = position,
                PreviousPosition = position, Radius = math.max(0.05f, radius), Health = health, Faction = faction,
                Targetable = 1, Collider = CreateSphere(math.max(0.05f, radius)) });
            return entity;
        }

        public void UpdateProxy(Entity entity, float2 position, float health, bool targetable)
        {
            if (!EntityManager.Exists(entity)) return;
            var body = EntityManager.GetComponentData<CombatBody>(entity);
            body.PreviousPosition = math.distancesq(body.Position, position) > 64 ? position : body.Position;
            body.Position = position; body.Health = health; body.Targetable = (byte)(targetable ? 1 : 0);
            EntityManager.SetComponentData(entity, body);
        }

        public bool TryGetBody(Entity entity, out CombatBody body)
        {
            body = default;
            if (entity == Entity.Null || !EntityManager.Exists(entity) || !EntityManager.HasComponent<CombatBody>(entity)) return false;
            body = EntityManager.GetComponentData<CombatBody>(entity);
            return body.Health > 0 && body.Targetable != 0;
        }

        public bool TryFindEnemy(float2 position, float radius, out Entity entity)
        {
            entity = Entity.Null;
            float best = radius * radius;
            int2 min = SwarmMath.SpatialCell(position - radius), max = SwarmMath.SpatialCell(position + radius);
            for (int y = min.y; y <= max.y; y++)
            for (int x = min.x; x <= max.x; x++)
            {
                if (!m_Grid.TryGetFirstValue(new int2(x, y), out var n, out var it)) continue;
                do
                {
                    if (n.Body.Faction != 1 || !TryGetBody(n.Entity, out var current)) continue;
                    float d = math.lengthsq(current.Position - position);
                    if (d <= best) { entity = n.Entity; best = d; }
                } while (m_Grid.TryGetNextValue(out n, ref it));
            }
            return entity != Entity.Null;
        }

        public void QueueDamage(Entity target, int amount, Entity source = default)
        { if (amount > 0 && TryGetBody(target, out _)) m_Damage.Enqueue(new DamageEvent { Target = target, Source = source, Amount = amount }); }
        public bool TryTakeExternalDamage(out ExternalDamage damage) => m_ExternalDamage.TryDequeue(out damage);

        public Entity SpawnProjectile(ProjectileRequest request)
        {
            if (request.Target != Entity.Null && !TryGetBody(request.Target, out _)) return Entity.Null;
            Entity entity = EntityManager.CreateEntity(m_ProjectileArchetype);
            float speed = math.clamp(request.Speed, 0.1f, 200);
            float distance = math.distance(request.Position, request.Aim);
            float duration = math.max(0.15f, distance / speed);
            if (request.Arc != 0 && request.DurationFactor > 0)
            {
                float minimum = math.max(0.01f, request.MinDuration);
                duration = math.clamp(distance * request.DurationFactor, minimum, math.max(minimum, request.MaxDuration));
            }
            EntityManager.SetComponentData(entity, new SwarmProjectile
            {
                Owner = request.Owner, Target = request.Target, Position = request.Position, PreviousPosition = request.Position,
                Start = request.Position, Aim = request.Aim, Damage = math.max(0, request.Damage), Faction = request.Faction,
                Speed = speed, Radius = 0.08f, Duration = duration,
                Lifetime = 12, Arc = request.Arc, ArcHeight = request.ArcHeight, VisualId = request.VisualId,
                VisualScale = request.VisualScale > 0 ? request.VisualScale : 1, Spinning = request.Spinning
            });
            return entity;
        }

        public void Retreat(float2 center, float radius)
        {
            foreach (var (agent, body) in SystemAPI.Query<RefRW<SwarmAgent>, RefRW<CombatBody>>())
            {
                if (body.ValueRO.Health <= 0) continue;
                agent.ValueRW.State = SwarmState.Retreating; agent.ValueRW.StateTimer = 0;
                agent.ValueRW.Windup = -1; agent.ValueRW.Target = Entity.Null;
                agent.ValueRW.RetreatGoal = center + math.normalizesafe(body.ValueRO.Position - center, new float2(1, 0)) * radius;
                body.ValueRW.Targetable = 0;
            }
        }

        public void Remove(Entity entity) { if (EntityManager.Exists(entity)) EntityManager.DestroyEntity(entity); }
        public void ResetSimulation()
        {
            Dependency.Complete();
            EntityManager.DestroyEntity(m_Agents); EntityManager.DestroyEntity(m_Projectiles);
            m_RenderData.Clear(); m_Grid.Clear(); m_Targets.Clear(); m_Damage.Clear(); m_Shots.Clear(); m_ExternalDamage.Clear();
            TotalSpawned = 0; TotalHits = 0;
            TickCount = 0; Generation++;
        }

        protected override void OnUpdate()
        {
            if (!m_Configured || Paused) return;
            using var marker = TickMarker.Auto();
            long started = Stopwatch.GetTimestamp();
            int bodyCount = m_Bodies.CalculateEntityCount(), projectileCount = ProjectileCount;
            int capacity = math.max(65536, bodyCount * 32 + 65536);
            if (m_Grid.Capacity < capacity) m_Grid.Capacity = capacity;
            if (m_Targets.Capacity < bodyCount + 16) m_Targets.Capacity = math.ceilpow2(bodyCount + 16);
            if (m_DamageTotals.Capacity < bodyCount + 16) m_DamageTotals.Capacity = math.ceilpow2(bodyCount + 16);
            if (m_DamageSources.Capacity < bodyCount + 16) m_DamageSources.Capacity = math.ceilpow2(bodyCount + 16);
            if (m_RenderData.Capacity < bodyCount + projectileCount + 256) m_RenderData.Capacity = math.ceilpow2(bodyCount + projectileCount + 256);
            m_Grid.Clear(); m_Targets.Clear();
            JobHandle flowHandle = Dependency;
            if (m_FlowDirty)
            {
                flowHandle = new FlowFieldJob { Walkable = m_Walkable, Costs = m_Costs, Queue = m_FlowQueue,
                    Directions = m_Flow, Size = m_Size, Goal = m_LastGoalCell }.Schedule(Dependency);
                m_FlowDirty = false;
            }
            var spatial = new BuildSwarmSpatialJob { Grid = m_Grid.AsParallelWriter(), Targets = m_Targets.AsParallelWriter() }.ScheduleParallel(Dependency);
            var agents = new SwarmAgentJob
            {
                Grid = m_Grid, Targets = m_Targets, Walkable = m_Walkable, Flow = m_Flow,
                Damage = m_Damage.AsParallelWriter(), Shots = m_Shots.AsParallelWriter(),
                Origin = m_Origin, Size = m_Size, CellSize = m_CellSize, Goal = m_Goal,
                PrimaryTarget = m_PrimaryTarget, DeltaTime = math.min(SystemAPI.Time.DeltaTime, 0.05f)
            }.ScheduleParallel(JobHandle.CombineDependencies(spatial, flowHandle));
            agents.Complete(); // Snapshot boundary: queries below must see this tick's agent motion.
            m_Grid.Clear(); m_Targets.Clear();
            spatial = new BuildSwarmSpatialJob { Grid = m_Grid.AsParallelWriter(), Targets = m_Targets.AsParallelWriter() }.ScheduleParallel(default(JobHandle));
            Dependency = new SwarmProjectileJob { Grid = m_Grid, Targets = m_Targets, Damage = m_Damage.AsParallelWriter(),
                CastShape = m_ProjectileShape, DeltaTime = math.min(SystemAPI.Time.DeltaTime, 0.05f) }.ScheduleParallel(spatial);
            Dependency.Complete();
            ApplyDamage();
            new CommitProxyMotionJob().ScheduleParallel(default(JobHandle)).Complete();
            while (m_Shots.TryDequeue(out var shot)) SpawnProjectile(shot);
            int renderCapacity = AgentCount + ProjectileCount + 16;
            if (m_RenderData.Capacity < renderCapacity) m_RenderData.Capacity = math.ceilpow2(renderCapacity);
            m_RenderData.Clear();
            Dependency = new AgentRenderJob { Output = m_RenderData.AsParallelWriter() }.ScheduleParallel(default(JobHandle));
            Dependency = new ProjectileRenderJob { Output = m_RenderData.AsParallelWriter() }.ScheduleParallel(Dependency);
            using var commands = new EntityCommandBuffer(Allocator.TempJob);
            var writer = commands.AsParallelWriter();
            Dependency = new CleanupAgentJob { Commands = writer }.ScheduleParallel(Dependency);
            Dependency = new CleanupProjectileJob { Commands = writer }.ScheduleParallel(Dependency);
            Dependency.Complete(); commands.Playback(EntityManager);
            LastTickMilliseconds = (Stopwatch.GetTimestamp() - started) * 1000.0 / Stopwatch.Frequency;
            TickCount++;
        }

        void ApplyDamage()
        {
            m_DamageTotals.Clear();
            m_DamageSources.Clear();
            while (m_Damage.TryDequeue(out var hit))
            {
                if (!TryGetBody(hit.Target, out var body) || body.Faction < 0) continue;
                m_DamageTotals.TryGetValue(hit.Target, out int previous);
                m_DamageTotals[hit.Target] = previous + hit.Amount;
                m_DamageSources.TryAdd(hit.Target, hit.Source);
                TotalHits++;
            }
            foreach (var entry in m_DamageTotals)
            {
                if (!TryGetBody(entry.Key, out var body)) continue;
                body.Health -= entry.Value;
                EntityManager.SetComponentData(entry.Key, body);
                m_DamageSources.TryGetValue(entry.Key, out var source);
                if (EntityManager.HasComponent<GameplayActor>(entry.Key))
                {
                    var actor = EntityManager.GetComponentData<GameplayActor>(entry.Key);
                    actor.LastDamager = source; actor.DamageSequence++;
                    if ((actor.Kind == GameplayKind.Soldier || actor.Kind == GameplayKind.Archer || actor.Kind == GameplayKind.Tower)
                        && actor.Target == Entity.Null && TryGetBody(source, out var attacker) && attacker.Faction != body.Faction)
                        actor.Target = source;
                    EntityManager.SetComponentData(entry.Key, actor);
                }
                else if (body.ExternalId != 0) m_ExternalDamage.Enqueue(new ExternalDamage { ExternalId = body.ExternalId, Amount = entry.Value, Source = source });
            }
        }

        BlobAssetReference<Collider> CreateSphere(float radius)
        {
            if (m_Spheres.TryGetValue(radius, out var collider)) return collider;
            collider = Unity.Physics.SphereCollider.Create(new SphereGeometry { Center = 0, Radius = radius }, CollisionFilter.Default);
            m_Spheres.Add(radius, collider); return collider;
        }
        BlobAssetReference<Collider> CreateBox(float2 halfSize)
        {
            Vector2 key = new(halfSize.x, halfSize.y);
            if (m_Boxes.TryGetValue(key, out var collider)) return collider;
            collider = Unity.Physics.BoxCollider.Create(new BoxGeometry { Center = 0, Orientation = quaternion.identity,
                Size = new float3(halfSize * 2, 1), BevelRadius = 0 }, CollisionFilter.Default);
            m_Boxes.Add(key, collider); return collider;
        }
        void DisposeGrid()
        {
            if (m_Walkable.IsCreated) m_Walkable.Dispose();
            if (m_Flow.IsCreated) m_Flow.Dispose();
            if (m_Costs.IsCreated) m_Costs.Dispose();
            if (m_FlowQueue.IsCreated) m_FlowQueue.Dispose();
        }
        protected override void OnDestroy()
        {
            Dependency.Complete(); DisposeGrid();
            m_Grid.Dispose(); m_Targets.Dispose(); m_Damage.Dispose(); m_Shots.Dispose();
            m_ExternalDamage.Dispose(); m_DamageTotals.Dispose(); m_RenderData.Dispose();
            m_DamageSources.Dispose();
            foreach (var collider in m_Spheres.Values) if (collider.IsCreated) collider.Dispose();
            foreach (var collider in m_Boxes.Values) if (collider.IsCreated) collider.Dispose();
        }
    }

    [Unity.Burst.BurstCompile]
    [WithNone(typeof(SwarmAgent))]
    public partial struct CommitProxyMotionJob : IJobEntity
    {
        void Execute(ref CombatBody body) { body.PreviousPosition = body.Position; }
    }

    [Unity.Burst.BurstCompile]
    public partial struct CleanupAgentJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter Commands;
        void Execute([EntityIndexInQuery] int index, Entity entity, in SwarmAgent agent, in CombatBody body)
        {
            if ((agent.State == SwarmState.Dead && agent.StateTimer >= 0.8f)
                || (agent.State == SwarmState.Retreating && body.Health <= 0)) Commands.DestroyEntity(index, entity);
        }
    }
    [Unity.Burst.BurstCompile]
    public partial struct CleanupProjectileJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter Commands;
        void Execute([EntityIndexInQuery] int index, Entity entity, in SwarmProjectile shot)
        { if (shot.Consumed != 0) Commands.DestroyEntity(index, entity); }
    }
}
