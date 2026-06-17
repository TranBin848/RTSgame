using UnityEngine;

public class EnemyUnit : HumanoidUnit
{
    private float m_AttackCommitmentTime = 1f;
    private float m_CurrentAttackCommitmentTime = 0f;
    private bool m_IsRetreating;
    public override bool IsPlayer => false;
    public override bool IsTargetable => base.IsTargetable && !m_IsRetreating;

    public void ReturnToSpawnRingAndDespawn(Vector3 retreatPosition)
    {
        m_IsRetreating = true;
        SetTarget(null);
        SetTask(UnitTask.None);
        SetState(UnitState.Moving);
        StopMovement();
        OnRetreatStarted();
        MoveTo(retreatPosition);
    }

    public override void OnSpawnedFromPool(bool isReused)
    {
        base.OnSpawnedFromPool(isReused);
        m_IsRetreating = false;
    }

    protected override void UpdateBehaviour()
    {
        if (m_IsRetreating)
        {
            return;
        }

        switch (CurrentState)
        {
            case UnitState.Idle:
            case UnitState.Moving:
                if (hasTarget)
                {
                    if (!Target.IsTargetable)
                    {
                        SetTarget(null);
                        return;
                    }

                    if (IsTargetInRange(Target))
                    {
                        SetState(UnitState.Attacking);
                        StopMovement();
                    }
                    else
                    {
                        MoveTo(Target.transform.position);
                    }
                }
                else
                {
                    if (TryFindClosetFoe(out var foe))
                    {
                        SetTarget(foe);
                        MoveTo(foe.transform.position);
                    }
                }
                break;
            case UnitState.Attacking:
                if (hasTarget)
                {
                    if (!Target.IsTargetable)
                    {
                        SetTarget(null);
                        SetState(UnitState.Idle);
                        return;
                    }

                    if (IsTargetInRange(Target))
                    {
                        m_CurrentAttackCommitmentTime = m_AttackCommitmentTime;
                        TryAttackCurrentTarget();
                    }
                    else
                    {
                        m_CurrentAttackCommitmentTime -= Time.deltaTime;
                        if (m_CurrentAttackCommitmentTime <= 0f)
                        {
                            SetState(UnitState.Moving);
                        }
                    }
                }
                else
                {
                    SetState(UnitState.Idle);
                }
                break;
        }
    }

    protected override void OnDestinationReached()
    {
        base.OnDestinationReached();

        if (!m_IsRetreating)
        {
            return;
        }

        m_IsRetreating = false;
        DespawnToPool();
    }

    protected virtual void OnRetreatStarted()
    {
    }
}
