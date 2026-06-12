using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GameManager : SingletonManager<GameManager>, IPlayerResourceWallet
{
    [Header("UI")]
    [SerializeField] private PointToClick m_PointToClickPrefab;
    [SerializeField] private ActionBar m_ActionBar;
    [SerializeField] private ActionQueuePanel m_ActionQueuePanel;
    [SerializeField] private TextPopupController m_TextPopupController;
    [SerializeField] private ResourcesDataUI m_ResourcesDataUI;
    [Header("Selection")]
    [SerializeField] private SelectionSettingsDefinition m_SelectionSettings;

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
    public IReadOnlyList<Unit> SelectedUnits => m_SelectedUnits;
    private List<Unit> m_PlayerUnits = new();
    private List<Unit> m_EnemyUnits = new();
    private List<StructureUnit> m_PlayerStructures = new();
    private readonly List<Unit> m_SelectedUnits = new();
    private CameraController m_CameraController;
    private IResourceNodeLocator m_ResourceNodeLocator;
    private IResourceDepotLocator m_ResourceDepotLocator;
    public bool HasActiveUnit => ActiveUnit != null;
    public UnityAction<IReadOnlyList<Unit>> OnSelectionChanged = delegate { };
    private bool m_IsSelectionPointerDown;
    private bool m_IsSelectionDragging;
    private Vector2 m_SelectionStartScreenPosition;
    private Vector2 m_SelectionCurrentScreenPosition;
    private float m_SelectionPointerDownTime;
    private Texture2D m_SelectionTexture;

    protected override void Awake()
    {
        base.Awake();
        m_ResourceNodeLocator = new SceneResourceNodeLocator(m_TreeContainer, m_GoldStoneContainer, m_SheepContainer);
        m_ResourceDepotLocator = new SceneResourceDepotLocator(() => m_PlayerStructures);
        m_SelectionTexture = new Texture2D(1, 1);
        m_SelectionTexture.SetPixel(0, 0, Color.white);
        m_SelectionTexture.Apply();
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
        else
        {
            HandleSelectionCancelInput();
            UpdateSelectionInput();
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
                DeselectUnit(unit);
                ActiveUnit = null;
            }
            else
            {
                DeselectUnit(unit);
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

    public string EnqueueActionUI(ActionSO action)
    {
        if (m_ActionQueuePanel == null || action == null)
        {
            return string.Empty;
        }

        return m_ActionQueuePanel.Enqueue(action);
    }

    public void UpdateActionUIProgress(string queueId, float normalizedProgress)
    {
        if (m_ActionQueuePanel == null)
        {
            return;
        }

        m_ActionQueuePanel.SetProgress(queueId, normalizedProgress);
    }

    public void CompleteActionUI(string queueId)
    {
        if (m_ActionQueuePanel == null)
        {
            return;
        }

        m_ActionQueuePanel.Complete(queueId);
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
    }
    void DetectClick(Vector2 inputPosition)
    {
        if (Camera.main == null || GameUtils.iSPointOverUIElelement() || !GameUtils.IsScreenPositionInBounds(inputPosition))
        {
            return;
        }

        Vector2 worldPoint = Camera.main.ScreenToWorldPoint(inputPosition);
        RaycastHit2D hit = Physics2D.Raycast(worldPoint, Vector2.zero);

        if (HasExactlyOneUnitSelected() && ActiveUnit is WorkerUnit worker)
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
        if (m_SelectedUnits.Count > 1)
        {
            MoveSelectedUnits(worldPoint);
            DisplayClickEffect(worldPoint);
            return;
        }

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
        else if (HasExactlyOneUnitSelected() && ActiveUnit is WorkerUnit worker)
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
        if (m_SelectedUnits.Count > 1)
        {
            bool hasIssuedAttack = false;
            foreach (var selectedUnit in m_SelectedUnits)
            {
                if (selectedUnit == null || selectedUnit.CurrentState == UnitState.Dead || selectedUnit.IsBuilding)
                {
                    continue;
                }

                selectedUnit.SetTarget(unit);
                selectedUnit.SetTask(UnitTask.Attack);
                hasIssuedAttack = true;
            }

            if (hasIssuedAttack)
            {
                DisplayClickEffect(unit.GetTopPosition());
            }

            return;
        }

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
        SetSelectedUnits(new List<Unit> { unit });
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
        DeselectAllUnits();
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
            m_ActionBar.RegisterAction(action.Icon, action,
                () => ExecuteUnitAction(capturedAction));
        }

        m_ActionBar.FocusAction(0);
    }

    void UpdateSelectionInput()
    {
        if (Camera.main == null)
        {
            return;
        }

        if (GameUtils.isLeftClickOrTapDown)
        {
            m_IsSelectionPointerDown = !GameUtils.iSPointOverUIElelement();
            m_IsSelectionDragging = false;
            m_SelectionStartScreenPosition = GameUtils.InputPosition;
            m_SelectionCurrentScreenPosition = m_SelectionStartScreenPosition;
            m_SelectionPointerDownTime = Time.time;
        }

        if (!m_IsSelectionPointerDown)
        {
            return;
        }

        if (Input.GetMouseButton(0) || Input.touchCount > 0)
        {
            m_SelectionCurrentScreenPosition = GameUtils.InputPosition;
            if (!m_IsSelectionDragging
                && Time.time - m_SelectionPointerDownTime >= GetSelectionDragMinHoldTime()
                && Vector2.Distance(m_SelectionStartScreenPosition, m_SelectionCurrentScreenPosition) >= GetSelectionDragThreshold())
            {
                m_IsSelectionDragging = true;
            }
        }

        if (!GameUtils.isLeftClickOrTapUp)
        {
            return;
        }

        Vector2 releasePosition = GameUtils.InputPosition;
        bool wasDragging = m_IsSelectionDragging;
        m_IsSelectionPointerDown = false;
        m_IsSelectionDragging = false;
        m_SelectionCurrentScreenPosition = releasePosition;

        if (wasDragging)
        {
            SelectUnitsInRectangle(GetScreenSelectionRect(m_SelectionStartScreenPosition, releasePosition));
            return;
        }

        if (GameUtils.IsScreenPositionInBounds(releasePosition))
        {
            DetectClick(releasePosition);
        }
    }

    void HandleSelectionCancelInput()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            DeselectAllUnits();
        }
    }

    void SelectUnitsInRectangle(Rect selectionRect)
    {
        if (Camera.main == null || selectionRect.width <= 0f || selectionRect.height <= 0f)
        {
            return;
        }

        var unitsToSelect = new List<Unit>();
        foreach (var unit in m_PlayerUnits)
        {
            if (unit == null || unit.CurrentState == UnitState.Dead || unit.IsBuilding)
            {
                continue;
            }

            Vector3 screenPosition = Camera.main.WorldToScreenPoint(unit.transform.position);
            if (screenPosition.z < 0f)
            {
                continue;
            }

            screenPosition.y = Screen.height - screenPosition.y;
            if (selectionRect.Contains(screenPosition))
            {
                unitsToSelect.Add(unit);
            }
        }

        SetSelectedUnits(unitsToSelect);
    }

    void SetSelectedUnits(List<Unit> units)
    {
        DeselectAllUnits(false);

        foreach (var unit in units)
        {
            if (unit == null || unit.CurrentState == UnitState.Dead)
            {
                continue;
            }

            if (!m_SelectedUnits.Contains(unit))
            {
                m_SelectedUnits.Add(unit);
                unit.Select();
            }
        }

        ActiveUnit = m_SelectedUnits.Count > 0 ? m_SelectedUnits[0] : null;
        RefreshActionBarForSelection();
        NotifySelectionChanged();
    }

    void DeselectAllUnits(bool clearActionBar = true)
    {
        foreach (var unit in m_SelectedUnits)
        {
            unit?.Deselect();
        }

        m_SelectedUnits.Clear();
        ActiveUnit = null;

        if (clearActionBar)
        {
            ClearActionBarUI();
        }

        NotifySelectionChanged();
    }

    void DeselectUnit(Unit unit)
    {
        if (unit == null)
        {
            return;
        }

        unit.Deselect();
        m_SelectedUnits.Remove(unit);
        ActiveUnit = m_SelectedUnits.Count > 0 ? m_SelectedUnits[0] : null;
        RefreshActionBarForSelection();
        NotifySelectionChanged();
    }

    void RefreshActionBarForSelection()
    {
        if (m_SelectedUnits.Count == 1 && ActiveUnit != null)
        {
            ShowUnitAction(ActiveUnit);
            return;
        }

        ClearActionBarUI();
    }

    void NotifySelectionChanged()
    {
        OnSelectionChanged.Invoke(m_SelectedUnits);
    }

    bool HasExactlyOneUnitSelected()
    {
        return m_SelectedUnits.Count == 1 && ActiveUnit != null;
    }

    void MoveSelectedUnits(Vector2 centerPoint)
    {
        var movableUnits = new List<Unit>();
        foreach (var selectedUnit in m_SelectedUnits)
        {
            if (selectedUnit != null && !selectedUnit.IsBuilding && isHumanUnit(selectedUnit))
            {
                movableUnits.Add(selectedUnit);
            }
        }

        if (movableUnits.Count == 0)
        {
            return;
        }

        int columns = Mathf.CeilToInt(Mathf.Sqrt(movableUnits.Count));
        for (int i = 0; i < movableUnits.Count; i++)
        {
            int row = i / columns;
            int column = i % columns;
            Vector2 offset = new Vector2(
                (column - (columns - 1) * 0.5f) * GetGroupMoveSpacing(),
                -(row * GetGroupMoveSpacing())
            );

            movableUnits[i].MoveTo(centerPoint + offset, DestinationSource.PlayerClick);
        }
    }

    Rect GetScreenSelectionRect(Vector2 startScreenPosition, Vector2 endScreenPosition)
    {
        Vector2 startGui = ToGuiPoint(startScreenPosition);
        Vector2 endGui = ToGuiPoint(endScreenPosition);

        float xMin = Mathf.Min(startGui.x, endGui.x);
        float yMin = Mathf.Min(startGui.y, endGui.y);
        float xMax = Mathf.Max(startGui.x, endGui.x);
        float yMax = Mathf.Max(startGui.y, endGui.y);
        return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
    }

    Vector2 ToGuiPoint(Vector2 screenPoint)
    {
        return new Vector2(screenPoint.x, Screen.height - screenPoint.y);
    }

    void DrawSelectionRectangle()
    {
        if (!m_IsSelectionDragging || m_SelectionTexture == null)
        {
            return;
        }

        Rect selectionRect = GetScreenSelectionRect(m_SelectionStartScreenPosition, m_SelectionCurrentScreenPosition);
        if (selectionRect.width <= 0f || selectionRect.height <= 0f)
        {
            return;
        }

        Color previousColor = GUI.color;
        GUI.color = GetSelectionFillColor();
        GUI.DrawTexture(selectionRect, m_SelectionTexture);

        GUI.color = GetSelectionBorderColor();
        GUI.DrawTexture(new Rect(selectionRect.xMin, selectionRect.yMin, selectionRect.width, 2f), m_SelectionTexture);
        GUI.DrawTexture(new Rect(selectionRect.xMin, selectionRect.yMax - 2f, selectionRect.width, 2f), m_SelectionTexture);
        GUI.DrawTexture(new Rect(selectionRect.xMin, selectionRect.yMin, 2f, selectionRect.height), m_SelectionTexture);
        GUI.DrawTexture(new Rect(selectionRect.xMax - 2f, selectionRect.yMin, 2f, selectionRect.height), m_SelectionTexture);
        GUI.color = previousColor;
    }

    float GetSelectionDragThreshold()
    {
        return m_SelectionSettings != null ? m_SelectionSettings.SelectionDragThreshold : 12f;
    }

    float GetSelectionDragMinHoldTime()
    {
        return m_SelectionSettings != null ? m_SelectionSettings.SelectionDragMinHoldTime : 0.08f;
    }

    float GetGroupMoveSpacing()
    {
        return m_SelectionSettings != null ? m_SelectionSettings.GroupMoveSpacing : 0.9f;
    }

    Color GetSelectionFillColor()
    {
        return m_SelectionSettings != null
            ? m_SelectionSettings.SelectionFillColor
            : new Color(0.2f, 0.8f, 0.2f, 0.15f);
    }

    Color GetSelectionBorderColor()
    {
        return m_SelectionSettings != null
            ? m_SelectionSettings.SelectionBorderColor
            : new Color(0.2f, 1f, 0.2f, 0.9f);
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
        DrawSelectionRectangle();

        if (ActiveUnit != null)
        {
            GUI.Label(new Rect(10, 120, 200, 20), "State: " + ActiveUnit.CurrentState.ToString(), new GUIStyle { fontSize = 30 });
            GUI.Label(new Rect(10, 160, 200, 20), "Task: " + ActiveUnit.CurrentTask.ToString(), new GUIStyle { fontSize = 30 });
            GUI.Label(new Rect(10, 200, 200, 20), "Stance: " + ActiveUnit.CurrentStance.ToString(), new GUIStyle { fontSize = 30 });
            GUI.Label(new Rect(10, 240, 260, 20), "Selected: " + m_SelectedUnits.Count, new GUIStyle { fontSize = 30 });
        }
    }
};
