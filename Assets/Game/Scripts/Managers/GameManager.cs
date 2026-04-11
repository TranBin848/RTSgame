using System.Threading;
using UnityEngine;

public class GameManager : SingletonManager<GameManager>
{
    private Vector2 m_initialTouchPosition;
    public Unit ActiveUnit;
    public bool HasActiveUnit => ActiveUnit != null;
    void Update()
    {
        Vector2 inputPosition = Input.touchCount > 0 ? Input.GetTouch(0).position : Input.mousePosition;

        if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
        {
            m_initialTouchPosition = inputPosition;
        }

        if (Input.GetMouseButtonUp(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended))
        {
            if (Vector2.Distance(m_initialTouchPosition, inputPosition) < 10f)
            {
                DetectClick(inputPosition);
            }
        }
    }

    void DetectClick(Vector2 inputPosition)
    {
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
        if (ActiveUnit != null)
        {
            ActiveUnit.MoveTo(worldPoint);
        }
    }
    void handleClickOnUnit(Unit unit)
    {
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
    }
};