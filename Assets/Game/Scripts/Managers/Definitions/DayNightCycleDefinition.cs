using UnityEngine;

[CreateAssetMenu(fileName = "DayNightCycleDefinition", menuName = "Game/Definitions/Day Night Cycle")]
public class DayNightCycleDefinition : ScriptableObject
{
    [Header("Durations")]
    [SerializeField] private float m_DayDuration = 60f;
    [SerializeField] private float m_DuskDuration = 25f;
    [SerializeField] private float m_NightDuration = 35f;

    [Header("Global Light")]
    [SerializeField] private Color m_DayLightColor = Color.white;
    [SerializeField] private Color m_DuskLightColor = new Color(1f, 0.6f, 0.4f);
    [SerializeField] private Color m_NightLightColor = new Color(0.2f, 0.2f, 0.4f);
    
    [SerializeField] private float m_DayLightIntensity = 1.0f;
    [SerializeField] private float m_DuskLightIntensity = 0.6f;
    [SerializeField] private float m_NightLightIntensity = 0.1f;

    [Header("Post Processing")]
    [SerializeField] private float m_DayVignette = 0f;
    [SerializeField] private float m_DuskVignette = 0.25f;
    [SerializeField] private float m_NightVignette = 0.5f;

    [SerializeField] private Color m_DayColorFilter = Color.white;
    [SerializeField] private Color m_DuskColorFilter = new Color(1f, 0.9f, 0.8f);
    [SerializeField] private Color m_NightColorFilter = new Color(0.6f, 0.7f, 1f);

    public float DayDuration => Mathf.Max(0.01f, m_DayDuration);
    public float DuskDuration => Mathf.Max(0.01f, m_DuskDuration);
    public float NightDuration => Mathf.Max(0.01f, m_NightDuration);
    public float TotalDuration => DayDuration + DuskDuration + NightDuration;
    public Color DayLightColor => m_DayLightColor;
    public Color DuskLightColor => m_DuskLightColor;
    public Color NightLightColor => m_NightLightColor;

    public float DayLightIntensity => m_DayLightIntensity;
    public float DuskLightIntensity => m_DuskLightIntensity;
    public float NightLightIntensity => m_NightLightIntensity;

    public float DayVignette => m_DayVignette;
    public float DuskVignette => m_DuskVignette;
    public float NightVignette => m_NightVignette;

    public Color DayColorFilter => m_DayColorFilter;
    public Color DuskColorFilter => m_DuskColorFilter;
    public Color NightColorFilter => m_NightColorFilter;
}
