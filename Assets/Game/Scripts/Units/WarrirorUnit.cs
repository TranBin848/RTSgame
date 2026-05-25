using UnityEngine;

public class WarriorUnit : HumanoidUnit
{
    private bool m_IsRetreating = false;
    public override void SetStance(UnitStanceActionSO stance)
    {
        base.SetStance(stance);
        if (CurrentStance == UnitStance.Defensive)
        {
            SetState(UnitState.Idle);
            StopMovement();
            m_IsRetreating = false;
        }
    }
    protected override void OnSetState(UnitState oldState, UnitState newState)
    {
        if (newState == UnitState.Attacking)
        {
            m_NextAutoAttackTime = Time.time + m_AutoAttackFrequency / 2f;
        }
        base.OnSetState(oldState, newState);
    }

    protected override void OnSetTask(UnitTask oldTask, UnitTask newTask)
    {
        if (newTask == UnitTask.Attack && hasTarget)
        {
            MoveTo(Target.transform.position);
        }
        base.OnSetTask(oldTask, newTask);
    }

    protected override void OnSetDestination(DestinationSource source)
    {
        if (hasTarget && source == DestinationSource.PlayerClick && (CurrentTask == UnitTask.Attack || CurrentState == UnitState.Attacking))
        {
            m_IsRetreating = true;
            SetTask(UnitTask.None);
            SetTarget(null);
        }
    }
    protected override void OnDestinationReached()
    {
        m_IsRetreating = false;
    }
    protected override void UpdateBehaviour()
    {
        if (CurrentState == UnitState.Idle || CurrentState == UnitState.Moving)
        {
            if (hasTarget)
            {
                if (IsTargetInRange(Target.transform))
                {
                    StopMovement();
                    SetState(UnitState.Attacking);
                }
                else if (CurrentStance == UnitStance.Offensive)
                {
                    MoveTo(Target.transform.position);
                }
            }
            else
            {
                if (CurrentStance == UnitStance.Offensive)
                {
                    if (!m_IsRetreating && TryFindClosetFoe(out var foe))
                    {
                        SetTarget(foe);
                        SetTask(UnitTask.Attack);
                    }
                }
            }
        }
        else if (CurrentState == UnitState.Attacking)
        {
            if (hasTarget)
            {
                if (IsTargetInRange(Target.transform))
                {
                    TryAttackCurrentTarget();
                }
                else
                {
                    if (CurrentStance == UnitStance.Defensive)
                    {
                        SetTarget(null);
                        SetState(UnitState.Idle);
                    }
                    else
                    {
                        MoveTo(Target.transform.position);
                    }
                }
            }
            else
            {
                SetState(UnitState.Idle);
            }
        }
    }
}