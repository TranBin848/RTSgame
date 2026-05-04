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
    void Start()
    {
        m_Pathfinding = new Pathfinding(this);
    }
    public List<Vector3> FindPath(Vector3 startWorldPosition, Vector3 endWorldPosition)
    {
        return m_Pathfinding.FindPath(startWorldPosition, endWorldPosition);
    }
    public Node FindNode(Vector3 worldPosition)
    {
        return m_Pathfinding.FindNode(worldPosition);
    }
    public void UpdateNodesInArea(Vector3Int startPosition, int width, int height)
    {
        m_Pathfinding.UpdateNodesInArea(startPosition, width, height);
    }
    public bool CamWalkAtTile(Vector3Int tilePosition)
    {
        return m_WalkableTilemap.HasTile(tilePosition) &&
        !isInUnreachableTilemap(tilePosition) &&
        !isBlockByBuilding(tilePosition);
    }
    public bool CanPlaceTiles(Vector3Int tilePosition)
    {
        return m_WalkableTilemap.HasTile(tilePosition) &&
        !isInUnreachableTilemap(tilePosition) &&
        !isBlockByGameObject(tilePosition);
    }
    public bool isInUnreachableTilemap(Vector3Int tilePosition)
    {
        foreach (var tilemap in m_UnreachableTilemaps)
        {
            if (tilemap.HasTile(tilePosition))
            {
                return true;
            }
        }
        return false;
    }
    public bool isBlockByBuilding(Vector3Int tilePosition)
    {
        Vector3 worldPosition = m_WalkableTilemap.CellToWorld(tilePosition) + m_WalkableTilemap.cellSize / 2;
        int buildingLayerMask = 1 << LayerMask.NameToLayer("Building");

        Collider2D[] collider = Physics2D.OverlapPointAll(worldPosition, buildingLayerMask);
        return collider.Length > 0;

    }
    public bool isBlockByGameObject(Vector3Int tilePosition)
    {
        Vector3 tileSize = m_WalkableTilemap.cellSize;
        Collider2D[] colliders = Physics2D.OverlapBoxAll(
            new Vector2(tilePosition.x + tileSize.x / 2, tilePosition.y + tileSize.y / 2),
            tileSize * 0.9f,
            0f
            );
        foreach (var collider in colliders)
        {
            var layer = collider.gameObject.layer;
            if (layer == LayerMask.NameToLayer("Unit") || layer == LayerMask.NameToLayer("Building"))
            {
                return true;
            }
        }
        return false;
    }
    public void SetTileOverlay(Vector3Int tilePosition, Tile tile)
    {
        m_OverlayTilemap.SetTile(tilePosition, tile);
    }
}