using UnityEngine;

public abstract class ResourceNodeBase : MonoBehaviour, IResourceNode
{
    [SerializeField] private CapsuleCollider2D m_Collider;
    [SerializeField] private Animator m_Animator;
    [SerializeField] private float m_InteractionRadius = 0.1f;
    [SerializeField] private Transform[] m_InteractionPoints;

    private bool m_IsClaimed;

    public abstract ResourceType ResourceType { get; }
    public bool IsClaimed => m_IsClaimed;
    public float InteractionRadius => m_InteractionRadius;
    protected CapsuleCollider2D Collider => m_Collider;
    protected Animator Animator => m_Animator;

    protected virtual void Awake()
    {
        if (m_Collider == null)
        {
            m_Collider = GetComponent<CapsuleCollider2D>();
        }

        if (m_Animator == null)
        {
            m_Animator = GetComponent<Animator>();
        }

        OnInitialize();
    }

    public bool TryClaim()
    {
        if (m_IsClaimed)
        {
            return false;
        }

        m_IsClaimed = true;
        // When claimed, disable collider so worker can pathfind through resource tile
        if (m_Collider != null)
        {
            m_Collider.enabled = false;
        }
        UpdatePathfindingNode();
        return true;
    }

    public void Release()
    {
        m_IsClaimed = false;
        // When released, re-enable collider to block pathfinding again
        if (m_Collider != null)
        {
            m_Collider.enabled = true;
        }
        UpdatePathfindingNode();
    }

    private void UpdatePathfindingNode()
    {
        var tilemapManager = TilemapManager.Get();
        if (tilemapManager != null && tilemapManager.CanServePathfindingRequests())
        {
            if (m_Collider != null)
            {
                Bounds bounds = m_Collider.bounds;
                Vector3Int min = tilemapManager.WalkableTilemap.WorldToCell(bounds.min);
                Vector3Int max = tilemapManager.WalkableTilemap.WorldToCell(bounds.max);

                // Thêm padding 1 ô để bao phủ tất cả các ô bị đè lên
                min.x -= 1; min.y -= 1;
                max.x += 1; max.y += 1;

                int width = max.x - min.x + 1;
                int height = max.y - min.y + 1;

                tilemapManager.UpdateNodesInArea(min, width, height);
            }
            else
            {
                Vector3Int tilePos = tilemapManager.WalkableTilemap.WorldToCell(transform.position);
                tilemapManager.UpdateNodesInArea(tilePos, 1, 1);
            }
        }
    }

    public void Hit()
    {
        if (m_Animator != null)
        {
            m_Animator.SetTrigger("Hit");
        }
    }

    public Vector3 GetInteractionPoint(Vector3 requesterPosition)
    {
        if (m_InteractionPoints != null && m_InteractionPoints.Length > 0)
        {
            Transform closestPoint = null;
            float closestDistanceSqr = float.MaxValue;
            foreach (var point in m_InteractionPoints)
            {
                if (point == null) continue;
                float distSqr = (point.position - requesterPosition).sqrMagnitude;
                if (distSqr < closestDistanceSqr)
                {
                    closestDistanceSqr = distSqr;
                    closestPoint = point;
                }
            }
            if (closestPoint != null)
            {
                return closestPoint.position;
            }
        }

        return transform.position;
    }

    protected void SetInteractionRadius(float radius)
    {
        m_InteractionRadius = radius;
    }

    protected virtual void OnInitialize()
    {
    }

    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        if (m_InteractionPoints != null)
        {
            foreach (var point in m_InteractionPoints)
            {
                if (point != null)
                {
                    Gizmos.DrawWireSphere(point.position, m_InteractionRadius);
                }
            }
        }
    }

}
