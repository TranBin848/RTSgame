using UnityEngine;

public abstract class ProductionBuilding : StructureUnit
{
    [Header("Production")]
    [SerializeField] private Transform m_SpawnPoint;
    [SerializeField] private float m_SpawnScatterRadius = 0.75f;
    [SerializeField] private float m_SpawnExitDistance = 1.5f;

    private readonly System.Collections.Generic.Queue<ProductionRequest> m_SpawnQueue = new();
    private ProductionRequest m_CurrentSpawn;
    private float m_CurrentSpawnTimer;
    private int m_SpawnSequence;
    private int m_LastQueuedFrame = -1;

    protected void EnqueueProduction(ActionSO action, GameObject unitPrefab, float trainDuration, string completionMessage)
    {
        if (action == null || unitPrefab == null)
        {
            return;
        }

        if (m_LastQueuedFrame == Time.frameCount)
        {
            return;
        }

        m_LastQueuedFrame = Time.frameCount;
        string queueUiId = m_GameManager != null ? m_GameManager.EnqueueActionUI(action) : string.Empty;
        m_SpawnQueue.Enqueue(new ProductionRequest(unitPrefab, Mathf.Max(0.01f, trainDuration), queueUiId, completionMessage));
    }

    protected override void AfterConstructionUpdate()
    {
        base.AfterConstructionUpdate();

        if (CurrentState == UnitState.Dead)
        {
            return;
        }

        if (m_CurrentSpawn == null)
        {
            TryStartNextSpawn();
        }

        if (m_CurrentSpawn == null)
        {
            return;
        }

        m_CurrentSpawnTimer += Time.deltaTime;
        m_GameManager?.UpdateActionUIProgress(m_CurrentSpawn.QueueUiId, m_CurrentSpawnTimer / m_CurrentSpawn.Duration);

        if (m_CurrentSpawnTimer < m_CurrentSpawn.Duration)
        {
            return;
        }

        SpawnUnit(m_CurrentSpawn.UnitPrefab);
        m_GameManager?.UpdateActionUIProgress(m_CurrentSpawn.QueueUiId, 1f);
        m_GameManager?.CompleteActionUI(m_CurrentSpawn.QueueUiId);

        if (!string.IsNullOrWhiteSpace(m_CurrentSpawn.CompletionMessage))
        {
            m_GameManager?.ShowTextPopup(m_CurrentSpawn.CompletionMessage, Color.green, transform.position);
        }

        m_CurrentSpawn = null;
        m_CurrentSpawnTimer = 0f;
        TryStartNextSpawn();
    }

    private void TryStartNextSpawn()
    {
        if (m_SpawnQueue.Count == 0)
        {
            return;
        }

        m_CurrentSpawn = m_SpawnQueue.Dequeue();
        m_CurrentSpawnTimer = 0f;
        m_GameManager?.UpdateActionUIProgress(m_CurrentSpawn.QueueUiId, 0f);
    }

    private void SpawnUnit(GameObject unitPrefab)
    {
        Vector3 spawnOrigin = m_SpawnPoint != null ? m_SpawnPoint.position : (transform.position + Vector3.right);
        Vector3 spawnOffset = GetSpawnOffset();
        Vector3 spawnPos = spawnOrigin + spawnOffset;
        var go = Instantiate(unitPrefab, spawnPos, Quaternion.identity);

        AIPawn aiPawn = go.GetComponent<AIPawn>();
        if (aiPawn != null)
        {
            Vector3 exitDirection = spawnOffset.sqrMagnitude > 0.0001f ? spawnOffset.normalized : GetDefaultSpawnDirection();
            Vector3 exitDestination = spawnOrigin + (exitDirection * Mathf.Max(0f, m_SpawnExitDistance));
            aiPawn.SetDestination(exitDestination);
        }
    }

    private Vector3 GetSpawnOffset()
    {
        if (m_SpawnScatterRadius <= 0f)
        {
            return Vector3.zero;
        }

        float angle = m_SpawnSequence * 137.5f * Mathf.Deg2Rad;
        float radius = Mathf.Sqrt((m_SpawnSequence % 6) + 1) / Mathf.Sqrt(6f) * m_SpawnScatterRadius;
        m_SpawnSequence++;
        return new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
    }

    private Vector3 GetDefaultSpawnDirection()
    {
        if (m_SpawnPoint != null)
        {
            Vector3 pointDirection = m_SpawnPoint.right;
            if (pointDirection.sqrMagnitude > 0.0001f)
            {
                return pointDirection.normalized;
            }
        }

        return Vector3.right;
    }

    private sealed class ProductionRequest
    {
        public GameObject UnitPrefab { get; }
        public float Duration { get; }
        public string QueueUiId { get; }
        public string CompletionMessage { get; }

        public ProductionRequest(GameObject unitPrefab, float duration, string queueUiId, string completionMessage)
        {
            UnitPrefab = unitPrefab;
            Duration = duration;
            QueueUiId = queueUiId;
            CompletionMessage = completionMessage;
        }
    }
}
