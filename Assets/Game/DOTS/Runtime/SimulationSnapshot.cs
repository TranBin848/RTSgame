using System;
using Unity.Collections;
using Unity.Entities;

namespace RTS.DOTS
{
    // Detached copies at a completed tick boundary. Entity handles are for the integration remap only.
    public sealed class SimulationObjectSnapshot
    {
        public Entity Entity;
        public long Id;
        public Unity.Mathematics.float2 BoxHalfSize;
        public CombatBody? Body;
        public GameplayActor? Actor;
        public GameplayWorker? Worker;
        public GameplayResource? Resource;
        public GameplayDepot? Depot;
        public GameplayConstruction? Construction;
        public GameplayProducer? Producer;
        public SwarmAgent? Enemy;
        public SwarmProjectile? Projectile;
        public GatheringProfileData[] Profiles;
        public TrainingRequest[] Training;
    }
    public sealed class SimulationSnapshot
    {
        public SimulationObjectSnapshot[] Objects;
        public PlayerWallet Wallet;
        public PlayerResearch Research;
        public MatchStatistics Statistics;
        public long NextSaveId, GameplayTicks, SwarmTicks;
        public int TotalSpawned, TotalHits, SurvivalNights;
    }
    public partial class GameplaySimulationSystem
    {
        public bool SnapshotFenced => World.GetExistingSystemManaged<SwarmSimulationSystem>()?.SnapshotFenced == true;
        void RequireSnapshotMutable()
        { if (SnapshotFenced) throw new InvalidOperationException("Simulation commands are fenced for snapshot capture."); }
        public int PendingSnapshotEvents { get { Dependency.Complete(); return m_Events.Count; } }
        internal bool HasPendingSnapshotWrites => m_Damage.Count != 0 || m_Shots.Count != 0;
        public void PrepareSnapshotBoundary()
        {
            RequireSnapshotMutable(); Dependency.Complete();
            var combat = World.GetOrCreateSystemManaged<SwarmSimulationSystem>();
            while (m_Damage.TryDequeue(out var damage)) combat.QueueDamage(damage.Target, damage.Amount, damage.Source);
            while (m_Shots.TryDequeue(out var shot)) combat.SpawnProjectile(shot);
        }
        internal void CompleteSnapshotJobs() { Dependency.Complete(); }
    }
    public partial class SwarmSimulationSystem
    {
        public bool SnapshotFenced { get; private set; }
        void RequireSnapshotMutable()
        { if (SnapshotFenced) throw new InvalidOperationException("Simulation commands are fenced for snapshot capture."); }
        public int PendingSnapshotDamage { get { Dependency.Complete(); return m_Damage.Count; } }
        public int PendingSnapshotExternalDamage { get { Dependency.Complete(); return m_ExternalDamage.Count; } }
        public void PrepareSnapshotBoundary()
        {
            RequireSnapshotMutable(); Dependency.Complete();
            int capacity = Unity.Mathematics.math.ceilpow2(Unity.Mathematics.math.max(16, m_Bodies.CalculateEntityCount() + 16));
            if (m_DamageTotals.Capacity < capacity) m_DamageTotals.Capacity = capacity;
            if (m_DamageSources.Capacity < capacity) m_DamageSources.Capacity = capacity;
            ApplyDamage();
            while (m_Shots.TryDequeue(out var shot)) SpawnProjectile(shot);
        }
        public IDisposable AcquireSnapshotFence()
        {
            RequireSnapshotMutable();
            var gameplay = World.GetExistingSystemManaged<GameplaySimulationSystem>();
            Dependency.Complete(); gameplay?.CompleteSnapshotJobs();
            if (m_Damage.Count != 0 || m_Shots.Count != 0 || m_ExternalDamage.Count != 0 || gameplay?.PendingSnapshotEvents > 0
                || gameplay?.HasPendingSnapshotWrites == true)
                throw new InvalidOperationException("Commit damage and gameplay completion events before snapshot capture.");
            SnapshotFenced = true;
            return new SnapshotLease(this);
        }
        sealed class SnapshotLease : IDisposable
        {
            SwarmSimulationSystem m_Owner;
            public SnapshotLease(SwarmSimulationSystem owner) { m_Owner = owner; }
            public void Dispose() { if (m_Owner != null) m_Owner.SnapshotFenced = false; m_Owner = null; }
        }
        public SimulationSnapshot CaptureSnapshot()
        {
            if (!SnapshotFenced) throw new InvalidOperationException("Capture requires the shared simulation fence.");
            var gameplay = World.GetExistingSystemManaged<GameplaySimulationSystem>();
            if (gameplay == null || gameplay.IsMatchOver) throw new InvalidOperationException("Only a running gameplay match can be captured.");
            using var query = EntityManager.CreateEntityQuery(new EntityQueryDesc { Any = new[]
                { ComponentType.ReadOnly<GameplayActor>(), ComponentType.ReadOnly<GameplayResource>(), ComponentType.ReadOnly<SwarmAgent>(), ComponentType.ReadOnly<SwarmProjectile>() } });
            using var entities = query.ToEntityArray(Allocator.Temp);
            var objects = new SimulationObjectSnapshot[entities.Length];
            T? Read<T>(Entity e) where T : unmanaged, IComponentData => EntityManager.HasComponent<T>(e) ? EntityManager.GetComponentData<T>(e) : null;
            for (int i = 0; i < entities.Length; i++)
            {
                Entity e = entities[i];
                if (!EntityManager.HasComponent<SaveIdentity>(e)) throw new InvalidOperationException("Unidentified logical entity.");
                var body = Read<CombatBody>(e);
                if (body.HasValue) { var detached = body.Value; detached.Collider = default; detached.ExternalId = 0; body = detached; }
                objects[i] = new SimulationObjectSnapshot
                {
                    Entity = e, Id = EntityManager.GetComponentData<SaveIdentity>(e).Value, Body = body,
                    BoxHalfSize = EntityManager.HasComponent<CombatBody>(e) ? SnapshotBoxHalfSize(EntityManager.GetComponentData<CombatBody>(e)) : default,
                    Actor = Read<GameplayActor>(e), Worker = Read<GameplayWorker>(e), Resource = Read<GameplayResource>(e),
                    Depot = Read<GameplayDepot>(e), Construction = Read<GameplayConstruction>(e), Producer = Read<GameplayProducer>(e),
                    Enemy = Read<SwarmAgent>(e), Projectile = Read<SwarmProjectile>(e),
                    Profiles = EntityManager.HasBuffer<GatheringProfileData>(e) ? EntityManager.GetBuffer<GatheringProfileData>(e).AsNativeArray().ToArray() : null,
                    Training = EntityManager.HasBuffer<TrainingRequest>(e) ? EntityManager.GetBuffer<TrainingRequest>(e).AsNativeArray().ToArray() : null
                };
                if (objects[i].Resource.HasValue)
                { var resource = objects[i].Resource.Value; resource.ExternalId = 0; objects[i].Resource = resource; }
            }
            return new SimulationSnapshot { Objects = objects, Wallet = gameplay.Wallet, Research = gameplay.Research, Statistics = gameplay.Statistics,
                NextSaveId = NextSaveId, GameplayTicks = gameplay.TickCount, SwarmTicks = TickCount,
                TotalSpawned = TotalSpawned, TotalHits = TotalHits, SurvivalNights = gameplay.SurvivalNights };
        }
        static unsafe Unity.Mathematics.float2 SnapshotBoxHalfSize(CombatBody body)
        {
            if (!body.Collider.IsCreated || body.Collider.Value.Type != Unity.Physics.ColliderType.Box) return default;
            return ((Unity.Physics.BoxCollider*)body.Collider.GetUnsafePtr())->Geometry.Size.xy * .5f;
        }
    }
}
