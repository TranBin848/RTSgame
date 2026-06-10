using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ActionQueuePanel : MonoBehaviour
{
    [SerializeField] private HorizontalLayoutGroup m_LayoutGroup;
    [SerializeField] private ActionQueueItemUI m_ItemPrefab;
    [SerializeField] private bool m_HideWhenEmpty = true;

    private readonly List<QueueEntry> m_QueueEntries = new();
    private readonly Dictionary<string, QueueEntry> m_EntryLookup = new();
    private int m_NextQueueId;

    private void Awake()
    {
        if (m_LayoutGroup == null)
        {
            m_LayoutGroup = GetComponent<HorizontalLayoutGroup>();
        }

        ConfigureLayoutGroup();
        RefreshVisibility();
    }

    public string Enqueue(Sprite icon, int count = 1)
    {
        if (m_ItemPrefab == null)
        {
            Debug.LogWarning("ActionQueuePanel is missing item prefab.");
            return string.Empty;
        }

        string queueId = $"queue_{m_NextQueueId++}";
        ActionQueueItemUI itemInstance = Instantiate(m_ItemPrefab, transform);
        itemInstance.Init(queueId, icon, Mathf.Max(1, count));

        QueueEntry entry = new QueueEntry(queueId, itemInstance, Mathf.Max(1, count));
        m_QueueEntries.Add(entry);
        m_EntryLookup.Add(queueId, entry);

        RefreshLayout();
        return queueId;
    }

    public string Enqueue(ActionSO action, int count = 1)
    {
        if (action == null)
        {
            Debug.LogWarning("Cannot enqueue null action.");
            return string.Empty;
        }

        return Enqueue(action.Icon, count);
    }

    public void SetProgress(string queueId, float normalizedProgress)
    {
        if (!TryGetEntry(queueId, out var entry))
        {
            return;
        }

        entry.ItemUI.SetProgress(normalizedProgress);
    }

    public void SetCount(string queueId, int count)
    {
        if (!TryGetEntry(queueId, out var entry))
        {
            return;
        }

        entry.Count = Mathf.Max(1, count);
        entry.ItemUI.SetCount(entry.Count);
    }

    public void CompleteFront()
    {
        if (m_QueueEntries.Count == 0)
        {
            return;
        }

        RemoveEntryAt(0);
    }

    public void Complete(string queueId)
    {
        if (!TryGetEntry(queueId, out var entry))
        {
            return;
        }

        int entryIndex = m_QueueEntries.IndexOf(entry);
        if (entryIndex < 0)
        {
            return;
        }

        RemoveEntryAt(entryIndex);
    }

    public void Cancel(string queueId)
    {
        Complete(queueId);
    }

    public void Clear()
    {
        for (int i = m_QueueEntries.Count - 1; i >= 0; i--)
        {
            RemoveEntryAt(i);
        }
    }

    public bool HasEntries()
    {
        return m_QueueEntries.Count > 0;
    }

    private bool TryGetEntry(string queueId, out QueueEntry entry)
    {
        if (string.IsNullOrWhiteSpace(queueId))
        {
            entry = null;
            return false;
        }

        return m_EntryLookup.TryGetValue(queueId, out entry);
    }

    private void RemoveEntryAt(int index)
    {
        QueueEntry entry = m_QueueEntries[index];
        m_QueueEntries.RemoveAt(index);
        m_EntryLookup.Remove(entry.QueueId);

        if (entry.ItemUI != null)
        {
            Destroy(entry.ItemUI.gameObject);
        }

        RefreshLayout();
    }

    private void RefreshLayout()
    {
        if (m_LayoutGroup != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);
        }

        RefreshVisibility();
    }

    private void RefreshVisibility()
    {
        if (m_HideWhenEmpty)
        {
            gameObject.SetActive(m_QueueEntries.Count > 0);
        }
    }

    private void ConfigureLayoutGroup()
    {
        if (m_LayoutGroup == null)
        {
            return;
        }

        // m_LayoutGroup.spacing = Mathf.Max(0f, m_LayoutGroup.spacing);
        // m_LayoutGroup.childForceExpandWidth = false;
        // m_LayoutGroup.childForceExpandHeight = false;
        // m_LayoutGroup.childControlWidth = false;
        // m_LayoutGroup.childControlHeight = false;
    }

    [Serializable]
    private class QueueEntry
    {
        public string QueueId;
        public ActionQueueItemUI ItemUI;
        public int Count;

        public QueueEntry(string queueId, ActionQueueItemUI itemUI, int count)
        {
            QueueId = queueId;
            ItemUI = itemUI;
            Count = count;
        }
    }
}
