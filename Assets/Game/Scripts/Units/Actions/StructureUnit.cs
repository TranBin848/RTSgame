using UnityEngine;

public class StructureUnit : Unit
{
    private BuildingProcess m_BuildingProcess;

    public bool isUnderConstruction => m_BuildingProcess != null;

    void Update()
    {
        if (isUnderConstruction)
        {
            m_BuildingProcess.Update();
        }
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
}