using UnityEngine;
using System.Collections.Generic;

public class AIPawn : MonoBehaviour
{
    [SerializeField] private float m_Speed = 2.0f;
    private List<Node> m_CurrentPath = new();
    private TilemapManager m_TilemapManager;
    private int m_CurrentNodeIndex = 0;
    private Vector3? m_Destination;
    public Vector3 Destination
    {
        get => m_Destination ?? transform.position;
    }
    private void Start()
    {
        m_TilemapManager = TilemapManager.Get();
    }

    void Update()
    {
        if (!isPathValid())
        {
            m_Destination = null;
            return;
        }
        Node currentNode = m_CurrentPath[m_CurrentNodeIndex];
        Vector3 targetPosition = new Vector3(currentNode.centerX, currentNode.centerY, transform.position.z);
        Vector3 direction = (targetPosition - transform.position).normalized;

        transform.position += direction * m_Speed * Time.deltaTime;

        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            m_CurrentNodeIndex++;
        }
    }

    public void SetDestination(Vector3 destination)
    {
        m_CurrentPath = m_TilemapManager.FindPath(transform.position, destination);
        m_Destination = destination;
        m_CurrentNodeIndex = 0;
    }

    bool isPathValid()
    {
        return m_CurrentPath.Count > 0 && m_CurrentNodeIndex < m_CurrentPath.Count;
    }
}