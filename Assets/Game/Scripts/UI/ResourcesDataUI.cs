using UnityEngine;
using TMPro;

public class ResourcesDataUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_GoldText;
    [SerializeField] private TextMeshProUGUI m_WoodText;
    [SerializeField] private TextMeshProUGUI m_FoodText;

    public void UpdateResourcesData(int gold, int wood, int food)
    {
        m_GoldText.text = gold.ToString();
        m_WoodText.text = wood.ToString();
        m_FoodText.text = food.ToString();
    }
}