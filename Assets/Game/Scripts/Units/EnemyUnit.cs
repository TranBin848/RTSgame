using UnityEngine;

public class EnemyUnit : HumanoidUnit
{
    public override bool IsPlayer => false;
    protected override void UpdateBehaviour()
    {
        if (TryFindClosetFoe(out var foe))
        {
            Target = foe;
            Debug.Log($"{name} found a foe: {foe.name}");
        }
    }
}