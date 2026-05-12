using UnityEngine;

public class EnemyUnit : HumanoidUnit
{
    public override bool IsPlayer => false;
    protected override void UpdateBehaviour()
    {
        switch (CurrentState)
        {
            case UnitState.Idle:
            case UnitState.Moving:
                if (hasTarget)
                {
                    if (IsTargetInRange(Target.transform))
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
                    if (IsTargetInRange(Target.transform))
                    {
                        TryAttackCurrentTarget();
                    }
                    else
                    {
                        SetState(UnitState.Moving);
                    }
                }
                else
                {
                    SetState(UnitState.Idle);
                }
                break;
        }
    }
}