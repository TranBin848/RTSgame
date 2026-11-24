using Unity.Entities;
using Unity.Mathematics;

namespace RTS.DOTS
{
    public enum GroupCommandResult : byte { Accepted, AlreadyReturning, Unavailable, InvalidActor, InvalidTarget, Unreachable }

    public partial class GameplaySimulationSystem
    {
        int[] m_PreviewDistance, m_PreviewQueue;
        // Read-only BFS over the same radius-aware four-neighbour grid as actor pathfinding.
        // A planned footprint can be excluded without changing live navigation.
        public bool TryLogisticsRoute(float2 start, float radius, CombatBody target, float approach,
            out float distance, out float2 endpoint, float2 blockedMin = default, float2 blockedMax = default, bool targetIsFootprint = false)
        {
            distance = 0; endpoint = start;
            if (!IsConfigured || !math.all(math.isfinite(start)) || !math.all(math.isfinite(target.Position))
                || !math.isfinite(radius) || radius <= 0 || !math.isfinite(approach) || approach < 0) return false;
            Dependency.Complete();
            int count = m_Walkable.Length;
            if (m_PreviewDistance == null || m_PreviewDistance.Length != count)
            { m_PreviewDistance = new int[count]; m_PreviewQueue = new int[count]; }
            System.Array.Fill(m_PreviewDistance, -1);
            bool blocked = math.all(blockedMax > blockedMin);
            bool Fits(float2 p) => (!blocked || p.x + radius < blockedMin.x || p.x - radius > blockedMax.x || p.y + radius < blockedMin.y || p.y - radius > blockedMax.y)
                && SwarmMath.Fits(p, radius, m_Origin, m_CellSize, m_Size, m_Walkable);
            int2 first = (int2)math.floor((start - m_Origin) / m_CellSize);
            // Actor pathfinding also permits an occupied start cell and exits through valid neighbours.
            if (!SwarmMath.InBounds(first, m_Size) || (blocked && start.x + radius >= blockedMin.x && start.x - radius <= blockedMax.x
                && start.y + radius >= blockedMin.y && start.y - radius <= blockedMax.y)) return false;
            int head = 0, tail = 0, index = SwarmMath.GridIndex(first, m_Size);
            m_PreviewQueue[tail++] = index; m_PreviewDistance[index] = 0;
            while (head < tail)
            {
                index = m_PreviewQueue[head++]; int2 cell = new(index % m_Size.x, index / m_Size.x);
                float2 p = m_Origin + (new float2(cell) + .5f) * m_CellSize;
                float gap = targetIsFootprint ? math.length(math.max(math.max(blockedMin - p, p - blockedMax), 0)) : GameplayMath.DistanceToBody(p, target);
                if (Fits(p) && gap - radius <= approach)
                { distance = m_PreviewDistance[index] * m_CellSize; endpoint = p; return true; }
                for (int d = 0; d < 4; d++)
                {
                    int2 next = cell + (d == 0 ? new int2(1, 0) : d == 1 ? new int2(-1, 0) : d == 2 ? new int2(0, 1) : new int2(0, -1));
                    if (!SwarmMath.InBounds(next, m_Size)) continue;
                    int n = SwarmMath.GridIndex(next, m_Size);
                    if (m_PreviewDistance[n] >= 0 || !Fits(m_Origin + (new float2(next) + .5f) * m_CellSize)) continue;
                    m_PreviewDistance[n] = m_PreviewDistance[index] + 1; m_PreviewQueue[tail++] = n;
                }
            }
            return false;
        }

        bool CommandsAvailable => IsConfigured && !IsMatchOver && World.GetExistingSystemManaged<SwarmSimulationSystem>() is { Paused: false };
        public GroupCommandResult CanRecallWorker(Entity entity, Entity hall)
        {
            if (!CommandsAvailable) return GroupCommandResult.Unavailable;
            if (!TryGetActor(entity, out var actor, out var body) || actor.Kind != GameplayKind.Worker || body.Faction != 0 || body.Health <= 0 || actor.Speed <= 0
                || !TryGetWorker(entity, out var worker)) return GroupCommandResult.InvalidActor;
            if (!TryGetActor(hall, out _, out var depot) || depot.Health <= 0 || depot.Faction != 0 || !EntityManager.HasComponent<GameplayDepot>(hall)
                || EntityManager.GetComponentData<GameplayDepot>(hall).TownHall == 0 || !EntityManager.HasComponent<GameplayConstruction>(hall)
                || EntityManager.GetComponentData<GameplayConstruction>(hall).Active != 0) return GroupCommandResult.InvalidTarget;
            if ((worker.Phase == WorkPhase.ReturningToShelter || worker.Phase == WorkPhase.Sheltered) && worker.Shelter == hall) return GroupCommandResult.AlreadyReturning;
            return TryLogisticsRoute(body.Position, body.Radius, depot, .55f, out _, out _) ? GroupCommandResult.Accepted : GroupCommandResult.Unreachable;
        }
        public GroupCommandResult TryRallyActor(Entity entity, float2 destination)
        {
            if (!CommandsAvailable) return GroupCommandResult.Unavailable;
            if (!TryGetActor(entity, out var actor, out var body) || body.Health <= 0 || body.Faction != 0 || actor.Speed <= 0
                || (actor.Kind != GameplayKind.Soldier && actor.Kind != GameplayKind.Archer)) return GroupCommandResult.InvalidActor;
            if (!math.all(math.isfinite(destination)) || !SwarmMath.Fits(destination, body.Radius, m_Origin, m_CellSize, m_Size, m_Walkable)) return GroupCommandResult.InvalidTarget;
            if (!TryLogisticsRoute(body.Position, body.Radius, new CombatBody { Position = destination }, m_CellSize * .75f, out _, out _)) return GroupCommandResult.Unreachable;
            SetDestination(entity, destination, true); return GroupCommandResult.Accepted;
        }
    }
}
