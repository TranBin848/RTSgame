using System.Collections.Generic;
using UnityEngine;

public class GameManager : SingletonManager<GameManager>, IPlayerResourceWallet
{
    [Header("UI")]
    [SerializeField] private PointToClick m_PointToClickPrefab;
    [SerializeField] private ActionBar m_ActionBar;
    [SerializeField] private TextPopupController m_TextPopupController;
    [SerializeField] private ResourcesDataUI m_ResourcesDataUI;

    [Header("Resources")]
    [SerializeField] private Transform m_TreeContainer;
    [SerializeField] private Transform m_GoldStoneContainer;
    [SerializeField] private Transform m_SheepContainer;
    private PlacementProcess m_PlacementProcess;
    private int m_Gold = 0;
    private int m_Wood = 0;
    private int m_Meat = 0;
    public int Gold => m_Gold;
    public int Wood => m_Wood;
    public int Meat => m_Meat;
    public bool IsPlacingStructure => m_PlacementProcess != null;
    public Unit ActiveUnit;
    private List<Unit> m_PlayerUnits = new();
    private List<Unit> m_EnemyUnits = new();
    private List<StructureUnit> m_PlayerStructures = new();
    private CameraController m_CameraController;
    private IResourceNodeLocator m_ResourceNodeLocator;
    private IResourceDepotLocator m_ResourceDepotLocator;
    public bool HasActiveUnit => ActiveUnit != null;

    protected override void Awake()
    {
        base.Awake();
        m_ResourceNodeLocator = new SceneResourceNodeLocator(m_TreeContainer, m_GoldStoneContainer, m_SheepContainer);
        m_ResourceDepotLocator = new SceneResourceDepotLocator(() => m_PlayerStructures);
    }

    void Start()
    {
        m_CameraController = FindFirstObjectByType<CameraController>();
        if (m_CameraController == null)
        {
            Debug.LogWarning("CameraController not found in scene. Automatically adding to Main Camera.");
            m_CameraController = Camera.main.gameObject.AddComponent<CameraController>();
        }
        ClearActionBarUI();
        AddResources(100, 100, 100); // Starting resources for testing
    }
    void Update()
    {
        if (m_PlacementProcess != null)
        {
            m_PlacementProcess.Update();
            if (GameUtils.TryGetShortClickPosition(out Vector2 inputPosition))
            {
                TryPlaceCurrentBuild(inputPosition);
            }
        }
        else if (GameUtils.TryGetShortClickPosition(out Vector2 inputPosition))
        {
            DetectClick(inputPosition);
        }
    }
    public void RegisterUnit(Unit unit)
    {
        if (unit is WorkerUnit worker)
        {
            worker.Inject(m_ResourceNodeLocator, m_ResourceDepotLocator, this);
        }

        if (unit.IsPlayer)
        {
            if (unit.IsBuilding)
            {
                m_PlayerStructures.Add((StructureUnit)unit);
            }
            else
            {
                m_PlayerUnits.Add(unit);
            }
        }
        else
        {
            m_EnemyUnits.Add(unit);
        }
        //Debug.Log($"Registered {(unit.IsPlayer ? "Player" : "Enemy")} Unit: {unit.name}");
    }
    public void UnregisterUnit(Unit unit)
    {
        if (unit.IsPlayer)
        {
            if (m_PlacementProcess != null)
            {
                CancelBuildProcess();
            }
            if (ActiveUnit == unit)
            {
                ClearActionBarUI();
                ActiveUnit.Deselect();
                ActiveUnit = null;
            }
            unit.StopMovement();
            if (unit.IsBuilding)
            {
                m_PlayerStructures.Remove((StructureUnit)unit);
            }
            else
            {
                m_PlayerUnits.Remove(unit);
            }

        }
        else
        {
            m_EnemyUnits.Remove(unit);
        }
    }
    public void AddResources(int gold, int wood, int meat)
    {
        m_Gold += gold;
        m_Wood += wood;
        m_Meat += meat; // Assuming you have a similar variable for meat

        m_ResourcesDataUI.UpdateResourcesData(m_Gold, m_Wood, m_Meat);
    }

    public void AddResource(ResourceType resourceType, int amount)
    {
        switch (resourceType)
        {
            case ResourceType.Gold:
                AddResources(amount, 0, 0);
                break;
            case ResourceType.Wood:
                AddResources(0, amount, 0);
                break;
            case ResourceType.Meat:
                AddResources(0, 0, amount);
                break;
        }
    }

