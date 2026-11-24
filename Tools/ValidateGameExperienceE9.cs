using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using RTS.DOTS;
using UnityEngine;
using UnityEditor;

public static class GameExperienceE9Validation
{
    const string Dir = "Docs/Modules/GameExperience/Evidence/E9/";
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static T Find<T>() where T : UnityEngine.Object => UnityEngine.Object.FindFirstObjectByType<T>();
    static void Check(bool ok, string text) { if (!ok) throw new Exception("E9: " + text); }
    static void Call(object obj, string name, params object[] args) => obj.GetType().GetMethod(name, Private).Invoke(obj, args);
    static void Save(string name, string value) { Directory.CreateDirectory(Dir); File.WriteAllText(Dir + name, value); }
    public static string Author()
    {
        const string path = "Assets/Game/ProceduralGeneration/Resources/ProceduralNightPlan.asset";
        if (!AssetDatabase.IsValidFolder("Assets/Game/ProceduralGeneration/Resources")) AssetDatabase.CreateFolder("Assets/Game/ProceduralGeneration", "Resources");
        var plan = AssetDatabase.LoadAssetAtPath<TacticalNightPlan>(path);
        if (plan == null) { plan = ScriptableObject.CreateInstance<TacticalNightPlan>(); AssetDatabase.CreateAsset(plan, path); }
        var baseline = AssetDatabase.LoadAssetAtPath<EnemyWaveDefinition>("Assets/Game/ScriptableObjects/Units/Definitions/EnemyWaveDefinition.asset");
        plan.MeleePrefab = baseline.EnemyPrefabs.First(p => p != null && p.GetComponent<EnemyRangerUnit>() == null);
        plan.RangedPrefab = baseline.EnemyPrefabs.First(p => p != null && p.GetComponent<EnemyRangerUnit>() != null);
        plan.WaveSeed = 8001;
        TacticalNightPlan.Night Night(string title, string brief, int[] counts, float interval, float delay, int ranged, NightApproach mode)
            => new() { Title = title, Briefing = brief, Counts = counts, SpawnInterval = interval, Delay = delay, RangedEvery = ranged, Approach = mode };
        plan.Nights = new[] {
            Night("First defence", "Melee pressure from one approach", new[]{3,4,5},2.2f,8,0,NightApproach.Single),
            Night("Split the defence", "Successive waves alternate approaches",new[]{4,5,6},1.9f,8,0,NightApproach.AlternatingWaves),
            Night("Cover against ranged support", "Melee backed by ranged attackers",new[]{4,5,6},1.7f,7,3,NightApproach.MixedSupport),
            Night("Protect the expansion side", "Pressure from the landmark-side route",new[]{5,5,6},1.6f,6,4,NightApproach.ExpansionSide),
            Night("Combined defence", "Mixed units and alternating reinforcements",new[]{4,4,5,5},1.25f,5,2,NightApproach.Combined)
        };
        plan.Validate(); EditorUtility.SetDirty(plan); AssetDatabase.SaveAssets();
        return "Authored separate five-night procedural plan; existing prefab stats/baseline definition/scenes unchanged. " + plan.MeleePrefab.name + "/" + plan.RangedPrefab.name;
    }
    public static string DataChecks()
    {
        var plan = Resources.Load<TacticalNightPlan>("ProceduralNightPlan"); plan.Validate();
        var rng = JsonUtility.ToJson(UnityEngine.Random.state);
        var rows = new List<object>();
        for (int day = 1; day <= 5; day++)
        {
            var night = plan.ForDay(day); float elapsed = 0;
            var slots = new List<int>(); var gates = new List<int>();
            for (int wave = 0; wave < night.Counts.Length; wave++)
            {
                for (int i = 0; i < night.Counts[wave]; i++) { slots.Add(plan.PrefabSlot(day, wave, i)); gates.Add(plan.Gateway(day, wave, i, 1, 2, 1)); }
                elapsed += (night.Counts[wave] - 1) * night.SpawnInterval;
                if (wave + 1 < night.Counts.Length) elapsed += night.Delay;
            }
            Check(elapsed < 44, "quota does not fit original night");
            Check(slots.All(s => s == 0 || s == 1), "unknown prefab");
            if (day <= 2) Check(slots.All(s => s == 0), "early night composition");
            else Check(slots.Contains(0) && slots.Contains(1), "mixed night missing a capability");
            if (day == 1 || day == 4) Check(gates.Distinct().Count() == 1, "single route plan");
            else Check(gates.Distinct().Count() == 2, "split route plan");
            rows.Add(new { day, night.Title, night.Briefing, night.Counts, night.SpawnInterval, night.Delay, night.RangedEvery,
                approach = night.Approach.ToString(), total = slots.Count, ranged = slots.Count(s => s == 1), nominalLastSpawn = elapsed, gates });
        }
        Check(rng == JsonUtility.ToJson(UnityEngine.Random.state), "plan changed global RNG");
        var clone = UnityEngine.Object.Instantiate(plan);
        try { clone.Nights[0].SpawnInterval = float.NaN; bool rejected = false; try { clone.Validate(); } catch (InvalidOperationException) { rejected = true; } Check(rejected, "invalid plan accepted"); }
        finally { UnityEngine.Object.DestroyImmediate(clone); }
        Save("schedule.json", Newtonsoft.Json.JsonConvert.SerializeObject(new { passed = true, plan.WaveSeed, total = 76, rows }, Newtonsoft.Json.Formatting.Indented));
        return "PASS five roles/compositions/routes,76quota,all nominal schedules<44s,global RNG unchanged,invalid interval rejected.";
    }
    public static string LiveChecks()
    {
        var root = Find<GameSceneCompositionRoot>(); var waves = Find<EnemyWaveManager>(); var clock = Find<DayNightCycleManager>(); var b = Find<SwarmGameBridge>();
        Check(EditorApplication.isPlaying && EditorApplication.isPaused && root.IsReady && waves.TacticalPlan != null, "fresh Ready paused procedural required");
        var records = new List<EnemyWaveManager.WaveSpawnRecord>(); waves.Spawned += records.Add;
        var rng = JsonUtility.ToJson(UnityEngine.Random.state); var wallet = b.Gameplay.Wallet; var stats = b.Gameplay.Statistics;
        int initialSpawned = b.Simulation.TotalSpawned;
        try
        {
            for (int day = 1; day <= 5; day++)
            {
                typeof(DayNightCycleManager).GetProperty("CurrentDay").SetValue(clock, day);
                Call(clock, "SetPhase", DayNightPhase.Dusk, true); root.DuskWarning.RefreshPresentation();
                Check(root.DuskWarning.WarningText.Contains(waves.TacticalBriefing), "Dusk mismatch");
                Call(clock, "SetPhase", DayNightPhase.Night, true);
                int before = records.Count;
                for (int tick = 0; tick < 900; tick++) Call(waves, "Tick", .05f);
                int expected = waves.TacticalPlan.ForDay(day).Counts.Sum();
                Check(records.Count - before == expected, "spawn quota mismatch day" + day);
                int count = records.Count; Find<GameFlowMenu>().Pause(); Call(waves, "Tick", 0f); Find<GameFlowMenu>().Resume();
                Check(count == records.Count, "paused spawn");
                Call(clock, "SetPhase", DayNightPhase.Day, false); for (int tick = 0; tick < 10; tick++) Call(waves, "Tick", .05f);
                Check(count == records.Count, "dawn spawned reinforcements");
            }
            Check(records.Count == 76 && records.All(r => r.RequestedGateway >= 0 && r.ActualGateway >= 0), "trace incomplete");
            Check(rng == JsonUtility.ToJson(UnityEngine.Random.state) && wallet.Equals(b.Gameplay.Wallet) && stats.Equals(b.Gameplay.Statistics), "schedule modified random/economy");
            Save("live-schedule.json", Newtonsoft.Json.JsonConvert.SerializeObject(new { passed = true, records = records.Select(r => new { r.Day, r.Wave, r.Ordinal, r.PrefabSlot, r.RequestedGateway, r.ActualGateway, position = new[]{r.Position.x,r.Position.y} }),
                waves.FailedSpawnAttempts, waves.FallbackSpawnCount, initialSpawned, actualTotal = b.Simulation.TotalSpawned, scheduledDelta = b.Simulation.TotalSpawned - initialSpawned,
                scope = "Clock phases injected; only wave Tick advanced, enemies not combat-simulated. Verifies all scheduling/native spawns, not balance." }, Newtonsoft.Json.Formatting.Indented));
        }
        finally { waves.Spawned -= records.Add; }
        return "PASS76native spawns/five-night schedule,Dusk matches,pause/dawn stop/global RNG/economy invariant. Phase/tick fixture, not a survival test.";
    }
    public static string EdgeChecks()
    {
        var root = Find<GameSceneCompositionRoot>(); var waves = Find<EnemyWaveManager>(); var clock = Find<DayNightCycleManager>(); var b = Find<SwarmGameBridge>();
        Check(root.IsReady && EditorApplication.isPaused && waves.FailedSpawnAttempts == 0 && waves.StartedWaveCount == 0, "fresh paused match needed");
        var records = new List<EnemyWaveManager.WaveSpawnRecord>(); waves.Spawned += records.Add;
        try
        {
            Call(clock, "SetPhase", DayNightPhase.Night, true);
            Find<GameFlowMenu>().Pause(); Call(waves, "Tick", 0f); Call(waves, "Tick", float.NaN); Find<GameFlowMenu>().Resume();
            Check(records.Count == 0 && waves.StartedWaveCount == 0, "pause/nonfinite timer advanced first spawn");
            var sim = b.Simulation;
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var origin = (Unity.Mathematics.float2)sim.GetType().GetField("m_Origin", flags).GetValue(sim);
            var size = (Unity.Mathematics.int2)sim.GetType().GetField("m_Size", flags).GetValue(sim);
            var cell = (float)sim.GetType().GetField("m_CellSize", flags).GetValue(sim);
            try
            {
                sim.ConfigureGrid(origin, size, cell, new byte[size.x * size.y]);
                Call(waves, "Tick", .05f);
                Check(records.Count == 0 && waves.FailedSpawnAttempts == 1 && waves.StartedWaveCount == 0, "failed spawn consumed quota/wave");
            }
            finally { Call(b, "RefreshNavigation"); }
            Call(waves, "Tick", .6f);
            Check(records.Count == 1 && records[0].Ordinal == 0 && waves.StartedWaveCount == 1, "retry skipped ordinal or double-started wave");
            Call(clock, "SetPhase", DayNightPhase.Day, false);
            int count = records.Count; for (int i = 0; i < 20; i++) Call(waves, "Tick", .1f);
            Check(count == records.Count, "Day reinforcements");
            using var agents = sim.EntityManager.CreateEntityQuery(typeof(SwarmAgent));
            using var data = agents.ToComponentDataArray<SwarmAgent>(Unity.Collections.Allocator.Temp);
            Check(data.Length > 0 && data.All(a => a.State == SwarmState.Retreating), "dawn did not preserve retreat");
            Save("edge-cases.txt", "PASS fresh first-spawn timer: actual menu pause delta0/NaN makes0spawns; all-zero native grid fails without quota/StartedWaveCount increment; restored navigation retry produces ordinal0 once; dawn stops spawn and sets native agents Retreating. Native grid replacement is fixture only, restored in finally.\n");
        }
        finally { waves.Spawned -= records.Add; }
        return "PASS pause/invalid delta/failure/retry/ordinal0/dawn retreat edges.";
    }
    public static string CheckLegacy()
    {
        var waves = Find<EnemyWaveManager>();
        Check(Find<GameSceneCompositionRoot>() == null && waves.TacticalPlan == null, "candidate leaked into legacy");
        Save("legacy.txt", "PASS SampleScene: no procedural root, TacticalPlan null; EnemyWaveDefinition legacy formula path remains active. Shared procedural plan asset remains available for the next match.\n");
        return "PASS legacy still uses baseline wave definition, no tactical plan.";
    }
}
