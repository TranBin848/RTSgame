using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Jobs;

namespace RTS.DOTS
{
    public partial class SwarmSimulationSystem
    {
        void RequireImportPaused()
        { RequireSnapshotMutable(); if (!Paused || !IsConfigured) throw new InvalidOperationException("Import requires a configured, paused simulation."); Dependency.Complete(); }
        public Entity CreateImportShell() { RequireImportPaused(); return EntityManager.CreateEntity(typeof(SaveIdentity)); }
        public void ApplyImportedObject(Entity entity, SimulationObjectSnapshot state, int externalId)
        {
            RequireImportPaused();
            if (state == null || state.Id <= 0 || !EntityManager.Exists(entity)) throw new ArgumentException("Invalid import object.");
            void Write<T>(T value) where T : unmanaged, IComponentData
            { if (EntityManager.HasComponent<T>(entity)) EntityManager.SetComponentData(entity, value); else EntityManager.AddComponentData(entity, value); }
            Write(new SaveIdentity { Value = state.Id });
            if (state.Body is { } b)
            {
                b.ExternalId = externalId;
                b.Collider = math.all(state.BoxHalfSize > 0) ? CreateBox(state.BoxHalfSize) : CreateSphere(b.Radius);
                Write(b);
            }
            if (state.Actor is { } actor)
            {
                actor.PathDirty = actor.Moving;
                Write(actor);
                if (!EntityManager.HasBuffer<GameplayWaypoint>(entity)) EntityManager.AddBuffer<GameplayWaypoint>(entity);
                EntityManager.GetBuffer<GameplayWaypoint>(entity).Clear();
            }
            if (state.Worker is { } worker)
            {
                Write(worker);
                if (!EntityManager.HasBuffer<GatheringProfileData>(entity)) EntityManager.AddBuffer<GatheringProfileData>(entity);
                var profiles = EntityManager.GetBuffer<GatheringProfileData>(entity); profiles.Clear();
                foreach (var profile in state.Profiles ?? Array.Empty<GatheringProfileData>()) profiles.Add(profile);
            }
            if (state.Resource is { } resource) { resource.ExternalId = externalId; Write(resource); }
            if (state.Depot is { } depot) Write(depot);
            if (state.Construction is { } construction) Write(construction);
            if (state.Producer is { } producer)
            {
                Write(producer);
                if (!EntityManager.HasBuffer<TrainingRequest>(entity)) EntityManager.AddBuffer<TrainingRequest>(entity);
                var queue = EntityManager.GetBuffer<TrainingRequest>(entity); queue.Clear();
                foreach (var request in state.Training ?? Array.Empty<TrainingRequest>()) queue.Add(request);
            }
            if (state.Enemy is { } enemy) Write(enemy);
            if (state.Projectile is { } projectile) Write(projectile);
        }
        public void FinishSnapshotImport(long nextSaveId, long ticks, int spawned, int hits)
        {
            RequireImportPaused();
            var seen = new HashSet<long>();
            using var query = EntityManager.CreateEntityQuery(typeof(SaveIdentity));
            using var ids = query.ToComponentDataArray<SaveIdentity>(Allocator.Temp);
            foreach (var id in ids)
                if (id.Value <= 0 || id.Value >= nextSaveId || !seen.Add(id.Value)) throw new ArgumentException("Imported identity table is inconsistent.");
            m_SaveIds = new SaveIdAllocator(nextSaveId);
            TickCount = ticks; TotalSpawned = spawned; TotalHits = hits;
            m_FlowDirty = true; m_Grid.Clear(); m_Targets.Clear(); m_RenderData.Clear();
            int capacity = AgentCount + ProjectileCount + 16;
            if (m_RenderData.Capacity < capacity) m_RenderData.Capacity = math.ceilpow2(capacity);
            new AgentRenderJob { Output = m_RenderData.AsParallelWriter() }.ScheduleParallel(default(JobHandle)).Complete();
            new ProjectileRenderJob { Output = m_RenderData.AsParallelWriter() }.ScheduleParallel(default(JobHandle)).Complete();
        }
    }
    public partial class GameplaySimulationSystem
    {
        public void ApplyImportedPlayer(PlayerWallet wallet, PlayerResearch research, MatchStatistics statistics, long ticks)
        {
            RequireSnapshotMutable(); Dependency.Complete();
            if (World.GetExistingSystemManaged<SwarmSimulationSystem>()?.Paused != true || m_Events.Count != 0 || HasPendingSnapshotWrites)
                throw new InvalidOperationException("Player import requires a paused boundary without pending gameplay events.");
            EntityManager.SetComponentData(WalletEntity, wallet); EntityManager.SetComponentData(WalletEntity, research);
            EntityManager.SetComponentData(WalletEntity, statistics); TickCount = ticks; m_PathCursor = 0;
        }
    }
}
