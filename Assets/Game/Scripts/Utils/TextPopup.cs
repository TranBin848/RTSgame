using TMPro;
using UnityEngine;

public class TextPopup : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_Text;
    [SerializeField] private float m_Duration = 1f;
    [SerializeField] private AnimationCurve m_FontSizeCurve;
    [SerializeField] private AnimationCurve m_XOffsetCurve;
    [SerializeField] private AnimationCurve m_YOffsetCurve;
    [SerializeField] private AnimationCurve m_AlphaCurve;
    private float elapsedTime = 0f;
    public void SetText(string text, Color color)
    {
        m_Text.text = text;
        m_Text.color = color;
    }
    private void Update()
    {
        elapsedTime += Time.deltaTime;
        var normalizedTime = elapsedTime / m_Duration;
        if (normalizedTime >= 1f)
        {
            Destroy(gameObject);
            return;
        }
        var alpha = m_AlphaCurve.Evaluate(normalizedTime);
        m_Text.fontSize += m_FontSizeCurve.Evaluate(normalizedTime) / 5;
        m_Text.color = new Color(m_Text.color.r, m_Text.color.g, m_Text.color.b, alpha);
        float xOffset = m_XOffsetCurve.Evaluate(normalizedTime);
        float yOffset = m_YOffsetCurve.Evaluate(normalizedTime);

        transform.position += new Vector3(xOffset, yOffset, 0f) * Time.deltaTime;
    }
}