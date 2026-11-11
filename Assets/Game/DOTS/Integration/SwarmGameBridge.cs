using System.Collections.Generic;
using RTS.DOTS;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;

// This assembly can see legacy Unit classes; the RTS.DOTS assembly remains independent of them.
[DefaultExecutionOrder(-200)]
public sealed partial class SwarmGameBridge : MonoBehaviour
{
    public static SwarmGameBridge Instance { get; private set; }
    public SwarmSimulationSystem Simulation { get; private set; }
    public bool IsInitialized => Simulation != null && Gameplay != null;
    public bool IsSceneBound { get; private set; }
    readonly Dictionary<Unit, Entity> m_Proxies = new();
    readonly Dictionary<int, Unit> m_UnitsById = new();
    readonly List<Entity> m_Obstacles = new();
    TilemapManager m_Map;
    GameManager m_Game;
    FogOfWarManager m_Fog;
    DayNightCycleManager m_DayNight;
    SwarmInstancedRenderer m_Renderer;
    Texture2D m_FogTexture;
    Color32[] m_FogPixels;
    float m_NextMapRefresh;
    int m_MapVersion = -1;
    BoundsInt m_MapBounds;
    byte[] m_MapData;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Instance = null; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        var manager = FindFirstObjectByType<GameManager>();
        if (manager != null && manager.GetComponent<GameSceneCompositionRoot>() != null) return;
        if (manager != null && manager.GetComponent<SwarmGameBridge>() == null) manager.gameObject.AddComponent<SwarmGameBridge>();
    }
    void Awake()
    {
        if (GetComponent<GameSceneCompositionRoot>() != null) return;
        if (World.DefaultGameObjectInjectionWorld == null) return;
        // Legacy composition until scene roots take over in D1.
        Initialize(World.DefaultGameObjectInjectionWorld, GetComponent<GameManager>(),
            FindFirstObjectByType<TilemapManager>(), FindFirstObjectByType<FogOfWarManager>(),
            FindFirstObjectByType<DayNightCycleManager>());
    }
    public void Initialize(World world, GameManager game, TilemapManager map,
        FogOfWarManager fog, DayNightCycleManager dayNight)
    {
        if (world == null || !world.IsCreated) throw new System.ArgumentException("A live ECS World is required.", nameof(world));
        if (game == null) throw new System.ArgumentNullException(nameof(game));
        if (map == null) throw new System.ArgumentNullException(nameof(map));
        if (IsInitialized)
        {
            if (Simulation.World != world || m_Game != game || m_Map != map || m_Fog != fog || m_DayNight != dayNight)
                throw new System.InvalidOperationException("Bridge is already initialized with different dependencies.");
            return;
        }
        if (Instance != null && Instance != this)
            throw new System.InvalidOperationException("Another gameplay bridge already owns the scene simulation.");
        Simulation = world.GetOrCreateSystemManaged<SwarmSimulationSystem>();
        Gameplay = world.GetOrCreateSystemManaged<GameplaySimulationSystem>();
        m_Game = game;
        m_Map = map;
        m_Fog = fog;
        m_DayNight = dayNight;
        m_Renderer = GetComponent<SwarmInstancedRenderer>();
        if (m_Renderer == null) m_Renderer = gameObject.AddComponent<SwarmInstancedRenderer>();
        m_Renderer.Simulation = Simulation;
        Instance = this;
    }
    void Start()
    {
        if (Simulation == null || m_Map == null) { enabled = false; return; }
        if (GetComponent<GameSceneCompositionRoot>() != null) return;
        BindScene();
    }
    public void BindScene()
    {
        if (IsSceneBound) return;
        if (!IsInitialized || m_Map.WalkableTilemap == null)
            throw new System.InvalidOperationException("Bridge dependencies and map must be initialized before binding.");
        RefreshNavigation();
        if (!Simulation.IsConfigured || !Gameplay.IsConfigured)
            throw new System.InvalidOperationException("Navigation grid was not configured.");
        foreach (var resource in FindObjectsByType<ResourceNodeBase>(FindObjectsSortMode.None)) RegisterResource(resource);
        foreach (var unit in FindObjectsByType<Unit>(FindObjectsSortMode.None))
        {
            Register(unit);
        }
        UpdateGoal();
        if (m_Fog != null) { m_Fog.OnFogUpdated += RefreshFog; RefreshFog(); }
        IsSceneBound = true;
    }
    public void Register(Unit unit)
    {
        if (unit == null || m_Proxies.ContainsKey(unit)) return;
        if (TryConvertEnemy(unit)) return;
        RegisterGameplayUnit(unit);
    }
    public void Unregister(Unit unit)
    {
        if (Simulation != null && Simulation.World != null && Simulation.World.IsCreated && m_Proxies.TryGetValue(unit, out var entity)) Gameplay.Remove(entity);
        m_Proxies.Remove(unit); m_UnitsById.Remove(unit.GetInstanceID());
        m_BodyOffsets.Remove(unit); m_ResourceEntities.Remove(unit);
        unit.UnbindDots();
    }
    void Update()
    {
        if (ProceduralMapRuntime.IsCompositionBlocked) return;
        if (Simulation == null || Simulation.World == null || !Simulation.World.IsCreated) return;
        SynchronizeGameplay();
        if (Time.unscaledTime >= m_NextMapRefresh && m_MapVersion != m_Map.NavigationVersion)
        { RefreshNavigation(); m_NextMapRefresh = Time.unscaledTime + 0.25f; }
        UpdateGoal();
        if (m_DayNight != null) m_Renderer.EnvironmentColor = m_DayNight.EnvironmentTint;
    }
    void LateUpdate() { if (!ProceduralMapRuntime.IsCompositionBlocked) SynchronizeGameplay(); }
    void UpdateGoal()
    {
        if (m_Game != null && m_Game.TryFindClosestTownHall(Vector3.zero, out var hall))
        {
            Register(hall);
            Vector2 position = hall.Collider != null ? (Vector2)hall.Collider.bounds.center : (Vector2)hall.transform.position;
            Simulation.SetGoal(position, m_Proxies[hall]);
        }
    }
    void RefreshNavigation()
    {
        m_MapBounds = m_Map.WalkableTilemap.cellBounds;
        int width = m_MapBounds.size.x, height = m_MapBounds.size.y;
        if (width <= 0 || height <= 0) return;
        m_MapData = new byte[width * height];
        // Capture Unity objects on the main thread only. Jobs use this immutable byte grid.
        foreach (var cell in m_MapBounds.allPositionsWithin)
            m_MapData[(cell.y - m_MapBounds.yMin) * width + cell.x - m_MapBounds.xMin] = (byte)(m_Map.CamWalkAtTile(cell) ? 1 : 0);
        Vector3 origin = m_Map.WalkableTilemap.CellToWorld(m_MapBounds.min);
        float cellSize = m_Map.WalkableTilemap.cellSize.x * m_Map.WalkableTilemap.transform.lossyScale.x;
        // Native building footprints also block navigation when a source prefab has no legacy tag/layer.
        foreach (var unit in m_Proxies.Keys)
        {
            if (unit == null || !unit.IsBuilding || unit.CurrentState == UnitState.Dead || unit.Collider == null || !unit.Collider.enabled) continue;
            Bounds footprint = unit.Collider.bounds;
            int2 min = math.clamp((int2)math.floor(((float2)(Vector2)footprint.min - (float2)(Vector2)origin) / cellSize), 0, new int2(width - 1, height - 1));
            int2 max = math.clamp((int2)math.ceil(((float2)(Vector2)footprint.max - (float2)(Vector2)origin) / cellSize) - 1, 0, new int2(width - 1, height - 1));
            for (int y = min.y; y <= max.y; y++)
            for (int x = min.x; x <= max.x; x++) m_MapData[y * width + x] = 0;
        }
        Simulation.ConfigureGrid(new float2(origin.x, origin.y), new int2(width, height), cellSize, m_MapData);
        Gameplay.ConfigureGrid(new float2(origin.x, origin.y), new int2(width, height), cellSize, m_MapData);
        // Terrain collision is separate from combat boxes. A building must receive the hit itself.
        foreach (var entity in m_Obstacles) Simulation.Remove(entity);
        m_Obstacles.Clear();
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            var tile = new Vector3Int(m_MapBounds.xMin + x, m_MapBounds.yMin + y);
            if (m_Map.WalkableTilemap.HasTile(tile) && !m_Map.isInUnreachableTilemap(tile)) continue;
            int start = x;
            while (x + 1 < width)
            {
                var nextTile = new Vector3Int(m_MapBounds.xMin + x + 1, m_MapBounds.yMin + y);
                if (m_Map.WalkableTilemap.HasTile(nextTile) && !m_Map.isInUnreachableTilemap(nextTile)) break;
                x++;
            }
            float2 half = new float2((x - start + 1) * cellSize * 0.5f, cellSize * 0.5f);
            float2 center = new float2(origin.x + start * cellSize, origin.y + y * cellSize) + half;
            m_Obstacles.Add(Simulation.CreateProxy(0, center, half, float.MaxValue, -1));
        }
        foreach (var resource in FindObjectsByType<ResourceNodeBase>(FindObjectsSortMode.None))
        {
            var col = resource.GetComponent<CapsuleCollider2D>();
            if (col == null || !col.enabled) continue;
            Bounds bounds = col.bounds;
            m_Obstacles.Add(Simulation.CreateProxy(0, new float2(bounds.center.x, bounds.center.y),
                new float2(bounds.extents.x, bounds.extents.y), float.MaxValue, -1));
        }
        m_MapVersion = m_Map.NavigationVersion;
    }
    public bool SpawnEnemy(GameObject prefab, Vector3 position)
    {
        if (Simulation == null || !Simulation.IsConfigured || prefab == null || !prefab.TryGetComponent<Unit>(out var unit)) return false;
        var settings = unit.GetDotsSettings();
        settings.VisualId = unit is EnemyRangerUnit ? 1 : 0;
        settings.ProjectileVisualId = 3;
        if (!Simulation.TryResolveSpawnPosition(new float2(position.x, position.y), settings.Radius, out var resolved)) return false;
        return Simulation.SpawnEnemy(resolved, settings) != Entity.Null;
    }
    public void Retreat(Vector3 center, float radius) => Simulation?.Retreat(new float2(center.x, center.y), radius);
    public Entity ProxyFor(Unit unit) { if (unit == null) return Entity.Null; Register(unit); return m_Proxies.TryGetValue(unit, out var e) ? e : Entity.Null; }
    public bool TryFindEnemy(Vector3 origin, float radius, out Entity entity)
    {
        entity = Entity.Null;
        return Simulation != null && Simulation.TryFindEnemy(new float2(origin.x, origin.y), radius, out entity);
    }
    public bool TryGetEnemy(Entity entity, out Vector3 position, out float radius)
    {
        position = default; radius = 0;
        if (Simulation == null || !Simulation.TryGetBody(entity, out var body) || body.Faction != 1) return false;
        position = new Vector3(body.Position.x, body.Position.y); radius = body.Radius; return true;
    }
    public static ProjectileParameters ProjectileSettings(Projectile prefab)
    {
        return new ProjectileParameters { Speed = prefab.Speed, ArcHeight = prefab.ArcHeight, Arc = (byte)(prefab.UsesArc ? 1 : 0),
            Spinning = (byte)(prefab.IsSpinning ? 1 : 0), VisualScale = Mathf.Abs(prefab.transform.lossyScale.x),
            MinDuration = prefab.MinTravelDuration, MaxDuration = prefab.MaxTravelDuration, DurationFactor = prefab.DistanceDurationFactor };
    }
    public void Fire(Unit owner, Entity target, int damage, Vector3 position, Projectile prefab = null)
    {
        if (Simulation == null || !Simulation.TryGetBody(target, out var body)) return;
        var settings = prefab != null ? ProjectileSettings(prefab) : EnemySpawnSettings.Default.Projectile;
        Simulation.SpawnProjectile(new ProjectileRequest { Owner = ProxyFor(owner), Target = target,
            Position = new float2(position.x, position.y), Aim = body.Position, Damage = damage,
            Faction = owner.IsPlayer ? 0 : 1, Speed = settings.Speed, VisualId = 2, Arc = settings.Arc, ArcHeight = settings.ArcHeight,
            VisualScale = settings.VisualScale, Spinning = settings.Spinning, MinDuration = settings.MinDuration,
            MaxDuration = settings.MaxDuration, DurationFactor = settings.DurationFactor });
    }
    public bool ConvertLegacyProjectile(Unit owner, Unit target, int damage, Vector3 position, Projectile prefab)
    {
        if (Simulation == null || target == null || owner == null) return false;
        Entity targetEntity = ProxyFor(target);
        if (targetEntity == Entity.Null) return false;
        Simulation.SpawnProjectile(new ProjectileRequest { Owner = ProxyFor(owner), Target = targetEntity,
            Position = new float2(position.x, position.y), Aim = new float2(target.transform.position.x, target.transform.position.y),
            Damage = damage, Faction = owner.IsPlayer ? 0 : 1, Speed = prefab.Speed,
            VisualId = owner.IsPlayer ? 2 : 3, Arc = (byte)(prefab.UsesArc ? 1 : 0), ArcHeight = prefab.ArcHeight,
            VisualScale = Mathf.Abs(prefab.transform.lossyScale.x), Spinning = (byte)(prefab.IsSpinning ? 1 : 0),
            MinDuration = prefab.MinTravelDuration, MaxDuration = prefab.MaxTravelDuration, DurationFactor = prefab.DistanceDurationFactor });
        return true;
    }
    void RefreshFog()
    {
        if (m_Fog == null || !m_Fog.IsInitialized || m_Renderer == null) return;
        BoundsInt bounds = m_Fog.Bounds;
        int width = bounds.size.x, height = bounds.size.y;
        if (m_FogTexture == null || m_FogTexture.width != width || m_FogTexture.height != height)
        {
            if (m_FogTexture != null) Destroy(m_FogTexture);
            m_FogTexture = new Texture2D(width, height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            m_FogPixels = new Color32[width * height];
        }
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            var cell = new Vector3Int(bounds.xMin + x, bounds.yMin + y);
            bool visible = m_Fog.IsDisabled || (m_Fog.TryGetCellState(cell, out var state, out _) && state == FogCellState.Visible);
            m_FogPixels[y * width + x] = visible ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 255);
        }
        m_FogTexture.SetPixels32(m_FogPixels); m_FogTexture.Apply(false);
        m_Renderer.FogTexture = m_FogTexture; m_Renderer.FogBounds = new Vector4(bounds.xMin, bounds.yMin, width, height); m_Renderer.FogEnabled = true;
    }
    void OnDestroy()
    {
        if (m_Fog != null) m_Fog.OnFogUpdated -= RefreshFog;
        if (m_FogTexture != null) Destroy(m_FogTexture);
        if (Simulation != null && Simulation.World != null && Simulation.World.IsCreated)
        {
            Gameplay.ResetGameplay();
            Simulation.ResetSimulation();
            Simulation.Paused = false;
            foreach (var entity in m_Proxies.Values) Simulation.Remove(entity);
            foreach (var entity in m_Obstacles) Simulation.Remove(entity);
        }
        if (Instance == this) Instance = null;
    }
}
