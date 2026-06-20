using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.Events;
using System.Collections.Generic;

public enum FogCellState
{
    Unexplored,
    Explored,
    Visible
}

public class FogOfWarManager : MonoBehaviour
{
    [SerializeField] private FogOfWarDefinition m_Definition;
    [SerializeField] private Tilemap m_FogTilemap;
    [SerializeField] private TileBase m_FogTile;
    [SerializeField] private bool m_RevealPlayerStructures = true;

    private TilemapManager m_TilemapManager;
    private GameManager m_GameManager;
    private BoundsInt m_Bounds;
    private bool[,] m_Explored;
    private int[,] m_VisibilityCounts;
    private float[,] m_VisibilityStrengths;
    private float m_NextRefreshTime;
    private bool m_IsInitialized;
    private List<FogOfWarAffectable> m_Affectables = new();
    public UnityAction OnFogUpdated = delegate { };
    public bool IsInitialized => m_IsInitialized;
    public BoundsInt Bounds => m_Bounds;
    public Tilemap FogTilemap => m_FogTilemap;

    void Start()
    {
        Initialize();
        RefreshFog();
    }

    void Update()
    {
        if (!m_IsInitialized || m_Definition == null)
        {
            return;
        }

        if (Time.time < m_NextRefreshTime)
        {
            return;
        }

        RefreshFog();
    }

    void Initialize()
    {
        m_GameManager = GameManager.Get();
        m_TilemapManager = TilemapManager.Get();

        if (m_Definition == null || m_FogTilemap == null || m_FogTile == null || m_TilemapManager == null || m_TilemapManager.PathfindingTilemap == null)
        {
            return;
        }

        m_Bounds = m_TilemapManager.GetWorldMapBounds();

        if (m_Bounds.size.x <= 0 || m_Bounds.size.y <= 0)
        {
            return;
        }

        m_Explored = new bool[m_Bounds.size.x, m_Bounds.size.y];
        m_VisibilityCounts = new int[m_Bounds.size.x, m_Bounds.size.y];
        m_VisibilityStrengths = new float[m_Bounds.size.x, m_Bounds.size.y];
        
        m_Affectables.Clear();
        m_Affectables.AddRange(FindObjectsByType<FogOfWarAffectable>(FindObjectsSortMode.None));

        m_FogTilemap.ClearAllTiles();
        foreach (var position in m_Bounds.allPositionsWithin)
        {
            if (!m_TilemapManager.HasAnyMapTile(position))
            {
                continue;
            }

            m_FogTilemap.SetTile(position, m_FogTile);
            m_FogTilemap.SetTileFlags(position, TileFlags.None);
            m_FogTilemap.SetColor(position, m_Definition.UnexploredColor);
        }

        m_IsInitialized = true;
    }

    void RefreshFog()
    {
        if (!m_IsInitialized)
        {
            return;
        }

        ClearVisibility();
        RevealVisionSources();
        ApplyFogColors();
        m_NextRefreshTime = Time.time + m_Definition.RefreshInterval;
        OnFogUpdated.Invoke();
    }

    void ClearVisibility()
    {
        for (int x = 0; x < m_Bounds.size.x; x++)
        {
            for (int y = 0; y < m_Bounds.size.y; y++)
            {
            m_VisibilityCounts[x, y] = 0;
            m_VisibilityStrengths[x, y] = 0f;
        }
    }
    }

    void RevealVisionSources()
    {
        if (m_GameManager == null)
        {
            return;
        }

        var playerUnits = m_GameManager.GetFriendlyUnits(true);
        foreach (var unit in playerUnits)
        {
            if (unit == null || unit.CurrentState == UnitState.Dead)
            {
                continue;
            }

            RevealAroundWorldPosition(unit.transform.position, m_Definition.UnitVisionRadius);
        }

        if (!m_RevealPlayerStructures)
        {
            return;
        }

        foreach (var structure in m_GameManager.GetPlayerStructures())
        {
            if (structure == null || structure.CurrentState == UnitState.Dead)
            {
                continue;
            }

            RevealAroundWorldPosition(structure.transform.position, m_Definition.StructureVisionRadius);
        }
    }

