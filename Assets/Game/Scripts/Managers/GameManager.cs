using System.Threading;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

public class GameManager : SingletonManager<GameManager>
{
    [Header("Tilemaps")]
    [SerializeField] private Tilemap m_WalkableTilemap;
    [SerializeField] private Tilemap m_OverlayTilemap;
    [SerializeField] private Tilemap[] m_UnreachableTilemaps;
    [Header("UI")]
    [SerializeField] private PointToClick m_PointToClickPrefab;
    [SerializeField] private ActionBar m_ActionBar;
    [SerializeField] private ConfirmationBar m_ConfirmationBar;
    private PlacementProcess m_PlacementProcess;
    private int m_Gold = 1000;
    private int m_Wood = 1000;
    public int Gold => m_Gold;
    public int Wood => m_Wood;

    public Unit ActiveUnit;
    public bool HasActiveUnit => ActiveUnit != null;
    void Start()
    {
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

    public void StartBuildProcess(BuildActionSo buildAction)
    {
        if (m_PlacementProcess != null)
        {
            return;
        }

        m_PlacementProcess = new PlacementProcess(buildAction, m_WalkableTilemap, m_OverlayTilemap, m_UnreachableTilemaps);
        m_PlacementProcess.ShowPlacementOutline();
        m_ConfirmationBar.Show(m_PlacementProcess.BuildAction.GoldCost, m_PlacementProcess.BuildAction.WoodCost);
        m_ConfirmationBar.SetupHooks(ConfirmBuildProcess, CancelBuildProcess);
    }
    void DetectClick(Vector2 inputPosition)
    {
        if (GameUtils.iSPointOverUIElelement())
        {
            return;
        }
        Vector2 worldPoint = Camera.main.ScreenToWorldPoint(inputPosition);
        RaycastHit2D hit = Physics2D.Raycast(worldPoint, Vector2.zero);

        if (HasClickedOnUnit(hit, out var unit))
        {
            handleClickOnUnit(unit);
        }
        else
        {
            handleClickOnGround(worldPoint);
        }
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
            ActiveUnit.MoveTo(worldPoint);
        }
    }
    void handleClickOnUnit(Unit unit)
    {
        if (isClickedOnActiveUnit(unit))
        {
            cancelActiveUnit();
            return;
        }
        SelectNewUnit(unit);
    }
    void SelectNewUnit(Unit unit)
    {
        if (HasActiveUnit)
        {
            ActiveUnit.Deselect();
        }
        ActiveUnit = unit;
        ActiveUnit.Select();
        ShowUnitAction(unit);
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

            new BuildingProcess(m_PlacementProcess.BuildAction, buildPosition);
            ActiveUnit.MoveTo(buildPosition);

            m_PlacementProcess = null;

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
    }
};