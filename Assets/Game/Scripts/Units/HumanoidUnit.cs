using UnityEngine;
public class HumanoidUnit : Unit
{
    protected Vector2 m_Velocity;
    protected Vector3 m_lastPosition;
    protected float m_SmoothFactor = 50;
    protected float m_SmoothSpeed;
    public float CurrentSpeed => m_Velocity.magnitude;
    protected override void Start()
    {
        base.Start();
        m_lastPosition = transform.position;
    }
    void Update()
    {
        if (CurrentState == UnitState.Dead)
        {
            return;
        }
        UpdateVelocity();
        UpdateBehaviour();
        UpdateMovementAnimation();
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
        m_SmoothSpeed = Mathf.Lerp(m_SmoothSpeed, CurrentSpeed, Time.deltaTime * m_SmoothFactor);

        if (CurrentState != UnitState.Attacking)
        {
            var state = m_SmoothSpeed > 0.1f ? UnitState.Moving : UnitState.Idle;
            SetState(state);
        }

        m_Animator.SetFloat("Speed", Mathf.Clamp01(m_SmoothSpeed));
    }
    protected virtual void UpdateMovementAnimation()
    {
        m_Animator?.SetFloat("Speed", Mathf.Clamp01(CurrentSpeed));
    }
    protected override void PerformAttackAnimation()
    {
        Vector3 direction = (Target.transform.position - transform.position).normalized;
        m_SpriteRenderer.flipX = direction.x < 0;
        m_Animator.SetTrigger("Attack");
    }
}


