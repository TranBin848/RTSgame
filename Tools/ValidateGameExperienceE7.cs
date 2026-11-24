using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using RTS.DOTS;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEditor;
using Unity.Profiling;
using System.Collections.Generic;

public static class GameExperienceE7Validation
{
    const string Dir = "Docs/Modules/GameExperience/Evidence/E7/";
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static T Find<T>() where T : UnityEngine.Object => UnityEngine.Object.FindFirstObjectByType<T>();
    static void Check(bool value, string message) { if (!value) throw new Exception("E7: " + message); }
    static void Call(object obj, string method, params object[] args) => obj.GetType().GetMethod(method, Private).Invoke(obj, args);
    static object Field(object obj, string name) => obj.GetType().GetField(name, Private).GetValue(obj);
    static void Save(string file, string text) { Directory.CreateDirectory(Dir); File.WriteAllText(Dir + file, text); }
    static void Tick(SwarmGameBridge b, int count, bool combat = false)
    {
        var old = b.Gameplay.World.Time; bool paused = b.Simulation.Paused;
        try
        {
            b.Simulation.Paused = false;
            for (int i = 1; i <= count; i++)
            {
                b.Gameplay.World.SetTime(new TimeData(old.ElapsedTime + i * .05, .05f));
                if (combat) b.Simulation.Update(); else b.Gameplay.Update();
                Call(b, "SynchronizeGameplay");
            }
        }
        finally { b.Gameplay.World.SetTime(old); b.Simulation.Paused = paused; }
    }
    static void Balance(SwarmGameBridge b)
    {
        var s = b.Gameplay.Statistics; var w = b.Gameplay.Wallet;
        Check(w.Wood == s.Granted.Wood + s.Deposited.Wood - s.Spent.Wood
            && w.Gold == s.Granted.Gold + s.Deposited.Gold - s.Spent.Gold
            && w.Meat == s.Granted.Meat + s.Deposited.Meat - s.Spent.Meat, "wallet does not reconcile");
    }
    public static string Run()
    {
        var root = Find<GameSceneCompositionRoot>(); var b = Find<SwarmGameBridge>(); var game = Find<GameManager>();
        var clock = Find<DayNightCycleManager>(); var fog = Find<FogOfWarManager>(); var reports = root.Reports; var view = root.DawnReport;
        Check(EditorApplication.isPlaying && EditorApplication.isPaused && root.IsReady && reports.CompletedNight == 0, "fresh paused Ready match required");
        b.Simulation.Paused = true;
        Find<CameraController>().LockCamera = true;
        var log = new StringBuilder("E7 controlled ledger fixture; manual ECS ticks, body relocation/heal and clock phase transitions, not real five-night evidence.\n");
        view.Initialize(reports, Find<DayNightClockUI>().GetComponentInParent<Canvas>());
        Check(((Delegate)Field(reports, "NightClosed")).GetInvocationList().Count(d => d.Target == view) == 1, "duplicate consumer");
        var initial = b.Gameplay.Statistics;
        Check(initial.Granted.Wood == 100 && initial.Granted.Gold == 100 && initial.Granted.Meat == 100 && initial.Deposited.Wood == 0, "starting money misclassified");
        b.Gameplay.AddResources(20, 30, 40); b.Gameplay.AddResources(-5, -7, -9);
        var hall = Find<TownHall>();
        b.Simulation.Paused = false;
        Check(root.Research.Start("economy_tools", hall) == ResearchResult.Started, "research start");
        b.Simulation.Paused = true;
        var r = root.Research.State; r.Elapsed = root.Research.Duration - .025f;
        b.Gameplay.EntityManager.SetComponentData(b.Gameplay.WalletEntity, r); Tick(b, 1);
        var worker = game.GetFriendlyUnits(true).OfType<WorkerUnit>().First();
        b.Gameplay.TryGetActor(hall.DotsEntity, out _, out var hallBody);
        b.Gameplay.TryGetActor(worker.DotsEntity, out _, out var body);
        body.Position = body.PreviousPosition = hallBody.Position;
        b.Gameplay.EntityManager.SetComponentData(worker.DotsEntity, body);
        var node = b.Gameplay.CreateResource(new GameplayResource { Position = body.Position, Radius = .3f, Type = 0, Capacity = 4 });
        b.Gameplay.AssignResource(worker.DotsEntity, node);
        for (int i = 0; i < 300 && b.Gameplay.EntityManager.GetComponentData<GameplayWorker>(worker.DotsEntity).Carried == 0; i++) Tick(b, 1);
        var carried = b.Gameplay.EntityManager.GetComponentData<GameplayWorker>(worker.DotsEntity);
        Check(carried.Carried > 0 && b.Gameplay.Statistics.Deposited.Wood == 0, "gather counted as deposit");
        int amount = carried.Carried; carried.Phase = WorkPhase.Depositing; carried.Depot = hall.DotsEntity;
        b.Gameplay.EntityManager.SetComponentData(worker.DotsEntity, carried); Tick(b, 1); b.Gameplay.CancelWork(worker.DotsEntity);
        var soldier = game.GetFriendlyUnits(true).First(u => u is WarriorUnit);
        b.Simulation.QueueDamage(soldier.DotsEntity, 100000); b.Simulation.QueueDamage(hall.DotsEntity, 7); Tick(b, 1, true);
        body = b.Gameplay.EntityManager.GetComponentData<CombatBody>(hall.DotsEntity); body.Health += 3;
        b.Gameplay.EntityManager.SetComponentData(hall.DotsEntity, body);
        b.Simulation.QueueDamage(hall.DotsEntity, 5); Tick(b, 1, true);
        var stats = b.Gameplay.Statistics;
        Check(stats.Granted.Wood == 120 && stats.Granted.Gold == 130 && stats.Granted.Meat == 140, "grants");
        Check(stats.Deposited.Wood == amount && stats.Spent.Wood == 45 && stats.Spent.Gold == 22 && stats.Spent.Meat == 9, "resource ledger");
        Check(stats.UnitsLost == 1 && stats.ResearchCompleted == 1 && stats.TownHallDamage == 12, "combat/research ledger"); Balance(b);
        int explored = fog.ExploredCellCount; Call(fog, "RefreshFog"); Call(fog, "RefreshFog");
        Check(fog.ExploredCellCount >= explored, "exploration regressed");
        explored = fog.ExploredCellCount; Call(fog, "RefreshFog"); Check(fog.ExploredCellCount == explored, "repeat fog counts cells twice");
        Call(clock, "SetPhase", DayNightPhase.Night, true); Call(clock, "Tick", clock.Definition.NightDuration);
        Check(reports.CompletedNight == 1 && view.DawnSignalCount == 1 && reports.LatestNight.Statistics.Equals(stats), "first cycle snapshot");
        view.OpenLatest(); string text = view.DisplayedText;
        var frozen = reports.LatestNight;
        Find<GameFlowMenu>().Pause();
        for (int i = 0; i < 20; i++) { view.Close(); view.OpenLatest(); clock.OnPhaseChanged.Invoke(DayNightPhase.Day); Call(b, "SynchronizeGameplay"); }
        Check(view.DisplayedText == text && view.DawnSignalCount == 1 && b.Gameplay.Statistics.Equals(stats) && reports.LatestNight.Equals(frozen), "reopen/pause replay");
        Find<GameFlowMenu>().Resume(); Balance(b);
        log.AppendLine("PASS cycle1: " + Newtonsoft.Json.JsonConvert.SerializeObject(new { amount, stats, explored }));
        log.AppendLine("PASS 20 close/open + duplicate Day signals under actual menu pause preserve wallet/counters/snapshot/chime count.");
        b.Gameplay.AddResources(-3, 0, 0); b.Simulation.QueueDamage(hall.DotsEntity, 4); Tick(b, 1, true);
        Call(clock, "SetPhase", DayNightPhase.Night, true); Call(clock, "Tick", clock.Definition.NightDuration);
        var second = reports.LatestNight.Statistics;
        Check(reports.CompletedNight == 2 && view.DawnSignalCount == 2 && second.Spent.Wood == 3 && second.Spent.Gold == 0
            && second.Deposited.Wood == 0 && second.Granted.Wood == 0 && second.TownHallDamage == 4 && second.UnitsLost == 0 && second.ResearchCompleted == 0, "cycle2 includes cycle1 again");
        Balance(b); view.OpenLatest();
        log.AppendLine("PASS cycle2 delta only: " + Newtonsoft.Json.JsonConvert.SerializeObject(reports.LatestNight));
        log.AppendLine("PASS total: " + Newtonsoft.Json.JsonConvert.SerializeObject(reports.Total));
        Save("controlled-ledger.txt", log.ToString()); return log.ToString();
    }
    public static string FinalizeFixture()
    {
        var root = Find<GameSceneCompositionRoot>(); var clock = Find<DayNightCycleManager>();
        typeof(DayNightCycleManager).GetProperty("CurrentDay").SetValue(clock, 6);
        Call(Find<MatchSession>(), "LateUpdate");
        Check(root.Reports.IsFrozen && Find<SwarmGameBridge>().Gameplay.Outcome == MatchOutcome.Victory, "terminal fixture");
        string text = "TỔNG TRẬN\n" + MatchReportText.Format(root.Reports.Total);
        Check(Find<MatchSession>().ResultStatisticsText == text && Time.timeScale == 0, "result definitions differ");
        Save("result-fixture.txt", "PASS injected day6 tests terminal UI only, not survival evidence.\n" + text);
        return "PASS result uses exactly the same frozen snapshot/formatter; step then capture.";
    }
    public static string CaptureReport() { Capture("controlled-report.png"); return "Saved report Game View"; }
    public static string CaptureResult() { Capture("controlled-result.png"); return "Saved terminal fixture Game View"; }
    public static string BaselineAB()
    {
        var root = Find<GameSceneCompositionRoot>(); var view = root.DawnReport; var b = Find<SwarmGameBridge>(); var clock = Find<DayNightCycleManager>();
        Check(EditorApplication.isPlaying && EditorApplication.isPaused && !root.Reports.IsFrozen, "nonterminal paused fixture required");
        var before = root.Reports.Total; var wallet = b.Gameplay.Wallet; var rng = JsonUtility.ToJson(UnityEngine.Random.state);
        float phaseProgress = clock.CurrentPhaseProgress, previousScale = Time.timeScale;
        bool previousBackground = Application.runInBackground, previousPause = b.Simulation.Paused;
        Time.timeScale = 0; Application.runInBackground = true; b.Simulation.Paused = true;
        var gc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
        var rows = new List<(int block, bool open, double ms, long bytes, bool focused)>();
        double start = EditorApplication.timeSinceStartup, blockStart = start;
        int block = 0, lastFrame = -1; view.Close();
        EditorApplication.CallbackFunction update = null;
        void Finish(string error)
        {
            EditorApplication.update -= update; gc.Dispose(); view.Close();
            Time.timeScale = previousScale; Application.runInBackground = previousBackground; b.Simulation.Paused = previousPause;
            EditorApplication.isPaused = true;
            var text = new StringBuilder("block,report_open,frame_ms,gc_bytes,focused\n");
            foreach (var row in rows) text.AppendLine($"{row.block},{row.open},{row.ms.ToString("F5", System.Globalization.CultureInfo.InvariantCulture)},{row.bytes},{row.focused}");
            Save("editor-ab.csv", text.ToString());
            var groups = rows.GroupBy(r => r.block).Select(g =>
            {
                var ms = g.Select(r => r.ms).OrderBy(x => x).ToArray(); var bytes = g.Select(r => r.bytes).OrderBy(x => x).ToArray();
                return new { block = g.Key, reportOpen = g.First().open, frames = ms.Length, medianMs = ms[ms.Length / 2], p95Ms = ms[(int)((ms.Length - 1) * .95)], medianGc = bytes[bytes.Length / 2], focusStates = g.Select(r => r.focused).Distinct().ToArray() };
            }).ToArray();
            bool statsStable = before.Equals(root.Reports.Total), walletStable = wallet.Equals(b.Gameplay.Wallet), clockStable = phaseProgress == clock.CurrentPhaseProgress, rngStable = rng == JsonUtility.ToJson(UnityEngine.Random.state);
            bool stable = statsStable && walletStable && clockStable && rngStable;
            Save("editor-ab.json", Newtonsoft.Json.JsonConvert.SerializeObject(new { scope = "Editor GameView UI closed/open ABBA; frozen world, timeScale0, not Player or a G5 frame gate", groups, stable, statsStable, walletStable, clockStable, rngStable, error, resolution = new[] { Screen.width, Screen.height }, vsync = QualitySettings.vSyncCount, targetFps = Application.targetFrameRate }, Newtonsoft.Json.Formatting.Indented));
        }
        update = () =>
        {
            try
            {
                double now = EditorApplication.timeSinceStartup;
                if (!EditorApplication.isPlaying || now - start > 80) { Finish("watchdog/Play stopped"); return; }
                if (Time.frameCount != lastFrame && now - blockStart > 1)
                { lastFrame = Time.frameCount; rows.Add((block, view.IsOpen, Time.unscaledDeltaTime * 1000.0, gc.LastValue, Application.isFocused)); }
                if (now - blockStart < 5) return;
                block++; if (block == 4) { Finish(""); return; }
                blockStart = now; if (block == 1 || block == 2) view.OpenLatest(); else view.Close();
            }
            catch (Exception error) { Finish(error.ToString()); }
        };
        EditorApplication.update += update; EditorApplication.isPaused = false;
        return "Recording20s Editor frozen-world ABBA: closed/open/open/closed, 1s warmup each block. No Player performance claim.";
    }

