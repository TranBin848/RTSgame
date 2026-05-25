using UnityEngine;

public class StructureUnit : Unit
{
    private BuildingProcess m_BuildingProcess;
    public override bool IsBuilding => true;
    private TilemapManager m_TilemapManager;
    void Start()
    {
        base.Start();
        m_TilemapManager = TilemapManager.Get();
    }
    public bool isUnderConstruction => m_BuildingProcess != null;

    void Update()
    {
        if (isUnderConstruction)
        {
            m_BuildingProcess.Update();
        }
    }
    public void OnDestroy()
    {
        UpdateWalkability();
    }
    public void OnConstructionFinished()
    {
        m_BuildingProcess = null;
        UpdateWalkability();
    }
    public void RegisterBuildingProcess(BuildingProcess buildingProcess)
    {
        m_BuildingProcess = buildingProcess;
    }
    public void AssignWorkerToBuildProcess(WorkerUnit worker)
    {
        if (m_BuildingProcess == null)
        {
            Debug.LogWarning("No building process to assign worker to.");
            return;
        }
        m_BuildingProcess.AddWorker(worker);
    }
    public void UnassignWorkerFromBuildProcess()
    {
        if (m_BuildingProcess == null)
        {
            Debug.LogWarning("No building process to unassign worker from.");
            return;
        }
        m_BuildingProcess.RemoveWorker();
    }
    void UpdateWalkability()
    {
        int buildingWidthInTiles = 4;
        int buildingHeightInTiles = 4;

        float halfWidth = buildingHeightInTiles * 0.5f;
        float halfHeight = buildingHeightInTiles * 0.5f;

        Vector3Int startPosition = new Vector3Int(
            Mathf.RoundToInt(transform.position.x - halfWidth),
            Mathf.RoundToInt(transform.position.y - halfHeight),
            0);

        m_TilemapManager.UpdateNodesInArea(startPosition, buildingWidthInTiles, buildingHeightInTiles);
    }
}