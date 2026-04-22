using UnityEngine.Tilemaps;
using UnityEngine;

public class PathfindingManager : SingletonManager<PathfindingManager>
{
    [SerializeField] private Tilemap m_WalkableTilemap;
    private Pathfinding m_Pathfinding;
    void Start()
    {
        var bounds = m_WalkableTilemap.cellBounds;
        m_Pathfinding = new Pathfinding(bounds.size.x, bounds.size.y);
    }
}