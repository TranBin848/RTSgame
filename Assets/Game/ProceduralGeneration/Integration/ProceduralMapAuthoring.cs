using System;
using RTS.ProceduralGeneration;
using UnityEngine;
using UnityEngine.Tilemaps;

[DefaultExecutionOrder(-300)]
public sealed class ProceduralMapAuthoring : MonoBehaviour
{
    [Header("Generation")]
    [SerializeField] ProceduralMapSettings m_Settings;
    [SerializeField] TilemapManager m_TilemapManager;
    [SerializeField] Transform m_MapCenter;

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
    public bool IsReady => GeneratedMap != null && GeneratedMap.Validation.IsValid;

    void Awake()
    {
        ProceduralMapRuntime.Begin();
        DisableLegacyResourceSpawner();
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
        error = ValidateAuthoring();
        if (error != null) return false;

        var generator = new ProceduralMapGenerator();
        if (!generator.TryGenerate(m_Settings.ToData(), out MapData map, out error)) return false;

        GeneratedMap = map;
        ApplyTerrain(map);
        SpawnResources(map);
        Physics2D.SyncTransforms();
        m_TilemapManager.ReinitializePathfinding();
        return true;
    }

    string ValidateAuthoring()
    {
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
        Vector3Int origin = new Vector3Int(centerCell.x - map.StartX, centerCell.y - map.StartY, 0);
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
        if (m_GeneratedResourceRoot == null)
        {
            var root = new GameObject("Procedural Resources");
            root.transform.SetParent(transform, false);
            m_GeneratedResourceRoot = root.transform;
        }

        Tilemap walkable = m_TilemapManager.WalkableTilemap;
        Vector3 centerWorld = m_MapCenter != null ? m_MapCenter.position : transform.position;
        Vector3Int centerCell = walkable.WorldToCell(centerWorld);
        Vector3Int origin = new Vector3Int(centerCell.x - map.StartX, centerCell.y - map.StartY, 0);

        foreach (ResourcePlacement placement in map.Resources)
        {
            GameObject prefab = placement.Kind switch
            {
                ProceduralResourceKind.Tree => m_TreePrefab,
                ProceduralResourceKind.Gold => m_GoldPrefabs[Mathf.Clamp(placement.VariantId, 0, m_GoldPrefabs.Length - 1)],
                ProceduralResourceKind.Sheep => m_SheepPrefab,
                _ => null
            };
            if (prefab == null) continue;
            Vector3Int cell = new Vector3Int(origin.x + placement.X, origin.y + placement.Y, 0);
            Instantiate(prefab, walkable.GetCellCenterWorld(cell), Quaternion.identity, m_GeneratedResourceRoot);
        }
    }

    void DisableLegacyResourceSpawner()
    {
        foreach (ResourceSpawner spawner in FindObjectsByType<ResourceSpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            spawner.DisableForProceduralGeneration();
    }

    void RestoreLegacyResourceSpawner()
    {
        foreach (ResourceSpawner spawner in FindObjectsByType<ResourceSpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            spawner.enabled = true;
    }
}
