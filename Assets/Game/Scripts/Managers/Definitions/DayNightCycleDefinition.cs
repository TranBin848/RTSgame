using UnityEngine;

[CreateAssetMenu(fileName = "DayNightCycleDefinition", menuName = "Game/Definitions/Day Night Cycle")]
public class DayNightCycleDefinition : ScriptableObject
{
    [Header("Durations")]
    [SerializeField] private float m_DayDuration = 60f;
    [SerializeField] private float m_DuskDuration = 25f;
    [SerializeField] private float m_NightDuration = 35f;

    [Header("Overlay")]
    [SerializeField] private Color m_DayOverlayColor = new Color(0f, 0f, 0f, 0f);
    [SerializeField] private Color m_DuskOverlayColor = new Color(0.05f, 0.08f, 0.15f, 0.28f);
    [SerializeField] private Color m_NightOverlayColor = new Color(0.02f, 0.04f, 0.12f, 0.55f);

    public float DayDuration => Mathf.Max(0.01f, m_DayDuration);
    public float DuskDuration => Mathf.Max(0.01f, m_DuskDuration);
    public float NightDuration => Mathf.Max(0.01f, m_NightDuration);
    public float TotalDuration => DayDuration + DuskDuration + NightDuration;
    public Color DayOverlayColor => m_DayOverlayColor;
    public Color DuskOverlayColor => m_DuskOverlayColor;
    public Color NightOverlayColor => m_NightOverlayColor;
}
