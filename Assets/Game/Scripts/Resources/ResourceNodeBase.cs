using UnityEngine;

public abstract class ResourceNodeBase : MonoBehaviour, IResourceNode
{
    [SerializeField] private CapsuleCollider2D m_Collider;
    [SerializeField] private Animator m_Animator;
    [SerializeField] private float m_InteractionRadius = 0.1f;
    [SerializeField] private Transform m_InteractionPoint;

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
    }

    public void Hit()
    {
        if (m_Animator != null)
        {
            m_Animator.SetTrigger("Hit");
        }
    }

    public Vector3 GetInteractionPoint()
    {
        if (m_InteractionPoint != null)
        {

            return m_InteractionPoint.position;
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
        Vector3 position = GetInteractionPoint();
        Gizmos.DrawWireSphere(position, m_InteractionRadius);
    }

}
