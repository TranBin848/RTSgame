using UnityEngine;

public class BuildingProcess
{
    private BuildActionSo m_BuildAction;
    private WorkerUnit m_Worker;
    public bool hasActiveWorker => m_Worker != null;
    public BuildingProcess(
        BuildActionSo buildAction,
        Vector3 placementPosition,
        WorkerUnit worker
    )
    {
        m_BuildAction = buildAction;
        var structure = GameObject.Instantiate(m_BuildAction.StructureUnitPrefab);
        structure.SpriteRenderer.sprite = m_BuildAction.FoundationSprite;
        structure.transform.position = placementPosition;
        structure.RegisterBuildingProcess(this);
        worker.SendToBuild(structure);
    }
    public void Update()
    {
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
