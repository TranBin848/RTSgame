using UnityEngine;

[CreateAssetMenu(fileName = "MinimapSettingsDefinition", menuName = "Game/Definitions/Minimap Settings")]
public class MinimapSettingsDefinition : ScriptableObject
{
    [Header("Map Colors")]
    [SerializeField] private Color m_UnexploredColor = Color.black;
    [SerializeField] private Color m_ExploredWaterColor = new Color(0.08f, 0.18f, 0.32f, 1f);
    [SerializeField] private Color m_VisibleWaterColor = new Color(0.18f, 0.42f, 0.72f, 1f);
    [SerializeField] private Color m_ExploredGroundColor = new Color(0.18f, 0.18f, 0.2f, 1f);
    [SerializeField] private Color m_VisibleGroundColor = new Color(0.55f, 0.6f, 0.45f, 1f);
    [SerializeField] private Color m_UnwalkableColor = new Color(0.1f, 0.1f, 0.12f, 1f);

    [Header("Viewport")]
    [SerializeField] private float m_ViewportWidthScale = 1f;
    [SerializeField] private float m_ViewportHeightScale = 1f;
    [SerializeField] private float m_ViewportPadding = 0f;

    public Color UnexploredColor => m_UnexploredColor;
    public Color ExploredWaterColor => m_ExploredWaterColor;
    public Color VisibleWaterColor => m_VisibleWaterColor;
    public Color ExploredGroundColor => m_ExploredGroundColor;
    public Color VisibleGroundColor => m_VisibleGroundColor;
    public Color UnwalkableColor => m_UnwalkableColor;
    public float ViewportWidthScale => Mathf.Max(0.1f, m_ViewportWidthScale);
    public float ViewportHeightScale => Mathf.Max(0.1f, m_ViewportHeightScale);
    public float ViewportPadding => Mathf.Max(0f, m_ViewportPadding);
}
