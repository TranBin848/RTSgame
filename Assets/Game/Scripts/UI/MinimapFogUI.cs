using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MinimapFogUI : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    [SerializeField] private FogOfWarManager m_FogOfWarManager;
    [SerializeField] private MinimapSettingsDefinition m_Settings;
    [SerializeField] private RawImage m_MinimapImage;
    [SerializeField] private RectTransform m_CameraViewportRect;

    private Texture2D m_MinimapTexture;
    private TilemapManager m_TilemapManager;
    private CameraController m_CameraController;
    private Camera m_MainCamera;

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

        m_CameraController = FindFirstObjectByType<CameraController>();
        m_MainCamera = Camera.main;
        m_FogOfWarManager.OnFogUpdated += RefreshMinimap;
        InitializeTextureIfNeeded();
        RefreshMinimap();
    }

    void Update()
    {
        UpdateCameraViewportRect();
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
            m_MinimapImage.color = Color.white;
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
            return GetUnexploredColor();
        }

        bool isWater = m_TilemapManager.HasWaterTile(cellPosition);
        bool isUnreachable = m_TilemapManager.HasUnreachableTile(cellPosition) && !isWater;

        if (cellState == FogCellState.Unexplored)
        {
            return GetUnexploredColor();
        }

        if (isWater)
        {
            return cellState == FogCellState.Visible
                ? Color.Lerp(GetExploredWaterColor(), GetVisibleWaterColor(), Mathf.Clamp01(visibilityStrength))
                : GetExploredWaterColor();
        }

        if (isUnreachable)
        {
            return GetUnwalkableColor();
        }

        return cellState switch
        {
            FogCellState.Visible => Color.Lerp(GetExploredGroundColor(), GetVisibleGroundColor(), Mathf.Clamp01(visibilityStrength)),
            FogCellState.Explored => GetExploredGroundColor(),
            _ => GetUnexploredColor()
        };
    }

    void UpdateCameraViewportRect()
    {
        if (m_CameraViewportRect == null || m_MinimapImage == null || m_FogOfWarManager == null || !m_FogOfWarManager.IsInitialized)
        {
            return;
        }

        if (m_MainCamera == null)
        {
            m_MainCamera = Camera.main;
        }

        if (m_MainCamera == null || !m_MainCamera.orthographic)
        {
            return;
        }

        BoundsInt bounds = m_FogOfWarManager.Bounds;
        RectTransform minimapRect = m_MinimapImage.rectTransform;
        Rect rect = minimapRect.rect;
        float worldWidth = Mathf.Max(0.01f, bounds.size.x);
        float worldHeight = Mathf.Max(0.01f, bounds.size.y);
        float cameraWorldHeight = m_MainCamera.orthographicSize * 2f;
        float cameraWorldWidth = cameraWorldHeight * m_MainCamera.aspect;

        float normalizedWidth = Mathf.Clamp01((cameraWorldWidth / worldWidth) * GetViewportWidthScale());
        float normalizedHeight = Mathf.Clamp01((cameraWorldHeight / worldHeight) * GetViewportHeightScale());

        m_CameraViewportRect.anchorMin = new Vector2(0.5f, 0.5f);
        m_CameraViewportRect.anchorMax = new Vector2(0.5f, 0.5f);
        m_CameraViewportRect.pivot = new Vector2(0.5f, 0.5f);
        m_CameraViewportRect.sizeDelta = new Vector2(rect.width * normalizedWidth, rect.height * normalizedHeight);

        Vector2 normalizedCameraPosition = GetNormalizedWorldPosition(m_MainCamera.transform.position, bounds, cameraWorldWidth, cameraWorldHeight);
        float anchoredX = (normalizedCameraPosition.x - 0.5f) * rect.width;
        float anchoredY = (normalizedCameraPosition.y - 0.5f) * rect.height;
        float halfViewportWidth = (m_CameraViewportRect.sizeDelta.x * 0.5f) + GetViewportPadding();
        float halfViewportHeight = (m_CameraViewportRect.sizeDelta.y * 0.5f) + GetViewportPadding();
        anchoredX = Mathf.Clamp(anchoredX, (-rect.width * 0.5f) + halfViewportWidth, (rect.width * 0.5f) - halfViewportWidth);
        anchoredY = Mathf.Clamp(anchoredY, (-rect.height * 0.5f) + halfViewportHeight, (rect.height * 0.5f) - halfViewportHeight);
        m_CameraViewportRect.anchoredPosition = new Vector2(anchoredX, anchoredY);
    }

    Vector2 GetNormalizedWorldPosition(Vector3 worldPosition, BoundsInt bounds, float cameraWorldWidth, float cameraWorldHeight)
    {
        float mapMinX = bounds.xMin;
        float mapMaxX = bounds.xMin + bounds.size.x;
        float mapMinY = bounds.yMin;
        float mapMaxY = bounds.yMin + bounds.size.y;

        float minX = mapMinX + (cameraWorldWidth * 0.5f);
        float maxX = mapMaxX - (cameraWorldWidth * 0.5f);
        float minY = mapMinY + (cameraWorldHeight * 0.5f);
        float maxY = mapMaxY - (cameraWorldHeight * 0.5f);

        float normalizedX = Mathf.Approximately(minX, maxX)
            ? 0.5f
            : Mathf.InverseLerp(minX, maxX, Mathf.Clamp(worldPosition.x, minX, maxX));
        float normalizedY = Mathf.Approximately(minY, maxY)
            ? 0.5f
            : Mathf.InverseLerp(minY, maxY, Mathf.Clamp(worldPosition.y, minY, maxY));
        return new Vector2(normalizedX, normalizedY);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        MoveCameraFromPointer(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        MoveCameraFromPointer(eventData);
    }

    void MoveCameraFromPointer(PointerEventData eventData)
    {
        if (m_MinimapImage == null || m_FogOfWarManager == null || !m_FogOfWarManager.IsInitialized)
        {
            return;
        }

        if (m_CameraController == null)
        {
            m_CameraController = FindFirstObjectByType<CameraController>();
        }

        if (m_CameraController == null)
        {
            return;
        }

        RectTransform minimapRect = m_MinimapImage.rectTransform;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(minimapRect, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
        {
            return;
        }

        Rect rect = minimapRect.rect;
        float normalizedX = Mathf.InverseLerp(rect.xMin, rect.xMax, localPoint.x);
        float normalizedY = Mathf.InverseLerp(rect.yMin, rect.yMax, localPoint.y);
        Vector3 worldPosition = GetWorldPositionFromNormalized(new Vector2(normalizedX, normalizedY));
        m_CameraController.FocusWorldPosition(worldPosition);
        UpdateCameraViewportRect();
    }

    Vector3 GetWorldPositionFromNormalized(Vector2 normalizedPosition)
    {
        BoundsInt bounds = m_FogOfWarManager.Bounds;
        float worldX = Mathf.Lerp(bounds.xMin, bounds.xMin + bounds.size.x, Mathf.Clamp01(normalizedPosition.x));
        float worldY = Mathf.Lerp(bounds.yMin, bounds.yMin + bounds.size.y, Mathf.Clamp01(normalizedPosition.y));
        float currentZ = m_MainCamera != null ? m_MainCamera.transform.position.z : -10f;
        return new Vector3(worldX, worldY, currentZ);
    }

    Color GetUnexploredColor()
    {
        return m_Settings != null ? m_Settings.UnexploredColor : Color.black;
    }

    Color GetExploredWaterColor()
    {
        return m_Settings != null ? m_Settings.ExploredWaterColor : new Color(0.08f, 0.18f, 0.32f, 1f);
    }

    Color GetVisibleWaterColor()
    {
        return m_Settings != null ? m_Settings.VisibleWaterColor : new Color(0.18f, 0.42f, 0.72f, 1f);
    }

    Color GetExploredGroundColor()
    {
        return m_Settings != null ? m_Settings.ExploredGroundColor : new Color(0.18f, 0.18f, 0.2f, 1f);
    }

    Color GetVisibleGroundColor()
    {
        return m_Settings != null ? m_Settings.VisibleGroundColor : new Color(0.55f, 0.6f, 0.45f, 1f);
    }

    Color GetUnwalkableColor()
    {
        return m_Settings != null ? m_Settings.UnwalkableColor : new Color(0.1f, 0.1f, 0.12f, 1f);
    }

    float GetViewportWidthScale()
    {
        return m_Settings != null ? m_Settings.ViewportWidthScale : 1f;
    }

    float GetViewportHeightScale()
    {
        return m_Settings != null ? m_Settings.ViewportHeightScale : 1f;
    }

    float GetViewportPadding()
    {
        return m_Settings != null ? m_Settings.ViewportPadding : 0f;
    }
}
