using System.Collections.Generic;
using TreeEditor;
using UnityEngine;

public class GameManager : SingletonManager<GameManager>
{
    [Header("UI")]
    [SerializeField] private PointToClick m_PointToClickPrefab;
    [SerializeField] private ActionBar m_ActionBar;
    [SerializeField] private ConfirmationBar m_ConfirmationBar;
    [SerializeField] private TextPopupController m_TextPopupController;

    [Header("Resources")]
    [SerializeField] private Transform m_TreeContainer;
    private PlacementProcess m_PlacementProcess;
    private int m_Gold = 1000;
    private int m_Wood = 1000;
    public int Gold => m_Gold;
    public int Wood => m_Wood;
    public bool IsPlacingStructure => m_PlacementProcess != null;
    public Unit ActiveUnit;
    private Tree[] m_Trees = new Tree[0];
    private List<Unit> m_PlayerUnits = new();
    private List<Unit> m_EnemyUnits = new();
    private List<StructureUnit> m_PlayerStructures = new();
    private CameraController m_CameraController;
    public bool HasActiveUnit => ActiveUnit != null;
    void Start()
    {
        m_CameraController = FindObjectOfType<CameraController>();
        if (m_CameraController == null)
        {
            Debug.LogWarning("CameraController not found in scene. Automatically adding to Main Camera.");
            m_CameraController = Camera.main.gameObject.AddComponent<CameraController>();
        }
        ClearActionBarUI();
    }
    void Update()
    {
        if (m_PlacementProcess != null)
        {
            m_PlacementProcess.Update();
        }
        else if (GameUtils.TryGetShortClickPosition(out Vector2 inputPosition))
        {
            DetectClick(inputPosition);
        }
    }
    public void RegisterUnit(Unit unit)
    {
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
    public void ShowTextPopup(string text, Color color, Vector3 position)
    {
        m_TextPopupController.Spam(text, color, position);
    }
    public Tree FindClosetUnclaimedTree(Vector3 originPosition)
    {
        Tree closestTree = null;
        float closestDistanceSqr = float.MaxValue;

        if (m_Trees.Length == 0)
        {
            m_Trees = new Tree[m_TreeContainer.childCount];

            for (int i = 0; i < m_TreeContainer.childCount; i++)
            {
                //Get the empty object first and then get the Tree component from it, to avoid potential issues with missing Tree components
                var treeObject = m_TreeContainer.GetChild(i).gameObject;
                var treeComponent = treeObject.GetComponentInChildren<Tree>();
                if (treeComponent != null)
                {
                    m_Trees[i] = treeComponent;
                    Debug.Log($"Found tree: {treeComponent.name} at position {treeComponent.transform.position}");
                }
            }
        }

        //Debug.Log(m_Trees.Length + " trees found in the scene.");
        foreach (var tree in m_Trees)
        {
            if (tree == null || tree.Claimed)
            {
                continue;
            }
            float distanceSqr = (tree.transform.position - originPosition).sqrMagnitude;
            if (distanceSqr < closestDistanceSqr)
            {
                closestDistanceSqr = distanceSqr;
                closestTree = tree;
            }
        }
        return closestTree;
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
    public StructureUnit FindClosetWoodStorage(Vector3 originPoint)
    {
        float closetDistanceSqr = float.MaxValue;
        StructureUnit closestStorage = null;

        foreach (var structure in m_PlayerStructures)
        {
            if (!structure.CanStoreWood || structure.CurrentState == UnitState.Dead)
            {
                continue;
            }
            float distanceSqr = (structure.transform.position - originPoint).sqrMagnitude;
            if (distanceSqr < closetDistanceSqr)
            {
                closetDistanceSqr = distanceSqr;
                closestStorage = structure;
            }
        }
        Debug.Log($"Closest wood storage to point {originPoint} is {closestStorage?.name ?? "none"} at distance {Mathf.Sqrt(closetDistanceSqr)}");
        return closestStorage;
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
        m_PlacementProcess = new PlacementProcess(buildAction, tilemapManager);
        m_PlacementProcess.ShowPlacementOutline();
        m_ConfirmationBar.Show(m_PlacementProcess.BuildAction.GoldCost, m_PlacementProcess.BuildAction.WoodCost);
        m_ConfirmationBar.SetupHooks(ConfirmBuildProcess, CancelBuildProcess);
        m_CameraController.LockCamera = true;
    }
    void DetectClick(Vector2 inputPosition)
    {
        if (Camera.main == null || GameUtils.iSPointOverUIElelement() || !GameUtils.IsScreenPositionInBounds(inputPosition))
        {
            return;
        }

        Vector2 worldPoint = Camera.main.ScreenToWorldPoint(inputPosition);
        RaycastHit2D hit = Physics2D.Raycast(worldPoint, Vector2.zero);

        if (workerHasClickedOnTree(hit, out Tree tree))
        {
            ((WorkerUnit)ActiveUnit).SendToChop(tree);
            return;
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
    bool workerHasClickedOnTree(RaycastHit2D hit, out Tree tree)
    {
        tree = null;
        if (hit.collider != null)
        {
            // Debug.Log("Clicked on: " + hit.collider.gameObject.name);
            var treeLayerMask = LayerMask.GetMask("Tree");
            if (HasActiveUnit && ActiveUnit is WorkerUnit && ((1 << hit.collider.gameObject.layer & treeLayerMask) != 0))
            {
                tree = hit.collider.GetComponentInChildren<Tree>();
                return true;
            }
        }
        return false;
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
        else if (WorkerClickedOnUnfinishedBuilding(unit))
        {
            ((WorkerUnit)ActiveUnit).SendToBuild((StructureUnit)unit);
            return;
        }

        SelectNewUnit(unit);
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
            m_ActionBar.RegisterAction(action.Icon,
                () => action.Excute(this));
        }

        m_ActionBar.FocusAction(0);
    }
    void ClearActionBarUI()
    {
        m_ActionBar.ClearActions();
        m_ActionBar.Hide();
    }

    void ConfirmBuildProcess()
    {
        if (!TryDeductResources(m_PlacementProcess.GoldCost, m_PlacementProcess.WoodCost))
        {
            Debug.Log("Not enough resources");
            return;
        }
        if (m_PlacementProcess.TryFinalizePlacement(out Vector3 buildPosition))
        {
            m_ConfirmationBar.Hide();

            new BuildingProcess(
                m_PlacementProcess.BuildAction,
                 buildPosition,
                 (WorkerUnit)ActiveUnit
                 );

            m_PlacementProcess = null;
            m_CameraController.LockCamera = false;
        }
        else
        {
            RevertResources(m_PlacementProcess.GoldCost, m_PlacementProcess.WoodCost);
        }

    }
    void RevertResources(int gold, int wood)
    {
        m_Gold += gold;
        m_Wood += wood;
    }
    void CancelBuildProcess()
    {
        m_ConfirmationBar.Hide();
        m_PlacementProcess.CleanUp();
        m_PlacementProcess = null;
        m_CameraController.LockCamera = false;
        Debug.Log("Build Process Canceled");
    }
    bool TryDeductResources(int goldCost, int woodCost)
    {
        if (m_Gold >= goldCost && m_Wood >= woodCost)
        {
            m_Gold -= goldCost;
            m_Wood -= woodCost;
            return true;
        }
        return false;
    }
    void OnGUI()
    {
        GUI.Label(new Rect(10, 40, 200, 20), "Gold: " + m_Gold.ToString(), new GUIStyle { fontSize = 30 });
        GUI.Label(new Rect(10, 80, 200, 20), "Wood: " + m_Wood.ToString(), new GUIStyle { fontSize = 30 });

        if (ActiveUnit != null)
        {
            GUI.Label(new Rect(10, 120, 200, 20), "State: " + ActiveUnit.CurrentState.ToString(), new GUIStyle { fontSize = 30 });
            GUI.Label(new Rect(10, 160, 200, 20), "Task: " + ActiveUnit.CurrentTask.ToString(), new GUIStyle { fontSize = 30 });
            GUI.Label(new Rect(10, 200, 200, 20), "Stance: " + ActiveUnit.CurrentStance.ToString(), new GUIStyle { fontSize = 30 });
        }
    }
};
