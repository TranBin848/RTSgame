using UnityEngine;

[CreateAssetMenu(fileName = "FogOfWarDefinition", menuName = "Game/Definitions/Fog Of War")]
public class FogOfWarDefinition : ScriptableObject
{
    [Header("Reveal")]
    [SerializeField] private int m_UnitVisionRadius = 4;
    [SerializeField] private int m_StructureVisionRadius = 5;
    [SerializeField] private float m_SoftEdgeSize = 1.5f;
    [SerializeField] private float m_RefreshInterval = 0.15f;

    [Header("Colors")]
    [SerializeField] private Color m_UnexploredColor = new Color(0f, 0f, 0f, 1f);
    [SerializeField] private Color m_ExploredColor = new Color(0f, 0f, 0f, 0.55f);
    [SerializeField] private Color m_VisibleColor = new Color(0f, 0f, 0f, 0f);

    public int UnitVisionRadius => Mathf.Max(1, m_UnitVisionRadius);
    public int StructureVisionRadius => Mathf.Max(1, m_StructureVisionRadius);
    public float SoftEdgeSize => Mathf.Max(0f, m_SoftEdgeSize);
    public float RefreshInterval => Mathf.Max(0.02f, m_RefreshInterval);
    public Color UnexploredColor => m_UnexploredColor;
    public Color ExploredColor => m_ExploredColor;
    public Color VisibleColor => m_VisibleColor;
}
