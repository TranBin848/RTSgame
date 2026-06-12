using UnityEngine;

[CreateAssetMenu(fileName = "SelectionSettingsDefinition", menuName = "Game/Definitions/Selection Settings")]
public class SelectionSettingsDefinition : ScriptableObject
{
    [Header("Visual")]
    [SerializeField] private Color m_SelectionFillColor = new Color(0.2f, 0.8f, 0.2f, 0.15f);
    [SerializeField] private Color m_SelectionBorderColor = new Color(0.2f, 1f, 0.2f, 0.9f);

    [Header("Input")]
    [SerializeField] private float m_SelectionDragThreshold = 12f;
    [SerializeField] private float m_SelectionDragMinHoldTime = 0.08f;

    [Header("Formation")]
    [SerializeField] private float m_GroupMoveSpacing = 0.9f;

    public Color SelectionFillColor => m_SelectionFillColor;
    public Color SelectionBorderColor => m_SelectionBorderColor;
    public float SelectionDragThreshold => m_SelectionDragThreshold;
    public float SelectionDragMinHoldTime => m_SelectionDragMinHoldTime;
    public float GroupMoveSpacing => m_GroupMoveSpacing;
}
