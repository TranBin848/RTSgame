using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;

namespace RTS.DOTS
{
    public static class GameplayMath
    {
        public static float2 NearestWalkable(float2 position, float radius, float2 origin, float cellSize, int2 size, NativeArray<byte> walkable)
        {
            if (!walkable.IsCreated || SwarmMath.Fits(position, radius, origin, cellSize, size, walkable)) return position;
            int2 center = math.clamp((int2)math.floor((position - origin) / cellSize), 0, size - 1);
            float best = float.MaxValue; float2 result = position;
            for (int y = -16; y <= 16; y++)
            for (int x = -16; x <= 16; x++)
            {
                int2 cell = center + new int2(x, y); if (!SwarmMath.InBounds(cell, size)) continue;
                float2 candidate = origin + (new float2(cell) + 0.5f) * cellSize;
                float d = math.distancesq(position, candidate);
                if (d < best && SwarmMath.Fits(candidate, radius, origin, cellSize, size, walkable)) { result = candidate; best = d; }
            }
            return result;
        }
        public static float DistanceToBody(float2 position, CombatBody body)
        {
            if (body.Collider.IsCreated && body.Collider.Value.CalculateDistance(new PointDistanceInput
                { Position = new float3(position - body.Position, 0), MaxDistance = float.MaxValue, Filter = CollisionFilter.Default }, out DistanceHit hit))
                return math.max(0, hit.Distance);
            return math.max(0, math.distance(position, body.Position) - body.Radius);
        }
    }

    // Bounded request processing shares one persistent scratch arena rather than allocating a grid per actor.
    [BurstCompile]
    public struct GameplayPathJob : IJob
    {
        [ReadOnly] public NativeArray<Entity> Entities;
        [ReadOnly] public ComponentLookup<CombatBody> Bodies;
        public ComponentLookup<GameplayActor> Actors;
        public BufferLookup<GameplayWaypoint> Paths;
        [ReadOnly] public NativeArray<byte> Walkable;
        public NativeArray<int> Costs, Parents, Heap, HeapPositions;
        public float2 Origin;
        public float CellSize;
        public int2 Size;
        public int StartIndex, Budget;
        int m_HeapCount;
        int2 m_Goal;

