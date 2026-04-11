using System.Threading;
using UnityEngine;

public class GameManager : SingletonManager<GameManager>
{
    private Vector2 m_initialTouchPosition;
    public Unit ActiveUnit;
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
        handleClickOnGround(worldPoint);
    }
    void handleClickOnGround(Vector2 worldPoint)
    {
        if (ActiveUnit != null)
        {
            ActiveUnit.MoveTo(worldPoint);
        }
    }

};