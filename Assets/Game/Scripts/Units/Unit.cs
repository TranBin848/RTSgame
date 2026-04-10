using UnityEngine;

public abstract class Unit : MonoBehaviour
{
    public bool isMoving = false;
    protected Animator m_Animator;

    protected void Awake()
    {
        if (TryGetComponent<Animator>(out var animator))
        {
            m_Animator = animator;
        }
        GameManager.Get()?.Test();
    }
}