    public void ShowTextPopup(string text, Color color, Vector3 position)
    {
        m_TextPopupController.Spam(text, color, position);
    }
    public Unit FindClosetUnit(Vector3 originPosition, float maxDistance, bool isPlayer)
    {
        List<Unit> units = isPlayer ? m_PlayerUnits : m_EnemyUnits;
        float maxDistanceSqr = maxDistance * maxDistance;
        Unit closestUnit = null;
        float closestDistanceSqr = float.MaxValue;

        foreach (var unit in units)
        {
            if (unit.CurrentState == UnitState.Dead)
            {
                continue;
            }
            float distance = (unit.transform.position - originPosition).sqrMagnitude;
            //Debug.Log($"Checking unit {unit.name} at distance {Mathf.Sqrt(distance)} (sqr: {distance}) against max distance {maxDistance} (sqr: {maxDistanceSqr})");
            if (distance <= maxDistanceSqr && distance <= closestDistanceSqr)
            {
                closestDistanceSqr = distance;
                closestUnit = unit;
            }
        }
        return closestUnit;
    }
    public List<Unit> GetFriendlyUnits(bool isPlayer)
    {
        return isPlayer ? m_PlayerUnits : m_EnemyUnits;
    }

    public void StartBuildProcess(BuildActionSo buildAction)
    {
        if (m_PlacementProcess != null)
        {
            return;
        }
        var tilemapManager = TilemapManager.Get();
        Vector3 startPosition = ActiveUnit != null ? ActiveUnit.transform.position + Vector3.right : Vector3.zero;
        m_PlacementProcess = new PlacementProcess(buildAction, tilemapManager, startPosition);
        m_PlacementProcess.ShowPlacementOutline();
        m_ActionBar.ShowRequirements(m_PlacementProcess.BuildAction.GoldCost, m_PlacementProcess.WoodCost);
    }
    void DetectClick(Vector2 inputPosition)
    {
        if (Camera.main == null || GameUtils.iSPointOverUIElelement() || !GameUtils.IsScreenPositionInBounds(inputPosition))
        {
            return;
        }

        Vector2 worldPoint = Camera.main.ScreenToWorldPoint(inputPosition);
        RaycastHit2D hit = Physics2D.Raycast(worldPoint, Vector2.zero);

        if (HasActiveUnit && ActiveUnit is WorkerUnit worker)
        {
            if (TryGetClickedResourceNode(hit, out var resourceNode))
            {
                worker.TryAssignResourceNode(resourceNode);
                return;
            }
        }

        if (HasClickedOnUnit(hit, out var unit))
        {
            if (unit.IsPlayer)
            {
                handleClickOnPlayerUnit(unit);
            }
            else
            {
                handleClickOnEnemyUnit(unit);
            }
        }
        else
        {
            handleClickOnGround(worldPoint);
        }
    }

    public void FocusActionUI(int idx)
    {
        m_ActionBar.FocusAction(idx);
    }
    bool TryGetClickedResourceNode(RaycastHit2D hit, out IResourceNode resourceNode)
    {
        resourceNode = null;
        return m_ResourceNodeLocator != null && m_ResourceNodeLocator.TryGetNodeFromHit(hit, out resourceNode);
    }

