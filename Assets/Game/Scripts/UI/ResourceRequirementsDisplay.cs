using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ResourceRequirementsDisplay : MonoBehaviour
{
    [SerializeField] private GameObject m_RibbonPrefab;
    [SerializeField] private VerticalLayoutGroup m_VerticalLayoutGroup;
    private Dictionary<ResourceType, GameObject> m_ActiveRibbons = new();
    private GameManager m_GameManager;

    private void Awake()
    {
        m_GameManager = GameManager.Get();
        if (m_VerticalLayoutGroup == null)
        {
            m_VerticalLayoutGroup = GetComponent<VerticalLayoutGroup>();
        }
    }

    public void Show(Dictionary<ResourceType, int> resourceCosts)
    {
        ClearRibbons();

        if (resourceCosts == null || resourceCosts.Count == 0)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        foreach (var kvp in resourceCosts)
        {
            ResourceType resourceType = kvp.Key;
            int cost = kvp.Value;

            if (cost <= 0)
            {
                continue;
            }

            CreateRibbon(resourceType, cost);
        }

        if (m_VerticalLayoutGroup != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
        }
    }

    public void Hide()
    {
        ClearRibbons();
        gameObject.SetActive(false);
    }

    private void CreateRibbon(ResourceType resourceType, int cost)
    {
        if (m_RibbonPrefab == null)
        {
            Debug.LogWarning("Ribbon prefab not assigned to ResourceRequirementsDisplay.");
            return;
        }

        GameObject ribbonGO = Instantiate(m_RibbonPrefab, transform);
        m_ActiveRibbons[resourceType] = ribbonGO;

        GameObject imageObject = ribbonGO.transform.Find("Img_Icon")?.gameObject;
        Image iconImage = imageObject?.GetComponent<Image>();
        iconImage.sprite = GetResourceIcon(resourceType);
        // Image iconImage = ribbonGO.GetComponentInChildren<Image>();  // Lấy con
        // iconImage.sprite = GetResourceIcon(resourceType);

        // Lấy text component của con
        TextMeshProUGUI costText = ribbonGO.GetComponentInChildren<TextMeshProUGUI>();

        if (costText != null)
        {
            costText.text = cost.ToString();
            UpdateRibbonColor(costText, resourceType, cost);
        }
    }

    private void ClearRibbons()
    {
        foreach (var ribbon in m_ActiveRibbons.Values)
        {
            Destroy(ribbon);
        }
        m_ActiveRibbons.Clear();
    }

    private void UpdateRibbonColor(TextMeshProUGUI text, ResourceType resourceType, int cost)
    {
        if (m_GameManager == null)
        {
            return;
        }

        int available = resourceType switch
        {
            ResourceType.Gold => m_GameManager.Gold,
            ResourceType.Wood => m_GameManager.Wood,
            ResourceType.Meat => m_GameManager.Meat,
            _ => 0
        };

        text.color = available >= cost ? Color.green : Color.red;
    }

    private Sprite GetResourceIcon(ResourceType resourceType)
    {
        // Load icon from Resources folder based on resource type
        string iconPath = $"Icons/{resourceType}Icon";
        Sprite icon = Resources.Load<Sprite>(iconPath);
        if (icon == null)
        {
            Debug.LogWarning($"Icon not found at {iconPath}");
        }
        return icon;
    }
}
