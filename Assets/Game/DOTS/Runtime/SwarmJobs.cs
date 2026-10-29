using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

namespace RTS.DOTS
{
    [BurstCompile]
    public partial struct BuildSwarmSpatialJob : IJobEntity
    {
        public NativeParallelMultiHashMap<int2, TargetSnapshot>.ParallelWriter Grid;
        public NativeParallelHashMap<Entity, TargetSnapshot>.ParallelWriter Targets;

        void Execute(Entity entity, in CombatBody body)
        {
            if (body.Targetable == 0 || body.Health <= 0) return;
            var snapshot = new TargetSnapshot { Entity = entity, Body = body };
            Targets.TryAdd(entity, snapshot);
            int2 min = SwarmMath.SpatialCell(math.min(body.PreviousPosition, body.Position) - body.Radius);
            int2 max = SwarmMath.SpatialCell(math.max(body.PreviousPosition, body.Position) + body.Radius);
            for (int y = min.y; y <= max.y; y++)
            for (int x = min.x; x <= max.x; x++) Grid.Add(new int2(x, y), snapshot);
        }
    }

    [BurstCompile]
    public partial struct SwarmAgentJob : IJobEntity
    {
        [ReadOnly] public NativeParallelMultiHashMap<int2, TargetSnapshot> Grid;
        [ReadOnly] public NativeParallelHashMap<Entity, TargetSnapshot> Targets;
        [ReadOnly] public NativeArray<byte> Walkable;
        [ReadOnly] public NativeArray<float2> Flow;
        public NativeQueue<DamageEvent>.ParallelWriter Damage;
        public NativeQueue<ProjectileRequest>.ParallelWriter Shots;
        public Entity PrimaryTarget;
        public float DeltaTime, CellSize;
        public float2 Origin, Goal;
        public int2 Size;

        void Execute(Entity entity, ref CombatBody body, ref SwarmAgent agent)
        {
            body.PreviousPosition = body.Position;
            agent.StateTimer += DeltaTime;
            if (body.Health <= 0)
            {
                if (agent.State != SwarmState.Dead) agent.StateTimer = 0;
                agent.State = SwarmState.Dead;
                body.Targetable = 0;
                agent.Velocity = 0;
                return;
            }
            if (agent.State == SwarmState.Retreating)
            {
                Move(entity, ref body, ref agent, agent.RetreatGoal, true);
                if (math.distance(body.Position, agent.RetreatGoal) < 0.6f || agent.StateTimer > 12)
                    body.Health = 0;
                return;
            }

            agent.ThinkTimer -= DeltaTime;
            bool hasTarget = Targets.TryGetValue(agent.Target, out var target) && target.Body.Faction != body.Faction;
            if (agent.ThinkTimer <= 0)
            {
                agent.ThinkTimer = 0.25f + (entity.Index % 7) * 0.013f;
                float best = agent.DetectionRange * agent.DetectionRange;
                int2 min = SwarmMath.SpatialCell(body.Position - agent.DetectionRange);
                int2 max = SwarmMath.SpatialCell(body.Position + agent.DetectionRange);
                int examined = 0;
                for (int y = min.y; y <= max.y; y++)
                for (int x = min.x; x <= max.x; x++)
                {
                    if (!Grid.TryGetFirstValue(new int2(x, y), out var candidate, out var iterator)) continue;
                    do
                    {
                        if (++examined > 128) break;
                        if (candidate.Body.Faction != 0) continue;
                        float d = math.lengthsq(candidate.Body.Position - body.Position);
                        if (d < best) { best = d; target = candidate; agent.Target = candidate.Entity; hasTarget = true; }
                    } while (Grid.TryGetNextValue(out candidate, ref iterator));
                }
                if (!hasTarget && Targets.TryGetValue(PrimaryTarget, out target))
                { agent.Target = PrimaryTarget; hasTarget = true; }
            }

            agent.Cooldown = math.max(0, agent.Cooldown - DeltaTime);
            float distance = float.MaxValue;
            if (hasTarget && target.Body.Collider.IsCreated)
            {
                var input = new PointDistanceInput { Position = new float3(body.Position - target.Body.Position, 0),
                    MaxDistance = float.MaxValue, Filter = CollisionFilter.Default };
                if (target.Body.Collider.Value.CalculateDistance(input, out DistanceHit hit)) distance = math.max(0, hit.Distance);
            }
            if (agent.Windup >= 0)
            {
                agent.Windup -= DeltaTime;
                if (agent.Windup <= 0)
                {
                    agent.Windup = -1;
                    if (hasTarget && distance <= agent.AttackRange + 0.25f)
                    {
                        if (agent.Ranged != 0) Shots.Enqueue(new ProjectileRequest
                        {
                            Owner = entity, Target = agent.Target, Position = body.Position,
                            Aim = target.Body.Position, Damage = agent.Damage, Faction = body.Faction,
                            VisualId = agent.ProjectileVisualId, Speed = agent.Projectile.Speed,
                            ArcHeight = agent.Projectile.ArcHeight, Arc = agent.Projectile.Arc, Spinning = agent.Projectile.Spinning,
                            VisualScale = agent.Projectile.VisualScale, MinDuration = agent.Projectile.MinDuration,
                            MaxDuration = agent.Projectile.MaxDuration, DurationFactor = agent.Projectile.DurationFactor
                        });
                        else Damage.Enqueue(new DamageEvent { Target = agent.Target, Source = entity, Amount = agent.Damage });
                    }
                }
            }
            if (hasTarget && distance <= agent.AttackRange)
            {
                if (agent.State != SwarmState.Attacking) agent.StateTimer = 0;
                agent.State = SwarmState.Attacking;
                agent.Velocity = math.normalizesafe(target.Body.Position - body.Position) * 0.001f;
                if (agent.Cooldown <= 0 && agent.Windup < 0)
                { agent.Cooldown = agent.AttackInterval; agent.Windup = agent.AttackDelay; agent.StateTimer = 0; }
                return;
            }
            if (agent.State != SwarmState.Moving) agent.StateTimer = 0;
            agent.State = SwarmState.Moving;
            Move(entity, ref body, ref agent, hasTarget ? target.Body.Position : Goal, false);
        }

