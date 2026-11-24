using UnityEngine;
using Unity.Entities;
using RTS.DOTS;

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
    public int SaveSpawnSequence => m_SpawnSequence;
    public void RestoreSpawnSequence(int value) { m_SpawnSequence = value; }
    private int m_LastQueuedFrame = -1;
    private GameManager m_ProductionGame;
    private SwarmGameBridge m_ProductionBridge;
    public bool IsProductionInitialized { get; private set; }

    public void InitializeProduction(GameManager game, SwarmGameBridge bridge)
    {
        if (game == null) throw new System.ArgumentNullException(nameof(game));
        if (game.gameObject.scene != gameObject.scene || (bridge == null && DotsEntity != Entity.Null))
            throw new System.ArgumentException("Production dependencies must belong to the building's scene.");
        if (bridge != null && (!bridge.IsInitialized || bridge.gameObject != game.gameObject
            || !bridge.Gameplay.EntityManager.Exists(DotsEntity) || !bridge.Gameplay.EntityManager.HasComponent<GameplayProducer>(DotsEntity)))
            throw new System.ArgumentException("Production requires its bound producer and scene bridge.");
        if (IsProductionInitialized && (m_ProductionGame != game || m_ProductionBridge != bridge))
            throw new System.InvalidOperationException("Production is already initialized with different dependencies.");
        m_ProductionGame = game; m_ProductionBridge = bridge;
        IsProductionInitialized = true;
    }

    public bool IsProductionOwner(GameManager game) => IsProductionInitialized && m_ProductionGame == game;

    internal void UnbindProduction(SwarmGameBridge bridge)
    {
        if (m_ProductionBridge != bridge) return;
        if (m_ProductionGame != null)
        {
            if (m_CurrentSpawn != null) m_ProductionGame.CompleteActionUI(m_CurrentSpawn.QueueUiId);
            foreach (var request in m_SpawnQueue) m_ProductionGame.CompleteActionUI(request.QueueUiId);
        }
        m_SpawnQueue.Clear(); m_CurrentSpawn = null; m_CurrentSpawnTimer = 0;
        m_LastQueuedFrame = -1; m_SpawnSequence = 0;
        m_ProductionGame = null; m_ProductionBridge = null; IsProductionInitialized = false;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (IsProductionInitialized) UnbindProduction(m_ProductionBridge);
    }

    protected bool EnqueueProduction(ActionSO action, GameObject unitPrefab, float trainDuration, string completionMessage)
    {
        if (!IsProductionInitialized || m_ProductionGame == null || CurrentState == UnitState.Dead
            || ProceduralMapRuntime.IsCompositionBlocked || Time.timeScale <= 0
            || action == null || unitPrefab == null || trainDuration <= 0 || float.IsNaN(trainDuration) || float.IsInfinity(trainDuration))
        {
            return false;
        }
        if (m_ProductionBridge != null && (!m_ProductionBridge.IsSceneBound || !m_ProductionBridge.CanUsePrefab(unitPrefab))) return false;

        if (m_LastQueuedFrame == Time.frameCount)
        {
            return false;
        }

        if (m_ProductionBridge != null)
        {
            if (!m_ProductionBridge.EnqueueTraining(this, action, unitPrefab, trainDuration, completionMessage)) return false;
        }
        else
        {
            string queueUiId = m_ProductionGame.EnqueueActionUI(action);
            m_SpawnQueue.Enqueue(new ProductionRequest(unitPrefab, trainDuration, queueUiId, completionMessage));
        }
        m_LastQueuedFrame = Time.frameCount;
        return true;
    }

    public Vector3 GetSpawnOrigin()
    {
        return m_SpawnPoint != null ? m_SpawnPoint.position : (transform.position + Vector3.right);
    }

    public Vector3 GetSpawnExitDestination()
    {
        Vector3 spawnOrigin = GetSpawnOrigin();
        Vector3 exitDirection = GetDefaultSpawnDirection();
        return spawnOrigin + (exitDirection * Mathf.Max(0f, m_SpawnExitDistance));
    }

    protected override void AfterConstructionUpdate()
    {
        base.AfterConstructionUpdate();
        if (!IsProductionInitialized) return;

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
        m_ProductionGame?.UpdateActionUIProgress(m_CurrentSpawn.QueueUiId, m_CurrentSpawnTimer / m_CurrentSpawn.Duration);

        if (m_CurrentSpawnTimer < m_CurrentSpawn.Duration)
        {
            return;
        }

        SpawnUnit(m_CurrentSpawn.UnitPrefab);
        m_ProductionGame?.UpdateActionUIProgress(m_CurrentSpawn.QueueUiId, 1f);
        m_ProductionGame?.CompleteActionUI(m_CurrentSpawn.QueueUiId);

        if (!string.IsNullOrWhiteSpace(m_CurrentSpawn.CompletionMessage))
        {
            m_ProductionGame?.ShowTextPopup(m_CurrentSpawn.CompletionMessage, Color.green, transform.position);
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
        m_ProductionGame?.UpdateActionUIProgress(m_CurrentSpawn.QueueUiId, 0f);
    }

    private void SpawnUnit(GameObject unitPrefab)
    {
        Vector3 spawnOrigin = m_SpawnPoint != null ? m_SpawnPoint.position : (transform.position + Vector3.right);
        Vector3 spawnOffset = GetSpawnOffset();
        Vector3 spawnPos = spawnOrigin + spawnOffset;
        var go = m_ProductionBridge != null ? m_ProductionBridge.SpawnGameplay(unitPrefab, spawnPos, Quaternion.identity)
            : Instantiate(unitPrefab, spawnPos, Quaternion.identity);
        if (go == null) return;

        if (m_ProductionBridge != null && go.TryGetComponent<Unit>(out var unit))
        {
            Vector3 direction = spawnOffset.sqrMagnitude > 0.0001f ? spawnOffset.normalized : GetDefaultSpawnDirection();
            m_ProductionBridge.MoveActor(unit, spawnOrigin + direction * Mathf.Max(0f, m_SpawnExitDistance), false);
            return;
        }
        AIPawn aiPawn = go.GetComponent<AIPawn>();
        if (aiPawn != null)
        {
            Vector3 exitDirection = spawnOffset.sqrMagnitude > 0.0001f ? spawnOffset.normalized : GetDefaultSpawnDirection();
            Vector3 exitDestination = spawnOrigin + (exitDirection * Mathf.Max(0f, m_SpawnExitDistance));
            aiPawn.SetDestination(exitDestination);
        }
    }

    public void SpawnDotsView(GameObject prefab) { if (IsProductionInitialized) SpawnUnit(prefab); }

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