    void RevealAroundWorldPosition(Vector3 worldPosition, int radius)
    {
        Vector3Int centerCell = m_TilemapManager.PathfindingTilemap.WorldToCell(worldPosition);
        int radiusSqr = radius * radius;

        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                if (x * x + y * y > radiusSqr)
                {
                    continue;
                }

                Vector3Int cellPosition = new Vector3Int(centerCell.x + x, centerCell.y + y, 0);
                if (!TryGetFogIndices(cellPosition, out int fogX, out int fogY))
                {
                    continue;
                }

                m_VisibilityCounts[fogX, fogY]++;
                m_Explored[fogX, fogY] = true;
                m_VisibilityStrengths[fogX, fogY] = Mathf.Max(m_VisibilityStrengths[fogX, fogY], CalculateVisibilityStrength(x, y, radius));
            }
        }
    }

    void ApplyFogColors()
    {
        foreach (var cellPosition in m_Bounds.allPositionsWithin)
        {
            int fogX = cellPosition.x - m_Bounds.xMin;
            int fogY = cellPosition.y - m_Bounds.yMin;

            Color color = m_Definition.UnexploredColor;
            if (m_VisibilityCounts[fogX, fogY] > 0)
            {
                color = Color.Lerp(m_Definition.ExploredColor, m_Definition.VisibleColor, m_VisibilityStrengths[fogX, fogY]);
            }
            else if (m_Explored[fogX, fogY])
            {
                color = m_Definition.ExploredColor;
            }

            m_FogTilemap.SetColor(cellPosition, color);
        }

        ApplyAffectableVisibility();
    }

    bool TryGetFogIndices(Vector3Int cellPosition, out int fogX, out int fogY)
    {
        fogX = cellPosition.x - m_Bounds.xMin;
        fogY = cellPosition.y - m_Bounds.yMin;

        return fogX >= 0
            && fogX < m_Bounds.size.x
            && fogY >= 0
            && fogY < m_Bounds.size.y;
    }

    float CalculateVisibilityStrength(int offsetX, int offsetY, int radius)
    {
        if (m_Definition == null)
        {
            return 1f;
        }

        float distance = Mathf.Sqrt((offsetX * offsetX) + (offsetY * offsetY));
        float hardVisibleRadius = Mathf.Max(0f, radius - m_Definition.SoftEdgeSize);

        if (distance <= hardVisibleRadius)
        {
            return 1f;
        }

        if (m_Definition.SoftEdgeSize <= 0.001f)
        {
            return distance <= radius ? 1f : 0f;
        }

        float t = Mathf.InverseLerp(radius, hardVisibleRadius, distance);
        return Mathf.Clamp01(t);
    }

    void ApplyAffectableVisibility()
    {
        if (m_Affectables == null || m_TilemapManager == null || m_TilemapManager.PathfindingTilemap == null)
        {
            return;
        }

        for (int i = 0; i < m_Affectables.Count; i++)
        {
            ApplyVisibilityToAffectable(m_Affectables[i]);
        }
    }

    public void ApplyVisibilityToAffectable(FogOfWarAffectable affectable)
    {
        if (affectable == null)
        {
            return;
        }

        if (affectable.AlwaysVisible)
        {
            affectable.ApplyVisibility(1f);
            return;
        }

        Vector3Int cellPosition = m_TilemapManager.PathfindingTilemap.WorldToCell(affectable.transform.position);
        if (!TryGetFogIndices(cellPosition, out int fogX, out int fogY))
        {
            affectable.ApplyVisibility(1f);
            return;
        }

        if (m_VisibilityCounts[fogX, fogY] > 0)
        {
            affectable.ApplyVisibility(1f);
        }
        else if (m_Explored[fogX, fogY])
        {
            affectable.ApplyVisibility(affectable.ExploredAlpha);
        }
        else
        {
            affectable.ApplyVisibility(affectable.UnexploredAlpha);
        }
    }

    public void RegisterAffectable(FogOfWarAffectable affectable)
    {
        if (affectable != null && !m_Affectables.Contains(affectable))
        {
            m_Affectables.Add(affectable);
            if (m_IsInitialized)
            {
                ApplyVisibilityToAffectable(affectable);
            }
        }
    }

    public void UnregisterAffectable(FogOfWarAffectable affectable)
    {
        if (affectable != null)
        {
            m_Affectables.Remove(affectable);
        }
    }

    public bool TryGetCellState(Vector3Int cellPosition, out FogCellState cellState, out float visibilityStrength)
    {
        visibilityStrength = 0f;
        cellState = FogCellState.Unexplored;

        if (!m_IsInitialized || !TryGetFogIndices(cellPosition, out int fogX, out int fogY))
        {
            return false;
        }

        visibilityStrength = m_VisibilityStrengths[fogX, fogY];
        if (m_VisibilityCounts[fogX, fogY] > 0)
        {
            cellState = FogCellState.Visible;
        }
        else if (m_Explored[fogX, fogY])
        {
            cellState = FogCellState.Explored;
        }

        return true;
    }

    public bool IsWorldPositionExplored(Vector3 worldPosition)
    {
        if (!m_IsInitialized || m_TilemapManager == null || m_TilemapManager.PathfindingTilemap == null)
        {
            return true;
        }

        Vector3Int cellPosition = m_TilemapManager.PathfindingTilemap.WorldToCell(worldPosition);
        return TryGetCellState(cellPosition, out var cellState, out _)
            && cellState != FogCellState.Unexplored;
    }
}
