using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ActionBar : MonoBehaviour
{
    [SerializeField] private Image m_BackgroundImage;
    [SerializeField] private ActionButton m_ActionButtonPrefab;
    [SerializeField] private ResourceRequirementsDisplay m_ResourceRequirementsDisplay;
    private Color m_OriginalBackgroundColor;
    private List<ActionButton> m_ActionButtons = new();
    private List<ActionSO> m_ActionSOList = new();

    void Awake()
    {
        m_OriginalBackgroundColor = m_BackgroundImage.color;
        Hide();
        HideRequirements();
    }
    public void RegisterAction(Sprite icon, ActionSO action, UnityAction actionCallback)
    {
        var actionButton = Instantiate(m_ActionButtonPrefab, transform);
        m_ActionButtons.Add(actionButton);
        m_ActionSOList.Add(action);
        actionButton.Init(icon, actionCallback, () => FocusAction(m_ActionButtons.IndexOf(actionButton)));
    }
    public void ClearActions()
    {
        foreach (var button in m_ActionButtons)
        {
            Destroy(button.gameObject);
        }
        m_ActionButtons.Clear();
        m_ActionSOList.Clear();
        HideRequirements();
    }
    public void FocusAction(int idx)
    {
        if (idx < 0 || idx >= m_ActionButtons.Count) return;

        foreach (var button in m_ActionButtons)
        {
            button.Unfocus();
        }

        m_ActionButtons[idx].Focus();

        // Show requirements for the focused action
        if (idx < m_ActionSOList.Count && m_ActionSOList[idx] != null)
        {
            ShowRequirements(m_ActionSOList[idx]);
        }
    }
    public void Show()
    {
        m_BackgroundImage.color = m_OriginalBackgroundColor;
    }
    public void Hide()
    {
        m_BackgroundImage.color = new Color(0, 0, 0, 0);
    }
    public void ShowRequirements(Dictionary<ResourceType, int> resourceCosts)
    {
        if (m_ResourceRequirementsDisplay == null)
        {
            return;
        }
        m_ResourceRequirementsDisplay.Show(resourceCosts);
    }

    public void ShowRequirements(ActionSO action)
    {
        if (m_ResourceRequirementsDisplay == null || action == null)
        {
            return;
        }

        var resourceCosts = ExtractResourceCosts(action);
        m_ResourceRequirementsDisplay.Show(resourceCosts);
    }

    public void HideRequirements()
    {
        if (m_ResourceRequirementsDisplay == null)
        {
            return;
        }
        m_ResourceRequirementsDisplay.Hide();
    }

    private Dictionary<ResourceType, int> ExtractResourceCosts(ActionSO action)
    {
        var costs = new Dictionary<ResourceType, int>();

        if (action is BuildActionSo buildAction)
        {
            if (buildAction.GoldCost > 0)
                costs[ResourceType.Gold] = buildAction.GoldCost;
            if (buildAction.WoodCost > 0)
                costs[ResourceType.Wood] = buildAction.WoodCost;
        }
        else if (action is SpawnVillagerActionSO spawnAction)
        {
            if (spawnAction.MeatCost > 0)
                costs[ResourceType.Meat] = spawnAction.MeatCost;
        }

        return costs;
    }
}