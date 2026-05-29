using System.Collections.Generic;
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
            }
        }
    }
    public List<Vector3> FindPath(Vector3 startWorldPosition, Vector3 endWorldPosition)
    {
        Node startNode = FindNode(startWorldPosition);
        Node endNode = FindNode(endWorldPosition);
        if (startNode == null || endNode == null)
        {
            return new List<Vector3>();
        }
        List<Node> openList = new();
        HashSet<Node> closedList = new();

        openList.Add(startNode);

        Node closetNode = startNode;
        float closetDistanceToEnd = GetDistance(closetNode, endNode);

        while (openList.Count > 0)
        {
            Node currentNode = GetLowerFCostNode(openList);
            if (currentNode == endNode)
            {
                var path = RetracePath(startNode, endNode, startWorldPosition);
                //Debug.Log("Path found: " + string.Join(" -> ", path));
                ResetNodes(openList, closedList);
                return path;
            }

            openList.Remove(currentNode);
            closedList.Add(currentNode);

            //Debug.Log("OL: " + string.Join(", ", openList));
            //Debug.Log("CL: " + string.Join(", ", closedList));

            foreach (Node neighbor in GetNeighbors(currentNode))
            {
                if (!neighbor.isWalkable || closedList.Contains(neighbor))
                {
                    continue;
                }
                float tentativeG = currentNode.gCost + GetDistance(currentNode, neighbor);
                if (tentativeG < neighbor.gCost || !openList.Contains(neighbor))
                {
                    var distance = GetDistance(neighbor, endNode);
                    neighbor.gCost = tentativeG;
                    neighbor.hCost = distance;
                    neighbor.fCost = neighbor.gCost + neighbor.hCost;
                    neighbor.parent = currentNode;

                    if (distance < closetDistanceToEnd)
                    {
                        closetNode = neighbor;
                        closetDistanceToEnd = distance;
                    }

                    if (!openList.Contains(neighbor))
                    {
                        openList.Add(neighbor);
                    }
                }
            }

        }
        var unFinishedPath = RetracePath(startNode, closetNode, startWorldPosition);
        ResetNodes(openList, closedList);
        return unFinishedPath;
    }
    Node GetLowerFCostNode(List<Node> openList)
    {
        Node lowerFCostNode = openList[0];
        foreach (Node node in openList)
        {
            if (node.fCost < lowerFCostNode.fCost || (node.fCost == lowerFCostNode.fCost && node.hCost < lowerFCostNode.hCost))
            {
                lowerFCostNode = node;
            }
        }
        return lowerFCostNode;
    }
    List<Vector3> RetracePath(Node startNode, Node endNode, Vector3 startPosition)
    {
        List<Vector3> path = new();
        Node currentNode = endNode;

        while (currentNode != startNode)
        {
            path.Add(new Vector3(currentNode.centerX, currentNode.centerY));
            currentNode = currentNode.parent;
        }
        path.Add(startPosition);
        path.Reverse();
        return path;
    }
    float GetDistance(Node a, Node b)
    {
        int dstX = Mathf.Abs(a.x - b.x);
        int dstY = Mathf.Abs(a.y - b.y);
        if (dstX > dstY)
            return 14 * dstY + 10 * (dstX - dstY);
        return 14 * dstX + 10 * (dstY - dstX);
    }
    List<Node> GetNeighbors(Node node)
    {
        List<Node> neighbors = new();
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0)
                    continue;

                int checkX = node.x + x - m_GridOffset.x;
                int checkY = node.y + y - m_GridOffset.y;

                if (checkX >= 0 && checkX < m_Width && checkY >= 0 && checkY < m_Height)
                {
                    var neighbor = m_Grid[checkX, checkY];
                    if (neighbor.isWalkable)
                    {
                        neighbors.Add(neighbor);
                    }
                }
            }
        }
        return neighbors;
    }
    public Node FindNode(Vector3 worldPosition)
    {
        Vector3Int flooredPosition = new Vector3Int(
            Mathf.FloorToInt(worldPosition.x),
            Mathf.FloorToInt(worldPosition.y),
            0
            );
        int gridx = flooredPosition.x - m_GridOffset.x;
        int gridy = flooredPosition.y - m_GridOffset.y;
        if (gridx >= 0 && gridx < m_Width && gridy >= 0 && gridy < m_Height)
        {
            return m_Grid[gridx, gridy];
        }
        return null;
    }
    public void UpdateNodesInArea(Vector3Int startPosition, int width, int height)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int xPosition = startPosition.x + x;
                int yPosition = startPosition.y + y;

                int gridX = xPosition - m_GridOffset.x;
                int gridY = yPosition - m_GridOffset.y;
                if (gridX >= 0 && gridX < m_Width && gridY >= 0 && gridY < m_Height)
                {
                    Node node = m_Grid[gridX, gridY];
                    Vector3Int tilePosition = new Vector3Int(xPosition, yPosition, 0);
                    node.isWalkable = m_TilemapManager.CamWalkAtTile(tilePosition);
                }
            }
        }
    }
    void ResetNodes(List<Node> OL, HashSet<Node> CL)
    {
        foreach (var node in OL)
        {
            node.gCost = 0;
            node.hCost = 0;
            node.fCost = 0;
            node.parent = null;
        }
        foreach (var node in CL)
        {
            node.gCost = 0;
            node.hCost = 0;
            node.fCost = 0;
            node.parent = null;
        }
        OL.Clear();
        CL.Clear();
    }
}