        void Move(Entity entity, ref CombatBody body, ref SwarmAgent agent, float2 destination, bool retreat)
        {
            float2 direction = math.normalizesafe(destination - body.Position);
            int2 cell = (int2)math.floor((body.Position - Origin) / CellSize);
            if (!retreat && SwarmMath.InBounds(cell, Size))
            {
                float2 flow = Flow[SwarmMath.GridIndex(cell, Size)];
                // Direct steering only with a clear short corridor. Otherwise follow the shared field.
                bool direct = math.distancesq(body.Position, destination) < 16;
                if (direct)
                {
                    for (int i = 1; i <= 8; i++)
                        if (!SwarmMath.Fits(math.lerp(body.Position, destination, i / 8f), body.Radius, Origin, CellSize, Size, Walkable))
                        { direct = false; break; }
                }
                if (!direct) direction = SwarmMath.FlowDirection(body.Position, flow, Origin, CellSize);
            }
            float2 separation = 0;
            int2 center = SwarmMath.SpatialCell(body.Position);
            int count = 0;
            for (int y = -1; y <= 1; y++)
            for (int x = -1; x <= 1; x++)
            {
                int2 c = center + new int2(x, y);
                if (!Grid.TryGetFirstValue(c, out var n, out var it)) continue;
                do
                {
                    if (count >= 48) break;
                    if (n.Entity == entity || n.Body.Faction != body.Faction || !math.all(SwarmMath.SpatialCell(n.Body.Position) == c)) continue;
                    count++;
                    float2 delta = body.Position - n.Body.Position;
                    float length = math.length(delta), preferred = body.Radius + n.Body.Radius + 0.12f;
                    if (length < preferred)
                    {
                        float2 away = length > 0.001f ? delta / length : math.normalizesafe(new float2((entity.Index % 11) - 5, (entity.Index % 7) - 3), new float2(1, 0));
                        separation += away * (preferred - length) / math.max(preferred, 0.1f);
                    }
                } while (Grid.TryGetNextValue(out n, ref it));
            }
            float2 desired = math.normalizesafe(direction + separation * 2.5f) * agent.Speed;
            agent.Velocity = math.lerp(agent.Velocity, desired, math.saturate(DeltaTime * 12));
            body.Position = SwarmMath.MoveOnGrid(body.Position, agent.Velocity * DeltaTime, body.Radius, Origin, CellSize, Size, Walkable);
        }
    }

