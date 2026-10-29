using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
    [Serializable] class Result
    {
        public string unity, cpu, gpu, scenario, csv;
        public int enemies, projectiles, hits, visible, drawCalls;
        public double lastTickMs;
        public string[] errors;
        public string cameraPosition;
        public int renderSnapshots;
        public bool offscreenRender;
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        var args = Environment.GetCommandLineArgs();
        if (Array.IndexOf(args, "--swarm-smoke") < 0 && Array.IndexOf(args, "--rts-smoke") < 0) return;
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
        bool rts = Array.IndexOf(args, "--rts-smoke") >= 0;
        if (rts && SceneManager.GetActiveScene().name != "SampleScene") { SceneManager.LoadScene("SampleScene"); yield return null; }
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
        if (rts && bridge != null)
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
        var recorder = demo != null ? demo.GetComponent<SwarmBenchmarkRecorder>() : gameObject.AddComponent<SwarmBenchmarkRecorder>();
        recorder.Simulation = simulation; recorder.WarmupSeconds = 5; recorder.CaptureSeconds = 10;
        recorder.Renderer = FindFirstObjectByType<SwarmInstancedRenderer>(); recorder.OffscreenCapture = true;
        recorder.OutputDirectoryOverride = m_Output; recorder.BeginCapture();
        yield return new WaitForSecondsRealtime(16);
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
