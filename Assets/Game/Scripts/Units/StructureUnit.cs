using UnityEngine;

public class StructureUnit : Unit, IResourceDepot
{
    [SerializeField] private bool m_CanStoreWood = false;
    [SerializeField] private bool m_CanStoreGold = false;
    [SerializeField] private bool m_CanStoreMeat = false;
    private BuildingProcess m_BuildingProcess;
    private bool m_IsQuitting;
    public override bool IsBuilding => true;
    private TilemapManager m_TilemapManager;
    public bool CanStoreWood => m_CanStoreWood;
    public bool CanStoreGold => m_CanStoreGold;
    public bool CanStoreMeat => m_CanStoreMeat;
    protected override void Start()
    {
        base.Start();
        m_TilemapManager = TilemapManager.Get();
        UpdateWalkability();
    }
    public bool isUnderConstruction => m_BuildingProcess != null;

    void Update()
    {
        if (isUnderConstruction)
        {
            m_BuildingProcess.Update();
        }
        else
        {
            AfterConstructionUpdate();
        }
    }
    void OnApplicationQuit()
    {
        m_IsQuitting = true;
    }

    public void OnDestroy()
    {
        if (m_IsQuitting)
        {
            return;
        }
        
        var col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false;
        }

        UpdateWalkability();
    }
    public virtual void OnConstructionFinished()
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
    protected virtual void AfterConstructionUpdate()
    {

    }

    public bool CanStore(ResourceType resourceType)
    {
        return resourceType switch
        {
            ResourceType.Wood => m_CanStoreWood,
            ResourceType.Gold => m_CanStoreGold,
            ResourceType.Meat => m_CanStoreMeat,
            _ => false
        };
    }

    public Vector3 GetDeliveryPoint(Vector3 originPosition)
    {
        return Collider != null ? Collider.ClosestPoint(originPosition) : transform.position;
    }

    void UpdateWalkability()
    {
        if (m_TilemapManager == null || !m_TilemapManager.CanServePathfindingRequests())
        {
            return;
        }

        var col = GetComponent<Collider2D>();
        if (col != null)
        {
            Bounds bounds = col.bounds;
            Vector3Int min = m_TilemapManager.WalkableTilemap.WorldToCell(bounds.min);
            Vector3Int max = m_TilemapManager.WalkableTilemap.WorldToCell(bounds.max);

            // Thêm padding để bao phủ chắc chắn cả những ô rìa
            min.x -= 2; min.y -= 2;
            max.x += 2; max.y += 2;

            int width = max.x - min.x + 1;
            int height = max.y - min.y + 1;

            m_TilemapManager.UpdateNodesInArea(min, width, height);
        }
        else
        {
            // Fallback nếu nhà không có collider (mặc dù nên có)
            int buildingWidthInTiles = 6;
            int buildingHeightInTiles = 6;

            float halfWidth = buildingWidthInTiles * 0.5f;
            float halfHeight = buildingHeightInTiles * 0.5f;

            Vector3Int startPosition = new Vector3Int(
                Mathf.RoundToInt(transform.position.x - halfWidth),
                Mathf.RoundToInt(transform.position.y - halfHeight),
                0);

            m_TilemapManager.UpdateNodesInArea(startPosition, buildingWidthInTiles, buildingHeightInTiles);
        }
    }
}
