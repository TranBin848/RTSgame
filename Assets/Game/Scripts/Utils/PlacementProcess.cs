using UnityEngine;
using UnityEngine.Tilemaps;
public class PlacementProcess
{
    private GameObject m_PlacementOutline;
    private BuildActionSo m_BuildAction;
    private Vector3Int[] m_HighlightPositions;
    private Tilemap m_WalkableTilemap;
    private Tilemap m_OverlayTilemap;
    private Tilemap[] m_UnreachableTilemaps;
    private Sprite m_PlaceholderTileSprite;
    private Color m_HightlightColor = new Color(1f, 1f, 1f, 0.5f);
    private Color m_BlockColor = new Color(1f, 0f, 0f, 0.8f);

    public PlacementProcess(BuildActionSo buildAction, Tilemap walkableTilemap, Tilemap overlayTilemap, Tilemap[] unreachableTilemaps)
    {
        m_PlaceholderTileSprite = Resources.Load<Sprite>("Images/PlaceholderTileSprite");
        m_BuildAction = buildAction;
        m_WalkableTilemap = walkableTilemap;
        m_OverlayTilemap = overlayTilemap;
        m_UnreachableTilemaps = unreachableTilemaps;
    }
    public void Update()
    {
        if (m_PlacementOutline != null)
        {
            HighLightTiles(m_PlacementOutline.transform.position);
        }
        if (GameUtils.TryGetHoldPosition(out Vector3 worldPosition))
        {
            m_PlacementOutline.transform.position = SnapToGrid(worldPosition);
        }
    }
    public void ShowPlacementOutline()
    {
        m_PlacementOutline = new GameObject("PlacementOutline");
        var spriteRenderer = m_PlacementOutline.AddComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = 999;
        spriteRenderer.color = new Color(1f, 1f, 1f, 0.7f);
        spriteRenderer.sprite = m_BuildAction.PlacementSprite;
    }
    Vector3 SnapToGrid(Vector3 worldPosition)
    {
        return new Vector3(Mathf.Round(worldPosition.x), Mathf.Round(worldPosition.y), 0f);
    }

    void HighLightTiles(Vector3 outlinePosition)
    {
        Vector3Int buildingSize = m_BuildAction.BuildingSize;
        Vector3 pivotPosition = outlinePosition + new Vector3(m_BuildAction.OriginalOffset.x, m_BuildAction.OriginalOffset.y, 0);

        ClearHighlight();
        m_HighlightPositions = new Vector3Int[buildingSize.x * buildingSize.y];

        for (int x = 0; x < buildingSize.x; x++)
        {
            for (int y = 0; y < buildingSize.y; y++)
            {
                m_HighlightPositions[x + y * buildingSize.x] = new Vector3Int(
                    Mathf.RoundToInt(pivotPosition.x) + x,
                    Mathf.RoundToInt(pivotPosition.y) + y,
                    0
                    );
            }
        }
        foreach (var pos in m_HighlightPositions)
        {
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = m_PlaceholderTileSprite;
            if (CanPlaceTiles(pos))
            {
                tile.color = m_HightlightColor;
            }
            else
            {
                tile.color = m_BlockColor;
            }
            m_OverlayTilemap.SetTile(pos, tile);
        }
    }
    void ClearHighlight()
    {
        if (m_HighlightPositions == null) return;
        foreach (var pos in m_HighlightPositions)
        {
            m_OverlayTilemap.SetTile(pos, null);
        }
    }
    bool CanPlaceTiles(Vector3Int tilePosition)
    {

        return m_WalkableTilemap.HasTile(tilePosition) && !isInUnreachableTilemap(tilePosition);
    }
    bool isInUnreachableTilemap(Vector3Int tilePosition)
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
}