using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace RTS.DOTS
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(SwarmSimulationSystem))]
    public partial class GameplaySimulationSystem : SystemBase
    {
        EntityQuery m_Actors, m_Workers, m_Buildings, m_Depots, m_Resources, m_Bodies;
        NativeParallelHashMap<Entity, TargetSnapshot> m_Targets;
        NativeParallelMultiHashMap<int2, TargetSnapshot> m_Spatial;
        NativeQueue<GameplayEvent> m_Events;
        NativeQueue<DamageEvent> m_Damage;
        NativeQueue<ProjectileRequest> m_Shots;
        NativeArray<byte> m_Walkable;
        NativeArray<int> m_Costs, m_Parents, m_Heap, m_Positions;
        float2 m_Origin;
        int2 m_Size;
        float m_CellSize;
        int m_PathCursor;
        SwarmSimulationSystem m_Combat;
        public Entity WalletEntity { get; private set; }
        public bool IsConfigured => m_Walkable.IsCreated;
        public int ActorCount => m_Actors.CalculateEntityCount();
        public long TickCount { get; private set; }
        public int PathBudget { get; set; } = 16;
        public PlayerWallet Wallet => EntityManager.GetComponentData<PlayerWallet>(WalletEntity);

        protected override void OnCreate()
        {
            m_Actors = GetEntityQuery(typeof(GameplayActor), typeof(CombatBody), typeof(GameplayWaypoint));
            m_Workers = GetEntityQuery(typeof(GameplayWorker), typeof(GameplayActor));
            m_Buildings = GetEntityQuery(typeof(GameplayConstruction), typeof(GameplayActor));
            m_Depots = GetEntityQuery(typeof(GameplayDepot), typeof(CombatBody));
            m_Resources = GetEntityQuery(typeof(GameplayResource));
            m_Bodies = GetEntityQuery(typeof(CombatBody));
            m_Targets = new NativeParallelHashMap<Entity, TargetSnapshot>(16384, Allocator.Persistent);
            m_Spatial = new NativeParallelMultiHashMap<int2, TargetSnapshot>(65536, Allocator.Persistent);
            m_Events = new NativeQueue<GameplayEvent>(Allocator.Persistent);
            m_Damage = new NativeQueue<DamageEvent>(Allocator.Persistent);
            m_Shots = new NativeQueue<ProjectileRequest>(Allocator.Persistent);
            WalletEntity = EntityManager.CreateEntity(typeof(PlayerWallet));
        }

        public void ConfigureGrid(float2 origin, int2 size, float cellSize, byte[] walkable)
        {
            if (size.x <= 0 || size.y <= 0 || cellSize <= 0 || walkable == null || walkable.Length != size.x * size.y)
                throw new ArgumentException("Invalid gameplay navigation grid.");
            Dependency.Complete(); DisposeGrid();
            m_Origin = origin; m_Size = size; m_CellSize = cellSize;
            m_Walkable = new NativeArray<byte>(walkable, Allocator.Persistent);
            m_Costs = new NativeArray<int>(walkable.Length, Allocator.Persistent);
            m_Parents = new NativeArray<int>(walkable.Length, Allocator.Persistent);
            m_Heap = new NativeArray<int>(walkable.Length, Allocator.Persistent);
            m_Positions = new NativeArray<int>(walkable.Length, Allocator.Persistent);
            foreach (var actor in SystemAPI.Query<RefRW<GameplayActor>>())
                if (actor.ValueRO.Moving != 0) actor.ValueRW.PathDirty = 1;
        }

        public void AttachActor(Entity entity, GameplayActor actor)
        {
            actor.Interval = math.max(0.1f, actor.Interval); actor.Delay = math.max(0, actor.Delay);
            actor.Windup = -1; actor.Facing = new float2(1, 0);
            EntityManager.AddComponentData(entity, actor);
            EntityManager.AddBuffer<GameplayWaypoint>(entity);
        }
        public void AttachWorker(Entity entity, GatheringProfileData[] profiles)
        {
            EntityManager.AddComponentData(entity, new GameplayWorker());
            var buffer = EntityManager.AddBuffer<GatheringProfileData>(entity);
            if (profiles != null) foreach (var profile in profiles) buffer.Add(profile);
        }
        public void AttachBuilding(Entity entity, int resourceMask, bool townHall, bool producer)
        {
            EntityManager.AddComponentData(entity, new GameplayConstruction());
            EntityManager.AddComponentData(entity, new GameplayDepot { ResourceMask = resourceMask, TownHall = (byte)(townHall ? 1 : 0) });
            if (producer) { EntityManager.AddComponentData(entity, new GameplayProducer()); EntityManager.AddBuffer<TrainingRequest>(entity); }
        }
        public Entity CreateResource(GameplayResource resource)
        { Entity entity = EntityManager.CreateEntity(typeof(GameplayResource)); EntityManager.SetComponentData(entity, resource); return entity; }
        public bool TryGetActor(Entity entity, out GameplayActor actor, out CombatBody body)
        {
            actor = default; body = default;
            if (!EntityManager.Exists(entity) || !EntityManager.HasComponent<GameplayActor>(entity)) return false;
            actor = EntityManager.GetComponentData<GameplayActor>(entity); body = EntityManager.GetComponentData<CombatBody>(entity); return true;
        }
        public bool TryGetWorker(Entity entity, out GameplayWorker worker)
        {
            worker = default; if (!EntityManager.Exists(entity) || !EntityManager.HasComponent<GameplayWorker>(entity)) return false;
            worker = EntityManager.GetComponentData<GameplayWorker>(entity); return true;
        }
        public bool TryGetResource(Entity entity, out GameplayResource resource)
        {
            resource = default; if (!EntityManager.Exists(entity) || !EntityManager.HasComponent<GameplayResource>(entity)) return false;
            resource = EntityManager.GetComponentData<GameplayResource>(entity); return true;
        }
        public bool TryClaim(Entity entity)
        {
            if (!TryGetResource(entity, out var resource) || resource.Claims >= resource.Capacity) return false;
            resource.Claims++; EntityManager.SetComponentData(entity, resource); return true;
        }
        public void Release(Entity entity)
        { if (TryGetResource(entity, out var resource)) { resource.Claims = math.max(0, resource.Claims - 1); EntityManager.SetComponentData(entity, resource); } }
        public void CancelWork(Entity entity)
        {
            if (!TryGetWorker(entity, out var worker)) return;
            bool wasSheltered = worker.Phase == WorkPhase.Sheltered;
            Release(worker.Node);
            if (EntityManager.Exists(worker.Structure) && EntityManager.HasComponent<GameplayConstruction>(worker.Structure))
            {
                var build = EntityManager.GetComponentData<GameplayConstruction>(worker.Structure);
                if (build.Worker == entity) { build.Worker = Entity.Null; EntityManager.SetComponentData(worker.Structure, build); }
            }
            worker.Node = worker.Depot = worker.Structure = worker.Shelter = Entity.Null; worker.Timer = 0; worker.Phase = WorkPhase.None;
            EntityManager.SetComponentData(entity, worker);
            var body = EntityManager.GetComponentData<CombatBody>(entity); if (body.Health > 0) body.Targetable = 1;
            if (wasSheltered && IsConfigured) body.Position = body.PreviousPosition = GameplayMath.NearestWalkable(body.Position, body.Radius, m_Origin, m_CellSize, m_Size, m_Walkable);
            EntityManager.SetComponentData(entity, body);
            var actor = EntityManager.GetComponentData<GameplayActor>(entity); actor.Target = Entity.Null; actor.Task = GameplayTask.None; actor.State = GameplayState.Idle; actor.Moving = 0; actor.PathDirty = 0; actor.Windup = -1;
            EntityManager.SetComponentData(entity, actor); EntityManager.GetBuffer<GameplayWaypoint>(entity).Clear();
        }
        public bool AssignResource(Entity entity, Entity node)
        {
            if (!TryGetWorker(entity, out var worker) || !TryGetResource(node, out var resource)) return false;
            if (worker.Node == node) return true;
            bool supported = false;
            foreach (var profile in EntityManager.GetBuffer<GatheringProfileData>(entity)) if (profile.ResourceType == resource.Type) { supported = true; break; }
            if (!supported || resource.Claims >= resource.Capacity) return false;
            bool deliverPreviousCargo = worker.Carried > 0 && worker.ResourceType != resource.Type;
            CancelWork(entity); TryClaim(node); worker = EntityManager.GetComponentData<GameplayWorker>(entity);
            worker.Node = node;
            if (!deliverPreviousCargo) worker.ResourceType = resource.Type;
            worker.Phase = deliverPreviousCargo ? WorkPhase.Depositing : WorkPhase.Gathering;
            EntityManager.SetComponentData(entity, worker); return true;
        }
        public void SetDestination(Entity entity, float2 destination, bool playerCommand = false)
        {
            if (!TryGetActor(entity, out var actor, out var body) || body.Health <= 0 || actor.Speed <= 0) return;
            if (playerCommand) { CancelWork(entity); actor = EntityManager.GetComponentData<GameplayActor>(entity); actor.Target = Entity.Null; actor.Task = GameplayTask.None; actor.SuppressAcquire = 1; }
            if (actor.Moving != 0 && math.distancesq(actor.Destination, destination) < 0.01f) return;
            actor.Destination = destination; actor.Moving = 1; actor.PathDirty = 1; actor.State = GameplayState.Moving;
            EntityManager.SetComponentData(entity, actor); EntityManager.GetBuffer<GameplayWaypoint>(entity).Clear();
        }
        public void Stop(Entity entity)
        {
            if (!TryGetActor(entity, out var actor, out _)) return;
            actor.Moving = 0; actor.PathDirty = 0; EntityManager.SetComponentData(entity, actor); EntityManager.GetBuffer<GameplayWaypoint>(entity).Clear();
        }
        public void Attack(Entity entity, Entity target)
        {
            if (!TryGetActor(entity, out var actor, out var body) || body.Health <= 0) return;
            CancelWork(entity); actor = EntityManager.GetComponentData<GameplayActor>(entity);
            actor.Target = target; actor.Task = target == Entity.Null ? GameplayTask.None : GameplayTask.Attack; actor.Windup = -1; actor.SuppressAcquire = 0;
            EntityManager.SetComponentData(entity, actor);
        }
        public void SetStance(Entity entity, bool offensive)
        {
            if (!TryGetActor(entity, out var actor, out _)) return;
            actor.Offensive = (byte)(offensive ? 1 : 0);
            if (!offensive) { actor.Target = Entity.Null; actor.Moving = 0; actor.State = GameplayState.Idle; actor.Task = GameplayTask.None; }
            EntityManager.SetComponentData(entity, actor);
        }
        public void AssignConstruction(Entity entity, Entity structure)
        {
            if (!TryGetWorker(entity, out _)) return;
            CancelWork(entity); var worker = EntityManager.GetComponentData<GameplayWorker>(entity); worker.Structure = structure; worker.Phase = WorkPhase.Constructing;
            EntityManager.SetComponentData(entity, worker);
            var actor = EntityManager.GetComponentData<GameplayActor>(entity); actor.Task = GameplayTask.Build; EntityManager.SetComponentData(entity, actor);
        }
        public void SetDepot(Entity entity, Entity depot)
        {
            if (!TryGetWorker(entity, out var worker) || worker.Carried <= 0) return;
            worker.Depot = depot; worker.Phase = WorkPhase.Depositing; EntityManager.SetComponentData(entity, worker);
        }
        public void Shelter(Entity entity, Entity hall)
        {
            if (!TryGetWorker(entity, out _)) return;
            CancelWork(entity); var worker = EntityManager.GetComponentData<GameplayWorker>(entity); worker.Shelter = hall; worker.Phase = WorkPhase.ReturningToShelter;
            EntityManager.SetComponentData(entity, worker);
            if (EntityManager.Exists(hall)) SetDestination(entity, EntityManager.GetComponentData<CombatBody>(hall).Position);
        }
        public void LeaveShelter(Entity entity, float2 origin, float2 exit)
        {
            if (!TryGetWorker(entity, out _)) return;
            CancelWork(entity); var body = EntityManager.GetComponentData<CombatBody>(entity);
            body.Position = body.PreviousPosition = GameplayMath.NearestWalkable(origin, body.Radius, m_Origin, m_CellSize, m_Size, m_Walkable); body.Targetable = 1;
            EntityManager.SetComponentData(entity, body); SetDestination(entity, exit);
        }
        public void BeginConstruction(Entity entity, float duration, int requestId)
        { EntityManager.SetComponentData(entity, new GameplayConstruction { Active = 1, Duration = math.max(0.01f, duration), RequestId = requestId }); }
        public void EnqueueTraining(Entity entity, int requestId, float duration)
        { EntityManager.GetBuffer<TrainingRequest>(entity).Add(new TrainingRequest { RequestId = requestId, Duration = math.max(0.01f, duration) }); }
        public void AddResources(int wood, int gold, int meat)
        {
            var wallet = Wallet; wallet.Wood += wood; wallet.Gold += gold; wallet.Meat += meat;
            EntityManager.SetComponentData(WalletEntity, wallet);
        }
        public bool TryTakeEvent(out GameplayEvent value) => m_Events.TryDequeue(out value);
        public void Remove(Entity entity)
        {
            if (!EntityManager.Exists(entity)) return;
            CancelWork(entity); EntityManager.DestroyEntity(entity);
        }
        public void ResetGameplay()
        {
            Dependency.Complete(); EntityManager.DestroyEntity(m_Actors); EntityManager.DestroyEntity(m_Resources);
            EntityManager.SetComponentData(WalletEntity, new PlayerWallet()); m_Events.Clear(); m_Damage.Clear(); m_Shots.Clear(); TickCount = 0;
        }

        protected override void OnUpdate()
        {
            if (!IsConfigured) return;
            m_Combat ??= World.GetOrCreateSystemManaged<SwarmSimulationSystem>();
            if (m_Combat.Paused) return;
            float dt = math.min(SystemAPI.Time.DeltaTime, 0.05f);
            using var workers = m_Workers.ToEntityArray(Allocator.TempJob);
            using var buildings = m_Buildings.ToEntityArray(Allocator.TempJob);
            using var depots = m_Depots.ToEntityArray(Allocator.TempJob);
            Dependency = new GameplayEconomyJob
            {
                Workers = workers, Buildings = buildings, Depots = depots,
                Actors = GetComponentLookup<GameplayActor>(), WorkerData = GetComponentLookup<GameplayWorker>(),
                Resources = GetComponentLookup<GameplayResource>(), Bodies = GetComponentLookup<CombatBody>(),
                Construction = GetComponentLookup<GameplayConstruction>(), Producers = GetComponentLookup<GameplayProducer>(),
                Wallets = GetComponentLookup<PlayerWallet>(), DepotData = GetComponentLookup<GameplayDepot>(true),
                Profiles = GetBufferLookup<GatheringProfileData>(true), Requests = GetBufferLookup<TrainingRequest>(),
                Events = m_Events, Wallet = WalletEntity, DeltaTime = dt, Walkable = m_Walkable, Origin = m_Origin, CellSize = m_CellSize, Size = m_Size
            }.Schedule(Dependency);
            Dependency.Complete();

            int bodies = m_Bodies.CalculateEntityCount();
            if (m_Targets.Capacity < bodies + 16) m_Targets.Capacity = math.ceilpow2(bodies + 16);
            if (m_Spatial.Capacity < bodies * 32 + 65536) m_Spatial.Capacity = bodies * 32 + 65536;
            m_Targets.Clear(); m_Spatial.Clear();
            Dependency = new BuildSwarmSpatialJob { Targets = m_Targets.AsParallelWriter(), Grid = m_Spatial.AsParallelWriter() }.ScheduleParallel(Dependency);
            Dependency = new GameplayActorJob
            {
                Targets = m_Targets, Grid = m_Spatial, Resources = GetComponentLookup<GameplayResource>(true),
                Construction = GetComponentLookup<GameplayConstruction>(true), Walkable = m_Walkable,
                Damage = m_Damage.AsParallelWriter(), Shots = m_Shots.AsParallelWriter(),
                DeltaTime = dt, CellSize = m_CellSize, Origin = m_Origin, Size = m_Size
            }.ScheduleParallel(Dependency);
            Dependency.Complete();
            using var actors = m_Actors.ToEntityArray(Allocator.TempJob);
            Dependency = new GameplayPathJob
            {
                Entities = actors, Bodies = GetComponentLookup<CombatBody>(true), Actors = GetComponentLookup<GameplayActor>(), Paths = GetBufferLookup<GameplayWaypoint>(),
                Walkable = m_Walkable, Costs = m_Costs, Parents = m_Parents, Heap = m_Heap, HeapPositions = m_Positions,
                Origin = m_Origin, CellSize = m_CellSize, Size = m_Size, StartIndex = m_PathCursor, Budget = math.max(1, PathBudget)
            }.Schedule(Dependency);
            Dependency.Complete();
            m_PathCursor = actors.Length > 0 ? (m_PathCursor + math.max(1, PathBudget)) % actors.Length : 0;
            while (m_Damage.TryDequeue(out var damage)) m_Combat.QueueDamage(damage.Target, damage.Amount, damage.Source);
            while (m_Shots.TryDequeue(out var shot)) m_Combat.SpawnProjectile(shot);
            TickCount++;
        }
        void DisposeGrid()
        {
            if (m_Walkable.IsCreated) m_Walkable.Dispose();
            if (m_Costs.IsCreated) m_Costs.Dispose(); if (m_Parents.IsCreated) m_Parents.Dispose();
            if (m_Heap.IsCreated) m_Heap.Dispose(); if (m_Positions.IsCreated) m_Positions.Dispose();
        }
        protected override void OnDestroy()
        { Dependency.Complete(); DisposeGrid(); m_Targets.Dispose(); m_Spatial.Dispose(); m_Events.Dispose(); m_Damage.Dispose(); m_Shots.Dispose(); }
    }
}
