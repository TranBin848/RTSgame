using System.Collections.Generic;
using UnityEngine;

public class CursorManager : SingletonManager<CursorManager>
{
    [System.Serializable]
    public struct CursorMapping
    {
        public UnitTask Task;
        public Texture2D CursorTexture;
        public Vector2 Hotspot;
    }

    [Header("Cursor Settings")]
    [SerializeField] private CursorMode m_CursorMode = CursorMode.Auto;
    [SerializeField] private bool m_UseCustomSize = false;
    [SerializeField] private Vector2Int m_CursorSize = new Vector2Int(32, 32);
    [SerializeField] private Texture2D m_DefaultCursorTexture;
    [SerializeField] private Vector2 m_DefaultHotspot = Vector2.zero;
    [SerializeField] private List<CursorMapping> m_CursorMappings = new();

    private Dictionary<UnitTask, CursorMapping> m_MappingDict = new();
    private Dictionary<UnitTask, CursorMapping> m_ScaledMappingDict = new();
    private Texture2D m_ScaledDefaultTexture;
    private Vector2 m_ScaledDefaultHotspot;
    private List<Texture2D> m_ScaledTextures = new();
    private UnitTask m_CurrentTask = UnitTask.None;

    protected override void Awake()
    {
        base.Awake();
        InitializeMappings();
    }

    private void Start()
    {
        SetCursor(UnitTask.None);
    }

    private void OnDestroy()
    {
        // Clean up dynamically created scaled textures to prevent memory leaks
        foreach (var tex in m_ScaledTextures)
        {
            if (tex != null)
            {
                Destroy(tex);
            }
        }
        m_ScaledTextures.Clear();
    }

    private void InitializeMappings()
    {
        m_MappingDict.Clear();
        m_ScaledMappingDict.Clear();

        foreach (var tex in m_ScaledTextures)
        {
            if (tex != null) Destroy(tex);
        }
        m_ScaledTextures.Clear();

        // 1. Process default custom cursor
        if (m_UseCustomSize && m_DefaultCursorTexture != null && IsTextureReadable(m_DefaultCursorTexture))
        {
            m_ScaledDefaultTexture = ScaleTexture(m_DefaultCursorTexture, m_CursorSize.x, m_CursorSize.y);
            if (m_ScaledDefaultTexture != null)
            {
                m_ScaledTextures.Add(m_ScaledDefaultTexture);
                float scaleX = (float)m_CursorSize.x / m_DefaultCursorTexture.width;
                float scaleY = (float)m_CursorSize.y / m_DefaultCursorTexture.height;
                m_ScaledDefaultHotspot = new Vector2(m_DefaultHotspot.x * scaleX, m_DefaultHotspot.y * scaleY);
            }
            else
            {
                m_ScaledDefaultTexture = m_DefaultCursorTexture;
                m_ScaledDefaultHotspot = m_DefaultHotspot;
            }
        }
        else
        {
            m_ScaledDefaultTexture = m_DefaultCursorTexture;
            m_ScaledDefaultHotspot = m_DefaultHotspot;
        }

        // 2. Process all mapped cursors
        foreach (var mapping in m_CursorMappings)
        {
            if (!m_MappingDict.ContainsKey(mapping.Task))
            {
                m_MappingDict.Add(mapping.Task, mapping);

                if (m_UseCustomSize && mapping.CursorTexture != null && IsTextureReadable(mapping.CursorTexture))
                {
                    Texture2D scaledTex = ScaleTexture(mapping.CursorTexture, m_CursorSize.x, m_CursorSize.y);
                    if (scaledTex != null)
                    {
                        m_ScaledTextures.Add(scaledTex);
                        float scaleX = (float)m_CursorSize.x / mapping.CursorTexture.width;
                        float scaleY = (float)m_CursorSize.y / mapping.CursorTexture.height;
                        Vector2 scaledHotspot = new Vector2(mapping.Hotspot.x * scaleX, mapping.Hotspot.y * scaleY);

                        CursorMapping scaledMapping = new CursorMapping
                        {
                            Task = mapping.Task,
                            CursorTexture = scaledTex,
                            Hotspot = scaledHotspot
                        };
                        m_ScaledMappingDict.Add(mapping.Task, scaledMapping);
                    }
                    else
                    {
                        m_ScaledMappingDict.Add(mapping.Task, mapping);
                    }
                }
                else
                {
                    m_ScaledMappingDict.Add(mapping.Task, mapping);
                }
            }
            else
            {
                Debug.LogWarning($"Duplicate cursor mapping for task: {mapping.Task}");
            }
        }
    }

    private void Update()
    {
        UnitTask targetTask = DetermineHoverTask();
        if (targetTask != m_CurrentTask)
        {
            m_CurrentTask = targetTask;
            SetCursor(m_CurrentTask);
        }
    }