        public void Execute()
        {
            int processed = 0;
            for (int offset = 0; offset < Entities.Length && processed < Budget; offset++)
            {
                Entity entity = Entities[(StartIndex + offset) % Entities.Length];
                var actor = Actors[entity];
                if (actor.PathDirty == 0 || actor.Moving == 0 || Bodies[entity].Health <= 0) continue;
                processed++;
                var path = Paths[entity]; path.Clear();
                var body = Bodies[entity];
                int2 start = math.clamp((int2)math.floor((body.Position - Origin) / CellSize), 0, Size - 1);
                int2 goal = math.clamp((int2)math.floor((actor.Destination - Origin) / CellSize), 0, Size - 1);
                if (!FindGoal(ref goal, actor.Destination, body.Radius))
                { actor.PathDirty = 0; actor.Moving = 0; Actors[entity] = actor; continue; }
                m_Goal = goal; m_HeapCount = 0;
                for (int i = 0; i < Costs.Length; i++) { Costs[i] = int.MaxValue; Parents[i] = -1; HeapPositions[i] = -1; }
                int first = SwarmMath.GridIndex(start, Size), last = SwarmMath.GridIndex(goal, Size);
                Costs[first] = 0; Push(first);
                int nearest = first, bestHeuristic = Heuristic(first);
                while (m_HeapCount > 0)
                {
                    int index = Pop();
                    int h = Heuristic(index);
                    if (h < bestHeuristic) { nearest = index; bestHeuristic = h; }
                    if (index == last) { nearest = index; break; }
                    int2 c = new int2(index % Size.x, index / Size.x);
                    for (int d = 0; d < 4; d++)
                    {
                        int2 next = c + (d == 0 ? new int2(1, 0) : d == 1 ? new int2(-1, 0) : d == 2 ? new int2(0, 1) : new int2(0, -1));
                        if (!SwarmMath.InBounds(next, Size) || !Fits(next, body.Radius)) continue;
                        int n = SwarmMath.GridIndex(next, Size);
                        if (HeapPositions[n] == -2 || Costs[index] + 1 >= Costs[n]) continue;
                        Costs[n] = Costs[index] + 1; Parents[n] = index;
                        if (HeapPositions[n] < 0) Push(n); else Up(HeapPositions[n]);
                    }
                }
                int current = nearest, length = 0;
                while (current != first && current >= 0 && length < Costs.Length)
                { Heap[length++] = current; current = Parents[current]; }
                for (int i = length - 1; i >= 0; i--)
                { int index = Heap[i]; path.Add(new GameplayWaypoint { Position = Origin + (new float2(index % Size.x, index / Size.x) + 0.5f) * CellSize }); }
                if (nearest == last && SwarmMath.Fits(actor.Destination, body.Radius, Origin, CellSize, Size, Walkable))
                    path.Add(new GameplayWaypoint { Position = actor.Destination });
                actor.PathDirty = 0; actor.RepathTimer = 0;
                if (path.Length == 0) { actor.Moving = 0; actor.SuppressAcquire = 0; }
                Actors[entity] = actor;
            }
        }
        bool Fits(int2 c, float radius) => SwarmMath.Fits(Origin + (new float2(c) + 0.5f) * CellSize, radius, Origin, CellSize, Size, Walkable);
        bool FindGoal(ref int2 goal, float2 destination, float radius)
        {
            if (Fits(goal, radius)) return true;
            float best = float.MaxValue; int2 result = goal;
            // Scan a bounded neighborhood around blocked building/resource centers for an approach point.
            for (int y = -12; y <= 12; y++)
            for (int x = -12; x <= 12; x++)
            {
                int2 c = goal + new int2(x, y);
                if (!SwarmMath.InBounds(c, Size) || !Fits(c, radius)) continue;
                float distance = math.distancesq(Origin + (new float2(c) + 0.5f) * CellSize, destination);
                if (distance < best) { result = c; best = distance; }
            }
            goal = result; return best < float.MaxValue;
        }
        int Heuristic(int index) => math.abs(index % Size.x - m_Goal.x) + math.abs(index / Size.x - m_Goal.y);
        int Score(int index) => Costs[index] + Heuristic(index);
        void Swap(int a, int b) { int old = Heap[a]; Heap[a] = Heap[b]; Heap[b] = old; HeapPositions[Heap[a]] = a; HeapPositions[Heap[b]] = b; }
        void Up(int i) { while (i > 0) { int p = (i - 1) / 2; if (Score(Heap[p]) <= Score(Heap[i])) break; Swap(p, i); i = p; } }
        void Push(int index) { Heap[m_HeapCount] = index; HeapPositions[index] = m_HeapCount; Up(m_HeapCount++); }
        int Pop()
        {
            int result = Heap[0]; m_HeapCount--;
            if (m_HeapCount > 0)
            {
                Heap[0] = Heap[m_HeapCount]; HeapPositions[Heap[0]] = 0;
                int i = 0;
                while (i * 2 + 1 < m_HeapCount)
                {
                    int child = i * 2 + 1;
                    if (child + 1 < m_HeapCount && Score(Heap[child + 1]) < Score(Heap[child])) child++;
                    if (Score(Heap[i]) <= Score(Heap[child])) break;
                    Swap(i, child); i = child;
                }
            }
            HeapPositions[result] = -2; return result;
        }
    }

    [BurstCompile]
    public partial struct GameplayActorJob : IJobEntity
    {
        public float TowerMultiplier;
        [ReadOnly] public NativeParallelHashMap<Entity, TargetSnapshot> Targets;
        [ReadOnly] public NativeParallelMultiHashMap<int2, TargetSnapshot> Grid;
        [ReadOnly] public ComponentLookup<GameplayResource> Resources;
        [ReadOnly] public ComponentLookup<GameplayConstruction> Construction;
        [ReadOnly] public NativeArray<byte> Walkable;
        public NativeQueue<DamageEvent>.ParallelWriter Damage;
        public NativeQueue<ProjectileRequest>.ParallelWriter Shots;
        public float DeltaTime, CellSize;
        public float2 Origin;
        public int2 Size;

