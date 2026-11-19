using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RTS.DOTS;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

// Activated only by an explicit Player/Editor command-line flag. No effect during normal gameplay.
[DefaultExecutionOrder(300)]
public sealed class SwarmSmokeAutomation : MonoBehaviour
{
    readonly List<string> m_Errors = new();
    string m_Output;
    RenderTexture m_CaptureTarget;
    Camera m_CaptureCamera;
    RenderPipeline.StandardRequest m_RenderRequest;
    bool m_Progression;
    double m_LoadRequested;
    [Serializable] class Result
    {
        public string unity, cpu, gpu, scenario, csv;
        public int enemies, projectiles, hits, visible, drawCalls;
        public double lastTickMs;
        public string[] errors;
        public string cameraPosition;
        public int renderSnapshots;
        public bool offscreenRender;
        public int seed, generatorVersion, actors, workers, day, spawned;
        public double sceneToReadyMs, generatorAwakeToReadyMs, coreMs, terrainMs, resourceMs, navigationMs, initializeMs, bindMs;
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        var args = Environment.GetCommandLineArgs();
        if (Array.IndexOf(args, "--swarm-smoke") < 0 && Array.IndexOf(args, "--rts-smoke") < 0 && Array.IndexOf(args, "--progression-smoke") < 0) return;
        var host = new GameObject("DOTS smoke automation"); DontDestroyOnLoad(host); host.AddComponent<SwarmSmokeAutomation>();
    }
    IEnumerator Start()
    {
        Application.logMessageReceived += OnLog;
        Application.runInBackground = true;
        var args = Environment.GetCommandLineArgs();
        int pathIndex = Array.IndexOf(args, "--swarm-output");
        m_Output = pathIndex >= 0 && pathIndex + 1 < args.Length ? args[pathIndex + 1] : Path.GetFullPath(".utmp/swarm-smoke");
        Directory.CreateDirectory(m_Output);
        m_Progression = Array.IndexOf(args, "--progression-smoke") >= 0;
        bool rts = m_Progression || Array.IndexOf(args, "--rts-smoke") >= 0;
        string targetScene = m_Progression ? "ProceduralGame" : "SampleScene";
        m_LoadRequested = Time.realtimeSinceStartupAsDouble;
        if (rts && SceneManager.GetActiveScene().name != targetScene) { SceneManager.LoadScene(targetScene); yield return null; }
        yield return null;
        var demo = FindFirstObjectByType<SwarmDemoController>();
        var bridge = SwarmGameBridge.Instance;
        SwarmSimulationSystem simulation = demo != null ? demo.Simulation : bridge != null ? bridge.Simulation : null;
        if (simulation == null) { m_Errors.Add("No swarm simulation was initialized."); Finish(null, null, rts); yield break; }
        var cameraController = FindFirstObjectByType<CameraController>();
        if (cameraController != null) cameraController.LockCamera = true;
        m_CaptureCamera = Camera.main;
        if (m_CaptureCamera != null)
        {
            // A hidden/minimized window can suppress screen rendering. An explicit target still exercises the GPU pipeline.
            m_CaptureTarget = new RenderTexture(1920, 1080, 24) { name = "Swarm smoke render target" };
            m_CaptureTarget.Create(); m_CaptureCamera.targetTexture = m_CaptureTarget;
            m_RenderRequest = new RenderPipeline.StandardRequest { destination = m_CaptureTarget };
        }
        if (demo != null)
        {
            int countIndex = Array.IndexOf(args, "--swarm-count");
            if (countIndex >= 0 && countIndex + 1 < args.Length && int.TryParse(args[countIndex + 1], out int count))
            { demo.EnemyPopulation = Mathf.Clamp(count, 100, 20000); demo.ResetDemo(); }
            if (Array.IndexOf(args, "--swarm-no-render") >= 0) demo.GetComponent<SwarmInstancedRenderer>().DrawEntities = false;
        }
        if (rts && !m_Progression && bridge != null)
        {
            var hall = FindFirstObjectByType<TownHall>();
            var settings = EnemySpawnSettings.Default;
            if (hall != null)
            {
                Vector3 center = hall.transform.position;
                for (int i = 0; i < 100; i++)
                {
                    float angle = i * 2.399963f;
                    simulation.SpawnEnemy(new float2(center.x + Mathf.Cos(angle) * 7, center.y + Mathf.Sin(angle) * 7), settings);
                }
                var warrior = FindFirstObjectByType<WarriorUnit>();
                yield return new WaitForSecondsRealtime(0.5f);
                if (warrior != null && simulation.TryFindEnemy(new float2(warrior.transform.position.x, warrior.transform.position.y), 100, out var enemy)) warrior.SetDotsTarget(enemy);
            }
        }
        if (m_Progression && bridge != null)
        {
            var root = FindFirstObjectByType<GameSceneCompositionRoot>();
            if (root == null || !root.IsReady) m_Errors.Add("Procedural composition did not become ready.");
            var workers = FindObjectsByType<WorkerUnit>(FindObjectsSortMode.None);
            var trees = FindObjectsByType<Tree>(FindObjectsSortMode.None);
            var stones = FindObjectsByType<GoldStone>(FindObjectsSortMode.None);
            for (int i = 0; i < workers.Length; i++)
            {
                var tree = trees.OrderBy(t => (t.transform.position - workers[i].transform.position).sqrMagnitude).FirstOrDefault();
                var gold = stones.OrderBy(t => (t.transform.position - workers[i].transform.position).sqrMagnitude).FirstOrDefault();
                if (i == 1 && gold != null) workers[i].TryAssignResourceNode(gold);
                else if (tree != null) workers[i].TryAssignResourceNode(tree);
            }
            var hall = FindFirstObjectByType<TownHall>();
            if (root != null && hall != null)
            {
                root.Research.Start(GameplaySimulationSystem.EconomyToolsId, hall);
                var game = hall.GetComponentInParent<GameManager>();
                if (game == null) game = FindFirstObjectByType<GameManager>();
                game.ActiveUnit = hall;
                foreach (var action in hall.Actions) if (action is SpawnVillagerActionSO) { action.Excute(game); break; }
            }
        }
        var recorder = demo != null ? demo.GetComponent<SwarmBenchmarkRecorder>() : gameObject.AddComponent<SwarmBenchmarkRecorder>();
        recorder.Simulation = simulation; recorder.WarmupSeconds = 5; recorder.CaptureSeconds = 10;
        if (m_Progression) { recorder.WarmupSeconds = 30; recorder.CaptureSeconds = 90; }
        recorder.Renderer = FindFirstObjectByType<SwarmInstancedRenderer>(); recorder.OffscreenCapture = true;
        recorder.OutputDirectoryOverride = m_Output; recorder.BeginCapture();
        yield return new WaitForSecondsRealtime(recorder.WarmupSeconds + recorder.CaptureSeconds + 1);
        yield return new WaitForEndOfFrame();
        if (m_CaptureTarget != null)
        {
            RenderTexture previous = RenderTexture.active; RenderTexture.active = m_CaptureTarget;
            var screenshot = new Texture2D(m_CaptureTarget.width, m_CaptureTarget.height, TextureFormat.RGB24, false);
            screenshot.ReadPixels(new Rect(0, 0, screenshot.width, screenshot.height), 0, 0); screenshot.Apply();
            File.WriteAllBytes(Path.Combine(m_Output, "screenshot.png"), screenshot.EncodeToPNG());
            Destroy(screenshot); RenderTexture.active = previous;
        }
        yield return new WaitForEndOfFrame();
        Finish(simulation, recorder, rts);
    }
    void OnLog(string condition, string stack, LogType type)
    {
        if ((type == LogType.Error || type == LogType.Exception || type == LogType.Assert) && m_Errors.Count < 32)
            m_Errors.Add(condition + "\n" + stack);
    }
    void LateUpdate()
    {
        if (m_CaptureCamera != null && m_RenderRequest != null)
            RenderPipeline.SubmitRenderRequest(m_CaptureCamera, m_RenderRequest);
    }
    void Finish(SwarmSimulationSystem simulation, SwarmBenchmarkRecorder recorder, bool rts)
    {
        var renderer = FindFirstObjectByType<SwarmInstancedRenderer>();
        var result = new Result { unity = Application.unityVersion, cpu = SystemInfo.processorType, gpu = SystemInfo.graphicsDeviceName,
            scenario = rts ? "RTS integration" : "Swarm demo", errors = m_Errors.ToArray(), csv = recorder != null ? recorder.Export() : null,
            enemies = simulation?.AgentCount ?? 0, projectiles = simulation?.ProjectileCount ?? 0, hits = simulation?.TotalHits ?? 0,
            lastTickMs = simulation?.LastTickMilliseconds ?? 0, visible = renderer != null ? renderer.VisibleInstances : 0,
            drawCalls = renderer != null ? renderer.DrawCalls : 0 };
        result.renderSnapshots = simulation != null ? simulation.RenderData.Length : 0;
        result.cameraPosition = m_CaptureCamera != null ? m_CaptureCamera.transform.position.ToString() : "missing camera";
        result.offscreenRender = m_CaptureTarget != null;
        if (m_Progression)
        {
            result.scenario = "Procedural progression; natural first night; workers/research/training; offscreen world render";
            var generator = FindFirstObjectByType<ProceduralMapAuthoring>(); var root = FindFirstObjectByType<GameSceneCompositionRoot>();
            var bridge = FindFirstObjectByType<SwarmGameBridge>();
            if (root != null && generator != null)
            {
                result.sceneToReadyMs = (root.ReadyRealtime - m_LoadRequested) * 1000;
                result.generatorAwakeToReadyMs = (root.ReadyRealtime - generator.BuildStartedRealtime) * 1000;
                result.coreMs = generator.CoreMilliseconds; result.terrainMs = generator.TerrainMilliseconds;
                result.resourceMs = generator.ResourceMilliseconds; result.navigationMs = generator.NavigationMilliseconds;
                result.initializeMs = root.InitializeMilliseconds; result.bindMs = root.BindMilliseconds;
                result.seed = generator.GeneratedMap.Seed; result.generatorVersion = generator.GeneratedMap.GeneratorVersion;
            }
            result.actors = bridge != null ? bridge.Gameplay.ActorCount : 0;
            result.workers = FindObjectsByType<WorkerUnit>(FindObjectsSortMode.None).Length;
            result.day = FindFirstObjectByType<DayNightCycleManager>().CurrentDay;
            result.spawned = simulation != null ? simulation.TotalSpawned : 0;
        }
        File.WriteAllText(Path.Combine(m_Output, "result.json"), JsonUtility.ToJson(result, true));
        Application.logMessageReceived -= OnLog;
        Application.Quit(m_Errors.Count == 0 ? 0 : 1);
    }
    void OnDestroy()
    {
        Application.logMessageReceived -= OnLog;
        if (m_CaptureCamera != null) m_CaptureCamera.targetTexture = null;
        if (m_CaptureTarget != null) { m_CaptureTarget.Release(); Destroy(m_CaptureTarget); }
    }
}
