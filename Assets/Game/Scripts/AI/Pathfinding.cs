using UnityEngine;

public class Pathfinding
{
    private Node[,] m_Grid;
    private Node[,] Grid => m_Grid;
    public Pathfinding(int width, int height)
    {
        m_Grid = new Node[width, height];
        Debug.Log($"Created grid with size {width}x{height}");
    }
}