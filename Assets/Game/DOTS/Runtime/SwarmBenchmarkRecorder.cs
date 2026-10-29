using System;
using System.Globalization;
using System.IO;
using System.Text;
using Unity.Profiling;
using UnityEngine;

namespace RTS.DOTS
{
    public sealed class SwarmBenchmarkRecorder : MonoBehaviour
    {
        public SwarmSimulationSystem Simulation { get; set; }
        public float WarmupSeconds = 5, CaptureSeconds = 60;
        public string OutputDirectoryOverride;
        public SwarmInstancedRenderer Renderer;
        public bool OffscreenCapture;
        public string Status { get; private set; } = "idle";
        const int Capacity = 36000;
        readonly float[] m_Frames = new float[Capacity], m_Ticks = new float[Capacity];
        readonly int[] m_Agents = new int[Capacity], m_Projectiles = new int[Capacity];
        readonly long[] m_GC = new long[Capacity];
        readonly long[] m_Allocated = new long[Capacity], m_TickIds = new long[Capacity];
        int m_Generation;
        SwarmInstancedRenderer m_CaptureRenderer;
        bool m_RenderingAtStart;
        float m_NextMemorySample;
        long m_LastAllocatedBytes;
        int m_Count;
        float m_Started;
        bool m_Capturing;
        ProfilerRecorder m_GCRecorder;
        void OnEnable() { m_GCRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame"); }
        void OnDisable() { m_GCRecorder.Dispose(); }
        public void BeginCapture()
        {
            m_Started = Time.unscaledTime; m_Count = 0; m_Capturing = true; Status = "warmup";
            m_Generation = Simulation != null ? Simulation.Generation : -1;
            m_CaptureRenderer = Renderer != null ? Renderer : GetComponent<SwarmInstancedRenderer>();
            m_RenderingAtStart = m_CaptureRenderer != null && m_CaptureRenderer.DrawEntities;
            m_NextMemorySample = 0;
        }
        void Update()
        {
            if (!m_Capturing || Simulation == null) return;
            if (Simulation.Generation != m_Generation) { m_Capturing = false; Status = "aborted: simulation reset"; return; }
            if (m_CaptureRenderer != null && m_CaptureRenderer.DrawEntities != m_RenderingAtStart)
            { m_Capturing = false; Status = "aborted: rendering changed"; return; }
            float elapsed = Time.unscaledTime - m_Started;
            if (elapsed < WarmupSeconds) return;
            if (elapsed >= WarmupSeconds + CaptureSeconds || m_Count >= Capacity)
            { m_Capturing = false; Status = $"complete ({m_Count} frames)"; return; }
            if (Simulation.Paused) { m_Capturing = false; Status = "aborted: simulation paused"; return; }
            m_Frames[m_Count] = Time.unscaledDeltaTime * 1000; m_Ticks[m_Count] = (float)Simulation.LastTickMilliseconds;
            m_Agents[m_Count] = Simulation.AgentCount; m_Projectiles[m_Count] = Simulation.ProjectileCount;
            m_GC[m_Count] = m_GCRecorder.Valid ? m_GCRecorder.LastValue : -1;
            if (Time.unscaledTime >= m_NextMemorySample)
            {
                m_LastAllocatedBytes = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
                m_NextMemorySample = Time.unscaledTime + 1;
            }
            m_Allocated[m_Count] = m_LastAllocatedBytes;
            m_TickIds[m_Count] = Simulation.TickCount;
            m_Count++; Status = "recording";
        }
        public string Export()
        {
            if (m_Count == 0) { Status = "no samples; press B first"; return null; }
            string folder = !string.IsNullOrEmpty(OutputDirectoryOverride) ? OutputDirectoryOverride : Application.isEditor ? Path.GetFullPath(Path.Combine(Application.dataPath, "../.utmp/benchmarks"))
                : Path.Combine(Application.persistentDataPath, "Benchmarks");
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, "swarm-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + ".csv");
            var text = new StringBuilder(1024 + m_Count * 80);
            text.AppendLine("# UTC: " + DateTime.UtcNow.ToString("O"));
            text.AppendLine("# CPU: " + SystemInfo.processorType + "; logical cores: " + SystemInfo.processorCount);
            text.AppendLine("# GPU: " + SystemInfo.graphicsDeviceName + "; RAM MB: " + SystemInfo.systemMemorySize);
            text.AppendLine("# Unity: " + Application.unityVersion + "; Editor: " + Application.isEditor + "; resolution: " + Screen.width + "x" + Screen.height);
            text.AppendLine("# vSync: " + QualitySettings.vSyncCount + "; target FPS: " + Application.targetFrameRate);
            var demo = GetComponent<SwarmDemoController>();
            if (demo != null) text.AppendLine("# scenario: " + demo.Scenario + "; seed: " + demo.Seed + "; enemy goal: " + demo.EnemyPopulation + "; projectile goal: " + demo.ProjectilePopulation);
            var renderer = Renderer != null ? Renderer : GetComponent<SwarmInstancedRenderer>();
            text.AppendLine("# rendering: " + (renderer != null && renderer.DrawEntities));
            text.AppendLine("# offscreen render request: " + OffscreenCapture);
            float[] sorted = new float[m_Count]; Array.Copy(m_Frames, sorted, m_Count); Array.Sort(sorted);
            text.AppendLine("# frame ms median: " + sorted[m_Count / 2].ToString("F3", CultureInfo.InvariantCulture)
                + "; P95: " + sorted[Math.Min(m_Count - 1, (int)(m_Count * 0.95))].ToString("F3", CultureInfo.InvariantCulture)
                + "; P99: " + sorted[Math.Min(m_Count - 1, (int)(m_Count * 0.99))].ToString("F3", CultureInfo.InvariantCulture));
            text.AppendLine("frame,frame_ms,ecs_tick_ms,enemies,projectiles,gc_bytes,unity_allocated_bytes,ecs_tick_id");
            for (int i = 0; i < m_Count; i++)
                text.Append(i).Append(',').Append(m_Frames[i].ToString("F4", CultureInfo.InvariantCulture)).Append(',')
                    .Append(m_Ticks[i].ToString("F4", CultureInfo.InvariantCulture)).Append(',').Append(m_Agents[i])
                    .Append(',').Append(m_Projectiles[i]).Append(',').Append(m_GC[i]).Append(',').Append(m_Allocated[i])
                    .Append(',').Append(m_TickIds[i]).AppendLine();
            File.WriteAllText(path, text.ToString(), new UTF8Encoding(false));
            Status = "exported: " + path; Debug.Log("Swarm benchmark exported: " + path); return path;
        }
    }
}
