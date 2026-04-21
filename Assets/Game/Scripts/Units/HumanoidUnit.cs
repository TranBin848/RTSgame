using UnityEngine;
public class HumanoidUnit : Unit
{
    protected Vector2 m_Velocity;
    protected Vector3 m_lastPosition;
    public float CurrentSpeed => m_Velocity.magnitude;
    void Start()
    {
        m_lastPosition = transform.position;
    }
    void Update()
    {
        UpdateVelocity();
        UpdateBehaviour();
    }
    protected virtual void UpdateBehaviour()
    {

    }
    protected virtual void UpdateVelocity()
    {
        m_Velocity = new Vector2(
            transform.position.x - m_lastPosition.x,
            transform.position.y - m_lastPosition.y
            ) / Time.deltaTime;

        m_lastPosition = transform.position;
        var state = m_Velocity.magnitude > 0.01f ? UnitState.Moving : UnitState.Idle;
        SetState(state);

        m_Animator.SetFloat("Speed", CurrentSpeed);
    }
}


