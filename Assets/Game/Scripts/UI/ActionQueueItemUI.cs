using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ActionQueueItemUI : MonoBehaviour
{
    [SerializeField] private Image m_IconImage;
    [SerializeField] private Image m_ProgressFillImage;
    [SerializeField] private GameObject m_CountRoot;
    [SerializeField] private TextMeshProUGUI m_CountText;
    [SerializeField] private bool m_UseRadialClockwiseFill = true;
    [SerializeField] private Image.Origin360 m_FillOrigin = Image.Origin360.Top;

    public string QueueId { get; private set; }

    private void Awake()
    {
        ConfigureProgressFill();
    }

    public void Init(string queueId, Sprite icon, int count)
    {
        QueueId = queueId;

        if (m_IconImage != null)
        {
            m_IconImage.sprite = icon;
        }

        EnsureProgressFillSprite(icon);

        SetProgress(0f);
        SetCount(count);
    }

    public void SetProgress(float normalizedProgress)
    {
        if (m_ProgressFillImage == null)
        {
            return;
        }

        float clampedProgress = Mathf.Clamp01(normalizedProgress);
        m_ProgressFillImage.fillAmount = 1f - clampedProgress;
    }

    public void SetCount(int count)
    {
        bool shouldShowCount = count > 1;

        if (m_CountRoot != null)
        {
            m_CountRoot.SetActive(shouldShowCount);
        }

        if (m_CountText != null)
        {
            m_CountText.text = count.ToString();
        }
    }

    private void ConfigureProgressFill()
    {
        if (m_ProgressFillImage == null || !m_UseRadialClockwiseFill)
        {
            return;
        }

        m_ProgressFillImage.type = Image.Type.Filled;
        m_ProgressFillImage.fillMethod = Image.FillMethod.Radial360;
        m_ProgressFillImage.fillClockwise = true;
        m_ProgressFillImage.fillOrigin = (int)m_FillOrigin;
        m_ProgressFillImage.fillAmount = 1f;
    }

    private void EnsureProgressFillSprite(Sprite fallbackSprite)
    {
        if (m_ProgressFillImage == null)
        {
            return;
        }

        if (m_ProgressFillImage.sprite == null)
        {
            m_ProgressFillImage.sprite = fallbackSprite;
        }

        m_ProgressFillImage.enabled = m_ProgressFillImage.sprite != null;
    }
}
