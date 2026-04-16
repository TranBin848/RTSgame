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
        Debug.Log($"Updating placement process for {m_BuildAction.name}");
    }
    public void ShowPlacementOutline()
    {
        m_PlacementOutline = new GameObject("PlacementOutline");
        var spriteRenderer = m_PlacementOutline.AddComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = 999;
        spriteRenderer.color = new Color(1f, 1f, 1f, 0.7f);
        spriteRenderer.sprite = m_BuildAction.PlacementSprite;
    }
}