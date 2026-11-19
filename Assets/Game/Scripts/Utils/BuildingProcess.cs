using UnityEngine;
using RTS.DOTS;
using Unity.Entities;

public class BuildingProcess
{
    private BuildActionSo m_BuildAction;
    private WorkerUnit m_Worker;
    private StructureUnit m_Structure;
    private GameManager m_GameManager;
    private readonly SwarmGameBridge m_Bridge;
    private string m_QueueUiId;
    public float m_ProgressTimer;
    public bool m_isMiddle;
    public bool m_isFinished;
    public bool InProgress => hasActiveWorker && m_Worker.CurrentState == UnitState.Building;
    public bool hasActiveWorker => m_Worker != null;
    public BuildingProcess(
        BuildActionSo buildAction,
        Vector3 placementPosition,
        WorkerUnit worker,
        GameManager gameManager,
        SwarmGameBridge bridge
    )
    {
        if (buildAction == null || buildAction.StructureUnitPrefab == null)
            throw new System.ArgumentException("Construction requires an action with a structure prefab.", nameof(buildAction));
        if (worker == null) throw new System.ArgumentNullException(nameof(worker));
        if (gameManager == null) throw new System.ArgumentNullException(nameof(gameManager));
        if (worker.gameObject.scene != gameManager.gameObject.scene || worker.CurrentState == UnitState.Dead)
            throw new System.ArgumentException("Construction requires a living worker from the same scene.", nameof(worker));
        if (bridge == null && worker.DotsEntity != Entity.Null)
            throw new System.ArgumentException("A bound worker requires its scene bridge.", nameof(bridge));
        if (bridge != null && (!bridge.IsSceneBound || bridge.gameObject != gameManager.gameObject
            || !bridge.Gameplay.TryGetWorker(worker.DotsEntity, out _)))
            throw new System.ArgumentException("Construction requires the bound worker and bridge owned by this GameManager.", nameof(bridge));
        m_BuildAction = buildAction;
        m_GameManager = gameManager;
        m_Bridge = bridge;
        if (!m_GameManager.CanBuild(buildAction))
            throw new System.InvalidOperationException("Construction requires research before it can begin.");
        if (m_Bridge != null)
        {
            var view = m_Bridge.SpawnGameplay(m_BuildAction.StructureUnitPrefab.gameObject, placementPosition, Quaternion.identity);
            if (view == null) throw new System.InvalidOperationException("Construction view could not be spawned.");
            m_Structure = view.GetComponent<StructureUnit>();
        }
        else m_Structure = GameObject.Instantiate(m_BuildAction.StructureUnitPrefab);
        m_QueueUiId = m_GameManager.EnqueueActionUI(m_BuildAction);
        m_Structure.SpriteRenderer.sprite = m_BuildAction.FoundationSprite;
        m_Structure.transform.position = placementPosition;
        m_Structure.RegisterBuildingProcess(this);
        if (m_Bridge != null)
        {
            m_Bridge.BeginConstruction(m_Structure, this, m_BuildAction.ConstructionTime);
            m_Bridge.Gameplay.AssignConstruction(worker.DotsEntity, m_Structure.DotsEntity);
        }
        else worker.SendToBuild(m_Structure);
    }
    public void Update()
    {
        if (m_isFinished || m_Structure == null || m_Structure.CurrentState == UnitState.Dead)
        {
            return;
        }
        if (m_Bridge != null) { UpdateDotsPresentation(); return; }
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
        if (m_Structure == null || m_Bridge == null || m_Bridge.Gameplay == null
            || !m_Bridge.Gameplay.World.IsCreated || !m_Bridge.Gameplay.EntityManager.Exists(m_Structure.DotsEntity)) return;
        var build = m_Bridge.Gameplay.EntityManager.GetComponentData<GameplayConstruction>(m_Structure.DotsEntity);
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
