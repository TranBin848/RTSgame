using UnityEngine;

public class EnemyUnit : HumanoidUnit
{
    public override bool IsPlayer => false;
    protected override void UpdateBehaviour()
    {
        switch (CurrentState)
        {
            case UnitState.Idle:
                break;
            case UnitState.Moving:
                if (hasTarget)
                {
                    if (IsTargetInRange(Target.transform))
                    {
                        SetState(UnitState.Attacking);
                        Debug.Log($"{name} is attacking {Target.name}");
                    }
                    else
                    {
                        MoveTo(Target.transform.position);
                        Debug.Log($"{name} is moving towards {Target.name}");
                    }
                }
                else
                {
                    if (TryFindClosetFoe(out var foe))
                    {
                        SetTarget(foe);
                        MoveTo(foe.transform.position);
                        Debug.Log($"{name} is moving towards {foe.name}");
                    }
                }
                break;
            case UnitState.Attacking:
                if (hasTarget)
                {
                    if (IsTargetInRange(Target.transform))
                    {
                        // Attack logic here
                        Debug.Log($"{name} is attacking {Target.name}");
                    }
                    else
                    {
                        SetState(UnitState.Moving);
                        Debug.Log($"{name} is moving towards {Target.name}");
                    }
                }
                else
                {
                    SetState(UnitState.Idle);
                    Debug.Log($"{name} has no target and is now idle");
                }
                break;
        }
    }
}