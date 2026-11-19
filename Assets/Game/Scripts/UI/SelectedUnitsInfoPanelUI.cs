using System.Collections.Generic;
using UnityEngine;

public class SelectedUnitsInfoPanelUI : MonoBehaviour
{
    [SerializeField] private GameObject m_ContentRoot;
    [SerializeField] private SelectedUnitInfoItemUI m_ItemPrefab;

    private readonly List<SelectedUnitInfoItemUI> m_SpawnedItems = new();
    private GameManager m_GameManager;

    void Awake()
    {
        if (m_ContentRoot == null)
        {
            m_ContentRoot = gameObject;
        }

        // SetVisible(false);
    }

    bool m_IsInitialized;
    public void Initialize(GameManager game)
    {
        if (game == null) throw new System.ArgumentException("Presentation dependencies are required.");
        if (m_IsInitialized)
        {
            if (m_GameManager != game) throw new System.InvalidOperationException("Presentation already initialized with different dependencies.");
            return;
        }
        m_GameManager = game;
        m_GameManager.OnSelectionChanged += HandleSelectionChanged;
        HandleSelectionChanged(m_GameManager.SelectedUnits);
        m_IsInitialized = true;
    }

    void OnDestroy()
    {
        if (m_GameManager != null)
        {
            m_GameManager.OnSelectionChanged -= HandleSelectionChanged;
        }
    }

    void Update()
    {
        if (m_GameManager == null || m_SpawnedItems.Count == 0)
        {
            return;
        }

        IReadOnlyList<Unit> selectedUnits = m_GameManager.SelectedUnits;
        int itemCount = Mathf.Min(m_SpawnedItems.Count, selectedUnits.Count);
        for (int i = 0; i < itemCount; i++)
        {
            m_SpawnedItems[i].Bind(selectedUnits[i]);
        }
    }

    void HandleSelectionChanged(IReadOnlyList<Unit> selectedUnits)
    {
        ClearItems();

        if (selectedUnits == null || selectedUnits.Count == 0 || m_ItemPrefab == null || m_ContentRoot == null)
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);

        foreach (var unit in selectedUnits)
        {
            if (unit == null || unit.CurrentState == UnitState.Dead)
            {
                continue;
            }

            var item = Instantiate(m_ItemPrefab, m_ContentRoot.transform);
            item.Bind(unit);
            m_SpawnedItems.Add(item);
        }

        SetVisible(m_SpawnedItems.Count > 0);
    }

    void ClearItems()
    {
        foreach (var item in m_SpawnedItems)
        {
            if (item != null)
            {
                Destroy(item.gameObject);
            }
        }

        m_SpawnedItems.Clear();
    }

    void SetVisible(bool isVisible)
    {
        if (m_ContentRoot != null)
        {
            m_ContentRoot.SetActive(isVisible);
        }
        else
        {
            gameObject.SetActive(isVisible);
        }
    }
}
