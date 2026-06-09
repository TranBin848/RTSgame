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

    void Awake()
    {
        m_OriginalBackgroundColor = m_BackgroundImage.color;
        Hide();
        HideRequirements();
    }
    public void RegisterAction(Sprite icon, UnityAction action)
    {
        var actionButton = Instantiate(m_ActionButtonPrefab, transform);
        m_ActionButtons.Add(actionButton);
        actionButton.Init(icon, action, () => FocusAction(m_ActionButtons.IndexOf(actionButton)));
    }
    public void ClearActions()
    {
        foreach (var button in m_ActionButtons)
        {
            Destroy(button.gameObject);
        }
        m_ActionButtons.Clear();
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
    }
    public void Show()
    {
        m_BackgroundImage.color = m_OriginalBackgroundColor;
    }
    public void Hide()
    {
        m_BackgroundImage.color = new Color(0, 0, 0, 0);
    }
    public void ShowRequirements(int gold, int wood)
    {
        if (m_ResourceRequirementsDisplay == null)
        {
            return;
        }
        m_ResourceRequirementsDisplay.Show(gold, wood);
    }
    public void HideRequirements()
    {
        if (m_ResourceRequirementsDisplay == null)
        {
            return;
        }
        m_ResourceRequirementsDisplay.Hide();
    }
}