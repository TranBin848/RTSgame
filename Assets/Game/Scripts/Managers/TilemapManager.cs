using UnityEngine.Tilemaps;
using System.Collections.Generic;
using UnityEngine;

public class TilemapManager : SingletonManager<TilemapManager>
{
    [Header("Tilemaps")]
    [SerializeField] private Tilemap m_WalkableTilemap;
    [SerializeField] private Tilemap m_OverlayTilemap;
    [SerializeField] private Tilemap[] m_UnreachableTilemaps;

    [Header("Testing")]

    public Tilemap PathfindingTilemap => m_WalkableTilemap;
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

        Vector3 worldPosition = m_WalkableTilemap.CellToWorld(tilePosition) + m_WalkableTilemap.cellSize / 2;
        int buildingLayerMask = 1 << LayerMask.NameToLayer("Unit");
        Collider2D[] collider = Physics2D.OverlapPointAll(worldPosition, buildingLayerMask);

        foreach (var col in collider)
        {
            if (col.CompareTag("Building")) return true;
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
        int unitLayerMask = 1 << LayerMask.NameToLayer("Unit");
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
}