        void Execute(Entity entity, ref GameplayActor actor, ref CombatBody body, DynamicBuffer<GameplayWaypoint> path)
        {
            body.PreviousPosition = body.Position;
            if (body.Health <= 0)
            { actor.State = GameplayState.Dead; actor.Moving = 0; actor.Target = Entity.Null; actor.Grazing = 0; body.Targetable = 0; path.Clear(); return; }
            if (body.Targetable == 0) return; // Sheltered workers retain ECS data but cannot act or be hit.
            if (actor.Kind == GameplayKind.Sheep)
            {
                if (actor.LastDamager != Entity.Null)
                {
                    float2 away = Targets.TryGetValue(actor.LastDamager, out var attacker) ? body.Position - attacker.Body.Position : new float2(1, 0);
                    var random = Unity.Mathematics.Random.CreateFromIndex((uint)(entity.Index * 157 + actor.DamageSequence + 1));
                    actor.Destination = body.Position + math.normalizesafe(away, random.NextFloat2Direction()) * actor.FleeDistance + random.NextFloat2Direction() * actor.FleeJitter;
                    actor.Moving = 1; actor.PathDirty = 1; actor.Grazing = 0; actor.LastDamager = Entity.Null;
                }
                bool claimed = Resources.HasComponent(entity) && Resources[entity].Claims > 0;
                if (actor.Moving != 0 || claimed) { actor.Grazing = 0; actor.GrazeTimer = math.max(actor.MinIdle, 0.1f); }
                else
                {
                    actor.GrazeTimer -= DeltaTime;
                    if (actor.GrazeTimer <= 0)
                    {
                        var random = Unity.Mathematics.Random.CreateFromIndex((uint)(entity.Index * 37 + actor.AttackSequence++ + 1));
                        actor.Grazing = (byte)(actor.Grazing == 0 ? 1 : 0);
                        actor.GrazeTimer = actor.Grazing != 0 ? random.NextFloat(actor.MinGraze, actor.MaxGraze) : random.NextFloat(actor.MinIdle, actor.MaxIdle);
                    }
                }
            }
            else if (actor.Kind == GameplayKind.Soldier || actor.Kind == GameplayKind.Archer || actor.Kind == GameplayKind.Tower || actor.Target != Entity.Null)
                Combat(entity, ref actor, body, path);

            if (actor.Moving == 0 || actor.Speed <= 0) return;
            if (actor.PathDirty != 0 || path.Length == 0) { actor.State = GameplayState.Moving; return; }
            float2 destination = path[0].Position;
            float distance = math.distance(body.Position, destination);
            if (distance <= 0.08f)
            {
                path.RemoveAt(0);
                if (path.Length == 0) { actor.Moving = 0; actor.SuppressAcquire = 0; actor.State = GameplayState.Idle; return; }
                destination = path[0].Position; distance = math.distance(body.Position, destination);
            }
            float2 direction = math.normalizesafe(destination - body.Position);
            float2 separation = 0;
            int2 cell = SwarmMath.SpatialCell(body.Position); int count = 0;
            for (int y = -1; y <= 1; y++)
            for (int x = -1; x <= 1; x++)
            {
                int2 c = cell + new int2(x, y);
                if (!Grid.TryGetFirstValue(c, out var n, out var iterator)) continue;
                do
                {
                    if (count >= 48) break;
                    if (n.Entity == entity || n.Body.Faction != body.Faction || !math.all(SwarmMath.SpatialCell(n.Body.Position) == c)) continue;
                    count++;
                    float2 delta = body.Position - n.Body.Position;
                    float length = math.length(delta), preferred = math.min(1, body.Radius + n.Body.Radius + 0.1f);
                    if (length > 0.001f && length < preferred) separation += delta / length * (preferred - length);
                } while (Grid.TryGetNextValue(out n, ref iterator));
            }
            float2 next = SwarmMath.MoveOnGrid(body.Position, math.normalizesafe(direction + separation) * math.min(distance, actor.Speed * DeltaTime), body.Radius, Origin, CellSize, Size, Walkable);
            actor.RepathTimer = math.distancesq(next, body.Position) < 0.000001f ? actor.RepathTimer + DeltaTime : 0;
            if (actor.RepathTimer > 1) { actor.PathDirty = 1; actor.RepathTimer = 0; }
            actor.Facing = math.normalizesafe(next - body.Position, actor.Facing); body.Position = next; actor.State = GameplayState.Moving;
        }

