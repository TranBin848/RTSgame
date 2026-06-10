using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Events;

public class AIPawn : MonoBehaviour
{
    [SerializeField] private float m_Speed = 8.0f;

    [Header("Seperation")]
    [SerializeField] private float m_SeparationRadius = 1f;
    [SerializeField] private float m_SeparationForce = 0.5f;
    [SerializeField] private bool m_ApplySeparation = true;
    private Vector3? m_CurrentDestination;
    private List<Vector3> m_CurrentPath = new();
    private TilemapManager m_TilemapManager;
    private int m_CurrentNodeIndex = 0;
    private GameManager m_GameManager;
    public UnityAction<Vector3> OnNewPositionSelected = delegate { };
    public UnityAction OnDestinationReached = delegate { };

    private void Start()
    {
        EnsureDependencies();
    }

    void Update()
    {
        if (!isPathValid())
        {
            m_CurrentDestination = null;
            return;
        }


        Vector3 separationVector = m_ApplySeparation ? CalculateSeperation() : Vector3.zero;
        Vector3 targetPosition = m_CurrentPath[m_CurrentNodeIndex];
        Vector3 direction = (targetPosition - transform.position).normalized;
        Vector3 combinedDirection = direction + separationVector;

        if (combinedDirection.magnitude > 1f)
        {
            combinedDirection.Normalize();
        }

        transform.position += combinedDirection * m_Speed * Time.deltaTime;

        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            if (m_CurrentNodeIndex == m_CurrentPath.Count - 1)
            {
                m_CurrentPath = new();
                OnDestinationReached.Invoke();
            }
            else
            {
                m_CurrentNodeIndex++;
                OnNewPositionSelected.Invoke(m_CurrentPath[m_CurrentNodeIndex]);
            }

        }
    }

    public void SetDestination(Vector3 destination)
    {
        if (!EnsureDependencies())
        {
            return;
        }

        if (m_CurrentDestination.HasValue && Vector3.Distance(m_CurrentDestination.Value, destination) < 0.1f)
        {
            return;
        }

        m_CurrentDestination = destination;
        m_CurrentPath = m_TilemapManager.FindPath(transform.position, destination);
        m_CurrentNodeIndex = 0;

        if (m_CurrentPath == null || m_CurrentPath.Count == 0)
        {
            m_CurrentPath = new();
            m_CurrentDestination = null;
            return;
        }

        OnNewPositionSelected.Invoke(m_CurrentPath[m_CurrentNodeIndex]);
    }
    public void Stop()
    {
        m_CurrentPath.Clear();
        m_CurrentNodeIndex = 0;
    }
    private Unit m_Unit;
    protected virtual bool GetPlayerStatus()
    {
        if (m_Unit != null)
        {
            return m_Unit.IsPlayer;
        }
        m_Unit = GetComponent<Unit>();
        return m_Unit.IsPlayer;
    }
    Vector3 CalculateSeperation()
    {
        if (!EnsureDependencies())
        {
            return Vector3.zero;
        }

        Vector3 separationVector = Vector3.zero;
        float separationRadiusSqr = m_SeparationRadius * m_SeparationRadius;
        List<Unit> units = m_GameManager.GetFriendlyUnits(GetPlayerStatus());

        foreach (var unit in units)
        {
            if (unit.gameObject == gameObject) continue;

            Vector3 opositeDirection = -unit.transform.position + transform.position;
            float sqrDistance = opositeDirection.sqrMagnitude;

            if (sqrDistance < separationRadiusSqr && sqrDistance > 0)
            {
                separationVector += opositeDirection.normalized / sqrDistance;
            }
        }

        return separationVector * m_SeparationForce;
    }
    bool isPathValid()
    {
        return m_CurrentPath.Count > 0 && m_CurrentNodeIndex < m_CurrentPath.Count;
    }

    bool EnsureDependencies()
    {
        if (m_GameManager == null)
        {
            m_GameManager = GameManager.Get();
        }

        if (m_TilemapManager == null)
        {
            m_TilemapManager = TilemapManager.Get();
        }

        return m_GameManager != null && m_TilemapManager != null;
    }
}
