using UnityEngine;
using UnityEngine.UIElements;

public class AIPawn : MonoBehaviour
{
    [SerializeField] private float m_Speed = 2.0f;
    private Vector3? m_Destination;
    public Vector3 Destination
    {
        get => m_Destination ?? transform.position;
    }

    void Update()
    {
        if (m_Destination.HasValue)
        {
            var dir = m_Destination.Value - transform.position;
            transform.position += dir.normalized * Time.deltaTime * m_Speed;
        }

        var distanceToDestination = Vector3.Distance(transform.position, Destination);
        if (distanceToDestination < 0.1f)
        {
            m_Destination = null;
        }
    }

    public void SetDestination(Vector3 destination)
    {
        m_Destination = destination;
    }


}