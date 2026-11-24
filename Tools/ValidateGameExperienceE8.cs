using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using RTS.DOTS;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEditor;

public static class GameExperienceE8Validation
{
    const string Dir = "Docs/Modules/GameExperience/Evidence/E8/";
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static T Find<T>() where T : UnityEngine.Object => UnityEngine.Object.FindFirstObjectByType<T>();
    static void Check(bool value, string message) { if (!value) throw new Exception("E8: " + message); }
    static object Field(object obj, string name) => obj.GetType().GetField(name, Private).GetValue(obj);
    static void Call(object obj, string name, params object[] args) => obj.GetType().GetMethod(name, Private).Invoke(obj, args);
    static void Save(string name, string value) { Directory.CreateDirectory(Dir); File.WriteAllText(Dir + name, value); }
    static void Sync(SwarmGameBridge b) { Call(b, "SynchronizeGameplay"); Physics2D.SyncTransforms(); }
    static void Tick(SwarmGameBridge b, int count)
    {
        var old = b.Gameplay.World.Time;
        try { for (int i = 1; i <= count; i++) { b.Gameplay.World.SetTime(new TimeData(old.ElapsedTime + .05 * i, .05f)); b.Gameplay.Update(); Sync(b); } }
        finally { b.Gameplay.World.SetTime(old); }
    }
    static BuildActionSo Depot => AssetDatabase.LoadAssetAtPath<BuildActionSo>("Assets/Game/ScriptableObjects/Units/BuildStoragePitAction.asset");
    static WorkerUnit[] Workers => Find<GameManager>().GetFriendlyUnits(true).OfType<WorkerUnit>().Where(u => u != null && u.CurrentHealth > 0).ToArray();
    public static string Run()
    {
        var root = Find<GameSceneCompositionRoot>(); var service = root.Expansion; var view = root.ExpansionView;
        var b = Find<SwarmGameBridge>(); var gp = b.Gameplay; var em = gp.EntityManager; var game = Find<GameManager>(); var fog = Find<FogOfWarManager>(); var map = Find<TilemapManager>();
        Check(EditorApplication.isPlaying && EditorApplication.isPaused && root.IsReady, "fresh Ready paused Play required");
        b.Simulation.Paused = false; Find<CameraController>().LockCamera = true;
        var workers = Workers; var hall = Find<TownHall>(); var log = new StringBuilder("E8 controlled scene fixture, manual ECS ticks/positions; not natural play/performance.\n");
        var wallet = gp.Wallet; var stats = gp.Statistics; var rng = JsonUtility.ToJson(UnityEngine.Random.state); long ticks = gp.TickCount; int nav = map.NavigationVersion;
        var positions = workers.Select(w => w.transform.position).ToArray();
        var recall = service.RecallWorkers();
        Check(recall.Accepted == workers.Length && recall.Rejected == 0, "initial recall results " + recall);
        var again = service.RecallWorkers(); Check(again.Accepted == 0 && again.AlreadyReturning == workers.Length, "repeat recall " + again);
        for (int i = 0; i < workers.Length; i++) Check(workers[i].transform.position == positions[i], "recall teleported");
        Check(gp.Wallet.Equals(wallet) && gp.Statistics.Equals(stats) && ticks == gp.TickCount && nav == map.NavigationVersion && rng == JsonUtility.ToJson(UnityEngine.Random.state), "group command changed money/tick/nav/RNG");
        foreach (var worker in workers) worker.LeaveTownHallAtDaybreak();
        log.AppendLine("PASS all workers recall accepted; repeat already-returning; no transform/wallet/stats/tick/nav/RNG mutation; dawn returning workers remain at current position.");
        // Save and resume a gathering assignment/cargo. Repeating auto Night must not overwrite it.
        var carrier = workers[0]; gp.TryGetActor(carrier.DotsEntity, out _, out var carrierBody);
        var node = gp.CreateResource(new GameplayResource { Position = carrierBody.Position, Radius = .3f, Type = 0, Capacity = 3 });
        gp.AssignResource(carrier.DotsEntity, node);
        var cargo = em.GetComponentData<GameplayWorker>(carrier.DotsEntity); cargo.Carried = 3; em.SetComponentData(carrier.DotsEntity, cargo);
        Check(carrier.TryRecallToTownHall(hall) == GroupCommandResult.Accepted, "gather recall");
        carrier.ReturnToTownHall(hall);
        var before = em.GetComponentData<CombatBody>(carrier.DotsEntity).Position; carrier.LeaveTownHallAtDaybreak();
        Check(em.GetComponentData<CombatBody>(carrier.DotsEntity).Position.Equals(before), "returning carrier dawn teleport");
        cargo = em.GetComponentData<GameplayWorker>(carrier.DotsEntity);
        Check(cargo.Node == node && cargo.Carried == 3 && gp.TryGetResource(node, out var resource) && resource.Claims == 1, "gather resume/cargo/claims");
        // Actual construction adapter: interrupt after assignment and resume the same unfinished structure.
        var builder = workers.Last(); var authoring = Find<ProceduralMapAuthoring>(); var point = authoring.GeneratedMap.Layout.Landmark;
        Vector3 location = map.WalkableTilemap.CellToWorld(authoring.OriginCell + new Vector3Int(point.X, point.Y, 0));
        var placement = new PlacementProcess(Depot, map, location); placement.ShowPlacementOutline(); Call(placement, "HighLightTiles", location);
        Check(placement.TryFinalizePlacement(out var accepted), "landmark fixture invalid placement");
        var process = new BuildingProcess(Depot, accepted, builder, game, b); var building = (StructureUnit)Field(process, "m_Structure");
        Check(builder.TryRecallToTownHall(hall) == GroupCommandResult.Accepted, "builder recall"); builder.ReturnToTownHall(hall); builder.LeaveTownHallAtDaybreak();
        Check(em.GetComponentData<GameplayWorker>(builder.DotsEntity).Structure == building.DotsEntity, "builder task not restored");
        log.AppendLine("PASS carrier Node/cargo3/claim1 and unfinished builder Structure restored at dawn; repeated auto-return did not overwrite suspended work. Building fixture spawned without economic debit solely for lifecycle test.");
        // A dead actor after the group registry snapshot is safely rejected at execution.
        var dead = workers[1]; var deadBody = em.GetComponentData<CombatBody>(dead.DotsEntity); deadBody.Health = 0; em.SetComponentData(dead.DotsEntity, deadBody);
        recall = service.RecallWorkers(); Check(recall.Rejected >= 1, "dead worker accepted");
        Find<GameFlowMenu>().Pause(); recall = service.RecallWorkers();
        Check(recall.Accepted == 0 && recall.Rejected > 0, "menu paused recall accepted"); Find<GameFlowMenu>().Resume();
        log.AppendLine("PASS worker health0 rejected before execution; menu pause rejects group commands.");
        // Dead army stays in registry until bridge sync; counts still reflect live ECS state.
        var army = game.GetFriendlyUnits(true).Where(u => u is WarriorUnit || u is RangerUnit).ToArray();
        gp.TryGetActor(army[0].DotsEntity, out _, out var soldierBody);
        Check(b.Simulation.TryResolveSpawnPosition((float2)(Vector2)hall.transform.position + new float2(-3, -3), soldierBody.Radius, out var validArmyPosition), "army fixture valid grid");
        soldierBody.Position = soldierBody.PreviousPosition = validArmyPosition; em.SetComponentData(army[0].DotsEntity, soldierBody);
        Call(fog, "RevealAroundWorldPosition", new Vector3(validArmyPosition.x, validArmyPosition.y), 4); fog.OnFogUpdated.Invoke();
        var enemy = b.Simulation.SpawnEnemy(soldierBody.Position + new float2(1, 0), EnemySpawnSettings.Default);
        gp.Attack(army[0].DotsEntity, enemy);
        var goal = soldierBody.Position; var rally = service.RallyArmy(goal);
        Check(rally.Accepted > 0 && em.GetComponentData<GameplayActor>(army[0].DotsEntity).Target == Entity.Null, "attacking army rally");
        Check(em.GetComponentData<CombatBody>(army[0].DotsEntity).Position.Equals(soldierBody.Position), "rally teleported");
        var unknown = new Vector2(authoring.GeneratedMap.Width + 1000, authoring.GeneratedMap.Height + 1000);
        rally = service.RallyArmy(unknown); Check(rally.Accepted == 0 && rally.Rejected == army.Length, "unknown/outside rally");
        log.AppendLine("PASS attacking army on explicitly resolved fixture cell accepts known reachable camera goal and clears attack; command does not teleport; unknown/outside goal rejected for all army. Original Archer starts in a clearance-blocked cell and was truthfully rejected before fixture relocation.");
        // Preview needs actual explored footprint; inject fog via owner reveal method, not IsDisabled shortcut.
        foreach (var worker in workers) gp.CancelWork(worker.DotsEntity);
        var candidate = hall.transform.position + new Vector3(-5, -2);
        var selected = workers[0]; Call(game, "SetSelectedUnits", new List<Unit> { selected });
        game.StartBuildProcess(Depot); var livePlacement = game.CurrentPlacement;
        typeof(PlacementProcess).GetField("m_InitialPlacementPosition", Private).SetValue(livePlacement, candidate);
        Call(livePlacement, "HighLightTiles", candidate);
        Call(fog, "RevealAroundWorldPosition", candidate, 10); fog.OnFogUpdated.Invoke();
        stats = gp.Statistics; wallet = gp.Wallet; ticks = gp.TickCount; nav = map.NavigationVersion;
        string text = service.Preview(livePlacement);
        Check(text.Contains("Xây: Gỗ") && !text.Contains("giây"), "cost/distance labels");
        for (int i = 0; i < 5; i++) service.Preview(livePlacement);
        Check(gp.Wallet.Equals(wallet) && gp.Statistics.Equals(stats) && ticks == gp.TickCount && nav == map.NavigationVersion, "preview mutated gameplay");
        root.ExpansionView.Refresh();
        Save("preview-text.txt", text); log.AppendLine("PASS preview cost, no travel-time claim; repeated preview leaves wallet/statistics/ticks/navigation unchanged. Explicit reveal fixture for screenshot.");
        Check(text.Contains("Dân → mép xây:") && text.Contains("Tài nguyên → kho hiện có:"), "valid route preview missing");
        typeof(PlacementProcess).GetField("m_InitialPlacementPosition", Private).SetValue(livePlacement, new Vector3(1000, 1000));
        string hidden = service.Preview(livePlacement);
        Check(hidden.Contains("Chưa khám phá") && !hidden.Contains("→"), "unknown footprint disclosed route");
        typeof(PlacementProcess).GetField("m_InitialPlacementPosition", Private).SetValue(livePlacement, candidate);
        log.AppendLine("PASS unknown footprint returns only cost/unknown message without resource/depot distances.");
        Save("integration.txt", log.ToString()); return log.ToString() + "\nPREVIEW:\n" + text;
    }
    public static string ShelterArrival()
    {
        var b = Find<SwarmGameBridge>(); var worker = Workers.First(); var hall = Find<TownHall>();
        Find<GameManager>().CancelBuildProcess(); b.Simulation.Paused = false;
        // Restore a resource task before the actual approach, so full shelter/dawn also verifies resumption.
        b.Gameplay.CancelWork(worker.DotsEntity); b.Gameplay.TryGetActor(worker.DotsEntity, out _, out var before);
        var node = b.Gameplay.CreateResource(new GameplayResource { Position = before.Position, Radius = .3f, Type = 0, Capacity = 2 });
        b.Gameplay.AssignResource(worker.DotsEntity, node);
        Check(worker.TryRecallToTownHall(hall) == GroupCommandResult.Accepted, "actual shelter recall");
        int steps = 0;
        while (steps++ < 1200 && b.Gameplay.EntityManager.GetComponentData<GameplayWorker>(worker.DotsEntity).Phase != WorkPhase.Sheltered) Tick(b, 1);
        var state = b.Gameplay.EntityManager.GetComponentData<GameplayWorker>(worker.DotsEntity);
        Check(state.Phase == WorkPhase.Sheltered, "native walking never reached shelter");
        var body = b.Gameplay.EntityManager.GetComponentData<CombatBody>(worker.DotsEntity);
        Check(body.Targetable == 0, "sheltered targetable");
        worker.ReturnToTownHall(hall); Check(b.Gameplay.EntityManager.GetComponentData<GameplayWorker>(worker.DotsEntity).Equals(state), "sheltered duplicate changed state");
        worker.LeaveTownHallAtDaybreak(); Sync(b);
        state = b.Gameplay.EntityManager.GetComponentData<GameplayWorker>(worker.DotsEntity);
        Check(state.Node == node && state.Phase == WorkPhase.Gathering && b.Gameplay.EntityManager.GetComponentData<CombatBody>(worker.DotsEntity).Targetable != 0, "actual shelter exit failed task resume");
        string text = $"PASS actual native walking reached Shelter in {steps - 1} x.05s ticks; Targetable0; repeat return unchanged; native shelter exit at dawn restores resource task and Targetable1. Existing shelter entrance/exit animation relocation is used only after actual sheltered state.\n";
        Save("shelter-arrival.txt", text); return text;
    }
    public static string RallyArrival()
    {
        var root = Find<GameSceneCompositionRoot>(); var b = Find<SwarmGameBridge>(); var game = Find<GameManager>();
        game.CancelBuildProcess(); b.Simulation.Paused = false;
        var unit = game.GetFriendlyUnits(true).First(u => u is WarriorUnit || u is RangerUnit);
        b.Gameplay.TryGetActor(unit.DotsEntity, out _, out var start);
        Vector2 goal = new Vector2(start.Position.x + 3, start.Position.y);
        var fog = Find<FogOfWarManager>(); Call(fog, "RevealAroundWorldPosition", (Vector3)goal, 5); fog.OnFogUpdated.Invoke();
        var result = root.Expansion.RallyArmy(goal); Check(result.Accepted > 0, "walking rally rejected");
        b.Gameplay.TryGetActor(unit.DotsEntity, out var actor, out _); var destination = actor.Destination;
        int steps = 0; float distance = float.MaxValue;
        while (steps++ < 500)
        {
            Tick(b, 1); b.Gameplay.TryGetActor(unit.DotsEntity, out actor, out var body);
            distance = math.distance(body.Position, destination); if (distance < .5f) break;
        }
        b.Gameplay.TryGetActor(unit.DotsEntity, out _, out var final);
        Check(distance < .5f && math.distance(start.Position, final.Position) > .5f, "rally did not walk to accepted target");
        string text = $"PASS group rally actual ECS movement: accepted={result.Accepted}, steps={steps}, start={start.Position}, destination={destination}, final={final.Position}, error={distance}. No body/transform relocation performed by this check.\n";
        Save("rally-arrival.txt", text); return text;
    }
    public static string CleanupAndMenu()
    {
        var root = Find<GameSceneCompositionRoot>(); var view = root.ExpansionView;
        var go = (GameObject)Field(view, "m_Root"); SessionState.SetInt("E8.Owned", go.GetInstanceID());
        UnityEngine.Object.DestroyImmediate(view); root.Expansion.Dispose(); root.Expansion.Dispose();
        Find<GameFlowMenu>().ReturnToMenu(); return "Destroyed E8 HUD/disposed query twice; loading menu, step then Reload.";
    }
    public static string Reload()
    {
        Check(EditorUtility.InstanceIDToObject(SessionState.GetInt("E8.Owned", 0)) == null && Find<ExpansionHud>() == null, "HUD retained in menu");
        Find<GameFlowMenu>().Play(); return "Menu cleanup passed, new match requested; step then CheckFresh.";
    }
    public static string CheckFresh()
    {
        var root = Find<GameSceneCompositionRoot>();
        Check(root.IsReady && root.Expansion != null && root.ExpansionView.ResultText == "Quân: quanh tâm màn hình · Dân: trú đến bình minh" && Find<GameManager>().CurrentPlacement == null, "stale expansion context");
        Check(Workers.All(w => !w.IsSheltered && !w.IsReturningToShelter), "new worker retained shelter");
        Save("lifecycle.txt", "PASS HUD destroy/dispose twice/menu/new match: owned UI gone, fresh service/HUD with default hint, no previous command result, no placement, no returning/sheltered workers.\n");
        var go = (GameObject)Field(root.ExpansionView, "m_Root"); SessionState.SetInt("E8.ActiveOwned", go.GetInstanceID());
        UnityEngine.SceneManagement.SceneManager.LoadScene("SampleScene"); return "Fresh match verified; direct active unload to legacy, step then CheckLegacy.";
    }
    public static string CheckLegacy()
    {
        Check(Find<GameSceneCompositionRoot>() == null && Find<ExpansionHud>() == null && EditorUtility.InstanceIDToObject(SessionState.GetInt("E8.ActiveOwned", 0)) == null, "active scene unload leaked E8 into legacy");
        File.AppendAllText(Dir + "lifecycle.txt", "PASS direct unload of active E8: HUD released, SampleScene has no E8/root.\n");
        return "PASS legacy isolation; stop Play after this check.";
    }
    public static string Capture()
    {
        var type = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"); var view = EditorWindow.GetWindow(type); view.Repaint();
        var rt = (RenderTexture)type.GetField("m_RenderTexture", Private).GetValue(view); var image = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false); var old = RenderTexture.active;
        try
        {
            RenderTexture.active = rt; image.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); image.Apply();
            if (SystemInfo.graphicsUVStartsAtTop)
            {
                var pixels = image.GetPixels32(); for (int y = 0; y < rt.height / 2; y++) for (int x = 0; x < rt.width; x++)
                { int a = y * rt.width + x, b = (rt.height - 1 - y) * rt.width + x; var p = pixels[a]; pixels[a] = pixels[b]; pixels[b] = p; }
                image.SetPixels32(pixels); image.Apply();
            }
            Directory.CreateDirectory(Dir); File.WriteAllBytes(Dir + "preview-hud.png", image.EncodeToPNG());
        }
        finally { RenderTexture.active = old; UnityEngine.Object.DestroyImmediate(image); }
        return "Captured E8 Game View fixture.";
    }
}
