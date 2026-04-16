using UnityEngine;

public class PlacementProcess
{
    private GameObject m_PlacementOutline;
    private BuildActionSo m_BuildAction;
    public PlacementProcess(BuildActionSo buildAction)
    {
        m_BuildAction = buildAction;
    }
    public void Update()
    {
        if (GameUtils.TryGetHoldPosition(out Vector3 worldPosition))
        {
            m_PlacementOutline.transform.position = SnapToGrid(worldPosition);
        }
    }
    public void ShowPlacementOutline()
    {
        m_PlacementOutline = new GameObject("PlacementOutline");
        var spriteRenderer = m_PlacementOutline.AddComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = 999;
        spriteRenderer.color = new Color(1f, 1f, 1f, 0.7f);
        spriteRenderer.sprite = m_BuildAction.PlacementSprite;
    }
    Vector3 SnapToGrid(Vector3 worldPosition)
    {
        return new Vector3(Mathf.Round(worldPosition.x), Mathf.Round(worldPosition.y), 0f);
    }
}