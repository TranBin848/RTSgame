using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using RTS.DOTS;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;

// Disposable opt-in Release fixtures; discovery/reflection belongs to the harness, not gameplay.
public sealed class E10ReleaseValidation : MonoBehaviour
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    readonly List<string> m_Checks = new(), m_Errors = new();
    string m_Output;
    bool m_Finished;
    float m_Started;
    [Serializable] sealed class Report
    {
        public string scenario = "Release procedural lifecycle; injected terminal states, not a combat playtest";
        public bool passed, development;
        public string[] checks, errors;
    }
    static T Find<T>() where T : UnityEngine.Object => FindFirstObjectByType<T>();
    static void Call(object owner, string method, params object[] args) => owner.GetType().GetMethod(method, Private).Invoke(owner, args);
    static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    void Check(bool value, string message) { Require(value, message); m_Checks.Add(message); }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if (!Environment.GetCommandLineArgs().Contains("--e10-smoke")) return;
        var host = new GameObject("E10 Release lifecycle fixture"); DontDestroyOnLoad(host); host.AddComponent<E10ReleaseValidation>();
    }
    IEnumerator Start()
    {
        m_Started = Time.realtimeSinceStartup; Application.runInBackground = true;
        var args = Environment.GetCommandLineArgs(); int index = Array.IndexOf(args, "--swarm-output");
        m_Output = index >= 0 && index + 1 < args.Length ? args[index + 1] : Path.GetFullPath(".utmp/e10-smoke");
        Directory.CreateDirectory(m_Output); Application.logMessageReceived += OnLog;
        Check(SceneManager.GetActiveScene().name == "MainMenu", "authored MainMenu startup");
        Find<GameFlowMenu>().Play(); yield return null; yield return null;
        var root = Find<GameSceneCompositionRoot>(); var bridge = Find<SwarmGameBridge>();
        Check(root != null && root.IsReady && root.LastError == null, "procedural composition Ready");
        Find<CameraController>().LockCamera = true;
        var tree = Find<ResearchTreePanel>(); var menu = Find<GameFlowMenu>(); var clock = Find<DayNightCycleManager>();
        var fog = Find<FogOfWarManager>();
        Check(!fog.IsDisabled && fog.IsInitialized && root.ActionFeedback.IsInitialized && root.NightLighting.IsInitialized,
            "fog, feedback and lighting bound");
        tree.Open(); Check(tree.IsOpen && tree.BlocksGameplayInput, "tree blocks gameplay input");
        tree.Close(); yield return null; Check(!tree.IsOpen, "tree closes");
        var worker = Find<WorkerUnit>(); worker.MoveTo(worker.transform.position + Vector3.right * 2, DestinationSource.PlayerClick);
        yield return new WaitForSecondsRealtime(.2f);
        menu.Pause(); yield return null;
        float progress = clock.CycleNormalizedProgress; long tick = bridge.Simulation.TickCount;
        var pausedBody = bridge.Gameplay.EntityManager.GetComponentData<CombatBody>(worker.DotsEntity);
        var pausedWallet = bridge.Gameplay.Wallet;
        yield return new WaitForSecondsRealtime(.3f);
        var afterPause = bridge.Gameplay.EntityManager.GetComponentData<CombatBody>(worker.DotsEntity);
        Check(Time.timeScale == 0 && progress == clock.CycleNormalizedProgress && pausedWallet.Equals(bridge.Gameplay.Wallet)
            && math.all(pausedBody.Position == afterPause.Position) && pausedBody.Health == afterPause.Health,
            "pause freezes clock, wallet and actor state (zero-delta system updates may still count)");
        tree.Open(); tree.Close(); Check(Time.timeScale == 0, "tree preserves menu pause");
        menu.Resume(); yield return new WaitForSecondsRealtime(.15f);
        Check(Time.timeScale == 1 && bridge.Simulation.TickCount > tick, "resume advances simulation");
        var hall = Find<TownHall>(); var em = bridge.Gameplay.EntityManager;
        var body = em.GetComponentData<CombatBody>(hall.DotsEntity); body.Health = 0; em.SetComponentData(hall.DotsEntity, body);
        yield return null; yield return new WaitForEndOfFrame();
        Check(bridge.Gameplay.Outcome == MatchOutcome.Defeat && Time.timeScale == 0 && root.Reports.IsFrozen,
            "last Hall defeat freezes result");
        Check(Find<MatchSession>().ResultStatisticsText.Contains("TỔNG TRẬN"), "result contains cumulative statistics");
        Capture("defeat.png");
        var oldRoot = root; var oldFeedback = root.ActionFeedback; var oldLights = root.NightLighting;
        // Invoke the actual bound result button, exercising its listener after a terminal pause.
        var replay = (UnityEngine.UI.Button)typeof(MatchSession).GetField("m_ReplayButton", Private).GetValue(Find<MatchSession>());
        replay.onClick.Invoke(); yield return null; yield return null;
        root = Find<GameSceneCompositionRoot>(); bridge = Find<SwarmGameBridge>();
        Check(oldRoot == null && oldFeedback == null && oldLights == null && root.IsReady && Time.timeScale == 1,
            "result replay disposes old presentation and resumes fresh match");
        Check(bridge.Gameplay.Outcome == MatchOutcome.Running && root.Research.State.UnlockMask == 0
            && root.Reports.CompletedNight == 0 && bridge.Gameplay.Statistics.Deposited.Wood == 0,
            "replay resets research, reports and ledger");
        clock = Find<DayNightCycleManager>();
        // Five phase cycles exercise report/dawn listeners, without pretending to simulate five combats.
        for (int i = 0; i < 5; i++)
        {
            Call(clock, "SetPhase", DayNightPhase.Dusk, false);
            Call(clock, "SetPhase", DayNightPhase.Night, false);
            Call(clock, "SetPhase", DayNightPhase.Day, false);
        }
        yield return null; yield return new WaitForEndOfFrame();
        Check(clock.CurrentDay == 6 && root.Reports.CompletedNight == 5 && bridge.Gameplay.Outcome == MatchOutcome.Victory
            && Time.timeScale == 0 && root.Reports.IsFrozen, "fifth dawn victory and report freeze");
        Capture("victory.png");
        var button = (UnityEngine.UI.Button)typeof(MatchSession).GetField("m_MenuButton", Private).GetValue(Find<MatchSession>());
        button.onClick.Invoke(); yield return null; yield return null;
        Check(SceneManager.GetActiveScene().name == "MainMenu" && Find<GameSceneCompositionRoot>() == null
            && Find<ActionFeedbackPresentation>() == null && !ProceduralMapRuntime.IsActive && Time.timeScale == 1,
            "result menu clears match context");
        Find<GameFlowMenu>().Play(); yield return null; yield return null;
        Check(Find<GameSceneCompositionRoot>().IsReady && FindObjectsByType<ActionFeedbackPresentation>(FindObjectsSortMode.None).Length == 1
            && FindObjectsByType<StructureNightLighting>(FindObjectsSortMode.None).Length == 1, "menu starts one fresh presentation context");
        Finish();
    }
    void Update() { if (!m_Finished && Time.realtimeSinceStartup - m_Started > 60) { m_Errors.Add("lifecycle watchdog timeout"); Finish(); } }
    void Capture(string name)
    {
        var image = ScreenCapture.CaptureScreenshotAsTexture(); File.WriteAllBytes(Path.Combine(m_Output, name), image.EncodeToPNG()); Destroy(image);
    }
    void OnLog(string text, string stack, LogType type)
    {
        if ((type == LogType.Error || type == LogType.Exception || type == LogType.Assert) && m_Errors.Count < 32) m_Errors.Add(text + "\n" + stack);
    }
    void Finish()
    {
        m_Finished = true;
        File.WriteAllText(Path.Combine(m_Output, "lifecycle-result.json"), JsonUtility.ToJson(new Report {
            passed = m_Checks.Count == 15 && m_Errors.Count == 0, development = Debug.isDebugBuild,
            checks = m_Checks.ToArray(), errors = m_Errors.ToArray() }, true));
        Application.logMessageReceived -= OnLog; Application.Quit(m_Checks.Count == 15 && m_Errors.Count == 0 ? 0 : 1);
    }
    void OnDestroy() => Application.logMessageReceived -= OnLog;

    public static IEnumerator NightStress(GameSceneCompositionRoot root, SwarmGameBridge bridge, Camera camera)
    {
        var hall = Find<TownHall>(); var clock = Find<DayNightCycleManager>();
        var center = hall.transform.position;
        camera.transform.position = new Vector3(center.x, center.y, -10); camera.orthographicSize = 8;
        clock.enabled = false; Find<EnemyWaveManager>().enabled = false;
        Call(clock, "SetPhase", DayNightPhase.Night, true);
        for (int i = 0; i < 7; i++)
        {
            float angle = i * Mathf.PI * 2 / 7;
            var clone = bridge.SpawnGameplay(hall.gameObject, center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * 3.2f, Quaternion.identity);
            var inherited = clone.transform.Find("Night light presentation"); if (inherited != null) Destroy(inherited.gameObject);
        }
        foreach (var unit in FindObjectsByType<TownHall>(FindObjectsSortMode.None))
        {
            var body = bridge.Gameplay.EntityManager.GetComponentData<CombatBody>(unit.DotsEntity);
            body.Health = 1000000; bridge.Gameplay.EntityManager.SetComponentData(unit.DotsEntity, body);
        }
        var emit = typeof(ActionFeedbackPresentation).GetMethod("OnEvent", Private);
        var payload = new object[] { new GameplayEvent { Kind = GameplayEventKind.Gather, Position = new float2(center.x, center.y) } };
        // Saturate the real bounded presentation pool; these synthetic events never change the economy.
        while (root != null)
        {
            for (int i = 0; i < 40; i++) emit.Invoke(root.ActionFeedback, payload);
            for (int i = bridge.Simulation.AgentCount; i < 96; i++)
            {
                float angle = i * 2.399963f;
                bridge.Simulation.SpawnEnemy(new float2(center.x + Mathf.Cos(angle) * 4, center.y + Mathf.Sin(angle) * 4), EnemySpawnSettings.Default);
            }
            yield return new WaitForSeconds(.2f);
        }
    }
}
