using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IdleWorkerPanelUI : MonoBehaviour
{
    [SerializeField] private GameObject m_ContentRoot;
    [SerializeField] private Button m_Button;
    [SerializeField] private Image m_IconImage;
    [SerializeField] private TextMeshProUGUI m_CountText;
    [SerializeField] private float m_RefreshInterval = 0.25f;

    private GameManager m_GameManager;
    private CameraController m_CameraController;
    private readonly List<WorkerUnit> m_CachedIdleWorkers = new();
    private float m_NextRefreshTime;
    private int m_FocusIndex;

    void Awake()
    {
        if (m_ContentRoot == null)
        {
            m_ContentRoot = m_Button != null ? m_Button.gameObject : gameObject;
        }

        if (m_Button != null)
        {
            m_Button.onClick.RemoveAllListeners();
            m_Button.onClick.AddListener(FocusNextIdleWorker);
        }
    }

    void Start()
    {
        m_GameManager = GameManager.Get();
        m_CameraController = FindFirstObjectByType<CameraController>();
        RefreshIdleWorkers(true);
    }

    void OnDestroy()
    {
        if (m_Button != null)
        {
            m_Button.onClick.RemoveAllListeners();
        }
    }

    void Update()
    {
        if (Time.time >= m_NextRefreshTime)
        {
            RefreshIdleWorkers();
        }
    }

    void RefreshIdleWorkers(bool forceResetIndex = false)
    {
        m_NextRefreshTime = Time.time + Mathf.Max(0.05f, m_RefreshInterval);
        m_CachedIdleWorkers.Clear();

        if (m_GameManager != null)
        {
            m_CachedIdleWorkers.AddRange(m_GameManager.GetIdleWorkers());
        }

        if (forceResetIndex || m_FocusIndex >= m_CachedIdleWorkers.Count)
        {
            m_FocusIndex = 0;
        }

        UpdateVisuals();
    }

    void UpdateVisuals()
    {
        bool hasIdleWorkers = m_CachedIdleWorkers.Count > 0;
        if (m_ContentRoot != null)
        {
            m_ContentRoot.SetActive(hasIdleWorkers);
        }

        if (!hasIdleWorkers)
        {
            return;
        }

        WorkerUnit firstWorker = m_CachedIdleWorkers[0];
        if (m_IconImage != null)
        {
            m_IconImage.sprite = firstWorker != null ? firstWorker.UnitIcon : null;
            m_IconImage.enabled = m_IconImage.sprite != null;
        }

        if (m_CountText != null)
        {
            m_CountText.text = m_CachedIdleWorkers.Count.ToString();
        }
    }

    void FocusNextIdleWorker()
    {
        RefreshIdleWorkers();
        if (m_CachedIdleWorkers.Count == 0)
        {
            return;
        }

        if (m_FocusIndex >= m_CachedIdleWorkers.Count)
        {
            m_FocusIndex = 0;
        }

        WorkerUnit worker = m_CachedIdleWorkers[m_FocusIndex];
        m_FocusIndex = (m_FocusIndex + 1) % m_CachedIdleWorkers.Count;

        if (worker == null || worker.CurrentState == UnitState.Dead)
        {
            RefreshIdleWorkers(true);
            return;
        }

        if (m_CameraController == null)
        {
            m_CameraController = FindFirstObjectByType<CameraController>();
        }

        m_CameraController?.FocusWorldPosition(worker.transform.position);
    }
}
