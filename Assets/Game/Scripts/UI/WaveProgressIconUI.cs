using UnityEngine;
using UnityEngine.UI;

public class WaveProgressIconUI : MonoBehaviour
{
    [SerializeField] private Image m_IconImage;
    [SerializeField] private float m_PopDuration = 0.22f;
    [SerializeField] private float m_PopOvershoot = 0.25f;

    private bool m_IsPopped;
    private float m_PopTimer;

    void Awake()
    {
        if (m_IconImage == null)
        {
            m_IconImage = GetComponent<Image>();
        }

        transform.localScale = Vector3.zero;
    }

    void Update()
    {
        if (!m_IsPopped)
        {
            transform.localScale = Vector3.zero;
            return;
        }

        m_PopTimer += Time.deltaTime;
        float normalizedTime = m_PopDuration <= 0.0001f ? 1f : Mathf.Clamp01(m_PopTimer / m_PopDuration);
        float overshoot = Mathf.Sin(normalizedTime * Mathf.PI) * m_PopOvershoot;
        float scale = Mathf.Lerp(0f, 1f, normalizedTime) + overshoot;
        transform.localScale = Vector3.one * scale;
    }

    public void Initialize(Sprite icon)
    {
        if (m_IconImage == null)
        {
            m_IconImage = GetComponent<Image>();
        }

        if (m_IconImage != null && icon != null)
        {
            m_IconImage.sprite = icon;
        }

        SetPopped(false, true);
    }

    public void SetPopped(bool isPopped, bool instant = false)
    {
        if (m_IsPopped == isPopped && !instant)
        {
            return;
        }

        m_IsPopped = isPopped;
        m_PopTimer = instant && isPopped ? m_PopDuration : 0f;
        transform.localScale = isPopped && instant ? Vector3.one : Vector3.zero;
    }
}
