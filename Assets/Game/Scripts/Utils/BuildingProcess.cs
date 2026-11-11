using UnityEngine;
using RTS.DOTS;

public class BuildingProcess
{
    private BuildActionSo m_BuildAction;
    private WorkerUnit m_Worker;
    private StructureUnit m_Structure;
    private GameManager m_GameManager;
    private string m_QueueUiId;
    public float m_ProgressTimer;
    public bool m_isMiddle;
    public bool m_isFinished;
    public bool InProgress => hasActiveWorker && m_Worker.CurrentState == UnitState.Building;
    public bool hasActiveWorker => m_Worker != null;
    public BuildingProcess(
        BuildActionSo buildAction,
        Vector3 placementPosition,
        WorkerUnit worker
    )
    {
        m_BuildAction = buildAction;
        m_GameManager = GameManager.Get();
        if (m_GameManager != null && !m_GameManager.CanBuild(buildAction))
            throw new System.InvalidOperationException("Construction requires research before it can begin.");
        m_QueueUiId = m_GameManager != null ? m_GameManager.EnqueueActionUI(m_BuildAction) : string.Empty;
        m_Structure = SwarmGameBridge.Instance != null
            ? SwarmGameBridge.Instance.SpawnGameplay(m_BuildAction.StructureUnitPrefab.gameObject, placementPosition, Quaternion.identity).GetComponent<StructureUnit>()
            : GameObject.Instantiate(m_BuildAction.StructureUnitPrefab);
        m_Structure.SpriteRenderer.sprite = m_BuildAction.FoundationSprite;
        m_Structure.transform.position = placementPosition;
        m_Structure.RegisterBuildingProcess(this);
        if (m_Structure.IsDotsControlled) SwarmGameBridge.Instance.BeginConstruction(m_Structure, this, m_BuildAction.ConstructionTime);
        worker.SendToBuild(m_Structure);
    }
    public void Update()
    {
        if (m_Structure.IsDotsControlled) { UpdateDotsPresentation(); return; }
        if (m_isFinished)
        {
            return;
        }
        if (InProgress)
        {
            m_ProgressTimer += Time.deltaTime;
            if (m_GameManager != null && m_BuildAction.ConstructionTime > 0f)
            {
                m_GameManager.UpdateActionUIProgress(m_QueueUiId, m_ProgressTimer / m_BuildAction.ConstructionTime);
            }
            if (!m_isMiddle && m_ProgressTimer >= m_BuildAction.ConstructionTime / 2f)
            {
                m_isMiddle = true;
                m_Structure.SpriteRenderer.sprite = m_BuildAction.MiddleSprite;
                Debug.Log("Building construction is halfway done.");
            }
            if (m_ProgressTimer >= m_BuildAction.ConstructionTime)
            {
                m_isFinished = true;
                m_GameManager?.UpdateActionUIProgress(m_QueueUiId, 1f);
                m_GameManager?.CompleteActionUI(m_QueueUiId);
                m_Structure.SpriteRenderer.sprite = m_BuildAction.CompleteSprite;
                m_Structure.OnConstructionFinished();
                m_Worker.OnBuildingFinished();
                Debug.Log("Building construction completed.");
            }
        }
    }
    public void UpdateDotsPresentation()
    {
        if (m_Structure == null || !m_Structure.IsDotsControlled) return;
        var build = SwarmGameBridge.Instance.Gameplay.EntityManager.GetComponentData<GameplayConstruction>(m_Structure.DotsEntity);
        m_ProgressTimer = build.Elapsed;
        m_GameManager?.UpdateActionUIProgress(m_QueueUiId, build.Elapsed / Mathf.Max(0.01f, build.Duration));
    }
    public void ApplyDotsStage(bool complete)
    {
        if (m_Structure == null || m_Structure.CurrentState == UnitState.Dead) return;
        if (!complete) { m_isMiddle = true; m_Structure.SpriteRenderer.sprite = m_BuildAction.MiddleSprite; return; }
        m_isFinished = true; m_GameManager?.CompleteActionUI(m_QueueUiId);
        m_Structure.SpriteRenderer.sprite = m_BuildAction.CompleteSprite; m_Structure.OnConstructionFinished();
    }
    public void AddWorker(WorkerUnit worker)
    {
        if (hasActiveWorker)
        {
            return;
        }
        Debug.Log("Worker assigned to building process.");
        m_Worker = worker;
    }
    public void RemoveWorker()
    {
        if (!hasActiveWorker)
        {
            return;
        }
        Debug.Log("Worker removed from building process.");
        m_Worker = null;
    }
}
