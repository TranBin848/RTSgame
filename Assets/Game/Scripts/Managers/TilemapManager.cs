using UnityEngine.Tilemaps;
using System.Collections.Generic;
using UnityEngine;

public class TilemapManager : SingletonManager<TilemapManager>
{
    [Header("Tilemaps")]
    [SerializeField] private Tilemap m_WalkableTilemap;
    [SerializeField] private Tilemap m_OverlayTilemap;
    [SerializeField] private Tilemap m_WaterTilemap;
    [SerializeField] private Tilemap[] m_UnreachableTilemaps;

    [Header("Testing")]

    public Tilemap PathfindingTilemap => m_WalkableTilemap;
    public Tilemap WalkableTilemap => m_WalkableTilemap;
    public Tilemap WaterTilemap => m_WaterTilemap;
    public Tilemap[] UnreachableTilemaps => m_UnreachableTilemaps;
    private Pathfinding m_Pathfinding;
    private bool m_IsShuttingDown;

    void Start()
    {
        m_Pathfinding = new Pathfinding(this);
    }
    void OnApplicationQuit()
    {
        m_IsShuttingDown = true;
    }

    void OnDestroy()
    {
        m_IsShuttingDown = true;
    }

    public bool CanServePathfindingRequests()
    {
        return !m_IsShuttingDown
            && m_Pathfinding != null
            && m_WalkableTilemap != null
            && m_OverlayTilemap != null;
    }

    public List<Vector3> FindPath(Vector3 startWorldPosition, Vector3 endWorldPosition)
    {
        if (!CanServePathfindingRequests())
        {
            return new List<Vector3>();
        }

        return m_Pathfinding.FindPath(startWorldPosition, endWorldPosition);
    }
    public Node FindNode(Vector3 worldPosition)
    {
        if (!CanServePathfindingRequests())
        {
            return null;
        }

        return m_Pathfinding.FindNode(worldPosition);
    }
    public void UpdateNodesInArea(Vector3Int startPosition, int width, int height)
    {
        if (!CanServePathfindingRequests())
        {
            return;
        }

        m_Pathfinding.UpdateNodesInArea(startPosition, width, height);
    }
    public bool CamWalkAtTile(Vector3Int tilePosition)
    {
        if (m_IsShuttingDown || m_WalkableTilemap == null)
        {
            return false;
        }

        return m_WalkableTilemap.HasTile(tilePosition) &&
        !isInUnreachableTilemap(tilePosition) &&
        !isBlockByBuilding(tilePosition);
    }
    public bool CanPlaceTiles(Vector3Int tilePosition)
    {
        if (m_IsShuttingDown || m_WalkableTilemap == null)
        {
            return false;
        }

        return m_WalkableTilemap.HasTile(tilePosition) &&
        !isInUnreachableTilemap(tilePosition) &&
        !isBlockByGameObject(tilePosition);
    }
    public bool isInUnreachableTilemap(Vector3Int tilePosition)
    {
        if (m_IsShuttingDown)
        {
            return false;
        }

        foreach (var tilemap in m_UnreachableTilemaps)
        {
            if (tilemap != null && tilemap.HasTile(tilePosition))
            {
                return true;
            }
        }
        return false;
    }
    public bool isBlockByBuilding(Vector3Int tilePosition)
    {
        if (m_IsShuttingDown || m_WalkableTilemap == null)
        {
            return false;
        }

        Vector3 tileSize = m_WalkableTilemap.cellSize;
        Vector3 worldPosition = m_WalkableTilemap.CellToWorld(tilePosition) + tileSize / 2;
        int buildingLayerMask = LayerMask.GetMask("Unit", "GoldStone", "Tree");
        Collider2D[] collider = Physics2D.OverlapBoxAll(worldPosition, tileSize * 0.9f, 0f, buildingLayerMask);

        foreach (var col in collider)
        {
            if (col.CompareTag("Building") || col.CompareTag("Resource")) return true;
        }

        return false;

    }
    public bool isBlockByGameObject(Vector3Int tilePosition)
    {
        if (m_IsShuttingDown || m_WalkableTilemap == null)
        {
            return false;
        }

        Vector3 tileSize = m_WalkableTilemap.cellSize;
        int unitLayerMask = LayerMask.GetMask("Unit", "GoldStone", "Tree");
        Collider2D[] colliders = Physics2D.OverlapBoxAll(
            new Vector2(tilePosition.x + tileSize.x / 2, tilePosition.y + tileSize.y / 2),
            tileSize * 0.9f,
            0f, unitLayerMask
            );

        return colliders.Length > 0;
    }
    public void SetTileOverlay(Vector3Int tilePosition, Tile tile)
    {
        if (m_IsShuttingDown || m_OverlayTilemap == null)
        {
            return;
        }

        m_OverlayTilemap.SetTile(tilePosition, tile);
    }

