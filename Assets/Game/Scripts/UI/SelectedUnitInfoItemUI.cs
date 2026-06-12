using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SelectedUnitInfoItemUI : MonoBehaviour
{
    [SerializeField] private Image m_IconImage;
    [SerializeField] private Image m_HealthFillImage;
    [SerializeField] private TextMeshProUGUI m_AttackText;
    private Vector3 m_HealthFillOriginalScale = Vector3.one;
    private bool m_HasCachedHealthFillScale;

    void Awake()
    {
        if (m_HealthFillImage != null)
        {
            m_HealthFillOriginalScale = m_HealthFillImage.rectTransform.localScale;
            m_HasCachedHealthFillScale = true;
        }
    }

    public void Bind(Unit unit)
    {
        if (unit == null)
        {
            return;
        }

        if (m_IconImage != null)
        {
            m_IconImage.sprite = unit.UnitIcon;
            m_IconImage.enabled = m_IconImage.sprite != null;
        }

        if (m_HealthFillImage != null)
        {
            float normalizedHealth = unit.MaxHealth <= 0 ? 0f : Mathf.Clamp01((float)unit.CurrentHealth / unit.MaxHealth);
            if (m_HealthFillImage.type == Image.Type.Filled)
            {
                m_HealthFillImage.fillAmount = normalizedHealth;
            }
            else if (m_HasCachedHealthFillScale)
            {
                Vector3 scale = m_HealthFillOriginalScale;
                scale.x *= normalizedHealth;
                m_HealthFillImage.rectTransform.localScale = scale;
            }
        }

        if (m_AttackText != null)
        {
            m_AttackText.text = unit.AttackDamage.ToString();
        }
    }
}
