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
    EnvironmentPresentationDefinition m_Presentation;
    UnityEngine.Rendering.VolumeProfile m_RuntimeProfile;
    public EnvironmentPresentationDefinition Presentation => m_Presentation;
    [SerializeField] private DayNightPhase m_StartingPhase = DayNightPhase.Day;
    [SerializeField] private int m_StartingDay = 1;

    public UnityAction<DayNightPhase> OnPhaseChanged = delegate { };
    public UnityAction<int> OnDayChanged = delegate { };
    public DayNightPhase CurrentPhase { get; private set; }
    public int CurrentDay { get; private set; } = 1;
    public float CurrentPhaseProgress { get; private set; }
    public DayNightCycleDefinition Definition => m_Definition;
    // The candidate grades lit, unlit and instanced sprites together; avoid a second tint on instances.
    public Color EnvironmentTint => m_Presentation != null ? Color.white
        : m_GlobalLight != null ? m_GlobalLight.color * Mathf.Max(0.35f, m_GlobalLight.intensity) : Color.white;
    public float CycleNormalizedProgress => GetCycleNormalizedProgress();

    RTS.DOTS.GameplaySimulationSystem m_Match;
    public void InitializeMatch(RTS.DOTS.GameplaySimulationSystem match)
    {
        if (match == null) throw new System.ArgumentNullException(nameof(match));
        if (m_Match != null && m_Match != match) throw new System.InvalidOperationException("Clock already belongs to another match.");
        m_Match = match;
    }

    private float m_PhaseTimer;
    public bool SnapshotFenced { get; set; }
    public float SavePhaseElapsed => m_PhaseTimer;
    private bool m_HasStarted;

    public void InitializePresentation(EnvironmentPresentationDefinition presentation, Camera viewCamera)
    {
        if (presentation == null) throw new System.ArgumentNullException(nameof(presentation));
        if (m_Presentation != null && m_Presentation != presentation)
            throw new System.InvalidOperationException("Clock already owns another presentation palette.");
        presentation.Validate();
        var profile = m_PostProcessingVolume != null ? m_PostProcessingVolume.sharedProfile : null;
        var cameraData = viewCamera != null ? viewCamera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>() : null;
        if (m_GlobalLight == null || profile == null || cameraData == null || !cameraData.renderPostProcessing
            || viewCamera.gameObject.scene != gameObject.scene || !m_PostProcessingVolume.enabled
            || !m_PostProcessingVolume.gameObject.activeInHierarchy
            || !m_PostProcessingVolume.isGlobal || m_PostProcessingVolume.weight != 1
            || (cameraData.volumeLayerMask.value & (1 << m_PostProcessingVolume.gameObject.layer)) == 0
            || !profile.TryGet<UnityEngine.Rendering.Universal.ColorAdjustments>(out _)
            || !profile.TryGet<UnityEngine.Rendering.Universal.Vignette>(out _))
            throw new System.InvalidOperationException("Environment grading requires camera post processing, Global Light and an active global volume with ColorAdjustments/Vignette.");
        m_Presentation = presentation;
        if (m_HasStarted) UpdateEnvironmentEffects();
    }

    void Start()
    {
        if (m_PostProcessingVolume != null && m_PostProcessingVolume.sharedProfile != null)
        {
            m_RuntimeProfile = ScriptableObject.CreateInstance<UnityEngine.Rendering.VolumeProfile>();
            foreach (var component in m_PostProcessingVolume.sharedProfile.components)
                m_RuntimeProfile.components.Add(Instantiate(component));
            m_PostProcessingVolume.profile = m_RuntimeProfile;
            m_PostProcessingVolume.profile.TryGet(out m_Vignette);
            m_PostProcessingVolume.profile.TryGet(out m_ColorAdjustments);
        }

        if (MatchLoadBootstrap.IsLoading)
        {
            RestoreLoadedDay(MatchLoadBootstrap.Pending.Clock);
            return;
        }
        CurrentDay = Mathf.Max(1, m_StartingDay);
        OnDayChanged.Invoke(CurrentDay);
        SetPhase(m_StartingPhase, true);
        m_HasStarted = true;
    }

    void Update() => Tick(Time.deltaTime);
    public void RestoreLoadedDay(RTS.SaveLoad.ClockSaveData state)
    {
        if (state.Day != 1 || state.Phase != (int)DayNightPhase.Day || state.Elapsed >= m_Definition.DayDuration)
            throw new System.InvalidOperationException("SL4 requires a valid first-day clock.");
        CurrentDay = state.Day; CurrentPhase = (DayNightPhase)state.Phase; m_PhaseTimer = state.Elapsed;
        CurrentPhaseProgress = Mathf.Clamp01(m_PhaseTimer / m_Definition.DayDuration); m_HasStarted = true;
        UpdateEnvironmentEffects();
    }

    void Tick(float deltaTime)
    {
        if (SnapshotFenced) return;
        if (m_Match?.World.GetExistingSystemManaged<RTS.DOTS.SwarmSimulationSystem>()?.Paused == true) return;
        if (m_Match?.IsMatchOver == true || deltaTime <= 0 || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime)) return;
        if (ProceduralMapRuntime.IsCompositionBlocked || (ProceduralMapRuntime.IsActive && !ProceduralMapRuntime.IsReady))
        {
            return;
        }

        if (m_Definition == null)
        {
            return;
        }

        m_PhaseTimer += deltaTime;
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
        if (m_Presentation != null)
        {
            ApplyPalette(m_Presentation.Evaluate(CurrentPhase, CurrentPhaseProgress));
            return;
        }
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
        if (m_Presentation != null)
        {
            ApplyPalette(m_Presentation.Evaluate(phase, 0));
            return;
        }
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

    void ApplyPalette(EnvironmentPresentationDefinition.Palette palette)
    {
        // Neutral ambient keeps the lit terrain on the same grading path as unlit actors.
        m_GlobalLight.color = Color.white;
        m_GlobalLight.intensity = 1;
        if (m_ColorAdjustments != null)
        {
            m_ColorAdjustments.active = true;
            m_ColorAdjustments.colorFilter.Override(palette.Filter);
            m_ColorAdjustments.postExposure.Override(Mathf.Log(palette.Intensity, 2));
        }
        if (m_Vignette != null)
        {
            m_Vignette.active = true;
            m_Vignette.intensity.Override(palette.Vignette);
        }
    }

    void OnDestroy()
    {
        if (m_RuntimeProfile != null)
        {
            if (m_PostProcessingVolume != null) m_PostProcessingVolume.profile = null;
            foreach (var component in m_RuntimeProfile.components) Destroy(component);
            Destroy(m_RuntimeProfile);
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