        void Combat(Entity entity, ref GameplayActor actor, CombatBody body, DynamicBuffer<GameplayWaypoint> path)
        {
            if (Construction.HasComponent(entity) && Construction[entity].Active != 0) return;
            actor.Cooldown = math.max(0, actor.Cooldown - DeltaTime); actor.ThinkTimer -= DeltaTime;
            bool targetValid = Targets.TryGetValue(actor.Target, out var target) && target.Body.Faction != body.Faction;
            bool fighter = actor.Kind == GameplayKind.Soldier || actor.Kind == GameplayKind.Archer || actor.Kind == GameplayKind.Tower;
            if (!targetValid)
            {
                actor.Target = Entity.Null; actor.Windup = -1;
                if (actor.State == GameplayState.Attacking) { actor.State = GameplayState.Idle; actor.Task = GameplayTask.None; }
                if (fighter && actor.Offensive != 0 && actor.SuppressAcquire == 0 && actor.ThinkTimer <= 0)
                {
                    actor.ThinkTimer = 0.25f + (entity.Index % 7) * 0.01f;
                    float best = actor.Detection * actor.Detection;
                    int2 min = SwarmMath.SpatialCell(body.Position - actor.Detection), max = SwarmMath.SpatialCell(body.Position + actor.Detection);
                    for (int y = min.y; y <= max.y; y++)
                    for (int x = min.x; x <= max.x; x++)
                    {
                        if (!Grid.TryGetFirstValue(new int2(x, y), out var n, out var it)) continue;
                        do
                        {
                            if (n.Body.Faction != 1) continue;
                            float d = math.distancesq(n.Body.Position, body.Position);
                            if (d < best) { best = d; actor.Target = n.Entity; target = n; targetValid = true; }
                        } while (Grid.TryGetNextValue(out n, ref it));
                    }
                }
            }
            if (!targetValid) return;
            float distance = GameplayMath.DistanceToBody(body.Position, target.Body);
            bool inRange = distance <= actor.Range;
            if (actor.Windup >= 0)
            {
                actor.Windup -= DeltaTime;
                if (actor.Windup <= 0)
                {
                    actor.Windup = -1;
                    if (inRange)
                    {
                        if (actor.Kind == GameplayKind.Archer || actor.Kind == GameplayKind.Tower)
                        {
                            var p = actor.Projectile; float2 offset = actor.ProjectileOffset;
                            offset.x = math.abs(offset.x) * (target.Body.Position.x < body.Position.x ? -1 : 1);
                            Shots.Enqueue(new ProjectileRequest { Owner = entity, Target = actor.Target, Position = body.Position + offset,
                                Aim = target.Body.Position, Damage = GameplaySimulationSystem.EffectiveGatherAmount(actor.Damage, actor.Kind == GameplayKind.Tower ? TowerMultiplier : 1), Faction = body.Faction, VisualId = 2,
                                Speed = p.Speed, Arc = p.Arc, ArcHeight = p.ArcHeight, Spinning = p.Spinning, VisualScale = p.VisualScale,
                                MinDuration = p.MinDuration, MaxDuration = p.MaxDuration, DurationFactor = p.DurationFactor });
                        }
                        else Damage.Enqueue(new DamageEvent { Target = actor.Target, Source = entity, Amount = actor.Damage });
                    }
                }
            }
            if (inRange)
            {
                actor.Moving = 0; path.Clear(); actor.State = GameplayState.Attacking; actor.Task = GameplayTask.Attack;
                actor.Facing = math.normalizesafe(target.Body.Position - body.Position, actor.Facing);
                if (actor.Cooldown <= 0 && actor.Windup < 0)
                { actor.Cooldown = actor.Interval; actor.Windup = actor.Delay; actor.AttackSequence++; }
            }
            else if (actor.Speed > 0 && (actor.Offensive != 0 || actor.Kind == GameplayKind.Worker))
                GameplayEconomyJob.Approach(ref actor, target.Body.Position);
            else { actor.Target = Entity.Null; actor.State = GameplayState.Idle; actor.Task = GameplayTask.None; }
        }
    }
}
