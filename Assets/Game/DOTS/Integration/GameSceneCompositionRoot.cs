using System;
using Unity.Entities;
using UnityEngine;

[DefaultExecutionOrder(-250)]
[DisallowMultipleComponent]
public sealed class GameSceneCompositionRoot : MonoBehaviour
{
    [SerializeField] GameManager m_Game;
    [SerializeField] TilemapManager m_Map;
    [SerializeField] ProceduralMapAuthoring m_Generator;
    [SerializeField] FogOfWarManager m_Fog;
    [SerializeField] DayNightCycleManager m_DayNight;
    [SerializeField] EnemyWaveManager m_Waves;
    [SerializeField] CameraController m_Camera;
    [SerializeField] EconomyResearchDefinition m_ResearchDefinition;
    [SerializeField] ResearchPanel m_ResearchPanel;
    [SerializeField] ResearchTreePanel m_ResearchTree;
    [SerializeField] ProceduralMapPresentation m_MapPresentation;
    [SerializeField] MinimapFogUI m_Minimap;
    [SerializeField] MatchSession m_Match;
    public IResearchService Research { get; private set; }
    SwarmGameBridge m_Bridge;
    SceneResourceNodeLocator m_Nodes;
    SceneResourceDepotLocator m_Depots;
    public bool IsInitialized { get; private set; }
    public bool IsReady { get; private set; }
    public string LastError { get; private set; }
    public double InitializeMilliseconds { get; private set; }
    public double BindMilliseconds { get; private set; }
    public double ReadyRealtime { get; private set; }

    void Awake()
    {
        ProceduralMapRuntime.BeginComposition();
        var timer = System.Diagnostics.Stopwatch.StartNew();
        try { Initialize(); InitializeMilliseconds = timer.Elapsed.TotalMilliseconds; }
        catch (Exception error) { Fail(error); }
    }

    void Start()
    {
        if (!IsInitialized) return;
        try
        {
            var timer = System.Diagnostics.Stopwatch.StartNew();
            m_Bridge.BindScene();
            m_Waves.CaptureProceduralStartingRange();
            BindMilliseconds = timer.Elapsed.TotalMilliseconds;
            m_Game.BeginGameplay();
            m_Bridge.Simulation.Paused = false;
            IsReady = true;
            ProceduralMapRuntime.CompleteComposition();
            ReadyRealtime = Time.realtimeSinceStartupAsDouble;
        }
        catch (Exception error) { Fail(error); }
    }

    void Fail(Exception error)
    {
        LastError = error.Message;
        IsReady = false;
        ProceduralMapRuntime.FailComposition(LastError);
        if (m_Bridge != null && m_Bridge.Simulation != null) m_Bridge.Simulation.Paused = true;
        Debug.LogError($"Scene composition failed: {LastError}", this);
    }

    public void Initialize()
    {
        if (IsInitialized) return;
        if (m_Game == null || m_Game.gameObject != gameObject || m_Map == null || m_Generator == null || m_Camera == null || m_Waves == null || m_DayNight == null)
            throw new InvalidOperationException("Scene root requires its local GameManager, map, generator, camera, day/night and waves.");
        if (!m_Generator.IsReady || m_Map.WalkableTilemap == null)
            throw new InvalidOperationException("Generated map must be ready before scene composition.");
        if (m_ResearchDefinition == null) throw new InvalidOperationException("Economy research definition is required.");
        if(m_MapPresentation==null||m_Minimap==null)throw new InvalidOperationException("Procedural map presentation and minimap references are required.");
        var researchData = m_ResearchDefinition.ToCatalog();
        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            throw new InvalidOperationException("Scene composition requires a live ECS World.");
        m_Bridge = GetComponent<SwarmGameBridge>();
        if (m_Bridge == null) m_Bridge = gameObject.AddComponent<SwarmGameBridge>();
        m_Bridge.Initialize(world, m_Game, m_Map, m_Fog, m_DayNight);
        m_Bridge.Simulation.Paused = true;
        m_Bridge.Gameplay.ConfigureResearchCatalog(researchData);
        if (m_Match == null) throw new InvalidOperationException("Match session is required.");
        m_Match.Initialize(m_Bridge.Gameplay, m_DayNight, m_Game);
        Research = new ResearchService(m_Bridge);
        if (m_ResearchTree != null)
        {
            m_ResearchTree.Initialize(Research, m_Game, m_Camera);
            m_Game.InitializeResearchTree(m_ResearchTree);
        }
        else if (m_ResearchPanel != null)
            foreach (var panel in m_ResearchPanel.GetComponentsInChildren<ResearchPanel>(true)) panel.Initialize(Research, m_Game);
        m_Nodes = new SceneResourceNodeLocator(m_Game.ResourceContainers);
        m_Depots = new SceneResourceDepotLocator(() => m_Game.GetPlayerStructures());
        m_Game.Initialize(m_Bridge, m_Map, m_Camera, m_Fog, m_DayNight, m_Nodes, m_Depots);
        foreach (var spawner in m_Generator.LegacySpawners) spawner.Initialize(m_Game, m_Map, m_Bridge);
        m_Waves.Initialize(m_Game, m_Map, m_DayNight, m_Bridge, m_Generator);
        m_MapPresentation.Initialize(m_Generator.GeneratedMap,m_Map,m_Generator.OriginCell,m_Fog);
        m_Minimap.InitializeLandmarks(m_MapPresentation);
        ComposePresentation(m_Game, m_Map, m_Fog, m_DayNight, m_Waves, m_Camera, m_Bridge);
        IsInitialized = true;
    }


    public static void ComposePresentation(GameManager game, TilemapManager map, FogOfWarManager fog,
        DayNightCycleManager day, EnemyWaveManager waves, CameraController camera, SwarmGameBridge bridge)
    {
        if (game == null || map == null || fog == null || day == null || waves == null || camera == null || bridge == null)
            throw new ArgumentException("Scene presentation dependencies are required.");
        var viewCamera = camera.GetComponent<Camera>();
        if (viewCamera == null) viewCamera = Camera.main;
        if (viewCamera == null) throw new InvalidOperationException("Scene presentation requires a camera.");
        camera.Initialize(viewCamera);
        fog.Initialize(game, map);
        foreach (var sceneRoot in game.gameObject.scene.GetRootGameObjects())
        {
            foreach (var affectable in sceneRoot.GetComponentsInChildren<FogOfWarAffectable>(true)) affectable.Initialize(fog);
            foreach (var view in sceneRoot.GetComponentsInChildren<MonoBehaviour>(true))
                switch (view)
                {
                    case DayCounterUI ui: ui.Initialize(day); break;
                    case DayNightClockUI ui: ui.Initialize(day); break;
                    case WaveProgressUI ui: ui.Initialize(waves); break;
                    case SelectedUnitsInfoPanelUI ui: ui.Initialize(game); break;
                    case ResourceRequirementsDisplay ui: ui.Initialize(game); break;
                    case IdleWorkerPanelUI ui: ui.Initialize(game, camera); break;
                    case MinimapFogUI ui: ui.Initialize(fog, map, camera, viewCamera, bridge); break;
                    case CursorManager cursor: cursor.Initialize(game, bridge, viewCamera); break;
                    case GameFlowMenu menu: menu.InitializeResearchTree(game.ResearchTree); break;
                }
        }
    }

    void OnDestroy()
    {
        IsReady = false;
        m_Nodes?.ClearCache();
        ProceduralMapRuntime.EndComposition();
    }
}
