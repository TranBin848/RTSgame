using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public enum DayNightPhase
{
    Day,
    Dusk,
    Night
}

public class DayNightCycleManager : MonoBehaviour
{
    [SerializeField] private DayNightCycleDefinition m_Definition;
    [SerializeField] private Image m_ScreenOverlayImage;
    [SerializeField] private DayNightPhase m_StartingPhase = DayNightPhase.Day;
    [SerializeField] private int m_StartingDay = 1;

    public UnityAction<DayNightPhase> OnPhaseChanged = delegate { };
    public UnityAction<int> OnDayChanged = delegate { };
    public DayNightPhase CurrentPhase { get; private set; }
    public int CurrentDay { get; private set; } = 1;
    public float CurrentPhaseProgress { get; private set; }
    public DayNightCycleDefinition Definition => m_Definition;
    public float CycleNormalizedProgress => GetCycleNormalizedProgress();

    private float m_PhaseTimer;
    private bool m_HasStarted;

    void Start()
    {
        CurrentDay = Mathf.Max(1, m_StartingDay);
        OnDayChanged.Invoke(CurrentDay);
        SetPhase(m_StartingPhase, true);
        m_HasStarted = true;
    }

    void Update()
    {
        if (m_Definition == null)
        {
            return;
        }

        m_PhaseTimer += Time.deltaTime;
        float phaseDuration = GetCurrentPhaseDuration();
        CurrentPhaseProgress = Mathf.Clamp01(m_PhaseTimer / phaseDuration);
        UpdateOverlayColor();

        if (m_PhaseTimer >= phaseDuration)
        {
            AdvanceToNextPhase();
        }
    }

    void AdvanceToNextPhase()
    {
        DayNightPhase nextPhase = CurrentPhase switch
        {
            DayNightPhase.Day => DayNightPhase.Dusk,
            DayNightPhase.Dusk => DayNightPhase.Night,
            _ => DayNightPhase.Day
        };

        SetPhase(nextPhase);
    }

    void SetPhase(DayNightPhase phase, bool instant = false)
    {
        DayNightPhase previousPhase = CurrentPhase;
        CurrentPhase = phase;
        m_PhaseTimer = 0f;
        CurrentPhaseProgress = 0f;

        if (!instant && m_HasStarted && previousPhase == DayNightPhase.Night && phase == DayNightPhase.Day)
        {
            CurrentDay++;
            OnDayChanged.Invoke(CurrentDay);
        }

        if (instant)
        {
            ApplyOverlayColor(GetOverlayColorForPhase(CurrentPhase));
        }
        else
        {
            UpdateOverlayColor();
        }

        OnPhaseChanged.Invoke(CurrentPhase);
    }

    void UpdateOverlayColor()
    {
        if (m_ScreenOverlayImage == null || m_Definition == null)
        {
            return;
        }

        Color currentColor = GetOverlayColorForPhase(CurrentPhase);
        Color nextColor = GetOverlayColorForPhase(GetNextPhase(CurrentPhase));
        ApplyOverlayColor(Color.Lerp(currentColor, nextColor, CurrentPhaseProgress));
    }

    void ApplyOverlayColor(Color color)
    {
        if (m_ScreenOverlayImage != null)
        {
            m_ScreenOverlayImage.color = color;
        }
    }

    float GetCurrentPhaseDuration()
    {
        if (m_Definition == null)
        {
            return 1f;
        }

        return CurrentPhase switch
        {
            DayNightPhase.Day => m_Definition.DayDuration,
            DayNightPhase.Dusk => m_Definition.DuskDuration,
            _ => m_Definition.NightDuration
        };
    }

    Color GetOverlayColorForPhase(DayNightPhase phase)
    {
        if (m_Definition == null)
        {
            return Color.clear;
        }

        return phase switch
        {
            DayNightPhase.Day => m_Definition.DayOverlayColor,
            DayNightPhase.Dusk => m_Definition.DuskOverlayColor,
            _ => m_Definition.NightOverlayColor
        };
    }

    DayNightPhase GetNextPhase(DayNightPhase phase)
    {
        return phase switch
        {
            DayNightPhase.Day => DayNightPhase.Dusk,
            DayNightPhase.Dusk => DayNightPhase.Night,
            _ => DayNightPhase.Day
        };
    }

    float GetCycleNormalizedProgress()
    {
        if (m_Definition == null)
        {
            return 0f;
        }

        float elapsed = CurrentPhase switch
        {
            DayNightPhase.Day => m_PhaseTimer,
            DayNightPhase.Dusk => m_Definition.DayDuration + m_PhaseTimer,
            _ => m_Definition.DayDuration + m_Definition.DuskDuration + m_PhaseTimer
        };

        return Mathf.Repeat(elapsed / m_Definition.TotalDuration, 1f);
    }
}