    public static string CleanupAndMenu()
    {
        var root = Find<GameSceneCompositionRoot>(); var reports = root.Reports; var view = root.DawnReport;
        var clock = Find<DayNightCycleManager>(); var canvasRoot = (GameObject)Field(view, "m_Root"); var chime = (AudioClip)Field(view, "m_Chime");
        SessionState.SetString("E7.Owned", canvasRoot.GetInstanceID() + "," + chime.GetInstanceID());
        UnityEngine.Object.DestroyImmediate(view);
        var listeners = (Delegate)Field(reports, "NightClosed"); Check(listeners == null || listeners.GetInvocationList().All(d => d.Target != (object)view), "view listener retained");
        reports.Dispose(); reports.Dispose();
        Check(clock.OnPhaseChanged.GetInvocationList().All(d => d.Target != (object)reports), "service clock listener retained");
        Save("lifecycle.txt", "PASS component destroy unsubscribes; Dispose twice safely removes clock listener.\n");
        Find<GameFlowMenu>().ReturnToMenu(); return "Destroyed E7 presentation/disposed service; menu loading, step then Reload.";
    }
    public static string Reload()
    {
        Check(SessionState.GetString("E7.Owned", "").Split(',').Select(int.Parse).All(id => EditorUtility.InstanceIDToObject(id) == null), "owned UI/chime leaked");
        Check(Find<GameSceneCompositionRoot>() == null, "menu retained root");
        Find<GameFlowMenu>().Play(); return "New match requested; step then CheckFresh.";
    }
    public static string CheckFresh()
    {
        var root = Find<GameSceneCompositionRoot>(); var stats = root.Reports.Total.Statistics;
        Check(root.IsReady && root.DawnReport.DawnSignalCount == 0 && root.Reports.CompletedNight == 0 && !root.Reports.IsFrozen
            && stats.Spent.Wood == 0 && stats.Deposited.Wood == 0 && stats.UnitsLost == 0 && stats.ResearchCompleted == 0 && stats.TownHallDamage == 0
            && stats.Granted.Wood == 100 && !root.DawnReport.IsOpen, "match reports/counters not fresh");
        File.AppendAllText(Dir + "lifecycle.txt", "PASS menu removes owned UI/chime; new match Ready with reset counters, no dawn/closed report, initial grant100 separated.\n");
        return "PASS fresh report context; suitable for natural run.";
    }
    public static string ActiveUnload()
    {
        var root = Find<GameSceneCompositionRoot>(); var view = root.DawnReport;
        SessionState.SetString("E7.ActiveOwned", ((GameObject)Field(view, "m_Root")).GetInstanceID() + "," + ((AudioClip)Field(view, "m_Chime")).GetInstanceID());
        UnityEngine.SceneManagement.SceneManager.LoadScene("SampleScene"); return "Directly unloading active E7 into legacy; step then CheckLegacy.";
    }
    public static string CheckLegacy()
    {
        Check(Find<GameSceneCompositionRoot>() == null && Find<DawnReportPresentation>() == null, "legacy gained report consumer");
        Check(SessionState.GetString("E7.ActiveOwned", "").Split(',').Select(int.Parse).All(id => EditorUtility.InstanceIDToObject(id) == null), "active unload leaked UI/chime");
        File.AppendAllText(Dir + "lifecycle.txt", "PASS direct scene unload of active E7 removes UI/chime; SampleScene has no report consumer/root.\n");
        UnityEngine.SceneManagement.SceneManager.LoadScene("ProceduralGame"); return "Legacy passed; loading fresh procedural. Step then CheckFresh.";
    }
    public static string CaptureNextDawn()
    {
        var root = Find<GameSceneCompositionRoot>(); int target = root.Reports.CompletedNight + 1, frame = -1;
        double started = EditorApplication.timeSinceStartup;
        EditorApplication.CallbackFunction update = null;
        update = () =>
        {
            if (!EditorApplication.isPlaying || root == null || EditorApplication.timeSinceStartup - started > 170)
            { EditorApplication.update -= update; return; }
            if (root.Reports.CompletedNight < target) return;
            if (frame < 0) { frame = Time.frameCount; return; }
            if (Time.frameCount <= frame + 1) return;
            EditorApplication.update -= update;
            Capture("natural-dawn.png");
            var audio = (AudioSource)Field(root.DawnReport, "m_Audio");
            Save("natural-dawn.txt", $"Natural dawn{root.Reports.CompletedNight}; signals={root.DawnReport.DawnSignalCount}; timeScale={Time.timeScale}; reportOpen={root.DawnReport.IsOpen}; sourcePlaying={audio.isPlaying}; clip={audio.clip.name}. Software playback state, not confirmation of physical audibility.\n");
        };
        EditorApplication.update += update; return "Armed one read-only capture on next natural dawn; no clock/gameplay edits.";
    }
    public static string AuditFiles()
    {
        var result = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(Dir + "natural-result.json"));
        Check((string)result["outcome"] == "Victory" && (int)result["completedNights"] == 5 && (int)result["signals"] == 5, "natural five-night completion");
        var totals = result["total"]; var sums = new Dictionary<string, double>();
        string[] resources = { "Wood", "Gold", "Meat" }, categories = { "Deposited", "Spent", "Granted" };
        int checkedSamples = 0, dawns = 0;
        foreach (string line in File.ReadAllLines(Dir + "natural-five-nights.txt"))
        {
            if (line.StartsWith("t="))
            {
                int walletAt = line.IndexOf("wallet=") + 7, statsAt = line.IndexOf(";stats=");
                var wallet = line.Substring(walletAt, statsAt - walletAt).Split('/').Select(long.Parse).ToArray();
                var sample = Newtonsoft.Json.Linq.JObject.Parse(line.Substring(statsAt + 7))["Statistics"];
                for (int i = 0; i < 3; i++) Check(wallet[i] == (long)sample["Granted"][resources[i]] + (long)sample["Deposited"][resources[i]] - (long)sample["Spent"][resources[i]], "sample balance " + checkedSamples);
                checkedSamples++;
            }
            if (!line.StartsWith("dawn=")) continue;
            dawns++; var snapshot = Newtonsoft.Json.Linq.JObject.Parse(line.Substring(line.IndexOf("snapshot=") + 9));
            void Sum(string key, double value) { sums.TryGetValue(key, out double old); sums[key] = old + value; }
            foreach (string category in categories) foreach (string resource in resources)
                Sum(category + "." + resource, (double)snapshot["Statistics"][category][resource]);
            foreach (string counter in new[] { "UnitsLost", "ResearchCompleted", "TownHallDamage" }) Sum(counter, (double)snapshot["Statistics"][counter]);
            Sum("ExploredCells", (double)snapshot["ExploredCells"]);
        }
        Check(dawns == 5 && checkedSamples >= 40, "missing natural samples/dawns");
        var terminalTail = new Dictionary<string, double>();
        foreach (var pair in sums)
        {
            double value;
            if (pair.Key == "ExploredCells") value = (double)totals["ExploredCells"];
            else if (pair.Key.Contains(".")) { var key = pair.Key.Split('.'); value = (double)totals["Statistics"][key[0]][key[1]]; }
            else value = (double)totals["Statistics"][pair.Key];
            Check(value + .0001 >= pair.Value, "night sums exceed cumulative: " + pair.Key);
            terminalTail[pair.Key] = value - pair.Value;
        }
        for (int i = 0; i < 3; i++) Check((long)result["wallet"][resources[i]] == (long)totals["Statistics"]["Granted"][resources[i]]
            + (long)totals["Statistics"]["Deposited"][resources[i]] - (long)totals["Statistics"]["Spent"][resources[i]], "terminal wallet");
        var parsed = totals.ToObject<MatchReportSnapshot>();
        Check((string)result["resultText"] == "TỔNG TRẬN\n" + MatchReportText.Format(parsed), "terminal formatting not identical");
        var ab = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(Dir + "editor-ab.json"));
        Check((bool)ab["stable"] && (string)ab["error"] == "" && ab["groups"].Count() == 4, "invalid AB baseline");
        Save("ledger-audit.json", Newtonsoft.Json.JsonConvert.SerializeObject(new { passed = true, checkedSamples, dawns,
            dawnSums = sums, terminalTail, terminalWalletReconciled = true, sameResultFormatter = true, editorABStable = true }, Newtonsoft.Json.Formatting.Indented));
        return $"PASS independent file audit: {checkedSamples} wallet samples,5 dawns,terminal equation/same formatter and stable AB. Tail records any transactions after final dawn boundary.";
    }
    public static string RecoverTerminalExport()
    {
        var root = Find<GameSceneCompositionRoot>(); var b = Find<SwarmGameBridge>(); var clock = Find<DayNightCycleManager>(); var c = Camera.main;
        Check(EditorApplication.isPaused && root.Reports.IsFrozen && root.Reports.CompletedNight == 5 && b.Gameplay.Outcome == MatchOutcome.Victory, "completed natural match must remain loaded");
        var result = new { outcome = b.Gameplay.Outcome.ToString(), completedNights = root.Reports.CompletedNight, signals = root.DawnReport.DawnSignalCount,
            total = root.Reports.Total, wallet = b.Gameplay.Wallet, resultText = Find<MatchSession>().ResultStatisticsText,
            duration = 675.052, wallSeconds = 675.299, unity = Application.unityVersion, editor = true, timeScaleBeforeTerminal = 1,
            durations = new[] { clock.Definition.DayDuration, clock.Definition.DuskDuration, clock.Definition.NightDuration }, fogEnabled = !Find<FogOfWarManager>().IsDisabled,
            cameraPosition = new[] { c.transform.position.x, c.transform.position.y, c.transform.position.z }, orthographicSize = c.orthographicSize,
            exportRecovery = "Natural run completed; original terminal serialization failed on Vector3.normalized. Recovered from still-loaded frozen terminal state. Durations copied from original finished log; camera lock restored by runner cleanup." };
        Save("natural-result.json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
        Capture("natural-result.png"); return "Recovered terminal JSON/PNG from frozen completed match without replaying gameplay.";
    }
    public static void Capture(string name)
    {
        var view = EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")); view.Repaint();
        var render = (RenderTexture)view.GetType().GetField("m_RenderTexture", Private).GetValue(view);
        var old = RenderTexture.active; var image = new Texture2D(render.width, render.height, TextureFormat.RGB24, false);
        try
        {
            RenderTexture.active = render; image.ReadPixels(new Rect(0, 0, render.width, render.height), 0, 0); image.Apply();
            if (SystemInfo.graphicsUVStartsAtTop)
            {
                var pixels = image.GetPixels32();
                for (int y = 0; y < render.height / 2; y++) for (int x = 0; x < render.width; x++)
                { int a = y * render.width + x, b = (render.height - 1 - y) * render.width + x; var p = pixels[a]; pixels[a] = pixels[b]; pixels[b] = p; }
                image.SetPixels32(pixels); image.Apply();
            }
            Directory.CreateDirectory(Dir); File.WriteAllBytes(Dir + name, image.EncodeToPNG());
        }
        finally { RenderTexture.active = old; UnityEngine.Object.DestroyImmediate(image); }
    }
}
