using UnityEngine;

public class GoldStone : MonoBehaviour
{
    [SerializeField] private CapsuleCollider2D m_Collider;

    public Vector3 GetBottomPosition()
    {
        return m_Collider.bounds.min;
    }
}