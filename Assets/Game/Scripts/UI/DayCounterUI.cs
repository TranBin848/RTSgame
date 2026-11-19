using TMPro;
using UnityEngine;

public class DayCounterUI : MonoBehaviour
{
    [SerializeField] private DayNightCycleManager m_DayNightCycleManager;
    [SerializeField] private TextMeshProUGUI m_DayText;
    [SerializeField] private string m_DayFormat = "Day {0}";

    bool m_IsInitialized;
    public void Initialize(DayNightCycleManager day)
    {
        if (day == null) throw new System.ArgumentException("Presentation dependencies are required.");
        if (m_IsInitialized)
        {
            if (m_DayNightCycleManager != day) throw new System.InvalidOperationException("Presentation already initialized with different dependencies.");
            return;
        }
        m_DayNightCycleManager = day;
        m_DayNightCycleManager.OnDayChanged += UpdateDayText;
        UpdateDayText(m_DayNightCycleManager.CurrentDay);
        m_IsInitialized = true;
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
