using System;
using System.Collections.Generic;
using System.Linq;
using RTS.DOTS;
using RTS.SaveLoad;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/SaveLoad/Content catalog")]
public sealed class SaveContentCatalog : ScriptableObject
{
    [Serializable] public sealed class Definition
    {
        public string Id;
        public GameObject Prefab;
        public SaveCapability Capabilities;
        public int NativeVisualId = -1;
    }
    [Serializable] public sealed class ActionDefinition { public string Id; public ActionSO Action; }
    public Definition[] Definitions = Array.Empty<Definition>();
    public ActionDefinition[] Actions = Array.Empty<ActionDefinition>();
    public EconomyResearchDefinition Research;
    public DayNightCycleDefinition Clock;
    [SerializeField] string m_ContentHash;
    public string ContentHash => m_ContentHash;
    public SaveCatalog BuildRuntimeCatalog()
    {
        if (m_ContentHash == null || m_ContentHash.Length != 64 || Research == null || Clock == null || Definitions == null || Actions == null)
            throw new InvalidOperationException("Save content catalog has not been baked.");
        var seenPrefabs = new HashSet<GameObject>();
        var seenActions = new HashSet<ActionSO>();
        var nativeIds = new HashSet<string>();
        foreach (var item in Definitions)
        {
            if (item == null || item.Prefab == null || !seenPrefabs.Add(item.Prefab)
                || item.Capabilities != CapabilitiesFor(item.Prefab)
                || item.Prefab.GetComponent<SaveDefinitionTag>()?.DefinitionId != item.Id)
                throw new InvalidOperationException("Save definition does not match its tagged authoring prefab.");
            if (item.NativeVisualId >= 0 && !nativeIds.Add(((item.Capabilities & SaveCapability.Projectile) != 0 ? "projectile:" : "enemy:") + item.NativeVisualId))
                throw new InvalidOperationException("Ambiguous native visual mapping.");
        }
        foreach (var item in Actions)
            if (item == null || item.Action == null || !seenActions.Add(item.Action))
                throw new InvalidOperationException("Invalid save action catalog.");
        return new SaveCatalog(Definitions.Select(x => new SaveDefinition(x.Id, x.Capabilities)),
            Actions.Select(x => x.Id), Research.ToCatalog().Select(x => x.Id), m_ContentHash);
    }
    public string DefinitionFor(GameObject view)
    {
        var tag = view != null ? view.GetComponent<SaveDefinitionTag>() : null;
        if (tag == null || !Definitions.Any(x => x.Id == tag.DefinitionId && x.Capabilities == CapabilitiesFor(view)))
            throw new InvalidOperationException("Object has no allowed save definition: " + (view != null ? view.name : "null"));
        return tag.DefinitionId;
    }
    public string NativeDefinition(int visual, bool projectile)
    {
        var item = Definitions.SingleOrDefault(x => x.NativeVisualId == visual
            && ((x.Capabilities & SaveCapability.Projectile) != 0) == projectile
            && (projectile || (x.Capabilities & SaveCapability.Enemy) != 0));
        return item?.Id ?? throw new InvalidOperationException("Unknown native save visual: " + visual);
    }
    public GameObject ResolvePrefab(string id) => Definitions.SingleOrDefault(x => x.Id == id)?.Prefab
        ?? throw new InvalidOperationException("Unknown save prefab ID.");
    public string ActionId(ActionSO action) => Actions.SingleOrDefault(x => x.Action == action)?.Id
        ?? throw new InvalidOperationException("Unknown save action asset.");
    public ActionSO ResolveAction(string id) => Actions.SingleOrDefault(x => x.Id == id)?.Action
        ?? throw new InvalidOperationException("Unknown save action ID.");
    public static SaveCapability CapabilitiesFor(GameObject prefab)
    {
        if (prefab.GetComponent<Projectile>() != null) return SaveCapability.Projectile;
        if (prefab.GetComponent<EnemyUnit>() != null) return SaveCapability.Body | SaveCapability.Enemy;
        if (prefab.GetComponent<Unit>() is { } unit)
        {
            var flags = SaveCapability.Body | SaveCapability.Actor;
            if (unit is WorkerUnit) flags |= SaveCapability.Worker;
            if (unit is SheepUnit) flags |= SaveCapability.Resource;
            if (unit is StructureUnit) flags |= SaveCapability.Depot | SaveCapability.Construction;
            if (unit is ProductionBuilding) flags |= SaveCapability.Producer;
            return flags;
        }
        if (prefab.GetComponent<ResourceNodeBase>() != null) return SaveCapability.Resource;
        return 0;
    }
#if UNITY_EDITOR
    public void SetBakedHash(string value) { m_ContentHash = value; }
#endif
}
