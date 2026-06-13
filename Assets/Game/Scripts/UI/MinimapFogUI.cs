using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class MinimapFogUI : MonoBehaviour
{
    [SerializeField] private FogOfWarManager m_FogOfWarManager;
    [SerializeField] private RawImage m_MinimapImage;
    [SerializeField] private Color m_UnexploredColor = Color.black;
    [SerializeField] private Color m_ExploredWaterColor = new Color(0.08f, 0.18f, 0.32f, 1f);
    [SerializeField] private Color m_VisibleWaterColor = new Color(0.18f, 0.42f, 0.72f, 1f);
    [SerializeField] private Color m_ExploredGroundColor = new Color(0.18f, 0.18f, 0.2f, 1f);
    [SerializeField] private Color m_VisibleGroundColor = new Color(0.55f, 0.6f, 0.45f, 1f);
    [SerializeField] private Color m_UnwalkableColor = new Color(0.1f, 0.1f, 0.12f, 1f);

    private Texture2D m_MinimapTexture;
    private TilemapManager m_TilemapManager;

    void Start()
    {
        if (m_FogOfWarManager == null)
        {
            m_FogOfWarManager = FindFirstObjectByType<FogOfWarManager>();
        }

        if (m_FogOfWarManager == null)
        {
            return;
        }

        m_FogOfWarManager.OnFogUpdated += RefreshMinimap;
        InitializeTextureIfNeeded();
        RefreshMinimap();
    }

    void OnDestroy()
    {
        if (m_FogOfWarManager != null)
        {
            m_FogOfWarManager.OnFogUpdated -= RefreshMinimap;
        }
    }

    void InitializeTextureIfNeeded()
    {
        if (m_FogOfWarManager == null || !m_FogOfWarManager.IsInitialized)
        {
            return;
        }

        BoundsInt bounds = m_FogOfWarManager.Bounds;
        if (bounds.size.x <= 0 || bounds.size.y <= 0)
        {
            return;
        }

        if (m_MinimapTexture != null
            && m_MinimapTexture.width == bounds.size.x
            && m_MinimapTexture.height == bounds.size.y)
        {
            return;
        }

        m_TilemapManager = TilemapManager.Get();
        m_MinimapTexture = new Texture2D(bounds.size.x, bounds.size.y, TextureFormat.RGBA32, false);
        m_MinimapTexture.filterMode = FilterMode.Point;
        m_MinimapTexture.wrapMode = TextureWrapMode.Clamp;

        if (m_MinimapImage != null)
        {
            m_MinimapImage.texture = m_MinimapTexture;
        }
    }

    void RefreshMinimap()
    {
        if (m_FogOfWarManager == null || !m_FogOfWarManager.IsInitialized)
        {
            return;
        }

        InitializeTextureIfNeeded();
        if (m_MinimapTexture == null)
        {
            return;
        }

        BoundsInt bounds = m_FogOfWarManager.Bounds;
        for (int x = 0; x < bounds.size.x; x++)
        {
            for (int y = 0; y < bounds.size.y; y++)
            {
                Vector3Int cellPosition = new Vector3Int(bounds.xMin + x, bounds.yMin + y, 0);
                Color pixelColor = ResolveCellColor(cellPosition);
                m_MinimapTexture.SetPixel(x, y, pixelColor);
            }
        }

        m_MinimapTexture.Apply();
    }

    Color ResolveCellColor(Vector3Int cellPosition)
    {
        if (m_TilemapManager == null || !m_TilemapManager.HasAnyMapTile(cellPosition))
        {
            return new Color(0f, 0f, 0f, 0f);
        }

        if (!m_FogOfWarManager.TryGetCellState(cellPosition, out var cellState, out float visibilityStrength))
        {
            return m_UnexploredColor;
        }

        bool isWater = m_TilemapManager.HasWaterTile(cellPosition);
        bool isUnreachable = m_TilemapManager.HasUnreachableTile(cellPosition) && !isWater;

        if (cellState == FogCellState.Unexplored)
        {
            return m_UnexploredColor;
        }

        if (isWater)
        {
            return cellState == FogCellState.Visible
                ? Color.Lerp(m_ExploredWaterColor, m_VisibleWaterColor, Mathf.Clamp01(visibilityStrength))
                : m_ExploredWaterColor;
        }

        if (isUnreachable)
        {
            return m_UnwalkableColor;
        }

        return cellState switch
        {
            FogCellState.Visible => Color.Lerp(m_ExploredGroundColor, m_VisibleGroundColor, Mathf.Clamp01(visibilityStrength)),
            FogCellState.Explored => m_ExploredGroundColor,
            _ => m_UnexploredColor
        };
    }
}
