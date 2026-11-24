using System;
using System.Collections.Generic;
using System.Linq;
using RTS.DOTS;
using RTS.SaveLoad;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed partial class SwarmGameBridge
{
    public void PrepareSaveCapture()
    {
        if (!IsSceneBound) throw new InvalidOperationException("Scene bridge is not ready for capture.");
        // No timer advance: consume only already committed actions/damage/completions, exactly once.
        for (int pass = 0; pass < 4; pass++)
        {
            Gameplay.PrepareSnapshotBoundary(); Simulation.PrepareSnapshotBoundary();
            SynchronizeGameplay();
            if (Gameplay.PendingSnapshotEvents == 0 && Simulation.PendingSnapshotDamage == 0 && Simulation.PendingSnapshotExternalDamage == 0) return;
        }
        throw new InvalidOperationException("Snapshot boundary still has pending gameplay effects; capture was refused.");
    }
    public int SaveNextRequestId => m_NextRequestId;
    public long SaveIdForPlacement(GameObject view)
    {
        if (view == null) return 0;
        MonoBehaviour resource = view.GetComponent<ResourceNodeBase>();
        if (resource == null) resource = view.GetComponent<SheepUnit>();
        Entity entity = Entity.Null;
        if (resource != null) m_ResourceEntities.TryGetValue(resource, out entity);
        return entity != Entity.Null && Gameplay.EntityManager.Exists(entity) && Gameplay.EntityManager.HasComponent<SaveIdentity>(entity)
            ? Gameplay.EntityManager.GetComponentData<SaveIdentity>(entity).Value : 0;
    }
    public ObjectSaveData[] ConvertSaveObjects(SimulationSnapshot snapshot, SaveContentCatalog catalog)
    {
        var ids = snapshot.Objects.ToDictionary(x => x.Entity, x => x.Id);
        long Id(Entity entity)
        {
            if (entity == Entity.Null || !Gameplay.EntityManager.Exists(entity)) return 0;
            if (!ids.TryGetValue(entity, out long value)) throw new InvalidOperationException("Live gameplay reference is outside the captured object registry.");
            return value;
        }
        var views = m_Proxies.Where(x => x.Key != null).ToDictionary(x => x.Value, x => (MonoBehaviour)x.Key);
        foreach (var pair in m_ResourceEntities) if (pair.Key != null) views[pair.Value] = pair.Key;
        var result = new List<ObjectSaveData>(snapshot.Objects.Length);
        foreach (var state in snapshot.Objects.OrderBy(x => x.Id))
        {
            views.TryGetValue(state.Entity, out var view);
            var unit = view as Unit;
            string definition = state.Projectile.HasValue ? catalog.NativeDefinition(state.Projectile.Value.VisualId, true)
                : state.Enemy.HasValue ? catalog.NativeDefinition(state.Enemy.Value.VisualId, false)
                : catalog.DefinitionFor(view != null ? view.gameObject : null);
            var value = new ObjectSaveData { Id = state.Id, DefinitionId = definition, MaxHealth = unit != null ? unit.MaxHealth : 0 };
            if (state.Body is { } b)
            {
                value.Body = new BodySaveData { Position = V(b.Position), PreviousPosition = V(b.PreviousPosition), Radius = b.Radius,
                    Health = b.Health, Faction = b.Faction, Targetable = b.Targetable != 0, BoxHalfSize = V(state.BoxHalfSize) };
                if (unit != null && m_BodyOffsets.TryGetValue(unit, out var offset)) value.ViewOffset = V(offset);
            }
            if (state.Actor is { } a) value.Actor = new ActorSaveData
            {
                Kind = (int)a.Kind, State = (int)a.State, Task = (int)a.Task, Target = Id(a.Target), LastDamager = Id(a.LastDamager),
                Destination = V(a.Destination), Facing = V(a.Facing), ProjectileOffset = V(a.ProjectileOffset),
                Speed = a.Speed, Range = a.Range, Detection = a.Detection, Interval = a.Interval, Delay = a.Delay,
                Cooldown = a.Cooldown, Windup = a.Windup, ThinkTimer = a.ThinkTimer, RepathTimer = a.RepathTimer,
                MinIdle = a.MinIdle, MaxIdle = a.MaxIdle, MinGraze = a.MinGraze, MaxGraze = a.MaxGraze, GrazeTimer = a.GrazeTimer,
                FleeDistance = a.FleeDistance, FleeJitter = a.FleeJitter, Damage = a.Damage, AttackSequence = a.AttackSequence,
                DamageSequence = a.DamageSequence, RandomSeed = a.RandomSeed, Moving = a.Moving != 0, Offensive = a.Offensive != 0,
                SuppressAcquire = a.SuppressAcquire != 0, Grazing = a.Grazing != 0, Projectile = P(a.Projectile)
            };
            if (state.Worker is { } w)
            {
                var worker = unit as WorkerUnit;
                value.Worker = new WorkerSaveData { Node = Id(w.Node), Depot = Id(w.Depot), Structure = Id(w.Structure), Shelter = Id(w.Shelter),
                    Phase = (int)w.Phase, ResourceType = w.ResourceType, Carried = w.Carried, Timer = w.Timer,
                    ResumeResource = worker != null ? Id(worker.SaveResumeResource) : 0, ResumeConstruction = worker != null ? Id(worker.SaveResumeConstruction) : 0,
                    Profiles = (state.Profiles ?? Array.Empty<GatheringProfileData>()).Select(p => new GatheringSaveData
                        { ResourceType = p.ResourceType, Amount = p.Amount, Capacity = p.Capacity, TickTime = p.TickTime, State = (int)p.State, Task = (int)p.Task }).ToArray() };
            }
            if (state.Resource is { } r) value.Resource = new ResourceSaveData { Position = V(r.Position), Radius = r.Radius,
                Type = r.Type, Claims = r.Claims, Capacity = r.Capacity, RequiresKill = r.RequiresKill != 0 };
            if (state.Depot is { } d) value.Depot = new DepotSaveData { ResourceMask = d.ResourceMask, TownHall = d.TownHall != 0 };
            if (state.Construction is { } c)
            {
                m_SaveConstructionActions.TryGetValue(state.Entity, out var action);
                value.Construction = new ConstructionSaveData { Worker = Id(c.Worker), Duration = c.Duration, Elapsed = c.Elapsed,
                    RequestId = c.RequestId, RequiredTechnology = c.RequiredTechnology, Active = c.Active != 0, Middle = c.Middle != 0, Finished = c.Finished != 0,
                    ActionId = action != null ? catalog.ActionId(action) : null };
            }
            if (state.Producer is { } producer)
            {
                var building = unit as ProductionBuilding;
                value.Producer = new ProducerSaveData { Elapsed = producer.Elapsed, CurrentRequest = producer.CurrentRequest,
                    RequiresArchery = producer.RequiresArchery != 0, SpawnSequence = building != null ? building.SaveSpawnSequence : 0,
                    Queue = (state.Training ?? Array.Empty<TrainingRequest>()).Select(item =>
                    {
                        if (!m_TrainingViews.TryGetValue(item.RequestId, out var context) || context.Building != building)
                            throw new InvalidOperationException("Training request has no matching prefab/action context.");
                        return new TrainingSaveData { RequestId = item.RequestId, Duration = item.Duration,
                            ActionId = catalog.ActionId(context.Action), PrefabId = catalog.DefinitionFor(context.Prefab) };
                    }).ToArray() };
            }
            if (state.Enemy is { } e) value.Enemy = new EnemySaveData
            {
                Target = Id(e.Target), Velocity = V(e.Velocity), RetreatGoal = V(e.RetreatGoal), Speed = e.Speed,
                AttackRange = e.AttackRange, DetectionRange = e.DetectionRange, AttackInterval = e.AttackInterval, AttackDelay = e.AttackDelay,
                Cooldown = e.Cooldown, Windup = e.Windup, ThinkTimer = e.ThinkTimer, StateTimer = e.StateTimer, VisualScale = e.VisualScale,
                Damage = e.Damage, VisualId = e.VisualId, ProjectileVisualId = e.ProjectileVisualId, State = (int)e.State,
                RandomSeed = e.RandomSeed, Ranged = e.Ranged != 0, Projectile = P(e.Projectile)
            };
            if (state.Projectile is { } s) value.Projectile = new ProjectileSaveData
            {
                Owner = Id(s.Owner), Target = Id(s.Target), Position = V(s.Position), Start = V(s.Start), Aim = V(s.Aim), PreviousPosition = V(s.PreviousPosition),
                Speed = s.Speed, Radius = s.Radius, Lifetime = s.Lifetime, Age = s.Age, Duration = s.Duration, ArcHeight = s.ArcHeight, VisualScale = s.VisualScale,
                Damage = s.Damage, Faction = s.Faction, VisualId = s.VisualId, Arc = s.Arc != 0, Consumed = s.Consumed != 0, Spinning = s.Spinning != 0
            };
            result.Add(value);
        }
        return result.ToArray();
    }
    static SaveVector V(float2 value) => new() { X = value.x, Y = value.y };
    static SaveVector V(Vector2 value) => new() { X = value.x, Y = value.y };
    static ProjectileParametersData P(ProjectileParameters p) => new() { Speed = p.Speed, ArcHeight = p.ArcHeight, VisualScale = p.VisualScale,
        MinDuration = p.MinDuration, MaxDuration = p.MaxDuration, DurationFactor = p.DurationFactor, Arc = p.Arc != 0, Spinning = p.Spinning != 0 };
}
