using System.Collections.Generic;
using RTS.DOTS;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed partial class SwarmGameBridge
{
    public GameplaySimulationSystem Gameplay { get; private set; }
    readonly Dictionary<Unit, Vector2> m_BodyOffsets = new();
    readonly Dictionary<MonoBehaviour, Entity> m_ResourceEntities = new();
    readonly List<KeyValuePair<Unit, Entity>> m_ViewSnapshot = new();
    readonly List<MonoBehaviour> m_DeletedResources = new();
    readonly Dictionary<int, TrainingView> m_TrainingViews = new();
    readonly Dictionary<int, BuildingProcess> m_ConstructionViews = new();
    int m_NextRequestId = 1;
    struct TrainingView
    {
        public ProductionBuilding Building;
        public GameObject Prefab;
        public string UiId, Message;
    }

    public Unit ViewFor(Entity entity)
    {
        if (Gameplay != null && Gameplay.EntityManager.Exists(entity) && Gameplay.EntityManager.HasComponent<CombatBody>(entity))
        { var body = Gameplay.EntityManager.GetComponentData<CombatBody>(entity); if (m_UnitsById.TryGetValue(body.ExternalId, out var unit)) return unit; }
        return null;
    }
    public void MoveActor(Unit unit, Vector3 destination, bool playerCommand)
    {
        Vector2 offset = m_BodyOffsets.TryGetValue(unit, out var value) ? value : Vector2.zero;
        Gameplay.SetDestination(unit.DotsEntity, (Vector2)destination + offset, playerCommand);
    }

    public bool TryConvertEnemy(Unit unit)
    {
        if (unit is not EnemyUnit || unit.CurrentState == UnitState.Dead || !unit.gameObject.activeInHierarchy) return false;
        if (Simulation == null || !Simulation.IsConfigured) return false;
        if (!SpawnEnemy(unit.gameObject, unit.transform.position))
        {
            Debug.LogError($"Cannot create ECS enemy '{unit.name}' at {unit.transform.position}: invalid navigation position.", unit);
            // Do not leave an independently simulated legacy enemy in a DOTS gameplay scene.
            unit.DespawnToPool(); return true;
        }
        unit.DespawnToPool(); return true;
    }
    void RegisterGameplayUnit(Unit unit)
    {
        if (Gameplay == null || !Gameplay.IsConfigured || unit is EnemyUnit || (!unit.IsPlayer && unit is not SheepUnit)) return;
        var col = unit.Collider;
        Vector2 center = col != null ? (Vector2)col.bounds.center : (Vector2)unit.transform.position;
        Vector2 half = col != null ? (Vector2)col.bounds.extents : Vector2.one * 0.3f;
        var settings = unit.GetDotsSettings();
        Entity entity = unit.IsBuilding ? Simulation.CreateProxy(unit.GetInstanceID(), center, half, unit.CurrentHealth)
            : Simulation.CreateActorBody(unit.GetInstanceID(), center, settings.Radius, unit.CurrentHealth, unit.IsPlayer ? 0 : 2);
        m_Proxies.Add(unit, entity); m_UnitsById[unit.GetInstanceID()] = unit;
        m_BodyOffsets[unit] = center - (Vector2)unit.transform.position;
        var actor = new GameplayActor
        {
            Kind = unit is WorkerUnit ? GameplayKind.Worker : unit is SheepUnit ? GameplayKind.Sheep
                : unit is RangerUnit ? GameplayKind.Archer : unit is WarriorUnit ? GameplayKind.Soldier
                : unit is TowerUnit ? GameplayKind.Tower : GameplayKind.Building,
            State = (GameplayState)unit.CurrentState, Task = (GameplayTask)unit.CurrentTask,
            Speed = unit.IsBuilding ? 0 : settings.Speed, Range = settings.AttackRange,
            Detection = settings.DetectionRange, Interval = settings.AttackInterval, Delay = settings.AttackDelay,
            Damage = settings.Damage, Offensive = 1, ThinkTimer = (entity.Index % 11) * 0.02f
        };
        Projectile projectile = unit is RangerUnit ranger ? ranger.DotsProjectilePrefab : unit is TowerUnit tower ? tower.DotsProjectilePrefab : null;
        actor.Projectile = projectile != null ? ProjectileSettings(projectile) : EnemySpawnSettings.Default.Projectile;
        if (unit is RangerUnit archer) actor.ProjectileOffset = (Vector2)archer.DotsProjectileSpawnPosition - center;
        else if (unit is TowerUnit) actor.ProjectileOffset = (Vector2)unit.transform.position - center;
        if (unit is SheepUnit sheep) sheep.ConfigureDotsActor(ref actor);
        Gameplay.AttachActor(entity, actor);
        if (unit is WorkerUnit worker) Gameplay.AttachWorker(entity, worker.GetDotsGatheringProfiles());
        if (unit is StructureUnit structure)
        {
            int mask = (structure.CanStoreWood ? 1 : 0) | (structure.CanStoreGold ? 2 : 0) | (structure.CanStoreMeat ? 4 : 0);
            Gameplay.AttachBuilding(entity, mask, unit is TownHall, unit is ProductionBuilding, (byte)(unit is ArcheryRange ? 1 : unit is TowerUnit ? 2 : 0));
        }
        if (unit is SheepUnit node)
        {
            Simulation.EntityManager.AddComponentData(entity, new GameplayResource { Position = center, Radius = node.InteractionRadius,
                Type = (int)node.ResourceType, Capacity = 1, ExternalId = node.GetInstanceID(), RequiresKill = 1 });
            m_ResourceEntities[node] = entity;
        }
        unit.BindDots(entity);
    }

    public Entity RegisterResource(ResourceNodeBase node)
    {
        if (node == null || Gameplay == null) return Entity.Null;
        if (m_ResourceEntities.TryGetValue(node, out var known) && Gameplay.EntityManager.Exists(known)) return known;
        Entity entity = Gameplay.CreateResource(new GameplayResource { Position = (Vector2)node.DotsCenter,
            Radius = node.InteractionRadius, Type = (int)node.ResourceType, Capacity = node.MaxWorkers, ExternalId = node.GetInstanceID() });
        m_ResourceEntities[node] = entity; return entity;
    }
    public Entity ResourceEntity(IResourceNode node)
    {
        if (node is ResourceNodeBase resource) return RegisterResource(resource);
        if (node is Unit unit) return ProxyFor(unit);
        return Entity.Null;
    }
    public void UnregisterResource(MonoBehaviour node)
    {
        if (Gameplay != null && Gameplay.World != null && Gameplay.World.IsCreated && m_ResourceEntities.TryGetValue(node, out var entity)) Gameplay.Remove(entity);
        m_ResourceEntities.Remove(node);
    }
    public bool AssignResource(WorkerUnit worker, IResourceNode node)
    {
        Entity entity = ResourceEntity(node);
        bool assigned = Gameplay.AssignResource(ProxyFor(worker), entity);
        SynchronizeResourceClaims(); // Collider state must match before the next navigation capture.
        return assigned;
    }
    public void FlashWorkerResource(WorkerUnit worker)
    {
        if (Gameplay == null || !Gameplay.TryGetWorker(worker.DotsEntity, out var data)
            || data.Phase != WorkPhase.Gathering || data.Node == Entity.Null) return;
        foreach (var pair in m_ResourceEntities)
            if (pair.Value == data.Node && pair.Key != null && pair.Key is IResourceNode visual) { visual.Hit(); break; }
    }
    public bool TryResourceClaimState(IResourceNode node, out GameplayResource resource)
    { resource = default; return Gameplay != null && Gameplay.TryGetResource(ResourceEntity(node), out resource); }
    public bool ClaimResource(IResourceNode node)
    { bool claimed = Gameplay.TryClaim(ResourceEntity(node)); SynchronizeResourceClaims(); return claimed; }
    public void ReleaseResource(IResourceNode node)
    { Gameplay.Release(ResourceEntity(node)); SynchronizeResourceClaims(); }

    void SynchronizeResourceClaims()
    {
        m_DeletedResources.Clear();
        foreach (var pair in m_ResourceEntities)
        {
            if (pair.Key == null) { m_DeletedResources.Add(pair.Key); continue; }
            if (pair.Key is ResourceNodeBase resource && Gameplay.TryGetResource(pair.Value, out var data)) resource.ApplyDotsClaims(data.Claims);
        }
        foreach (var deleted in m_DeletedResources) UnregisterResource(deleted);
    }
    void SynchronizeGameplay()
    {
        if (Gameplay == null || Gameplay.World == null || !Gameplay.World.IsCreated) return;
        SynchronizeResourceClaims();
        m_ViewSnapshot.Clear();
        foreach (var pair in m_Proxies) m_ViewSnapshot.Add(pair);
        foreach (var pair in m_ViewSnapshot)
        {
            if (pair.Key == null)
            {
                Gameplay.Remove(pair.Value); m_Proxies.Remove(pair.Key); m_BodyOffsets.Remove(pair.Key);
                continue;
            }
            if (Gameplay.TryGetActor(pair.Value, out var actor, out var body))
                pair.Key.ApplyDotsSnapshot(actor, body, m_BodyOffsets[pair.Key]);
        }
        while (Gameplay.TryTakeEvent(out var evt))
        {
            if (evt.Kind == GameplayEventKind.TrainComplete && m_TrainingViews.Remove(evt.RequestId, out var request))
            {
                if (request.Building != null && request.Building.CurrentState != UnitState.Dead)
                {
                    request.Building.SpawnDotsView(request.Prefab);
                    if (!string.IsNullOrWhiteSpace(request.Message)) m_Game.ShowTextPopup(request.Message, Color.green, request.Building.transform.position);
                }
                m_Game.CompleteActionUI(request.UiId);
            }
            else if ((evt.Kind == GameplayEventKind.ConstructionMiddle || evt.Kind == GameplayEventKind.ConstructionComplete)
                && m_ConstructionViews.TryGetValue(evt.RequestId, out var process))
            {
                process.ApplyDotsStage(evt.Kind == GameplayEventKind.ConstructionComplete);
                if (evt.Kind == GameplayEventKind.ConstructionComplete) m_ConstructionViews.Remove(evt.RequestId);
            }
            else if (evt.Kind == GameplayEventKind.Deposit && Gameplay.TryGetActor(evt.Entity, out _, out var body))
                m_Game.ShowTextPopup(evt.Amount.ToString(), Color.yellow, new Vector3(body.Position.x, body.Position.y + 0.5f));
        }
        foreach (var pair in m_TrainingViews)
        {
            var building = pair.Value.Building;
            if (building == null || !Gameplay.TryGetActor(building.DotsEntity, out _, out _)) continue;
            var producer = Gameplay.EntityManager.GetComponentData<GameplayProducer>(building.DotsEntity);
            var queue = Gameplay.EntityManager.GetBuffer<TrainingRequest>(building.DotsEntity);
            if (queue.Length > 0 && producer.CurrentRequest == pair.Key) m_Game.UpdateActionUIProgress(pair.Value.UiId, producer.Elapsed / queue[0].Duration);
        }
        m_Game?.RefreshDotsWallet(Gameplay.Wallet);
    }

    public void EnqueueTraining(ProductionBuilding building, ActionSO action, GameObject prefab, float duration, string message)
    {
        if (!CanUsePrefab(prefab)) return;
        int id = m_NextRequestId++;
        if (!Gameplay.EnqueueTraining(ProxyFor(building), id, duration)) return;
        m_TrainingViews[id] = new TrainingView { Building = building, Prefab = prefab, UiId = m_Game.EnqueueActionUI(action), Message = message };
    }
    public void BeginConstruction(StructureUnit structure, BuildingProcess process, float duration)
    {
        int id = m_NextRequestId++;
        if (Gameplay.BeginConstruction(ProxyFor(structure), duration, id)) m_ConstructionViews[id] = process;
    }
    public bool CanUsePrefab(GameObject prefab)
    {
        if (prefab == null || Gameplay == null) return false;
        if (prefab.GetComponent<EnemyUnit>() != null) return true;
        if ((prefab.GetComponent<ArcheryRange>() != null || prefab.GetComponent<RangerUnit>() != null) && !Gameplay.CanUseArchery) return false;
        return prefab.GetComponent<TowerUnit>() == null || Gameplay.CanUseTowers;
    }
    public GameObject SpawnGameplay(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (!CanUsePrefab(prefab)) return null;
        if (prefab.TryGetComponent<EnemyUnit>(out _)) { SpawnEnemy(prefab, position); return null; }
        var view = Instantiate(prefab, position, rotation, parent);
        if (view.TryGetComponent<Unit>(out var unit)) Register(unit);
        if (view.TryGetComponent<ResourceNodeBase>(out var resource)) RegisterResource(resource);
        return view;
    }
}
