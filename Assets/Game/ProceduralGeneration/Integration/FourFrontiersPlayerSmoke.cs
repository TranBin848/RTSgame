using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using RTS.DOTS;
using RTS.ProceduralGeneration;
using Unity.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

// Explicit validation entry point; absent from normal menu/game flow.
public sealed class FourFrontiersPlayerSmoke : MonoBehaviour
{
    string m_Mode, m_Folder;
    int m_Seed;
    double m_LoadStarted;
    ReadyState m_First;
    static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        var args = Environment.GetCommandLineArgs(); int index = Array.IndexOf(args, "--four-frontiers-smoke");
        if (index < 0) return;
        if (index + 3 >= args.Length) { Application.Quit(2); return; }
        var runner = new GameObject("FourFrontiers validation").AddComponent<FourFrontiersPlayerSmoke>();
        DontDestroyOnLoad(runner.gameObject);
        runner.m_Mode = args[index + 1]; runner.m_Folder = Path.GetFullPath(args[index + 2]); runner.m_Seed = int.Parse(args[index + 3]);
        runner.StartCoroutine(runner.Run());
    }
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    IEnumerator Run()
    {
        Directory.CreateDirectory(m_Folder); Application.runInBackground = true;
        ProceduralMapAuthoring.BenchmarkSeedOverride = m_Seed;
        ProceduralMapAuthoring.BenchmarkPresetOverride = m_Mode == "baseline" ? MapLayoutPreset.StrategicRegions : null;
        ProceduralMapAuthoring.BenchmarkForceExhaustion = m_Mode == "fallback" || m_Mode == "failure";
        ProceduralMapAuthoring.BenchmarkCorruptFallback = m_Mode == "failure";
        m_LoadStarted = Time.realtimeSinceStartupAsDouble;
        SceneManager.LoadScene("ProceduralGame");
        yield return null;
        yield return null;
        try
        {
            if (m_Mode == "failure")
            {
                Check(ProceduralMapRuntime.HasFailed && ProceduralMapRuntime.IsCompositionBlocked && Time.timeScale == 0, "Failure did not hold simulation.");
                var menu = UnityEngine.Object.FindFirstObjectByType<GameFlowMenu>();
                menu.Resume(); Check(Time.timeScale == 0, "Resume bypassed failure hold.");
                // Exercise the same actions as the visible failure panel.
                ProceduralMapAuthoring.BenchmarkForceExhaustion = false; ProceduralMapAuthoring.BenchmarkCorruptFallback = false;
                m_LoadStarted = Time.realtimeSinceStartupAsDouble; menu.Play();
            }
            else Check(m_Mode == "gameplay" || m_Mode == "fallback" || m_Mode == "baseline", "Unknown smoke mode.");
        }
        catch (Exception error) { Fail(error); yield break; }
        yield return null; yield return null;
        try
        {
            var first = ObserveReady(); m_First = first;
            if (m_Mode == "fallback") Check(first.usedFallback && first.actualSeed == 1 && first.requestedSeed == m_Seed, "Fallback provenance mismatch.");
            var economy = m_Mode == "gameplay" ? CheckEconomy() : null;
            var menu = UnityEngine.Object.FindFirstObjectByType<GameFlowMenu>(); menu.ReturnToMenu();
            File.WriteAllText(Path.Combine(m_Folder, "first.json"), JsonConvert.SerializeObject(new { first, economy }, Formatting.Indented));
        }
        catch (Exception error) { Fail(error); yield break; }
        yield return null;
        try
        {
            Check(SceneManager.GetActiveScene().name == "MainMenu" && !ProceduralMapRuntime.IsActive && !ProceduralMapRuntime.IsCompositionBlocked, "Menu teardown left map state.");
            ProceduralMapAuthoring.BenchmarkForceExhaustion = false;
            m_LoadStarted = Time.realtimeSinceStartupAsDouble;
            SceneManager.LoadScene("ProceduralGame");
        }
        catch (Exception error) { Fail(error); yield break; }
        yield return null; yield return null;
        try
        {
            var reloaded = ObserveReady();
            Check(reloaded.actors==m_First.actors && reloaded.worlds==m_First.worlds,"Reload leaked actors or Worlds.");
            Check(!reloaded.usedFallback, "Fallback leaked into next normal match.");
            File.WriteAllText(Path.Combine(m_Folder, "result.json"), JsonConvert.SerializeObject(new { passed = true, mode = m_Mode, seed = m_Seed, reloaded }, Formatting.Indented));
            Application.Quit(0);
        }
        catch (Exception error) { Fail(error); }
    }
    sealed class ReadyState
    {
        public int actualSeed, requestedSeed, regions, connections, gateways, placements, resourceEntities, actors, worlds;
        public bool usedFallback;
        public double coreMs, terrainMs, resourceMs, navigationMs, initializeMs, bindMs, sceneToReadyMs;
    }
    ReadyState ObserveReady()
    {
        var root = UnityEngine.Object.FindFirstObjectByType<GameSceneCompositionRoot>();
        Check(root != null && root.IsReady, root != null ? root.LastError ?? "Root not ready" : "No root");
        var author = UnityEngine.Object.FindFirstObjectByType<ProceduralMapAuthoring>(); var map = author.GeneratedMap;
        Check(map.GeneratorVersion == (m_Mode=="baseline"?3:4) && map.Layout.Regions.Length == (m_Mode=="baseline"?3:5) && map.Layout.Connections.Length == (m_Mode=="baseline"?2:4) && author.GatewayCount == 2, "Wrong map topology.");
        Check(map.Resources.Count == 150, "Duplicate/missing map placements.");
        for (int gate = 0; gate < 2; gate++)
        {
            Check(author.TryGetGatewaySpawnPosition(gate, out _), "Missing gate spawn.");
            Check(author.TryGetGatewayApproachPosition(gate, Vector3.zero, 20, out _), "Missing gate approach.");
        }
        var bridge = root.GetComponent<SwarmGameBridge>();
        using var query = bridge.Gameplay.EntityManager.CreateEntityQuery(typeof(GameplayResource));
        int resources = query.CalculateEntityCount(); Check(resources == 150, "Duplicate/missing resource entities.");
        return new ReadyState { actualSeed = map.Seed, requestedSeed = map.RequestedSeed, usedFallback = map.UsedFallback,
            regions = map.Layout.Regions.Length, connections = map.Layout.Connections.Length, gateways = 2, placements = map.Resources.Count, resourceEntities = resources, actors = bridge.Gameplay.ActorCount, worlds = Unity.Entities.World.All.Count,
            coreMs = author.CoreMilliseconds, terrainMs = author.TerrainMilliseconds, resourceMs = author.ResourceMilliseconds,
            navigationMs = author.NavigationMilliseconds, initializeMs = root.InitializeMilliseconds, bindMs = root.BindMilliseconds,
            sceneToReadyMs = (root.ReadyRealtime - m_LoadStarted) * 1000 };
    }
    object CheckEconomy()
    {
        Time.timeScale = 0;
        var bridge = UnityEngine.Object.FindFirstObjectByType<SwarmGameBridge>(); bridge.Simulation.Paused = false;
        var author = UnityEngine.Object.FindFirstObjectByType<ProceduralMapAuthoring>(); var map = author.GeneratedMap;
        var tiles = UnityEngine.Object.FindFirstObjectByType<TilemapManager>();
        var worker = UnityEngine.Object.FindObjectsByType<WorkerUnit>(FindObjectsSortMode.None).First(w => w.IsDotsControlled);
        var nodes = UnityEngine.Object.FindObjectsByType<ResourceNodeBase>(FindObjectsSortMode.None);
        var gp = bridge.Gameplay; var original = gp.World.Time; var results = new List<object>();
        var sync = typeof(SwarmGameBridge).GetMethod("SynchronizeGameplay", Private);
        var refresh = typeof(SwarmGameBridge).GetMethod("RefreshNavigation", Private);
        double elapsed = original.ElapsedTime;
        var initialWallet=gp.Wallet; int woodDeposited=0,goldDeposited=0;
        try
        {
            for (int id = 1; id < 5; id++)
            {
                var region = map.Layout.Regions[id]; bool economy = region.Role == RegionRole.Economy;
                var center = tiles.WalkableTilemap.GetCellCenterWorld(author.OriginCell + new Vector3Int(region.X, region.Y, 0));
                var target = nodes.Where(n => n.ResourceType == (economy ? ResourceType.Wood : ResourceType.Gold)).Where(n =>
                {
                    var cell = tiles.WalkableTilemap.WorldToCell(n.transform.position) - author.OriginCell;
                    return map.InBounds(cell.x, cell.y) && map.Layout.RegionIds[map.Index(cell.x, cell.y)] == id;
                }).OrderBy(n => (n.transform.position - center).sqrMagnitude).First();
                Check(bridge.AssignResource(worker, target), "Resource command rejected in region " + id);
                Physics2D.SyncTransforms(); refresh.Invoke(bridge,null);
                while (gp.TryTakeEvent(out _)) { }
                int deposited = 0, ticks = 0, navigation = tiles.NavigationVersion;
                for (; ticks < 12000 && deposited == 0; ticks++)
                {
                    elapsed += .05; gp.World.SetTime(new TimeData(elapsed, .05f)); gp.Update();
                    while (gp.TryTakeEvent(out var e)) if (e.Kind == GameplayEventKind.Deposit && e.Entity == worker.DotsEntity) deposited += e.Amount;
                    if (ticks % 10 == 0)
                    {
                        sync.Invoke(bridge, null); Physics2D.SyncTransforms();
                        if (navigation != tiles.NavigationVersion) { refresh.Invoke(bridge, null); navigation = tiles.NavigationVersion; }
                    }
                }
                Check(deposited > 0, "Native gather/deposit failed in region " + id);
                if(economy)woodDeposited+=deposited;else goldDeposited+=deposited;
                gp.CancelWork(worker.DotsEntity);
                results.Add(new { id, role = region.Role.ToString(), deposited, simulationSeconds = ticks * .05 });
            }
            Check(gp.Wallet.Wood==initialWallet.Wood+woodDeposited && gp.Wallet.Gold==initialWallet.Gold+goldDeposited,"Deposit/wallet mismatch.");
            var depositedWallet=gp.Wallet;
            var content=Resources.Load<SaveContentCatalog>("ProceduralSaveCatalog");
            var game=bridge.GetComponent<GameManager>();
            var depot=content.Actions.Select(a=>a.Action).OfType<BuildActionSo>().Single(a=>content.DefinitionFor(a.StructureUnitPrefab.gameObject)=="storagepit");
            var point=map.Layout.Landmark;
            var location=tiles.WalkableTilemap.CellToWorld(author.OriginCell+new Vector3Int(point.X,point.Y,0));
            var placement=new PlacementProcess(depot,tiles,location);placement.ShowPlacementOutline();
            typeof(PlacementProcess).GetMethod("HighLightTiles",Private).Invoke(placement,new object[]{location});
            Check(placement.TryFinalizePlacement(out var accepted),"Landmark depot placement rejected.");
            Check((bool)typeof(GameManager).GetMethod("TryDeductResources",Private).Invoke(game,new object[]{depot.GoldCost,depot.WoodCost}),"Depot cost rejected.");
            var process=new BuildingProcess(depot,accepted,worker,game,bridge);placement.CleanUp();
            Physics2D.SyncTransforms();refresh.Invoke(bridge,null);
            for(int tick=0;tick<12000&&!process.m_isFinished;tick++)
            {elapsed+=.05;gp.World.SetTime(new TimeData(elapsed,.05f));gp.Update();if(tick%10==0){sync.Invoke(bridge,null);Physics2D.SyncTransforms();}}
            Check(process.m_isFinished,"Native landmark construction failed.");
            var hall=UnityEngine.Object.FindFirstObjectByType<TownHall>();
            var train=content.Actions.Select(a=>a.Action).OfType<SpawnVillagerActionSO>().Single();
            game.RestoreLoadedSelection(new Unit[]{hall});int actors=gp.ActorCount,meat=gp.Wallet.Meat;
            Time.timeScale=1;train.Excute(game);Time.timeScale=0;
            Check(gp.Wallet.Meat==meat-train.MeatCost,"Training cost mismatch.");
            for(int tick=0;tick<2000&&gp.ActorCount==actors;tick++)
            {elapsed+=.05;gp.World.SetTime(new TimeData(elapsed,.05f));gp.Update();if(tick%10==0)sync.Invoke(bridge,null);}
            Check(gp.ActorCount==actors+1,"Native training failed or duplicated spawn.");
            return new { regions=results, woodDeposited, goldDeposited, depositedWallet, landmarkBuilt=true, trainedExactlyOne=true, finalWallet=gp.Wallet };
        }
        finally { gp.World.SetTime(original); }
    }
    void Fail(Exception error)
    {
        File.WriteAllText(Path.Combine(m_Folder, "error.json"), JsonConvert.SerializeObject(new { passed = false, mode = m_Mode, seed = m_Seed, error = error.ToString() }, Formatting.Indented));
        Application.Quit(1);
    }
}
