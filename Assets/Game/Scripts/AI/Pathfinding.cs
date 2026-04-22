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
        InitializeGrid();
    }
    void InitializeGrid()
    {
        Vector3 halfCellSize = m_TilemapManager.PathfindingTilemap.cellSize / 2f;
        Vector3Int offSet = m_TilemapManager.PathfindingTilemap.cellBounds.min;

        for (int x = 0; x < m_Width; x++)
        {
            for (int y = 0; y < m_Height; y++)
            {
                var nodeLeftBottomPosition = new Vector3Int(x + offSet.x, y + offSet.y, 0);
                var nodeCenterPosition = nodeLeftBottomPosition + halfCellSize;
                bool isWalkable = m_TilemapManager.CamWalkAtTile(nodeLeftBottomPosition);
                var node = new Node(nodeCenterPosition.x, nodeCenterPosition.y, isWalkable);
                m_Grid[x, y] = node;
                Debug.Log("Node at " + nodeCenterPosition + " is walkable: " + node.isWalkable);
            }
        }
    }
}