    [BurstCompile]
    public unsafe partial struct SwarmProjectileJob : IJobEntity
    {
        [ReadOnly] public NativeParallelMultiHashMap<int2, TargetSnapshot> Grid;
        [ReadOnly] public NativeParallelHashMap<Entity, TargetSnapshot> Targets;
        public NativeQueue<DamageEvent>.ParallelWriter Damage;
        [ReadOnly] public BlobAssetReference<Collider> CastShape;
        public float DeltaTime;

        void Execute(ref SwarmProjectile shot)
        {
            if (shot.Consumed != 0) return;
            shot.PreviousPosition = shot.Position;
            shot.Age += DeltaTime;
            if (shot.Age >= shot.Lifetime) { shot.Consumed = 1; return; }
            if (Targets.TryGetValue(shot.Target, out var aimed)) shot.Aim = aimed.Body.Position;
            else if (shot.Target != Entity.Null) { shot.Consumed = 1; return; }
            if (shot.Arc != 0) shot.Position = math.lerp(shot.Start, shot.Aim, math.saturate(shot.Age / shot.Duration));
            else
            {
                float2 delta = shot.Aim - shot.Position;
                shot.Position += math.normalizesafe(delta) * math.min(math.length(delta), shot.Speed * DeltaTime);
            }

            float closest = float.MaxValue;
            Entity hitEntity = Entity.Null;
            int2 min = SwarmMath.SpatialCell(math.min(shot.Position, shot.PreviousPosition) - shot.Radius);
            int2 max = SwarmMath.SpatialCell(math.max(shot.Position, shot.PreviousPosition) + shot.Radius);
            for (int y = min.y; y <= max.y; y++)
            for (int x = min.x; x <= max.x; x++)
            {
                if (!Grid.TryGetFirstValue(new int2(x, y), out var target, out var iterator)) continue;
                do
                {
                    if (target.Entity == shot.Owner || target.Body.Faction == shot.Faction || !target.Body.Collider.IsCreated) continue;
                    // Relative sweep also handles a moving victim crossing a stationary projectile.
                    var input = new ColliderCastInput
                    {
                        Collider = (Collider*)CastShape.GetUnsafePtr(), Orientation = quaternion.identity,
                        Start = new float3(shot.PreviousPosition - target.Body.PreviousPosition, 0),
                        End = new float3(shot.Position - target.Body.Position, 0)
                    };
                    if (target.Body.Collider.Value.CastCollider(input, out ColliderCastHit hit) && hit.Fraction < closest)
                    { closest = hit.Fraction; hitEntity = target.Entity; }
                } while (Grid.TryGetNextValue(out target, ref iterator));
            }
            if (hitEntity != Entity.Null)
            {
                Damage.Enqueue(new DamageEvent { Source = shot.Owner, Target = hitEntity, Amount = shot.Damage });
                shot.Consumed = 1;
            }
            else if (math.distancesq(shot.Position, shot.Aim) < 0.0001f) shot.Consumed = 1;
        }
    }

    [BurstCompile]
    public partial struct AgentRenderJob : IJobEntity
    {
        public NativeList<SwarmRenderData>.ParallelWriter Output;
        void Execute(Entity entity, in CombatBody body, in SwarmAgent agent)
        {
            Output.AddNoResize(new SwarmRenderData
            {
                Entity = entity, Position = body.Position, Direction = agent.Velocity, Health = body.Health,
                Age = agent.StateTimer, State = agent.State, VisualId = agent.VisualId, Faction = body.Faction, VisualScale = agent.VisualScale
            });
        }
    }

    [BurstCompile]
    public partial struct ProjectileRenderJob : IJobEntity
    {
        public NativeList<SwarmRenderData>.ParallelWriter Output;
        void Execute(Entity entity, in SwarmProjectile shot)
        {
            if (shot.Consumed != 0) return;
            float t = math.saturate(shot.Age / shot.Duration);
            Output.AddNoResize(new SwarmRenderData
            {
                Entity = entity, Position = shot.Position, Direction = shot.Position - shot.PreviousPosition,
                Height = shot.Arc != 0 ? 4 * shot.ArcHeight * t * (1 - t) : 0,
                Age = shot.Age, VisualId = shot.VisualId, Faction = shot.Faction, Projectile = 1, VisualScale = shot.VisualScale,
                Spinning = shot.Spinning
            });
        }
    }
}
