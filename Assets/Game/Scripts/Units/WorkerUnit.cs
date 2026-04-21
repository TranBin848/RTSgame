using System.Diagnostics.Tracing;
using NUnit.Framework.Constraints;
using UnityEngine;

public class WorkerUnit : HumanoidUnit
{
    protected override void UpdateBehaviour()
    {
        if (CurrentTask != UnitTask.None)
        {
            CheckForCloseObjects();

        }
    }
    private void CheckForCloseObjects()
    {
        var hits = RunProximityObjectDetection();
        foreach (var hit in hits)
        {
            if (CurrentTask == UnitTask.Build && hit.gameObject == Target.gameObject)
            {
                if (hit.TryGetComponent<StructureUnit>(out var structure))
                {
                    StartedBuilding(structure);
                }
            }
        }
    }
    void StartedBuilding(StructureUnit structure)
    {
        Debug.Log("Started Building" + structure.gameObject.name);
    }
}