    public bool TryResolveWalkableDestination(Vector3 requestedWorldPosition, out Vector3 resolvedWorldPosition, int maxSearchRadius = 24)
    {
        resolvedWorldPosition = requestedWorldPosition;

        if (m_IsShuttingDown || m_WalkableTilemap == null)
        {
            return false;
        }

        Vector3Int requestedCell = m_WalkableTilemap.WorldToCell(requestedWorldPosition);
        if (CamWalkAtTile(requestedCell))
        {
            resolvedWorldPosition = GetCellCenterWorld(requestedCell);
            return true;
        }

        BoundsInt bounds = GetWorldMapBounds();
        if (bounds.size.x <= 0 || bounds.size.y <= 0)
        {
            return false;
        }

        float closestDistanceSqr = float.MaxValue;
        bool found = false;
        Vector3Int closestCell = requestedCell;

        for (int radius = 1; radius <= maxSearchRadius; radius++)
        {
            bool foundInCurrentRing = false;

            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    if (Mathf.Abs(x) != radius && Mathf.Abs(y) != radius)
                    {
                        continue;
                    }

                    Vector3Int candidateCell = new Vector3Int(requestedCell.x + x, requestedCell.y + y, 0);
                    if (!bounds.Contains(candidateCell) || !CamWalkAtTile(candidateCell))
                    {
                        continue;
                    }

                    Vector3 candidateWorldPosition = GetCellCenterWorld(candidateCell);
                    float distanceSqr = (candidateWorldPosition - requestedWorldPosition).sqrMagnitude;
                    if (distanceSqr < closestDistanceSqr)
                    {
                        closestDistanceSqr = distanceSqr;
                        closestCell = candidateCell;
                        found = true;
                        foundInCurrentRing = true;
                    }
                }
            }

            if (foundInCurrentRing)
            {
                resolvedWorldPosition = GetCellCenterWorld(closestCell);
                return true;
            }
        }

        if (found)
        {
            resolvedWorldPosition = GetCellCenterWorld(closestCell);
            return true;
        }

        return false;
    }

    public BoundsInt GetWorldMapBounds()
    {
        BoundsInt? combinedBounds = null;

        AddTilemapBounds(m_WalkableTilemap, ref combinedBounds);
        AddTilemapBounds(m_WaterTilemap, ref combinedBounds);

        foreach (var tilemap in m_UnreachableTilemaps)
        {
            AddTilemapBounds(tilemap, ref combinedBounds);
        }

        return combinedBounds ?? new BoundsInt(0, 0, 0, 0, 0, 0);
    }

    public bool HasAnyMapTile(Vector3Int tilePosition)
    {
        if (m_WalkableTilemap != null && m_WalkableTilemap.HasTile(tilePosition))
        {
            return true;
        }

        if (m_WaterTilemap != null && m_WaterTilemap.HasTile(tilePosition))
        {
            return true;
        }

        foreach (var tilemap in m_UnreachableTilemaps)
        {
            if (tilemap != null && tilemap.HasTile(tilePosition))
            {
                return true;
            }
        }

        return false;
    }

    public bool HasWaterTile(Vector3Int tilePosition)
    {
        return m_WaterTilemap != null && m_WaterTilemap.HasTile(tilePosition);
    }

    public bool HasUnreachableTile(Vector3Int tilePosition)
    {
        return isInUnreachableTilemap(tilePosition);
    }

    void AddTilemapBounds(Tilemap tilemap, ref BoundsInt? combinedBounds)
    {
        if (tilemap == null)
        {
            return;
        }

        tilemap.CompressBounds();
        BoundsInt bounds = tilemap.cellBounds;
        if (bounds.size.x <= 0 || bounds.size.y <= 0)
        {
            return;
        }

        if (combinedBounds == null)
        {
            combinedBounds = bounds;
            return;
        }

        BoundsInt current = combinedBounds.Value;
        int xMin = Mathf.Min(current.xMin, bounds.xMin);
        int yMin = Mathf.Min(current.yMin, bounds.yMin);
        int xMax = Mathf.Max(current.xMax, bounds.xMax);
        int yMax = Mathf.Max(current.yMax, bounds.yMax);
        combinedBounds = new BoundsInt(xMin, yMin, 0, xMax - xMin, yMax - yMin, 1);
    }

    Vector3 GetCellCenterWorld(Vector3Int cellPosition)
    {
        Vector3 tileSize = m_WalkableTilemap.cellSize;
        return m_WalkableTilemap.CellToWorld(cellPosition) + tileSize / 2f;
    }
}
