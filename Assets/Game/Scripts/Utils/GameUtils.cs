using UnityEngine;
using UnityEngine.EventSystems;
public static class GameUtils
{
    public static Vector2 InputPosition => Input.touchCount > 0 ? Input.GetTouch(0).position : (Vector2)Input.mousePosition;
    public static bool isLeftClickOrTapDown => Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);
    public static bool isLeftClickOrTapUp => Input.GetMouseButtonUp(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended);
    private static Vector2 m_initialTouchPosition;

    public static bool IsScreenPositionInBounds(Vector2 inputPosition)
    {
        return inputPosition.x >= 0
            && inputPosition.x <= Screen.width
            && inputPosition.y >= 0
            && inputPosition.y <= Screen.height;
    }

    public static bool TryGetShortClickPosition(out Vector2 inputPosition, float maxDistance = 10f)
    {
        inputPosition = InputPosition;

        if (isLeftClickOrTapDown)
        {
            m_initialTouchPosition = inputPosition;
        }

        if (isLeftClickOrTapUp)
        {
            if (!IsScreenPositionInBounds(m_initialTouchPosition) || !IsScreenPositionInBounds(inputPosition))
            {
                return false;
            }

            if (Vector2.Distance(m_initialTouchPosition, inputPosition) < maxDistance)
            {
                return true;
            }
        }
        return false;
    }
    public static bool TryGetHoldPosition(out Vector3 worldPosition)
    {
        if (Input.touchCount > 0)
        {
            worldPosition = Camera.main.ScreenToWorldPoint(Input.GetTouch(0).position);
            return true;
        }
        else if (Input.GetMouseButton(0))
        {
            worldPosition = Camera.main.ScreenToWorldPoint((Vector2)Input.mousePosition);
            return true;
        }
        worldPosition = Vector3.zero;
        return false;
    }

    public static bool TryGetPointerWorldPosition(out Vector3 worldPosition)
    {
        if (Camera.main == null)
        {
            worldPosition = Vector3.zero;
            return false;
        }

        if (Input.touchCount > 0)
        {
            worldPosition = Camera.main.ScreenToWorldPoint(Input.GetTouch(0).position);
            worldPosition.z = 0f;
            return true;
        }

        worldPosition = Camera.main.ScreenToWorldPoint((Vector2)Input.mousePosition);
        worldPosition.z = 0f;
        return true;
    }

    public static bool iSPointOverUIElelement()
    {
        if (Input.touchCount > 0)
        {
            return UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
        }
        else
        {
            return UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
        }
    }
}
