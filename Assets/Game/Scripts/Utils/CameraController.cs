using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float m_PanSpeed = 20f;
    [SerializeField] private float m_EdgeSize = 30f;
    [SerializeField] private float m_Smoothing = 10f;

    [Header("Zoom Settings")]
    [SerializeField] private float m_ZoomSpeed = 5f;
    [SerializeField] private float m_MinZoom = 3f;
    [SerializeField] private float m_MaxZoom = 15f;

    private Camera m_MainCamera;
    private Vector3 m_DragOrigin;
    private bool m_IsDragging;
    private Vector3 m_CurrentVelocity;

    public bool LockCamera { get; set; }

    private void Start()
    {
        m_MainCamera = GetComponent<Camera>();
        if (m_MainCamera == null)
        {
            m_MainCamera = Camera.main;
        }
    }

    private void LateUpdate()
    {
        if (LockCamera || m_MainCamera == null)
        {
            return;
        }

        HandleTouchPanning();
        HandleMouseAndKeyboardPanning();
        HandleZoom();
    }

    private void HandleTouchPanning()
    {
        if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Moved)
        {
            Vector2 touchDeltaPosition = Input.GetTouch(0).deltaPosition;
            Vector2 normalizedDelta = touchDeltaPosition / new Vector2(Screen.width, Screen.height);
            m_MainCamera.transform.Translate(-normalizedDelta.x * m_PanSpeed, -normalizedDelta.y * m_PanSpeed, 0);
        }
    }

    private void HandleMouseAndKeyboardPanning()
    {
        if (Input.touchCount > 0) return;

        Vector3 moveDirection = Vector3.zero;

        // Bàn phím (WASD / Arrows)
        moveDirection.x = Input.GetAxisRaw("Horizontal");
        moveDirection.y = Input.GetAxisRaw("Vertical");

        // Edge Scrolling (Di chuyển khi chuột ở rìa màn hình)
        Vector3 mousePos = Input.mousePosition;
        if (mousePos.x >= 0 && mousePos.x <= Screen.width && mousePos.y >= 0 && mousePos.y <= Screen.height)
        {
            if (mousePos.x >= Screen.width - m_EdgeSize) moveDirection.x += 1f;
            else if (mousePos.x <= m_EdgeSize) moveDirection.x -= 1f;

            if (mousePos.y >= Screen.height - m_EdgeSize) moveDirection.y += 1f;
            else if (mousePos.y <= m_EdgeSize) moveDirection.y -= 1f;
        }

        // Kéo thả bằng chuột giữa (Middle Mouse Button)
        if (Input.GetMouseButtonDown(2))
        {
            m_DragOrigin = m_MainCamera.ScreenToWorldPoint(Input.mousePosition);
            m_IsDragging = true;
            m_CurrentVelocity = Vector3.zero; // Reset velocity khi bắt đầu kéo
        }

        if (Input.GetMouseButton(2))
        {
            Vector3 difference = m_DragOrigin - m_MainCamera.ScreenToWorldPoint(Input.mousePosition);
            m_MainCamera.transform.position += difference;
        }
        else
        {
            m_IsDragging = false;
        }

        // Áp dụng mượt mà (Smoothing) cho Edge Scroll và Keyboard
        if (!m_IsDragging)
        {
            Vector3 targetVelocity = moveDirection.normalized * m_PanSpeed;
            m_CurrentVelocity = Vector3.Lerp(m_CurrentVelocity, targetVelocity, Time.deltaTime * m_Smoothing);
            m_MainCamera.transform.Translate(m_CurrentVelocity * Time.deltaTime, Space.World);
        }
    }

    private void HandleZoom()
    {
        if (Input.touchCount == 2)
        {
            Touch touchZero = Input.GetTouch(0);
            Touch touchOne = Input.GetTouch(1);

            Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
            Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

            float prevMagnitude = (touchZeroPrevPos - touchOnePrevPos).magnitude;
            float currentMagnitude = (touchZero.position - touchOne.position).magnitude;

            float difference = currentMagnitude - prevMagnitude;
            Zoom(-difference * 0.01f);
        }
        else if (Input.touchCount == 0)
        {
            float scrollData = Input.GetAxis("Mouse ScrollWheel");
            if (scrollData != 0.0f)
            {
                Zoom(scrollData * m_ZoomSpeed);
            }
        }
    }

    private void Zoom(float increment)
    {
        if (m_MainCamera.orthographic)
        {
            m_MainCamera.orthographicSize -= increment;
            m_MainCamera.orthographicSize = Mathf.Clamp(m_MainCamera.orthographicSize, m_MinZoom, m_MaxZoom);
        }
        else
        {
            Vector3 pos = m_MainCamera.transform.position;
            pos += m_MainCamera.transform.forward * increment;
            m_MainCamera.transform.position = pos;
        }
    }
}