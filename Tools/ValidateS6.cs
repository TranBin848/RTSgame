// Run through Unity MCP script-execute in a disposable Play session; it spawns enemies.
using System;
using System.Linq;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using RTS.DOTS;

public static class S6Validation
{
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }

    public static string RunPaths()
    {
        var generator = UnityEngine.Object.FindFirstObjectByType<ProceduralMapAuthoring>();
        var bridge = UnityEngine.Object.FindFirstObjectByType<SwarmGameBridge>();
        var wave = UnityEngine.Object.FindFirstObjectByType<EnemyWaveManager>();
        var hall = UnityEngine.Object.FindFirstObjectByType<TownHall>();
        var world = bridge.Simulation.World;
        // The source scene includes two enemies; smoke spawns must not run before this check.
        Check(bridge.Simulation.AgentCount <= 2, "RunPaths needs a fresh paused Play session, before Run spawns smoke enemies.");
        var originalTime = world.Time;
        var em = world.EntityManager;
        var hallBody = em.GetComponentData<CombatBody>(hall.DotsEntity);
        var protectedHall = hallBody; protectedHall.Health = 1000000;
        em.SetComponentData(hall.DotsEntity, protectedHall);
        var enemies = new Entity[generator.GatewayCount];
        var nearest = new float[enemies.Length];
        var arrival = new float[enemies.Length];
        var settings = wave.Definition.EnemyPrefabs.First(p => p != null).GetComponent<Unit>().GetDotsSettings();
        settings.Damage = 0; settings.Health = 1000000;
        for (int i = 0; i < enemies.Length; i++)
        {
            Check(generator.TryGetGatewaySpawnPosition(i, out var position), "Missing gateway.");
            enemies[i] = bridge.Simulation.SpawnEnemy((Vector2)position, settings);
            Check(enemies[i] != Entity.Null, "Path probe spawn failed.");
            nearest[i] = float.MaxValue;
        }
        try
        {
            for (int tick = 1; tick <= 3600; tick++)
            {
                world.SetTime(new TimeData(originalTime.ElapsedTime + tick * 0.05, 0.05f));
                bridge.Gameplay.Update(); bridge.Simulation.Update();
                for (int i = 0; i < enemies.Length; i++)
                    if (em.Exists(enemies[i]))
                    {
                        nearest[i] = math.min(nearest[i], math.distance(em.GetComponentData<CombatBody>(enemies[i]).Position, hallBody.Position));
                        if (nearest[i] < 10 && arrival[i] == 0) arrival[i] = tick * 0.05f;
                    }
                if (nearest.All(d => d < 10)) break;
            }
            Check(nearest.All(d => d < 10), "Enemy failed to approach base: " + string.Join(",", nearest));
            return $"PASS seed={generator.GeneratedMap.Seed}; isolated path probes reached <10 world units from TownHall within 180 simulated seconds; arrivalSeconds="
                + string.Join(",", arrival.Select(d => d.ToString("F2"))) + "; nearest=" + string.Join(",", nearest.Select(d => d.ToString("F2")));
        }
        finally
        {
            world.SetTime(originalTime);
            if (em.Exists(hall.DotsEntity)) em.SetComponentData(hall.DotsEntity, hallBody);
        }
    }

    public static string Run()
    {
        var root = UnityEngine.Object.FindFirstObjectByType<GameSceneCompositionRoot>();
        var generator = UnityEngine.Object.FindFirstObjectByType<ProceduralMapAuthoring>();
        var wave = UnityEngine.Object.FindFirstObjectByType<EnemyWaveManager>();
        var game = UnityEngine.Object.FindFirstObjectByType<GameManager>();
        var map = UnityEngine.Object.FindFirstObjectByType<TilemapManager>();
        var day = UnityEngine.Object.FindFirstObjectByType<DayNightCycleManager>();
        var bridge = UnityEngine.Object.FindFirstObjectByType<SwarmGameBridge>();
        var hall = UnityEngine.Object.FindFirstObjectByType<TownHall>();
        Check(root.IsReady && wave.IsInitialized && bridge.IsSceneBound && bridge.Gameplay.IsConfigured
            && bridge.Simulation.IsConfigured && !ProceduralMapRuntime.IsCompositionBlocked, "Scene readiness failed.");
        int listeners = day.OnPhaseChanged.GetInvocationList().Count(x => ReferenceEquals(x.Target, wave));
        wave.Initialize(game, map, day, bridge, generator);
        root.Initialize(); bridge.BindScene();
        Check(listeners == 1 && day.OnPhaseChanged.GetInvocationList().Count(x => ReferenceEquals(x.Target, wave)) == 1,
            "Repeated composition duplicated wave listeners.");
        bool rejected = false;
        try { wave.Initialize(game, null, day, bridge, generator); } catch (ArgumentException) { rejected = true; }
        Check(rejected, "Missing wave dependency accepted.");
        rejected = false;
        try { wave.Initialize(game, map, day, bridge); } catch (InvalidOperationException) { rejected = true; }
        Check(rejected, "Replacing wave map provider accepted.");
        int actors = bridge.Gameplay.ActorCount;
        Check(!generator.TryGenerateAndApply(out var error) && error.Contains("already") && actors == bridge.Gameplay.ActorCount,
            "Repeated generation must fail without duplicating actors.");
        Check(generator.GatewayCount == 2 && !generator.TryGetGatewaySpawnPosition(-1, out _)
            && !generator.TryGetGatewaySpawnPosition(2, out _), "Invalid gateway contract.");
        Check(UnityEngine.Object.FindObjectsByType<Tree>(FindObjectsSortMode.None).Length == 120
            && UnityEngine.Object.FindObjectsByType<GoldStone>(FindObjectsSortMode.None).Length == 12
            && UnityEngine.Object.FindObjectsByType<SheepUnit>(FindObjectsSortMode.None).Length == 18, "Resource counts changed.");
        int spawned = bridge.Simulation.TotalSpawned;
        string positions = "";
        for (int i = 0; i < generator.GatewayCount; i++)
        {
            Check(generator.TryGetGatewaySpawnPosition(i, out var position), "Gateway missing.");
            var gate = generator.GeneratedMap.Layout.Gateways[i];
            var cell = map.WalkableTilemap.WorldToCell(position);
            var center = map.WalkableTilemap.WorldToCell(hall.transform.position);
            int x = gate.X == 0 ? 2 : gate.X == generator.GeneratedMap.Width - 1 ? generator.GeneratedMap.Width - 3 : gate.X;
            int y = gate.Y == 0 ? 2 : gate.Y == generator.GeneratedMap.Height - 1 ? generator.GeneratedMap.Height - 3 : gate.Y;
            Check(cell == center + new Vector3Int(x - generator.GeneratedMap.StartX, y - generator.GeneratedMap.StartY, 0),
                "Gateway world/grid origin mismatch.");
            foreach (var prefab in wave.Definition.EnemyPrefabs.Where(p => p != null))
            {
                float radius = prefab.GetComponent<Unit>().GetDotsSettings().Radius;
                Check(bridge.Simulation.TryResolveSpawnPosition((Vector2)position, radius, out var resolved)
                    && math.distance(resolved, (float2)(Vector2)position) < 0.01f, "Actual enemy radius does not fit at gateway.");
                Check(bridge.SpawnEnemy(prefab, position), "Gateway spawn rejected.");
            }
            positions += $" gate{i}=({cell.x},{cell.y})";
        }
        Check(wave.SpawnEnemy(hall) && bridge.Simulation.TotalSpawned > spawned, "Wave gateway spawn failed.");
        int beforeBlocked = bridge.Simulation.TotalSpawned;
        ProceduralMapRuntime.BeginComposition();
        try { Check(!wave.SpawnEnemy(hall) && bridge.Simulation.TotalSpawned == beforeBlocked, "Wave bypassed readiness gate."); }
        finally { ProceduralMapRuntime.CompleteComposition(); }
        Check(UnityEngine.Object.FindObjectsByType<SwarmGameBridge>(FindObjectsSortMode.None).Length == 1
            && actors == bridge.Gameplay.ActorCount, "Duplicate bridge/actors.");
        return $"PASS seed={generator.GeneratedMap.Seed} version={generator.GeneratedMap.GeneratorVersion} actors={actors} "
            + $"spawned={bridge.Simulation.TotalSpawned - spawned}; dependencies/listener/gate/readiness/resource counts/reapply checked;" + positions;
    }
}
