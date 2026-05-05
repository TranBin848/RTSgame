using UnityEngine;

public class CameraController
{
    private float m_PanSpeed;

    public bool LockCamera { get; set; }

    public CameraController(float panSpeed)
    {
        m_PanSpeed = panSpeed;
    }
    public void Update()
    {
        if (LockCamera)
        {
            return;
        }
        if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Moved)
        {
            Vector2 touchDeltaPosition = Input.GetTouch(0).deltaPosition;
            Vector2 normalizedDelta = touchDeltaPosition / new Vector2(Screen.width, Screen.height);
            Camera.main.transform.Translate(-normalizedDelta.x * m_PanSpeed, -normalizedDelta.y * m_PanSpeed, 0);
        }
        else if (Input.touchCount == 0 && Input.GetMouseButton(0))
        {
            Vector2 mousePosition = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
            Camera.main.transform.Translate(-mousePosition.x * Time.deltaTime * m_PanSpeed, -mousePosition.y * Time.deltaTime * m_PanSpeed, 0);
        }
    }
}