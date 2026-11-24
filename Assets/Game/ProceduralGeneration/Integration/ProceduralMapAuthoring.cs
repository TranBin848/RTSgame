using System;
using RTS.ProceduralGeneration;
using UnityEngine;
using UnityEngine.Tilemaps;

[DefaultExecutionOrder(-300)]
public sealed partial class ProceduralMapAuthoring : MonoBehaviour
{
    internal static int? BenchmarkSeedOverride { get; set; }
    [Header("Generation")]
    [SerializeField] ProceduralMapSettings m_Settings;
    [SerializeField] TilemapManager m_TilemapManager;
    [SerializeField] Transform m_MapCenter;
    [SerializeField] ResourceSpawner[] m_LegacySpawners = Array.Empty<ResourceSpawner>();
    public System.Collections.Generic.IReadOnlyList<ResourceSpawner> LegacySpawners => m_LegacySpawners;

    [Header("Tiles")]
    [SerializeField] TileBase m_GrassTile;
    [SerializeField] TileBase m_WaterTile;
    [SerializeField] Tilemap m_FoamTilemap;
    [SerializeField] TileBase m_FoamTile;

    [Header("Resources")]
    [SerializeField] GameObject m_TreePrefab;
    [SerializeField] GameObject[] m_GoldPrefabs = Array.Empty<GameObject>();
    [SerializeField] GameObject m_SheepPrefab;
    [SerializeField] Transform m_GeneratedResourceRoot;

    public MapData GeneratedMap { get; private set; }
    readonly System.Collections.Generic.Dictionary<int, GameObject> m_SavePlacementViews = new();
    public GameObject SavePlacementView(int index) => m_SavePlacementViews.TryGetValue(index, out var view) ? view : null;
    public MapGenerationSettingsData SaveSettings
    {
        get { var data = m_LoadedMap != null ? JsonUtility.FromJson<MapGenerationSettingsData>(m_LoadedMap.SettingsJson) : m_Settings.ToData(); if (GeneratedMap != null) data.Seed = GeneratedMap.Seed; return data; }
    }
    public bool IsReady => GeneratedMap != null && GeneratedMap.Validation.IsValid;
    public int GatewayCount => IsReady && GeneratedMap.Layout != null ? GeneratedMap.Layout.Gateways.Length : 0;
    Vector3Int m_Origin;
    RTS.SaveLoad.MapSaveData m_LoadedMap;
    public Vector3Int OriginCell => m_Origin;
    public Transform GeneratedResourceRoot => m_GeneratedResourceRoot;
    public double BuildStartedRealtime { get; private set; }
    public double CoreMilliseconds { get; private set; }
    public double TerrainMilliseconds { get; private set; }
    public double ResourceMilliseconds { get; private set; }
    public double NavigationMilliseconds { get; private set; }

    public bool TryGetGatewaySpawnPosition(int index, out Vector3 position)
    {
        position = default;
        if (index < 0 || index >= GatewayCount) return false;
        var map = GeneratedMap;
        var gateway = map.Layout.Gateways[index];
        // Keep the body inside the map; the bridge still checks its actual radius against navigation.
        int x = gateway.X == 0 ? 2 : gateway.X == map.Width - 1 ? map.Width - 3 : gateway.X;
        int y = gateway.Y == 0 ? 2 : gateway.Y == map.Height - 1 ? map.Height - 3 : gateway.Y;
        position = m_TilemapManager.WalkableTilemap.GetCellCenterWorld(m_Origin + new Vector3Int(x, y, 0));
        return true;
    }

    public bool TryGetGatewayApproachPosition(int index, Vector3 center, float radius, out Vector3 position)
    {
        position = default;
        if (index < 0 || index >= GatewayCount || radius < 0 || float.IsNaN(radius) || float.IsInfinity(radius)) return false;
        var map = GeneratedMap;
        var gate = map.Layout.Gateways[index];
        var region = map.Layout.Regions[gate.Region];
        float minimum = radius * radius, best = float.PositiveInfinity;
        Vector3 candidate = default;
        // Follow the same protected L centerlines as StrategicLayout, keeping the legacy spawn radius.
        Visit(map.StartX, map.StartY, region.X, region.Y);
        Visit(region.X, region.Y, gate.X, gate.Y);
        position = candidate;
        return !float.IsPositiveInfinity(best);

        void Visit(int x, int y, int targetX, int targetY)
        {
            while (true)
            {
                if (map.Layout.Routes[map.Index(x, y)] != 0)
                {
                    Vector3 point = m_TilemapManager.WalkableTilemap.GetCellCenterWorld(m_Origin + new Vector3Int(x, y, 0));
                    float distance = ((Vector2)(point - center)).sqrMagnitude;
                    if (distance >= minimum && distance < best) { best = distance; candidate = point; }
                }
                if (x == targetX && y == targetY) break;
                if (x != targetX) x += x < targetX ? 1 : -1;
                else y += y < targetY ? 1 : -1;
            }
        }
    }

