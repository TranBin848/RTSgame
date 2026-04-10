using System.Threading;
using UnityEngine;

public class GameManager : SingletonManager<GameManager>
{
    private Vector2 m_initialTouchPosition;
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
                DeleteClick(inputPosition);
            }
        }
    }

    void DeleteClick(Vector2 inputPosition)
    {
        Debug.Log($"Clicked at screen position: {inputPosition}");
    }
};