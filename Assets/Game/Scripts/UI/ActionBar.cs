using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ActionBar : MonoBehaviour
{
    [SerializeField] private Image m_BackgroundImage;
    [SerializeField] private ActionButton m_ActionButtonPrefab;
    private Color m_OriginalBackgroundColor;
    private List<ActionButton> m_ActionButtons = new();

    void Awake()
    {
        m_OriginalBackgroundColor = m_BackgroundImage.color;
        Hide();
    }
    public void RegisterAction(Sprite icon, UnityAction action)
    {
        var actionButton = Instantiate(m_ActionButtonPrefab, transform);
        actionButton.Init(icon, action);
        m_ActionButtons.Add(actionButton);
    }
    public void ClearActions()
    {
        foreach (var button in m_ActionButtons)
        {
            Destroy(button.gameObject);
        }
        m_ActionButtons.Clear();
    }
    public void Show()
    {
        m_BackgroundImage.color = m_OriginalBackgroundColor;
    }
    public void Hide()
    {
        m_BackgroundImage.color = new Color(0, 0, 0, 0);
    }
}