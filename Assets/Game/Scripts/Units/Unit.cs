using UnityEngine;

public abstract class Unit : MonoBehaviour
{
    public bool isMoving = false;
    protected Animator m_Animator;
    protected AIPawn m_AIPawn;
    protected SpriteRenderer m_SpriteRenderer;
    protected void Awake()
    {
        if (TryGetComponent<Animator>(out var animator))
        {
            m_Animator = animator;
        }
        if (TryGetComponent<AIPawn>(out var aiPawn))
        {
            m_AIPawn = aiPawn;
        }
        if (TryGetComponent<SpriteRenderer>(out var spriteRenderer))
        {
            m_SpriteRenderer = spriteRenderer;
        }
    }
    public void MoveTo(Vector3 destination)
    {
        var direction = (destination - transform.position).normalized;
        m_SpriteRenderer.flipX = direction.x < 0;

        if (m_AIPawn != null)
        {
            m_AIPawn.SetDestination(destination);
        }
    }
}