    void Awake()
    {
        BuildStartedRealtime = Time.realtimeSinceStartupAsDouble;
        ProceduralMapRuntime.Begin();
        if (MatchLoadBootstrap.IsLoading)
        {
            try
            {
                ApplySavedMap(MatchLoadBootstrap.Pending.Map);
                foreach (var root in gameObject.scene.GetRootGameObjects())
                    foreach (var unit in root.GetComponentsInChildren<Unit>(true))
                    { unit.gameObject.SetActive(false); Destroy(unit.gameObject); }
                ProceduralMapRuntime.Complete();
            }
            catch (Exception exception) { ProceduralMapRuntime.FailComposition(exception.Message); MatchLoadBootstrap.Fail(exception.Message); enabled = false; }
            return;
        }
        if (!TryGenerateAndApply(out string error))
        {
            ProceduralMapRuntime.Fail(error);
            RestoreLegacyResourceSpawner();
            Debug.LogError($"[ProceduralMap] {error}", this);
            enabled = false;
            return;
        }

        ProceduralMapRuntime.Complete();
        MapValidationReport report = GeneratedMap.Validation;
        Debug.Log($"[ProceduralMap] Ready. Seed={GeneratedMap.Seed}, attempt={GeneratedMap.Attempt}, "
            + $"grass={report.GrassCells}, water={report.WaterCells}, trees={report.TreeCount}, "
            + $"gold={report.GoldCount}, sheep={report.SheepCount}, repairs={report.Repairs}.", this);
    }

    public bool TryGenerateAndApply(out string error)
    {
        if (GeneratedMap != null)
        {
            error = "Map has already been applied. Reload the scene to generate another map.";
            return false;
        }
        error = ValidateAuthoring();
        if (error != null) return false;

        var generator = new ProceduralMapGenerator();
        var timer = System.Diagnostics.Stopwatch.StartNew();
        var data = m_Settings.ToData();
        data.Seed = BenchmarkSeedOverride ?? data.Seed;
        if (!generator.TryGenerate(data, out MapData map, out error)) return false;
        CoreMilliseconds = timer.Elapsed.TotalMilliseconds;

        DisableLegacyResourceSpawner();
        GeneratedMap = map;
        timer.Restart();
        ApplyTerrain(map);
        TerrainMilliseconds = timer.Elapsed.TotalMilliseconds;
        timer.Restart();
        SpawnResources(map);
        ResourceMilliseconds = timer.Elapsed.TotalMilliseconds;
        timer.Restart();
        Physics2D.SyncTransforms();
        m_TilemapManager.ReinitializePathfinding();
        NavigationMilliseconds = timer.Elapsed.TotalMilliseconds;
        return true;
    }

    string ValidateAuthoring()
    {
        if (m_LegacySpawners == null) return "Legacy spawner references are missing.";
        var unique = new System.Collections.Generic.HashSet<ResourceSpawner>();
        foreach (var spawner in m_LegacySpawners)
            if (spawner == null || spawner.gameObject.scene != gameObject.scene || !unique.Add(spawner))
                return "Legacy spawner references must be unique, non-null and in the same scene.";
        if (m_Settings == null) return "Map settings asset is missing.";
        if (m_TilemapManager == null) return "TilemapManager reference is missing.";
        if (m_TilemapManager.WalkableTilemap == null || m_TilemapManager.WaterTilemap == null)
            return "Walkable or water Tilemap is missing.";
        if (m_GrassTile == null || m_WaterTile == null) return "Grass or water Tile asset is missing.";
        if (m_FoamTilemap == null || m_FoamTile == null) return "Foam Tilemap or Tile asset is missing.";
        if (m_TreePrefab == null || m_SheepPrefab == null) return "Tree or sheep prefab is missing.";
        if (m_GoldPrefabs == null || m_GoldPrefabs.Length != 6) return "Exactly six gold prefab variants are required.";
        for (int i = 0; i < m_GoldPrefabs.Length; i++)
            if (m_GoldPrefabs[i] == null) return $"Gold prefab variant {i} is missing.";
        return null;
    }

