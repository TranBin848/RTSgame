using System.Net.Mail;
using UnityEngine;

public class Pathfinding
{
    private int m_Width;
    private int m_Height;
    private Node[,] m_Grid;
    private Node[,] Grid => m_Grid;
    private TilemapManager m_TilemapManager;
    public Pathfinding(TilemapManager tilemapManager)
    {
        m_TilemapManager = tilemapManager;
        m_TilemapManager.PathfindingTilemap.CompressBounds();
        var bounds = m_TilemapManager.PathfindingTilemap.cellBounds;
        m_Width = bounds.size.x;
        m_Height = bounds.size.y;
        m_Grid = new Node[m_Width, m_Height];
        InitializeGrid(bounds.min);
    }
    void InitializeGrid(Vector3Int offset)
    {
        for (int x = 0; x < m_Width; x++)
        {
            for (int y = 0; y < m_Height; y++)
            {
                var nodePosition = new Vector3Int(x + offset.x, y + offset.y, 0);
                var node = new Node(nodePosition.x, nodePosition.y, true);
                m_Grid[x, y] = node;
                Debug.Log("Node at " + nodePosition + " is walkable: " + node.isWalkable);
            }
        }
    }
}