using TMPro;
using UnityEngine;
public class ResourceRequirementsDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_GoldText;
    [SerializeField] private TextMeshProUGUI m_WoodText;

    public void Show(int reqGold, int reqWood)
    {
        m_GoldText.text = reqGold.ToString();
        m_WoodText.text = reqWood.ToString();
        gameObject.SetActive(true);
        UpdateColorRequirement(reqGold, reqWood);
    }
    public void Hide()
    {
        gameObject.SetActive(false);
    }
    void UpdateColorRequirement(int reqGold, int reqWood)
    {
        var manager = GameManager.Get();
        m_GoldText.color = manager.Gold >= reqGold ? Color.green : Color.red;
        m_WoodText.color = manager.Wood >= reqWood ? Color.green : Color.red;
    }
}