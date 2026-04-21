using UnityEngine;

public class BuildingProcess
{
    private BuildActionSo m_BuildAction;
    public BuildingProcess(
        BuildActionSo buildAction,
        Vector3 placementPosition
    )
    {

        m_BuildAction = buildAction;
        var structure = GameObject.Instantiate(m_BuildAction.StructureUnitPrefab);
        structure.SpriteRenderer.sprite = m_BuildAction.FoundationSprite;
        structure.transform.position = placementPosition;
        structure.RegisterBuildingProcess(this);
    }
    public void Update()
    {
        Debug.Log("Building...");
    }
}
