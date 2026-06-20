using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WaveProgressUI : MonoBehaviour
{
    [SerializeField] private EnemyWaveManager m_WaveManager;
    [SerializeField] private RectTransform m_RevealRoot;
    [SerializeField] private CanvasGroup m_CanvasGroup;
    [SerializeField] private Image m_FillImage;
    [SerializeField] private RectTransform m_IconRoot;
    [SerializeField] private WaveProgressIconUI m_WaveIconPrefab;
    [SerializeField] private Sprite m_WaveIconSprite;
    [SerializeField] private float m_RevealDuration = 0.35f;
    [SerializeField] private float m_HideDuration = 0.2f;
    [SerializeField] private float m_IconHorizontalPadding = 8f;

    private readonly List<WaveProgressIconUI> m_WaveIcons = new();
    private int m_LastWaveCount = -1;
    private float m_RevealAmount;

    void Awake()
    {
        if (m_RevealRoot == null)
        {
            m_RevealRoot = transform as RectTransform;
        }

        ConfigureFillImage();
        SetRevealAmount(0f);
    }

    void Start()
    {
        if (m_WaveManager == null)
        {
            m_WaveManager = FindFirstObjectByType<EnemyWaveManager>();
        }

        if (m_WaveManager != null)
        {
            m_WaveManager.OnWaveScheduleChanged += RebuildWaveIcons;
            m_WaveManager.OnWaveStarted += HandleWaveStarted;
            RebuildWaveIcons();
        }
    }

    void OnDestroy()
    {
        if (m_WaveManager != null)
        {
            m_WaveManager.OnWaveScheduleChanged -= RebuildWaveIcons;
            m_WaveManager.OnWaveStarted -= HandleWaveStarted;
        }
    }

    void Update()
    {
        if (m_WaveManager == null)
        {
            SetRevealAmount(Mathf.MoveTowards(m_RevealAmount, 0f, Time.deltaTime / Mathf.Max(0.0001f, m_HideDuration)));
            return;
        }

        bool shouldShow = m_WaveManager.IsNightWaveActive;
        float duration = shouldShow ? m_RevealDuration : m_HideDuration;
        float targetReveal = shouldShow ? 1f : 0f;
        SetRevealAmount(Mathf.MoveTowards(m_RevealAmount, targetReveal, Time.deltaTime / Mathf.Max(0.0001f, duration)));

        if (m_FillImage != null)
        {
            m_FillImage.fillAmount = shouldShow ? m_WaveManager.NightProgress : 0f;
        }

        if (shouldShow && m_LastWaveCount != m_WaveManager.CurrentWaveCount)
        {
            RebuildWaveIcons();
        }

        RefreshIconPopStates();
    }

    void ConfigureFillImage()
    {
        if (m_FillImage == null)
        {
            return;
        }

        m_FillImage.type = Image.Type.Filled;
        m_FillImage.fillMethod = Image.FillMethod.Horizontal;
        m_FillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        m_FillImage.fillAmount = 0f;
    }

    void RebuildWaveIcons()
    {
        ClearWaveIcons();

        if (m_WaveManager == null || m_WaveIconPrefab == null || m_IconRoot == null)
        {
            m_LastWaveCount = 0;
            return;
        }

        int waveCount = m_WaveManager.CurrentWaveCount;
        m_LastWaveCount = waveCount;
        float width = Mathf.Max(0f, m_IconRoot.rect.width - (m_IconHorizontalPadding * 2f));

        for (int i = 0; i < waveCount; i++)
        {
            WaveProgressIconUI icon = Instantiate(m_WaveIconPrefab, m_IconRoot);
            icon.Initialize(m_WaveIconSprite);

            RectTransform iconRect = icon.transform as RectTransform;
            if (iconRect != null)
            {
                iconRect.anchorMin = new Vector2(0f, 0.5f);
                iconRect.anchorMax = new Vector2(0f, 0.5f);
                iconRect.pivot = new Vector2(0.5f, 0.5f);

                float normalizedPosition = m_WaveManager.GetWaveStartNormalized(i);
                float xPosition = m_IconHorizontalPadding + normalizedPosition * width;
                iconRect.anchoredPosition = new Vector2(xPosition, iconRect.anchoredPosition.y);
            }

            icon.SetPopped(i < m_WaveManager.StartedWaveCount, true);
            m_WaveIcons.Add(icon);
        }
    }

    void ClearWaveIcons()
    {
        foreach (var icon in m_WaveIcons)
        {
            if (icon != null)
            {
                Destroy(icon.gameObject);
            }
        }

        m_WaveIcons.Clear();
    }

    void HandleWaveStarted(int waveIndex)
    {
        if (waveIndex < 0 || waveIndex >= m_WaveIcons.Count)
        {
            return;
        }

        m_WaveIcons[waveIndex].SetPopped(true);
    }

    void RefreshIconPopStates()
    {
        if (m_WaveManager == null)
        {
            return;
        }

        for (int i = 0; i < m_WaveIcons.Count; i++)
        {
            if (m_WaveIcons[i] != null)
            {
                m_WaveIcons[i].SetPopped(i < m_WaveManager.StartedWaveCount);
            }
        }
    }

    void SetRevealAmount(float revealAmount)
    {
        m_RevealAmount = Mathf.Clamp01(revealAmount);

        if (m_RevealRoot != null)
        {
            Vector3 scale = m_RevealRoot.localScale;
            scale.x = m_RevealAmount;
            scale.y = 1f;
            scale.z = 1f;
            m_RevealRoot.localScale = scale;
        }

        if (m_CanvasGroup != null)
        {
            m_CanvasGroup.alpha = m_RevealAmount;
            m_CanvasGroup.blocksRaycasts = m_RevealAmount > 0.99f;
            m_CanvasGroup.interactable = m_RevealAmount > 0.99f;
        }
    }
}