    private UnitTask DetermineHoverTask()
    {
        if (GameUtils.iSPointOverUIElelement() || Camera.main == null)
        {
            return UnitTask.None;
        }

        GameManager gameManager = GameManager.Get();
        if (gameManager == null)
        {
            return UnitTask.None;
        }

        // 1. Check if the player is currently in building placement mode
        if (gameManager.IsPlacingStructure)
        {
            return UnitTask.Build;
        }

        if (!gameManager.HasActiveUnit)
        {
            return UnitTask.None;
        }

        Unit activeUnit = gameManager.ActiveUnit;

        // 2. Check if the selected unit is actively performing a state/task
        if (activeUnit.CurrentState == UnitState.Building || activeUnit.CurrentTask == UnitTask.Build)
        {
            return UnitTask.Build;
        }

        if (activeUnit.CurrentState == UnitState.Attacking || activeUnit.CurrentTask == UnitTask.Attack)
        {
            return UnitTask.Attack;
        }

        // 3. Hover-based cursor: Check if hovering over interactive objects
        Vector2 worldPoint = Camera.main.ScreenToWorldPoint(GameUtils.InputPosition);
        RaycastHit2D hit = Physics2D.Raycast(worldPoint, Vector2.zero);

        if (hit.collider != null && hit.collider.TryGetComponent<Unit>(out var hoveredUnit))
        {
            // Case A: Attack - Hovering over an enemy unit
            if (!hoveredUnit.IsPlayer)
            {
                // Non-building units (e.g. humanoid combat units) can attack
                if (!activeUnit.IsBuilding)
                {
                    return UnitTask.Attack;
                }
            }
            // Case B: Build - Hovering over an unfinished building with a Worker selected
            else if (hoveredUnit is StructureUnit structure && structure.isUnderConstruction)
            {
                if (activeUnit is WorkerUnit)
                {
                    return UnitTask.Build;
                }
            }
        }

        // --- Extensibility Guide for Future Tasks ---
        // If you add new tasks in the future (e.g. Chop wood, Mine gold), you can add checks here:
        //
        // Example:
        // if (hit.collider != null && hit.collider.TryGetComponent<ResourceSource>(out var resource))
        // {
        //     if (activeUnit is WorkerUnit)
        //     {
        //         if (resource.Type == ResourceType.Wood) return UnitTask.Chop;
        //         if (resource.Type == ResourceType.Gold) return UnitTask.Mine;
        //     }
        // }

        return UnitTask.None;
    }

    private void SetCursor(UnitTask task)
    {
        if (m_ScaledMappingDict.TryGetValue(task, out var mapping) && mapping.CursorTexture != null)
        {
            if (IsTextureReadable(mapping.CursorTexture))
            {
                Cursor.SetCursor(mapping.CursorTexture, mapping.Hotspot, m_CursorMode);
            }
            else
            {
                UseFallbackCursor();
            }
        }
        else
        {
            UseFallbackCursor();
        }
    }

    private void UseFallbackCursor()
    {
        if (m_ScaledDefaultTexture != null && IsTextureReadable(m_ScaledDefaultTexture))
        {
            Cursor.SetCursor(m_ScaledDefaultTexture, m_ScaledDefaultHotspot, m_CursorMode);
        }
        else
        {
            Cursor.SetCursor(null, Vector2.zero, m_CursorMode);
        }
    }

    private bool IsTextureReadable(Texture2D texture)
    {
        if (texture == null) return false;
        if (!texture.isReadable)
        {
            Debug.LogWarning($"[CursorManager] Texture '{texture.name}' is not CPU accessible! Please select this texture in the project folder, check the 'Read/Write' checkbox under its Import Settings in the Inspector, and click Apply.");
            return false;
        }
        return true;
    }

    private Texture2D ScaleTexture(Texture2D source, int targetWidth, int targetHeight)
    {
        if (source == null) return null;
        if (!source.isReadable)
        {
            Debug.LogWarning($"[CursorManager] Texture '{source.name}' is not CPU accessible! Cannot scale texture.");
            return source;
        }

        // Set up temporary render texture to handle scaling efficiently on GPU
        RenderTexture rt = RenderTexture.GetTemporary(targetWidth, targetHeight, 0, RenderTextureFormat.ARGB32);
        RenderTexture.active = rt;

        // Clear and render scaled source
        GL.Clear(true, true, Color.clear);
        Graphics.Blit(source, rt);

        // Read pixels into a new Texture2D
        Texture2D result = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false);
        result.name = source.name + "_scaled";
        result.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
        result.Apply();

        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);

        return result;
    }
}
