using UnityEngine;

public class FogOfWarAffectable : MonoBehaviour
{
    [SerializeField] private SpriteRenderer[] m_TargetRenderers;
    [SerializeField] private bool m_AlwaysVisible = false;
    [SerializeField] private float m_ExploredAlpha = 0.45f;
    [SerializeField] private float m_UnexploredAlpha = 0f;

    private Color[] m_OriginalColors;

    public bool AlwaysVisible => m_AlwaysVisible;
    public float ExploredAlpha => Mathf.Clamp01(m_ExploredAlpha);
    public float UnexploredAlpha => Mathf.Clamp01(m_UnexploredAlpha);
    public SpriteRenderer[] TargetRenderers => m_TargetRenderers;

    private FogOfWarManager m_FogOfWarManager;

    public void Initialize(FogOfWarManager fog)
    {
        if (fog == null || fog.gameObject.scene != gameObject.scene)
            throw new System.ArgumentException("Affectable requires fog from its owner scene.");
        if (m_FogOfWarManager != null && m_FogOfWarManager != fog)
            throw new System.InvalidOperationException("Unbind affectable before changing its fog owner.");
        EnsureRenderers();
        m_FogOfWarManager = fog;
        if (isActiveAndEnabled) fog.RegisterAffectable(this);
    }

    public void Unbind()
    {
        if (m_FogOfWarManager != null) m_FogOfWarManager.UnregisterAffectable(this);
        m_FogOfWarManager = null;
        ApplyVisibility(1);
    }

    void OnEnable()
    {
        if (m_FogOfWarManager != null && m_FogOfWarManager.gameObject.scene == gameObject.scene)
        {
            m_FogOfWarManager.RegisterAffectable(this);
        }
    }

    void OnDisable()
    {
        if (m_FogOfWarManager != null)
        {
            m_FogOfWarManager.UnregisterAffectable(this);
        }
    }

    void Awake() => EnsureRenderers();
    void OnDestroy() => Unbind();

    void EnsureRenderers()
    {
        if (m_OriginalColors != null) return;
        if (m_TargetRenderers == null || m_TargetRenderers.Length == 0)
        {
            m_TargetRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        }

        m_OriginalColors = new Color[m_TargetRenderers.Length];
        for (int i = 0; i < m_TargetRenderers.Length; i++)
        {
            m_OriginalColors[i] = m_TargetRenderers[i] != null ? m_TargetRenderers[i].color : Color.white;
        }
    }

    public void ApplyVisibility(float alphaMultiplier)
    {
        EnsureRenderers();
        if (m_TargetRenderers == null) return;
        
        for (int i = 0; i < m_TargetRenderers.Length; i++)
        {
            SpriteRenderer renderer = m_TargetRenderers[i];
            if (renderer == null)
            {
                continue;
            }

            Color currentColor = renderer.color;
            float baseAlpha = i < m_OriginalColors.Length ? m_OriginalColors[i].a : 1f;
            currentColor.a = baseAlpha * Mathf.Clamp01(alphaMultiplier);
            renderer.color = currentColor;
        }
    }
}
