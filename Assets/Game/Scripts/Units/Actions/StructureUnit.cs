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
}