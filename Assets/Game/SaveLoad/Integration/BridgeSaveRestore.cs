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
    public void RestoreSL4(MatchSaveData saved, SaveContentCatalog content, ProceduralMapAuthoring generator)
    {
        MatchLoadBootstrap.ValidateSL4(saved, content);
        if (!IsSceneBound || !Simulation.Paused || m_Proxies.Count != 0 || m_ResourceEntities.Count != 0 || Gameplay.ActorCount != 0)
            throw new InvalidOperationException("Restore requires a fresh bound scene with no authored gameplay instances.");
        var entities = new Dictionary<long, Entity>();
        var views = new Dictionary<long, GameObject>();
        // Pass 1 creates all objects; no saved relationship is attached until the entire table exists.
        foreach (var item in saved.Objects.OrderBy(x => x.Actor == null && x.Resource != null ? 0 : x.Depot != null ? 1 : x.Actor != null ? 2 : 3).ThenBy(x => x.Id))
        {
            if (item.Enemy != null || item.Projectile != null)
            { entities.Add(item.Id, Simulation.CreateImportShell()); continue; }
            var prefab = content.ResolvePrefab(item.DefinitionId);
            Vector2 position = item.Body != null ? new Vector2(item.Body.Position.X - item.ViewOffset.X, item.Body.Position.Y - item.ViewOffset.Y)
                : new Vector2(item.Resource.Position.X, item.Resource.Position.Y);
            var view = Instantiate(prefab, position, Quaternion.identity, item.Resource != null ? generator.GeneratedResourceRoot : null);
            views.Add(item.Id, view);
            Entity entity;
            if (view.TryGetComponent<Unit>(out var unit)) { Register(unit); entity = unit.DotsEntity; m_BodyOffsets[unit] = new Vector2(item.ViewOffset.X, item.ViewOffset.Y); }
            else
            {
                var node = view.GetComponent<ResourceNodeBase>();
                view.transform.position += (Vector3)(position - (Vector2)node.DotsCenter);
                Physics2D.SyncTransforms(); entity = RegisterResource(node);
            }
            if (entity == Entity.Null) throw new InvalidOperationException("Restored prefab did not bind a gameplay entity.");
            entities.Add(item.Id, entity);
        }
        Entity E(long id) => id == 0 ? Entity.Null : entities[id];
        // Pass 2 copies authoritative values and remaps every reference; it never issues a purchase/spawn command.
        foreach (var item in saved.Objects)
        {
            var state = new SimulationObjectSnapshot { Id = item.Id };
            if (item.Body is { } b) state.Body = new CombatBody { Position = F(b.Position), PreviousPosition = F(b.PreviousPosition), Radius = b.Radius,
                Health = b.Health, Faction = b.Faction, Targetable = (byte)(b.Targetable ? 1 : 0) };
            if (item.Body != null) state.BoxHalfSize = F(item.Body.BoxHalfSize);
            if (item.Actor is { } a) state.Actor = new GameplayActor
            {
                Kind = (GameplayKind)a.Kind, State = (GameplayState)a.State, Task = (GameplayTask)a.Task, Target = E(a.Target), LastDamager = E(a.LastDamager),
                Destination = F(a.Destination), Facing = F(a.Facing), ProjectileOffset = F(a.ProjectileOffset), Speed = a.Speed, Range = a.Range,
                Detection = a.Detection, Interval = a.Interval, Delay = a.Delay, Cooldown = a.Cooldown, Windup = a.Windup, ThinkTimer = a.ThinkTimer,
                RepathTimer = a.RepathTimer, MinIdle = a.MinIdle, MaxIdle = a.MaxIdle, MinGraze = a.MinGraze, MaxGraze = a.MaxGraze,
                GrazeTimer = a.GrazeTimer, FleeDistance = a.FleeDistance, FleeJitter = a.FleeJitter, Damage = a.Damage,
                AttackSequence = a.AttackSequence, DamageSequence = a.DamageSequence, RandomSeed = a.RandomSeed,
                Moving = (byte)(a.Moving ? 1 : 0), Offensive = (byte)(a.Offensive ? 1 : 0), SuppressAcquire = (byte)(a.SuppressAcquire ? 1 : 0),
                Grazing = (byte)(a.Grazing ? 1 : 0), Projectile = PF(a.Projectile)
            };
            if (item.Worker is { } w)
            {
                state.Worker = new GameplayWorker { Node = E(w.Node), Depot = E(w.Depot), Structure = E(w.Structure), Shelter = E(w.Shelter),
                    Phase = (WorkPhase)w.Phase, ResourceType = w.ResourceType, Carried = w.Carried, Timer = w.Timer };
                state.Profiles = w.Profiles.Select(p => new GatheringProfileData { ResourceType = p.ResourceType, Amount = p.Amount, Capacity = p.Capacity,
                    TickTime = p.TickTime, State = (GameplayState)p.State, Task = (GameplayTask)p.Task }).ToArray();
            }
            if (item.Resource is { } r) state.Resource = new GameplayResource { Position = F(r.Position), Radius = r.Radius, Type = r.Type,
                Claims = r.Claims, Capacity = r.Capacity, RequiresKill = (byte)(r.RequiresKill ? 1 : 0) };
            if (item.Depot is { } d) state.Depot = new GameplayDepot { ResourceMask = d.ResourceMask, TownHall = (byte)(d.TownHall ? 1 : 0) };
            if (item.Construction is { } c) state.Construction = new GameplayConstruction { Worker = E(c.Worker), Duration = c.Duration, Elapsed = c.Elapsed,
                RequestId = c.RequestId, RequiredTechnology = (byte)c.RequiredTechnology, Active = (byte)(c.Active ? 1 : 0), Middle = (byte)(c.Middle ? 1 : 0), Finished = (byte)(c.Finished ? 1 : 0) };
            if (item.Producer is { } p)
            { state.Producer = new GameplayProducer { Elapsed = p.Elapsed, CurrentRequest = p.CurrentRequest, RequiresArchery = (byte)(p.RequiresArchery ? 1 : 0) }; state.Training = Array.Empty<TrainingRequest>(); }
            if (item.Enemy is { } e) state.Enemy = new SwarmAgent
            {
                Target = E(e.Target), Velocity = F(e.Velocity), RetreatGoal = F(e.RetreatGoal), Speed = e.Speed, AttackRange = e.AttackRange,
                DetectionRange = e.DetectionRange, AttackInterval = e.AttackInterval, AttackDelay = e.AttackDelay, Cooldown = e.Cooldown,
                Windup = e.Windup, ThinkTimer = e.ThinkTimer, StateTimer = e.StateTimer, VisualScale = e.VisualScale, Damage = e.Damage,
                VisualId = e.VisualId, ProjectileVisualId = e.ProjectileVisualId, State = (SwarmState)e.State, RandomSeed = e.RandomSeed,
                Ranged = (byte)(e.Ranged ? 1 : 0), Projectile = PF(e.Projectile)
            };
            views.TryGetValue(item.Id, out var view);
            Simulation.ApplyImportedObject(E(item.Id), state, view != null ? view.TryGetComponent<Unit>(out var viewUnit) ? viewUnit.GetInstanceID()
                : view.GetComponent<ResourceNodeBase>().GetInstanceID() : 0);
            if (view != null && view.TryGetComponent<Unit>(out var bound))
            {
                bound.HydratePresentation(state.Actor.Value, state.Body.Value, new Vector2(item.ViewOffset.X, item.ViewOffset.Y), item.MaxHealth);
                if (bound is ProductionBuilding producer) producer.RestoreSpawnSequence(item.Producer.SpawnSequence);
                if (item.Construction?.RequestId > 0)
                {
                    var action = (BuildActionSo)content.ResolveAction(item.Construction.ActionId);
                    m_SaveConstructionActions[E(item.Id)] = action;
                    bound.SpriteRenderer.sprite = action.CompleteSprite;
                }
            }
        }
        for (int i = 0; i < saved.Map.Placements.Length; i++)
            if (views.TryGetValue(saved.Map.Placements[i].ObjectId, out var view)) generator.BindSavedPlacement(i, view);
        uint mask = 0;
        var technologies = content.Research.ToCatalog();
        for (int i = 0; i < technologies.Length; i++) if (saved.Player.UnlockedTechnologyIds.Contains(technologies[i].Id)) mask |= 1u << i;
        var research = new PlayerResearch { UnlockMask = mask, Unlocked = (byte)(saved.Player.UnlockedTechnologyIds.Contains(GameplaySimulationSystem.EconomyToolsId) ? 1 : 0),
            Elapsed = saved.Player.ResearchElapsed };
        Gameplay.ApplyImportedPlayer(new PlayerWallet { Wood = (int)saved.Player.Wallet.Wood, Gold = (int)saved.Player.Wallet.Gold, Meat = (int)saved.Player.Wallet.Meat },
            research, StatsFromSave(saved.Player.Statistics), saved.GameplayTicks);
        m_NextRequestId = saved.NextRequestId;
        Physics2D.SyncTransforms(); m_Map.ReinitializePathfinding(); RefreshNavigation(); UpdateGoal();
        Simulation.FinishSnapshotImport(saved.NextSaveId, saved.SwarmTicks, saved.TotalSpawned, saved.TotalHits);
        m_Game.RefreshDotsWallet(Gameplay.Wallet);
        m_Game.RestoreLoadedSelection(saved.View.Selection.Select(id => ViewFor(E(id))));
    }
    static float2 F(SaveVector v) => new(v.X, v.Y);
    static ProjectileParameters PF(ProjectileParametersData p) => new() { Speed = p.Speed, ArcHeight = p.ArcHeight, VisualScale = p.VisualScale,
        MinDuration = p.MinDuration, MaxDuration = p.MaxDuration, DurationFactor = p.DurationFactor, Arc = (byte)(p.Arc ? 1 : 0), Spinning = (byte)(p.Spinning ? 1 : 0) };
    static ResourceTotals TotalsFromSave(ResourceAmounts x) => new() { Wood = x.Wood, Gold = x.Gold, Meat = x.Meat };
    static MatchStatistics StatsFromSave(StatisticsSaveData s) => new() { Deposited = TotalsFromSave(s.Deposited), Spent = TotalsFromSave(s.Spent), Granted = TotalsFromSave(s.Granted),
        UnitsLost = s.UnitsLost, ResearchCompleted = s.ResearchCompleted, TownHallDamage = s.TownHallDamage };
}
