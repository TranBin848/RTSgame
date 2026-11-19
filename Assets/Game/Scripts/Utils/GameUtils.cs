using UnityEngine;
using UnityEngine.EventSystems;
public static class GameUtils
{
    public static Vector2 InputPosition => Input.touchCount > 0 ? Input.GetTouch(0).position : (Vector2)Input.mousePosition;
    public static bool isLeftClickOrTapDown => Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);
    public static bool isLeftClickOrTapUp => Input.GetMouseButtonUp(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended);

    public static bool IsScreenPositionInBounds(Vector2 inputPosition)
    {
        return inputPosition.x >= 0
            && inputPosition.x <= Screen.width
            && inputPosition.y >= 0
            && inputPosition.y <= Screen.height;
    }

    public static bool TryGetShortClickPosition(ref Vector2 initialTouchPosition, out Vector2 inputPosition, float maxDistance = 10f)
    {
        inputPosition = InputPosition;

        if (isLeftClickOrTapDown)
        {
            initialTouchPosition = inputPosition;
        }

        if (isLeftClickOrTapUp)
        {
            if (!IsScreenPositionInBounds(initialTouchPosition) || !IsScreenPositionInBounds(inputPosition))
            {
                return false;
            }

            if (Vector2.Distance(initialTouchPosition, inputPosition) < maxDistance)
            {
                return true;
            }
        }
        return false;
    }
    public static bool TryGetHoldPosition(Camera camera, out Vector3 worldPosition)
    {
        if (camera == null) { worldPosition = Vector3.zero; return false; }
        if (Input.touchCount > 0)
        {
            worldPosition = camera.ScreenToWorldPoint(Input.GetTouch(0).position);
            return true;
        }
        else if (Input.GetMouseButton(0))
        {
            worldPosition = camera.ScreenToWorldPoint((Vector2)Input.mousePosition);
            return true;
        }
        worldPosition = Vector3.zero;
        return false;
    }

    public static bool TryGetPointerWorldPosition(Camera camera, out Vector3 worldPosition)
    {
        if (camera == null)
        {
            worldPosition = Vector3.zero;
            return false;
        }

        if (Input.touchCount > 0)
        {
            worldPosition = camera.ScreenToWorldPoint(Input.GetTouch(0).position);
            worldPosition.z = 0f;
            return true;
        }

        worldPosition = camera.ScreenToWorldPoint((Vector2)Input.mousePosition);
        worldPosition.z = 0f;
        return true;
    }

    public static bool iSPointOverUIElelement()
    {
        if (UnityEngine.EventSystems.EventSystem.current == null)
        {
            return false;
        }

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
