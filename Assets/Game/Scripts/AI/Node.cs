using UnityEngine;

public class Node
{
    public int x;
    public int y;
    public float centerX;
    public float centerY;
    public bool isWalkable;
    public float gCost;
    public float hCost;
    public float fCost;
    public Node parent;
    public Node(Vector3Int position, Vector3 cellSize, bool isWalkable)
    {
        this.x = position.x;
        this.y = position.y;
        Vector3 halfCell = cellSize / 2f;
        var nodeCenterPosition = position + halfCell;
        centerX = nodeCenterPosition.x;
        centerY = nodeCenterPosition.y;
        this.isWalkable = isWalkable;
    }
    public override string ToString()
    {
        return $"Node({x}, {y})";
    }
}