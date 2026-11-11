// Run this file through Unity MCP script-execute, className=D1Validation, methodName=Run, during Play Mode.
using System;
using System.Linq;
using System.Reflection;
using Unity.Entities;
using UnityEngine;

public static class D1Validation
{
    public static string Run()
    {
        var root = UnityEngine.Object.FindFirstObjectByType<GameSceneCompositionRoot>();
        var manager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
        var bridges = UnityEngine.Object.FindObjectsByType<SwarmGameBridge>(FindObjectsSortMode.None);
        if (root == null || !root.IsReady || !root.IsInitialized || manager == null || !manager.IsInitialized
            || bridges.Length != 1 || !bridges[0].IsSceneBound || ProceduralMapRuntime.IsCompositionBlocked)
            throw new Exception("Composition did not become ready exactly once.");
        var bridge = bridges[0];
        var actors = bridge.Gameplay.ActorCount;
        var wood = manager.Wood; var gold = manager.Gold; var meat = manager.Meat;
        root.Initialize(); bridge.BindScene();
        if (actors != bridge.Gameplay.ActorCount || wood != manager.Wood || gold != manager.Gold || meat != manager.Meat
            || bridge.GetComponents<RTS.DOTS.SwarmInstancedRenderer>().Length != 1)
            throw new Exception("Repeated initialization changed actor, renderer or wallet state.");
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var nodes = (IResourceNodeLocator)typeof(GameManager).GetField("m_ResourceNodeLocator", flags).GetValue(manager);
        var depots = (IResourceDepotLocator)typeof(GameManager).GetField("m_ResourceDepotLocator", flags).GetValue(manager);
        var map = UnityEngine.Object.FindFirstObjectByType<TilemapManager>();
        var camera = UnityEngine.Object.FindFirstObjectByType<CameraController>();
        var fog = UnityEngine.Object.FindFirstObjectByType<FogOfWarManager>();
        var day = UnityEngine.Object.FindFirstObjectByType<DayNightCycleManager>();
        manager.Initialize(bridge, map, camera, fog, day, nodes, depots);
        bool rejected = false;
        try { manager.Initialize(bridge, map, camera, fog, day, new SceneResourceNodeLocator(), depots); }
        catch (InvalidOperationException) { rejected = true; }
        if (!rejected) throw new Exception("Replacing injected dependencies was accepted.");
        var temporary = new GameObject("D1 duplicate bridge check");
        temporary.SetActive(false);
        try
        {
            var duplicate = temporary.AddComponent<SwarmGameBridge>();
            rejected = false;
            try { duplicate.Initialize(World.DefaultGameObjectInjectionWorld, manager, map, fog, day); }
            catch (InvalidOperationException) { rejected = true; }
            if (!rejected || duplicate.IsInitialized) throw new Exception("Duplicate simulation owner was accepted.");
        }
        finally { UnityEngine.Object.DestroyImmediate(temporary); }
        return $"PASS ready, manager injection, repeat initialization, dependency replacement, duplicate owner; actors={actors}; wallet={wood}/{gold}/{meat}";
    }
}
