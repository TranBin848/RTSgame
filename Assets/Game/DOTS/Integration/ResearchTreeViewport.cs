using UnityEngine;
using UnityEngine.EventSystems;

public sealed class ResearchTreeViewport : MonoBehaviour, IBeginDragHandler, IDragHandler,
    IEndDragHandler, IScrollHandler, IInitializePotentialDragHandler
{
    public RectTransform Content { get; set; }
    public float Zoom => m_Zoom;
    public bool IsDragging { get; private set; }
    public const float MinZoom = .45f, MaxZoom = 1.6f;
    float m_Zoom = 1;
    Vector2 m_Position, m_LastPointer;
    RectTransform View => (RectTransform)transform;

    public void ResetView()
    {
        Canvas.ForceUpdateCanvases();
        m_Zoom = Mathf.Clamp(Mathf.Min(View.rect.width / 1660, View.rect.height / 1040), MinZoom, 1);
        m_Position = new Vector2(0, 50 * m_Zoom);
        Content.anchoredPosition = m_Position;
        Content.localScale = Vector3.one * m_Zoom;
        IsDragging = false;
    }
    public void SetZoom(float value, Vector2 pivot)
    {
        float next = Mathf.Clamp(value, MinZoom, MaxZoom);
        if (!float.IsFinite(next) || !float.IsFinite(pivot.x) || !float.IsFinite(pivot.y)) return;
        m_Position = pivot - (pivot - m_Position) * (next / m_Zoom);
        m_Zoom = next;
        ClampPosition();
    }
    public void OnScroll(PointerEventData e)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(View, e.position, e.pressEventCamera, out var point);
        SetZoom(m_Zoom * Mathf.Exp(e.scrollDelta.y * .12f), point);
    }
    public void OnInitializePotentialDrag(PointerEventData e) => e.useDragThreshold = true;
    public void OnBeginDrag(PointerEventData e)
    {
        if (e.button != PointerEventData.InputButton.Left && e.button != PointerEventData.InputButton.Middle) return;
        IsDragging = true;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(View, e.position, e.pressEventCamera, out m_LastPointer);
    }
    public void OnDrag(PointerEventData e)
    {
        if (!IsDragging) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(View, e.position, e.pressEventCamera, out var point);
        m_Position += point - m_LastPointer;
        m_LastPointer = point;
        ClampPosition();
        Content.anchoredPosition = m_Position;
        e.eligibleForClick = false;
    }
    public void OnEndDrag(PointerEventData e) => IsDragging = false;
    void OnDisable() => IsDragging = false;
    void Update()
    {
        if (Content == null) return;
        float blend = 1 - Mathf.Exp(-16 * Time.unscaledDeltaTime);
        Content.localScale = Vector3.Lerp(Content.localScale, Vector3.one * m_Zoom, blend);
        Content.anchoredPosition = Vector2.Lerp(Content.anchoredPosition, m_Position, blend);
    }
    void ClampPosition()
    {
        var limit = new Vector2(850 * m_Zoom + View.rect.width * .3f, 600 * m_Zoom + View.rect.height * .3f);
        m_Position = Vector2.Max(-limit, Vector2.Min(limit, m_Position));
    }
}
