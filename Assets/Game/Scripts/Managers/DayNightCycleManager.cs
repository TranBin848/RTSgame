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
    [SerializeField] private UnityEngine.Rendering.Universal.Light2D m_GlobalLight;
    [SerializeField] private UnityEngine.Rendering.Volume m_PostProcessingVolume;
    
    private UnityEngine.Rendering.Universal.Vignette m_Vignette;
    private UnityEngine.Rendering.Universal.ColorAdjustments m_ColorAdjustments;
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
        if (m_PostProcessingVolume != null && m_PostProcessingVolume.profile != null)
        {
            m_PostProcessingVolume.profile = Instantiate(m_PostProcessingVolume.profile);
            m_PostProcessingVolume.profile.TryGet(out m_Vignette);
            m_PostProcessingVolume.profile.TryGet(out m_ColorAdjustments);
        }

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
        UpdateEnvironmentEffects();

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
            ApplyInstantEnvironmentEffects(CurrentPhase);
        }
        else
        {
            UpdateEnvironmentEffects();
        }

        OnPhaseChanged.Invoke(CurrentPhase);
    }

    void UpdateEnvironmentEffects()
    {
        if (m_Definition == null) return;

        DayNightPhase nextPhase = GetNextPhase(CurrentPhase);
        float progress = CurrentPhaseProgress;

        if (m_GlobalLight != null)
        {
            m_GlobalLight.color = Color.Lerp(GetLightColorForPhase(CurrentPhase), GetLightColorForPhase(nextPhase), progress);
            m_GlobalLight.intensity = Mathf.Lerp(GetLightIntensityForPhase(CurrentPhase), GetLightIntensityForPhase(nextPhase), progress);
        }

        if (m_Vignette != null)
        {
            m_Vignette.intensity.value = Mathf.Lerp(GetVignetteForPhase(CurrentPhase), GetVignetteForPhase(nextPhase), progress);
        }

        if (m_ColorAdjustments != null)
        {
            m_ColorAdjustments.colorFilter.value = Color.Lerp(GetColorFilterForPhase(CurrentPhase), GetColorFilterForPhase(nextPhase), progress);
        }
    }

    void ApplyInstantEnvironmentEffects(DayNightPhase phase)
    {
        if (m_Definition == null) return;

        if (m_GlobalLight != null)
        {
            m_GlobalLight.color = GetLightColorForPhase(phase);
            m_GlobalLight.intensity = GetLightIntensityForPhase(phase);
        }

        if (m_Vignette != null)
        {
            m_Vignette.intensity.value = GetVignetteForPhase(phase);
        }

        if (m_ColorAdjustments != null)
        {
            m_ColorAdjustments.colorFilter.value = GetColorFilterForPhase(phase);
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

    Color GetLightColorForPhase(DayNightPhase phase)
    {
        if (m_Definition == null) return Color.white;
        return phase switch
        {
            DayNightPhase.Day => m_Definition.DayLightColor,
            DayNightPhase.Dusk => m_Definition.DuskLightColor,
            _ => m_Definition.NightLightColor
        };
    }

    float GetLightIntensityForPhase(DayNightPhase phase)
    {
        if (m_Definition == null) return 1f;
        return phase switch
        {
            DayNightPhase.Day => m_Definition.DayLightIntensity,
            DayNightPhase.Dusk => m_Definition.DuskLightIntensity,
            _ => m_Definition.NightLightIntensity
        };
    }

    float GetVignetteForPhase(DayNightPhase phase)
    {
        if (m_Definition == null) return 0f;
        return phase switch
        {
            DayNightPhase.Day => m_Definition.DayVignette,
            DayNightPhase.Dusk => m_Definition.DuskVignette,
            _ => m_Definition.NightVignette
        };
    }

    Color GetColorFilterForPhase(DayNightPhase phase)
    {
        if (m_Definition == null) return Color.white;
        return phase switch
        {
            DayNightPhase.Day => m_Definition.DayColorFilter,
            DayNightPhase.Dusk => m_Definition.DuskColorFilter,
            _ => m_Definition.NightColorFilter
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
