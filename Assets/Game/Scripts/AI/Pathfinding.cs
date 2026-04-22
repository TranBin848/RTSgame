using System.Net.Mail;
using UnityEngine;

public class Pathfinding
{
    private int m_Width;
    private int m_Height;
    private Vector3Int m_GridOffset;
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
        m_GridOffset = bounds.min;
        InitializeGrid();
    }
    void InitializeGrid()
    {
        Vector3 cellSize = m_TilemapManager.PathfindingTilemap.cellSize;

        for (int x = 0; x < m_Width; x++)
        {
            for (int y = 0; y < m_Height; y++)
            {
                var nodeLeftBottomPosition = new Vector3Int(x + m_GridOffset.x, y + m_GridOffset.y, 0);
                bool isWalkable = m_TilemapManager.CamWalkAtTile(nodeLeftBottomPosition);
                var node = new Node(nodeLeftBottomPosition, cellSize, isWalkable);
                m_Grid[x, y] = node;
                Debug.Log("Node at " + nodeLeftBottomPosition + " is walkable: " + node.isWalkable);
            }
        }
    }
}