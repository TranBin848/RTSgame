using TMPro;
using UnityEngine;

public class DayCounterUI : MonoBehaviour
{
    [SerializeField] private DayNightCycleManager m_DayNightCycleManager;
    [SerializeField] private TextMeshProUGUI m_DayText;
    [SerializeField] private string m_DayFormat = "Day {0}";

    void Start()
    {
        if (m_DayNightCycleManager == null)
        {
            m_DayNightCycleManager = FindFirstObjectByType<DayNightCycleManager>();
        }

        if (m_DayNightCycleManager != null)
        {
            m_DayNightCycleManager.OnDayChanged += UpdateDayText;
            UpdateDayText(m_DayNightCycleManager.CurrentDay);
        }
    }

    void OnDestroy()
    {
        if (m_DayNightCycleManager != null)
        {
            m_DayNightCycleManager.OnDayChanged -= UpdateDayText;
        }
    }

    void UpdateDayText(int day)
    {
        if (m_DayText != null)
        {
            m_DayText.text = string.Format(m_DayFormat, day);
        }
    }
}
