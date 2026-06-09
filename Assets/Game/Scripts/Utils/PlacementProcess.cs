using UnityEngine;
using UnityEngine.Tilemaps;
public class PlacementProcess
{
    private GameObject m_PlacementOutline;
    private BuildActionSo m_BuildAction;
    private Vector3Int[] m_HighlightPositions;
    private Sprite m_PlaceholderTileSprite;
    private TilemapManager m_TilemapManager;
    private Vector3 m_InitialPlacementPosition;
    private float m_ShakeTime;
    private readonly float m_ShakeDuration = 0.15f;
    private readonly float m_ShakeMagnitude = 0.15f;
    private Color m_HightlightColor = new Color(1f, 1f, 1f, 0.5f);
    private Color m_BlockColor = new Color(1f, 0f, 0f, 0.8f);

    public BuildActionSo BuildAction => m_BuildAction;
    public int GoldCost => BuildAction.GoldCost;
    public int WoodCost => BuildAction.WoodCost;

    public PlacementProcess(BuildActionSo buildAction, TilemapManager tilemapManager, Vector3 initialPlacementPosition)
    {
        m_PlaceholderTileSprite = Resources.Load<Sprite>("Images/PlaceholderTileSprite");
        m_BuildAction = buildAction;
        m_TilemapManager = tilemapManager;
        m_InitialPlacementPosition = initialPlacementPosition;
    }
    public void Update()
    {
        if (m_PlacementOutline == null)
        {
            return;
        }

        if (GameUtils.iSPointOverUIElelement())
        {
            return;
        }

        if (GameUtils.TryGetPointerWorldPosition(out Vector3 worldPosition))
        {
            m_InitialPlacementPosition = SnapToGrid(worldPosition);
            m_PlacementOutline.transform.position = m_InitialPlacementPosition;
        }

        if (m_ShakeTime > 0f)
        {
            m_ShakeTime -= Time.deltaTime;
            Vector2 shakeOffset = Random.insideUnitCircle * m_ShakeMagnitude * (m_ShakeTime / m_ShakeDuration);
            m_PlacementOutline.transform.position = m_InitialPlacementPosition + (Vector3)shakeOffset;
        }

        HighLightTiles(m_PlacementOutline.transform.position);
    }
    public void ShowPlacementOutline()
    {
        m_PlacementOutline = new GameObject("PlacementOutline");
        m_PlacementOutline.transform.position = SnapToGrid(m_InitialPlacementPosition);
        var spriteRenderer = m_PlacementOutline.AddComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = 999;
        spriteRenderer.color = new Color(1f, 1f, 1f, 0.7f);
        spriteRenderer.sprite = m_BuildAction.PlacementSprite;
    }
    public void CleanUp()
    {
        if (m_PlacementOutline != null)
        {
            GameObject.Destroy(m_PlacementOutline);
        }
        ClearHighlight();
    }
    public bool TryFinalizePlacement(out Vector3 placementPosition)
    {
        if (isPlacementAreaValid())
        {
            ClearHighlight();
            placementPosition = m_InitialPlacementPosition;
            GameObject.Destroy(m_PlacementOutline);
            return true;
        }
        Debug.Log("Invalid Placement Area");
        placementPosition = Vector3Int.zero;
        return false;
    }

    public void Shake()
    {
        m_ShakeTime = m_ShakeDuration;
    }

    bool isPlacementAreaValid()
    {
        foreach (var tilePosition in m_HighlightPositions)
        {
            if (!m_TilemapManager.CanPlaceTiles(tilePosition)) return false;
        }

        return true;
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
            if (m_TilemapManager.CanPlaceTiles(pos))
            {
                tile.color = m_HightlightColor;
            }
            else
            {
                tile.color = m_BlockColor;
            }
            m_TilemapManager.SetTileOverlay(pos, tile);
        }
    }
    void ClearHighlight()
    {
        if (m_HighlightPositions == null) return;
        foreach (var pos in m_HighlightPositions)
        {
            m_TilemapManager.SetTileOverlay(pos, null);
        }
    }

}