    bool HasClickedOnUnit(RaycastHit2D hit, out Unit unit)
    {
        if (hit.collider != null && hit.collider.TryGetComponent<Unit>(out var clickedUnit))
        {
            unit = clickedUnit;
            return true;
        }
        unit = null;
        return false;
    }
    void handleClickOnGround(Vector2 worldPoint)
    {
        if (HasActiveUnit && isHumanUnit(ActiveUnit))
        {
            DisplayClickEffect(worldPoint);
            ActiveUnit.MoveTo(worldPoint, DestinationSource.PlayerClick);
        }
    }
    void handleClickOnPlayerUnit(Unit unit)
    {
        if (isClickedOnActiveUnit(unit))
        {
            cancelActiveUnit();
            return;
        }
        else if (ActiveUnit is WorkerUnit worker)
        {
            if (WorkerClickedOnUnfinishedBuilding(unit))
            {
                worker.SendToBuild((StructureUnit)unit);
                return;
            }
            else if (WorkerClickedOnValidDepot(worker, unit))
            {
                var closetPoint = unit.Collider.ClosestPoint(worker.transform.position);
                worker.MoveTo(closetPoint);
                worker.SetTask(UnitTask.ReturnResource);
                worker.SetWoodStorage((StructureUnit)unit);
                return;
            }
        }
        SelectNewUnit(unit);
    }
    bool WorkerClickedOnValidDepot(WorkerUnit worker, Unit clickedUnit)
    {
        if (clickedUnit is StructureUnit structure && worker.TryGetCurrentCarryType(out var carryType))
        {
            return structure.CanStore(carryType);
        }
        return false;
    }
    void handleClickOnEnemyUnit(Unit unit)
    {
        if (HasActiveUnit)
        {
            ActiveUnit.SetTarget(unit);
            ActiveUnit.SetTask(UnitTask.Attack);
            DisplayClickEffect(unit.GetTopPosition());
        }
    }
    bool WorkerClickedOnUnfinishedBuilding(Unit clickedUnit)
    {
        return
            ActiveUnit is WorkerUnit worker
            && clickedUnit is StructureUnit structure
            && structure.isUnderConstruction;
    }
    void SelectNewUnit(Unit unit)
    {
        if (unit.CurrentState == UnitState.Dead)
        {
            return;
        }
        if (HasActiveUnit)
        {
            ActiveUnit.Deselect();
        }
        ShowUnitAction(unit);
        ActiveUnit = unit;
        ActiveUnit.Select();

    }
    bool isClickedOnActiveUnit(Unit unit)
    {
        return HasActiveUnit && unit == ActiveUnit;
    }
    bool isHumanUnit(Unit unit)
    {
        return unit is HumanoidUnit;
    }
    void cancelActiveUnit()
    {
        ActiveUnit.Deselect();
        ActiveUnit = null;
        ClearActionBarUI();
    }
    void DisplayClickEffect(Vector2 worldPoint)
    {
        Instantiate(m_PointToClickPrefab, worldPoint, Quaternion.identity);
    }
    void ShowUnitAction(Unit unit)
    {
        ClearActionBarUI();
        if (unit.Actions.Length == 0)
        {
            return;
        }
        m_ActionBar.Show();
        foreach (var action in unit.Actions)
        {
            ActionSO capturedAction = action;
            m_ActionBar.RegisterAction(action.Icon,
                () => ExecuteUnitAction(capturedAction));
        }

        m_ActionBar.FocusAction(0);
    }

    void ExecuteUnitAction(ActionSO action)
    {
        if (m_PlacementProcess != null)
        {
            CancelBuildProcess();
        }
        action.Excute(this);
    }
    void ClearActionBarUI()
    {
        m_ActionBar.ClearActions();
        m_ActionBar.Hide();
        m_ActionBar.HideRequirements();
    }

    void CancelBuildProcess()
    {
        if (m_PlacementProcess == null)
        {
            return;
        }
        m_PlacementProcess.CleanUp();
        m_PlacementProcess = null;
        m_ActionBar.HideRequirements();
        Debug.Log("Build Process Canceled");
    }

    void TryPlaceCurrentBuild(Vector2 inputPosition)
    {
        if (Camera.main == null || GameUtils.iSPointOverUIElelement() || !GameUtils.IsScreenPositionInBounds(inputPosition))
        {
            return;
        }

        if (m_PlacementProcess == null)
        {
            return;
        }

        if (!m_PlacementProcess.TryFinalizePlacement(out Vector3 buildPosition))
        {
            m_PlacementProcess.Shake();
            return;
        }

        if (!TryDeductResources(m_PlacementProcess.GoldCost, m_PlacementProcess.WoodCost))
        {
            m_PlacementProcess.Shake();
            return;
        }

        new BuildingProcess(
            m_PlacementProcess.BuildAction,
            buildPosition,
            (WorkerUnit)ActiveUnit
        );

        m_PlacementProcess = null;
        m_ActionBar.HideRequirements();
    }
    bool TryDeductResources(int goldCost, int woodCost)
    {
        if (m_Gold >= goldCost && m_Wood >= woodCost)
        {
            AddResources(-goldCost, -woodCost, 0); // Update the UI with the new resource values
            return true;
        }
        return false;
    }
    void OnGUI()
    {
        if (ActiveUnit != null)
        {
            GUI.Label(new Rect(10, 120, 200, 20), "State: " + ActiveUnit.CurrentState.ToString(), new GUIStyle { fontSize = 30 });
            GUI.Label(new Rect(10, 160, 200, 20), "Task: " + ActiveUnit.CurrentTask.ToString(), new GUIStyle { fontSize = 30 });
            GUI.Label(new Rect(10, 200, 200, 20), "Stance: " + ActiveUnit.CurrentStance.ToString(), new GUIStyle { fontSize = 30 });
        }
    }
};
