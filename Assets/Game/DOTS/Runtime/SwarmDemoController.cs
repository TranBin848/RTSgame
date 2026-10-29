using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace RTS.DOTS
{
    public enum SwarmScenario { OpenField, NarrowPassage, BaseSiege }

    [DefaultExecutionOrder(-100)]
    public sealed class SwarmDemoController : MonoBehaviour
    {
        [Range(100, 20000)] public int EnemyPopulation = 5000;
        [Range(0, 10000)] public int ProjectilePopulation = 2000;
        public uint Seed = 2026;
        public SwarmScenario Scenario = SwarmScenario.BaseSiege;
        public bool AutoReplenish = true;
        public SwarmVisualLibrary Visuals;
        public SwarmSimulationSystem Simulation { get; private set; }
        SwarmInstancedRenderer m_Renderer;
        SwarmBenchmarkRecorder m_Benchmark;
        Unity.Mathematics.Random m_Random;
        readonly List<Entity> m_StaticEntities = new();
        Entity m_Base;
        Mesh m_TerrainMesh;
        GameObject m_Terrain;
        Material m_TerrainMaterial;
        Camera m_Camera;
        string m_Status = "Starting ECS world...";
        float m_NextStatus;
        bool m_ShowHUD = true;
        int m_PendingSpawn;
        static readonly int[] Populations = { 100, 1000, 5000, 10000 };

        void Start()
        {
            Application.runInBackground = true;
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) { Debug.LogError("No default ECS world. Check Entities package initialization."); enabled = false; return; }
            Simulation = world.GetOrCreateSystemManaged<SwarmSimulationSystem>();
            m_Renderer = gameObject.AddComponent<SwarmInstancedRenderer>();
            m_Renderer.SetLibrary(Visuals); m_Renderer.Simulation = Simulation;
            m_Benchmark = gameObject.AddComponent<SwarmBenchmarkRecorder>(); m_Benchmark.Simulation = Simulation;
            m_Camera = Camera.main;
            ResetDemo();
        }
        public void ResetDemo()
        {
            if (Simulation == null) return;
            Simulation.Paused = false; Simulation.ResetSimulation();
            foreach (var entity in m_StaticEntities) Simulation.Remove(entity);
            m_StaticEntities.Clear();
            m_Random = new Unity.Mathematics.Random(math.max(1u, Seed));
            const int size = 128;
            byte[] grid = new byte[size * size];
            var vertices = new List<Vector3>(); var triangles = new List<int>();
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                int wx = x - 64, wy = y - 64;
                bool blocked = x == 0 || y == 0 || x == size - 1 || y == size - 1;
                if (Scenario == SwarmScenario.NarrowPassage) blocked |= wx >= 10 && wx <= 12 && math.abs(wy) > 3;
                if (Scenario == SwarmScenario.BaseSiege)
                    blocked |= (math.abs(wx) >= 9 && math.abs(wx) <= 11 && math.abs(wy) < 18 && math.abs(wy) > 3)
                        || (math.abs(wy) >= 9 && math.abs(wy) <= 11 && math.abs(wx) < 18 && math.abs(wx) > 3);
                grid[y * size + x] = (byte)(blocked ? 0 : 1);
                if (blocked)
                {
                    int n = vertices.Count;
                    vertices.Add(new Vector3(wx, wy, 0)); vertices.Add(new Vector3(wx + 1, wy, 0));
                    vertices.Add(new Vector3(wx + 1, wy + 1, 0)); vertices.Add(new Vector3(wx, wy + 1, 0));
                    triangles.Add(n); triangles.Add(n + 1); triangles.Add(n + 2); triangles.Add(n + 2); triangles.Add(n + 3); triangles.Add(n);
                }
            }
            Simulation.ConfigureGrid(new float2(-64), new int2(size), 1, grid);
            m_Base = Simulation.CreateProxy(0, 0, new float2(1.5f), 1000000000);
            m_StaticEntities.Add(m_Base); Simulation.SetGoal(0, m_Base);
            // Collision boxes use exactly the same blocked strips as navigation.
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                if (grid[y * size + x] != 0) continue;
                int start = x;
                while (x + 1 < size && grid[y * size + x + 1] == 0) x++;
                float2 half = new float2((x - start + 1) * 0.5f, 0.5f);
                m_StaticEntities.Add(Simulation.CreateProxy(0, new float2(start - 64, y - 64) + half, half, float.MaxValue, -1));
            }
            if (m_Terrain != null) Destroy(m_Terrain);
            if (m_TerrainMesh != null) Destroy(m_TerrainMesh);
            if (m_TerrainMaterial != null) Destroy(m_TerrainMaterial);
            m_Terrain = new GameObject("Swarm obstacles (one mesh)"); m_Terrain.transform.SetParent(transform);
            m_TerrainMesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            m_TerrainMesh.SetVertices(vertices); m_TerrainMesh.SetTriangles(triangles, 0); m_TerrainMesh.RecalculateBounds();
            m_Terrain.AddComponent<MeshFilter>().sharedMesh = m_TerrainMesh;
            m_TerrainMaterial = new Material(Shader.Find("RTS/DOTS/SwarmTerrain"));
            m_TerrainMaterial.SetColor("_BaseColor", new Color(0.22f, 0.3f, 0.34f));
            m_Terrain.AddComponent<MeshRenderer>().sharedMaterial = m_TerrainMaterial;
            m_PendingSpawn = EnemyPopulation;
        }
        void Update()
        {
            if (Simulation == null || !Simulation.World.IsCreated) return;
            if (Input.GetKeyDown(KeyCode.F1)) m_ShowHUD = !m_ShowHUD;
            if (Input.GetKeyDown(KeyCode.Space)) Simulation.Paused = !Simulation.Paused;
            if (Input.GetKeyDown(KeyCode.R)) ResetDemo();
            if (Input.GetKeyDown(KeyCode.B)) m_Benchmark.BeginCapture();
            if (Input.GetKeyDown(KeyCode.E)) m_Benchmark.Export();
            if (Input.GetMouseButtonDown(1) && m_Camera != null)
            {
                Vector3 p = m_Camera.ScreenToWorldPoint(Input.mousePosition);
                Simulation.UpdateProxy(m_Base, new float2(p.x, p.y), 1000000000, true);
                Simulation.SetGoal(new float2(p.x, p.y), m_Base);
            }
            if (!Simulation.Paused)
            {
                int wanted = AutoReplenish ? math.max(0, EnemyPopulation - Simulation.AgentCount) : m_PendingSpawn;
                for (int i = 0; i < math.min(wanted, 200); i++)
                {
                    float angle = m_Random.NextFloat(0, math.PI * 2), radius = m_Random.NextFloat(25, 58);
                    float2 point = new float2(math.cos(angle), math.sin(angle)) * radius;
                    var settings = EnemySpawnSettings.Default;
                    settings.Speed = m_Random.NextFloat(2, 4); settings.Ranged = m_Random.NextFloat() < 0.15f;
                    settings.AttackRange = settings.Ranged ? 4 : 0.6f; settings.VisualId = settings.Ranged ? 1 : 0;
                    settings.ProjectileVisualId = 3;
                    if (Simulation.SpawnEnemy(point, settings) != Entity.Null) m_PendingSpawn = math.max(0, m_PendingSpawn - 1);
                }
                int shots = math.min(500, math.max(0, ProjectilePopulation - Simulation.ProjectileCount));
                int fired = 0;
                for (int i = 0; i < shots * 3 && fired < shots; i++)
                {
                    float angle = m_Random.NextFloat(0, math.PI * 2);
                    float2 launch = Simulation.Goal + new float2(math.cos(angle), math.sin(angle)) * 0.5f;
                    var renderData = Simulation.RenderData;
                    if (renderData.Length == 0) continue;
                    Entity target = renderData[m_Random.NextInt(renderData.Length)].Entity;
                    if (!Simulation.TryGetBody(target, out var body) || body.Faction != 1) continue;
                    Simulation.SpawnProjectile(new ProjectileRequest { Owner = m_Base, Target = target, Position = launch,
                        Aim = body.Position, Damage = 2, Faction = 0, Speed = 12, VisualId = 2, Arc = 1, ArcHeight = 1.5f });
                    fired++;
                }
            }
            if (m_ShowHUD && Time.unscaledTime >= m_NextStatus)
            {
                m_NextStatus = Time.unscaledTime + 0.5f;
                m_Status = $"DOTS Swarm | {Scenario}\nEnemies: {Simulation.AgentCount:N0} / {EnemyPopulation:N0}   Projectiles: {Simulation.ProjectileCount:N0} / {ProjectilePopulation:N0}\n"
                    + $"FPS: {1f / Mathf.Max(0.0001f, Time.unscaledDeltaTime):F0}   ECS tick: {Simulation.LastTickMilliseconds:F2} ms\n"
                    + $"Visible: {m_Renderer.VisibleInstances:N0}   Draw calls: {m_Renderer.DrawCalls}   Hits: {Simulation.TotalHits:N0}\n"
                    + $"Seed: {Seed}   Capture: {m_Benchmark.Status}\nSpace: pause | R: reset | right click: target | B: capture | E: export | F1: HUD";
            }
        }
        void OnGUI()
        {
            if (!m_ShowHUD || Simulation == null) return;
            GUI.Box(new Rect(12, 12, 600, 165), m_Status);
            for (int i = 0; i < Populations.Length; i++)
                if (GUI.Button(new Rect(12 + i * 110, 185, 100, 28), Populations[i].ToString())) { EnemyPopulation = Populations[i]; ResetDemo(); }
            if (GUI.Button(new Rect(452, 185, 145, 28), "Rendering on/off")) m_Renderer.DrawEntities = !m_Renderer.DrawEntities;
            for (int i = 0; i < 3; i++)
                if (GUI.Button(new Rect(12 + i * 170, 222, 160, 28), ((SwarmScenario)i).ToString())) { Scenario = (SwarmScenario)i; ResetDemo(); }
        }
        void OnDestroy()
        {
            if (Simulation != null && Simulation.World != null && Simulation.World.IsCreated)
            {
                Simulation.ResetSimulation();
                foreach (var entity in m_StaticEntities) Simulation.Remove(entity);
            }
            if (m_TerrainMesh != null) Destroy(m_TerrainMesh);
            if (m_TerrainMaterial != null) Destroy(m_TerrainMaterial);
        }
    }
}