    void ApplyTerrain(MapData map)
    {
        Tilemap walkable = m_TilemapManager.WalkableTilemap;
        Tilemap water = m_TilemapManager.WaterTilemap;
        Tilemap foam = m_FoamTilemap;
        Vector3 centerWorld = m_MapCenter != null ? m_MapCenter.position : transform.position;
        Vector3Int centerCell = walkable.WorldToCell(centerWorld);
        Vector3Int origin = m_LoadedMap != null ? new Vector3Int(m_LoadedMap.OriginCellX, m_LoadedMap.OriginCellY, 0)
            : new Vector3Int(centerCell.x - map.StartX, centerCell.y - map.StartY, 0);
        m_Origin = origin;
        var bounds = new BoundsInt(origin.x, origin.y, 0, map.Width, map.Height, 1);
        var grassTiles = new TileBase[map.Terrain.Length];
        var waterTiles = new TileBase[map.Terrain.Length];
        var foamTiles = new TileBase[map.Terrain.Length];
        for (int i = 0; i < map.Terrain.Length; i++)
        {
            if (map.Terrain[i] == (byte)TerrainCell.Grass)
            {
                grassTiles[i] = m_GrassTile;
                int x = i % map.Width;
                int y = i / map.Width;
                if (TouchesWater(map, x, y)) foamTiles[i] = m_FoamTile;
            }
            else waterTiles[i] = m_WaterTile;
        }

        walkable.ClearAllTiles();
        water.ClearAllTiles();
        foam.ClearAllTiles();
        if (m_TilemapManager.UnreachableTilemaps != null)
            foreach (Tilemap unreachable in m_TilemapManager.UnreachableTilemaps)
                if (unreachable != null) unreachable.ClearAllTiles();
        walkable.SetTilesBlock(bounds, grassTiles);
        water.SetTilesBlock(bounds, waterTiles);
        foam.SetTilesBlock(bounds, foamTiles);
        walkable.CompressBounds();
        water.CompressBounds();
        foam.CompressBounds();
    }

    static bool TouchesWater(MapData map, int x, int y)
    {
        for (int offsetY = -1; offsetY <= 1; offsetY++)
        for (int offsetX = -1; offsetX <= 1; offsetX++)
        {
            if (offsetX == 0 && offsetY == 0) continue;
            int neighborX = x + offsetX;
            int neighborY = y + offsetY;
            if (map.InBounds(neighborX, neighborY)
                && map.Terrain[map.Index(neighborX, neighborY)] == (byte)TerrainCell.Water)
                return true;
        }
        return false;
    }

    void SpawnResources(MapData map)
    {
        m_SavePlacementViews.Clear();
        if (m_GeneratedResourceRoot == null)
        {
            var root = new GameObject("Procedural Resources");
            root.transform.SetParent(transform, false);
            m_GeneratedResourceRoot = root.transform;
        }

        Tilemap walkable = m_TilemapManager.WalkableTilemap;

        for (int placementIndex = 0; placementIndex < map.Resources.Count; placementIndex++)
        {
            ResourcePlacement placement = map.Resources[placementIndex];
            GameObject prefab = placement.Kind switch
            {
                ProceduralResourceKind.Tree => m_TreePrefab,
                ProceduralResourceKind.Gold => m_GoldPrefabs[Mathf.Clamp(placement.VariantId, 0, m_GoldPrefabs.Length - 1)],
                ProceduralResourceKind.Sheep => m_SheepPrefab,
                _ => null
            };
            if (prefab == null) continue;
            Vector3Int cell = m_Origin + new Vector3Int(placement.X, placement.Y, 0);
            m_SavePlacementViews[placementIndex] = Instantiate(prefab, walkable.GetCellCenterWorld(cell), Quaternion.identity, m_GeneratedResourceRoot);
        }
    }

    void DisableLegacyResourceSpawner()
    {
        foreach (ResourceSpawner spawner in m_LegacySpawners)
            spawner.DisableForProceduralGeneration();
    }

    void RestoreLegacyResourceSpawner()
    {
        if (m_LegacySpawners == null) return;
        foreach (ResourceSpawner spawner in m_LegacySpawners)
        {
            if (spawner != null && spawner.gameObject.scene == gameObject.scene) spawner.RestoreLegacyGeneration();
        }
    }
}
