using UnityEngine.Tilemaps;
using UnityEngine;

public class TilemapManager : SingletonManager<TilemapManager>
{
    [Header("Tilemaps")]
    [SerializeField] private Tilemap m_WalkableTilemap;
    [SerializeField] private Tilemap m_OverlayTilemap;
    [SerializeField] private Tilemap[] m_UnreachableTilemaps;

    [Header("Testing")]
    [SerializeField] private Transform m_StartTransform;
    [SerializeField] private Transform m_EndTransform;
    public Tilemap PathfindingTilemap => m_WalkableTilemap;
    private Pathfinding m_Pathfinding;
    void Start()
    {
        m_Pathfinding = new Pathfinding(this);
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            m_Pathfinding.FindPath(m_StartTransform.position, m_EndTransform.position);
        }
    }
    public bool CamWalkAtTile(Vector3Int tilePosition)
    {
        return m_WalkableTilemap.HasTile(tilePosition) &&
        !isInUnreachableTilemap(tilePosition);
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