using UnityEngine;

public class WarriorUnit : HumanoidUnit
{
    private bool m_IsRetreating = false;
    protected override void OnSetTask(UnitTask oldTask, UnitTask newTask)
    {
        if (newTask == UnitTask.Attack && hasTarget)
        {
            MoveTo(Target.transform.position);
        }
        base.OnSetTask(oldTask, newTask);
    }

    protected override void OnSetDestination()
    {
        if (hasTarget && (CurrentTask == UnitTask.Attack || CurrentState == UnitState.Attacking))
        {
            m_IsRetreating = true;
        }
        if (CurrentTask == UnitTask.Attack)
        {
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
            }
            else
            {
                if (!m_IsRetreating && TryFindClosetFoe(out var foe))
                {
                    SetTarget(foe);
                    SetTask(UnitTask.Attack);
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
                    MoveTo(Target.transform.position);
                    SetState(UnitState.Idle);
                }
            }
            else
            {
                SetState(UnitState.Idle);
            }
        }
    }
}