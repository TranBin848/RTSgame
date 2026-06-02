using UnityEngine;

public class GoldStone : MonoBehaviour
{
    [SerializeField] private CapsuleCollider2D m_Collider;
    [SerializeField] private float m_ColliderRadius;
    public float ColliderRadius => m_ColliderRadius;
    public Animator m_Animator;
    private bool m_Claimed = false;
    public bool Claimed => m_Claimed;

    void Start()
    {
        if (m_Collider == null)
        {
            m_Collider = GetComponent<CapsuleCollider2D>();
        }
        m_ColliderRadius = m_Collider.size.x / 4f;
    }
    public Vector3 GetBottomPosition()
    {
        return m_Collider.bounds.min;
    }
    public void Hit()
    {
        m_Animator.SetTrigger("Hit");
    }
    public bool TryToClaim()
    {
        if (!m_Claimed)
        {
            m_Claimed = true;
            return true;
        }
        return false;
    }
    public void Release()
    {
        m_Claimed = false;
    }
}