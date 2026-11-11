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
    [SerializeField] CameraController m_Camera;
    [SerializeField] EconomyResearchDefinition m_ResearchDefinition;
    [SerializeField] ResearchPanel m_ResearchPanel;
    public IResearchService Research { get; private set; }
    SwarmGameBridge m_Bridge;
    SceneResourceNodeLocator m_Nodes;
    SceneResourceDepotLocator m_Depots;
    public bool IsInitialized { get; private set; }
    public bool IsReady { get; private set; }
    public string LastError { get; private set; }

    void Awake()
    {
        ProceduralMapRuntime.BeginComposition();
        try { Initialize(); }
        catch (Exception error) { Fail(error); }
    }

    void Start()
    {
        if (!IsInitialized) return;
        try
        {
            m_Bridge.BindScene();
            m_Bridge.Simulation.Paused = false;
            IsReady = true;
            ProceduralMapRuntime.CompleteComposition();
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
        if (m_Game == null || m_Game.gameObject != gameObject || m_Map == null || m_Generator == null || m_Camera == null)
            throw new InvalidOperationException("Scene root requires its local GameManager, map, generator and camera.");
        if (!m_Generator.IsReady || m_Map.WalkableTilemap == null)
            throw new InvalidOperationException("Generated map must be ready before scene composition.");
        if (m_ResearchDefinition == null) throw new InvalidOperationException("Economy research definition is required.");
        var researchData = m_ResearchDefinition.ToCatalog();
        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            throw new InvalidOperationException("Scene composition requires a live ECS World.");
        m_Bridge = GetComponent<SwarmGameBridge>();
        if (m_Bridge == null) m_Bridge = gameObject.AddComponent<SwarmGameBridge>();
        m_Bridge.Initialize(world, m_Game, m_Map, m_Fog, m_DayNight);
        m_Bridge.Simulation.Paused = true;
        m_Bridge.Gameplay.ConfigureResearchCatalog(researchData);
        Research = new ResearchService(m_Bridge);
        if (m_ResearchPanel != null)
            foreach (var panel in m_ResearchPanel.GetComponentsInChildren<ResearchPanel>(true)) panel.Initialize(Research, m_Game);
        m_Nodes = new SceneResourceNodeLocator(m_Game.ResourceContainers);
        m_Depots = new SceneResourceDepotLocator(() => m_Game.GetPlayerStructures());
        m_Game.Initialize(m_Bridge, m_Map, m_Camera, m_Fog, m_DayNight, m_Nodes, m_Depots);
        IsInitialized = true;
    }

    void OnDestroy()
    {
        IsReady = false;
        m_Nodes?.ClearCache();
        ProceduralMapRuntime.EndComposition();
    }
}
