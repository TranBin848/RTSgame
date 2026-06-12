using UnityEngine;
using UnityEngine.UI;

public class DayNightClockUI : MonoBehaviour
{
    [SerializeField] private DayNightCycleManager m_DayNightCycleManager;
    [SerializeField] private Image m_DaySliceImage;
    [SerializeField] private Image m_DuskSliceImage;
    [SerializeField] private Image m_NightSliceImage;
    [SerializeField] private RectTransform m_NeedleRectTransform;
    [SerializeField] private float m_StartAngle = 90f;
    [SerializeField] private bool m_Clockwise = true;
    [SerializeField] private bool m_UseEditorSliceRotation = false;
    [SerializeField] private float m_NeedleAngleOffset = 0f;

    void Start()
    {
        if (m_DayNightCycleManager == null)
        {
            m_DayNightCycleManager = FindFirstObjectByType<DayNightCycleManager>();
        }

        ConfigureSlices();
        UpdateClockVisuals();
    }

    void Update()
    {
        UpdateClockVisuals();
    }

    void ConfigureSlices()
    {
        if (m_DayNightCycleManager == null || m_DayNightCycleManager.Definition == null)
        {
            return;
        }

        float totalDuration = m_DayNightCycleManager.Definition.TotalDuration;
        float duskFraction = m_DayNightCycleManager.Definition.DuskDuration / totalDuration;
        float nightFraction = m_DayNightCycleManager.Definition.NightDuration / totalDuration;
        float duskStartFraction = m_DayNightCycleManager.Definition.DayDuration / totalDuration;
        float nightStartFraction = (m_DayNightCycleManager.Definition.DayDuration + m_DayNightCycleManager.Definition.DuskDuration) / totalDuration;

        ConfigureBaseSlice(m_DaySliceImage);
        ConfigureSlice(m_DuskSliceImage, duskFraction, GetSliceStartAngle(duskStartFraction));
        ConfigureSlice(m_NightSliceImage, nightFraction, GetSliceStartAngle(nightStartFraction));
    }

    void ConfigureBaseSlice(Image sliceImage)
    {
        if (sliceImage == null)
        {
            return;
        }

        if (!m_UseEditorSliceRotation)
        {
            sliceImage.rectTransform.localEulerAngles = new Vector3(0f, 0f, GetSliceRotation(0f));
        }
    }

    void ConfigureSlice(Image sliceImage, float fillAmount, float startAngle)
    {
        if (sliceImage == null)
        {
            return;
        }


        sliceImage.fillClockwise = m_Clockwise;
        sliceImage.fillAmount = Mathf.Clamp01(fillAmount);
        if (!m_UseEditorSliceRotation)
        {
            sliceImage.rectTransform.localEulerAngles = new Vector3(0f, 0f, GetSliceRotation(startAngle));
        }
    }

    void UpdateClockVisuals()
    {
        if (m_DayNightCycleManager == null)
        {
            return;
        }

        if (m_DayNightCycleManager.Definition != null)
        {
            ConfigureSlices();
        }

        if (m_NeedleRectTransform != null)
        {
            float normalizedProgress = m_DayNightCycleManager.CycleNormalizedProgress;
            float direction = m_Clockwise ? -1f : 1f;
            float angle = m_StartAngle + m_NeedleAngleOffset + (normalizedProgress * 360f * direction);
            m_NeedleRectTransform.localEulerAngles = new Vector3(0f, 0f, angle);
        }
    }

    float GetSliceStartAngle(float elapsedFraction)
    {
        return Mathf.Repeat(elapsedFraction, 1f) * 360f;
    }

    float GetSliceRotation(float startAngle)
    {
        float startOffset = m_StartAngle - 90f;
        float direction = m_Clockwise ? -1f : 1f;
        return startOffset + (startAngle * direction);
    }


}
