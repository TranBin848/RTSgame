using System.Diagnostics.Tracing;
using NUnit.Framework.Constraints;
using UnityEngine;

public class WorkerUnit : HumanoidUnit
{
    protected override void UpdateBehaviour()
    {
        if (CurrentTask == UnitTask.Build && hasTarget)
        {
            CheckForConstruction();
        }
    }
    protected override void OnSetDestination()
    {
        ResetState();
    }
    public void SendToBuild(StructureUnit structure)
    {
        MoveTo(structure.transform.position);
        SetTarget(structure);
        SetTask(UnitTask.Build);
    }
    void CheckForConstruction()
    {
        if (Target == null || !(Target is StructureUnit structure))
        {
            return;
        }
        var distanceToTarget = Vector2.Distance(transform.position, Target.transform.position);
        if (distanceToTarget <= m_ObjectDetectionRadius)
        {
            StartedBuilding(structure);
        }
    }
    void StartedBuilding(StructureUnit structure)
    {
        structure.AssignWorkerToBuildProcess(this);
    }
    void ResetState()
    {
        SetTask(UnitTask.None);
        if (hasTarget)
        {
            CleanUpTarget();
        }
    }
    void CleanUpTarget()
    {
        if (Target is StructureUnit structure)
        {
            structure.UnassignWorkerFromBuildProcess();
        }
        SetTarget(null);
    }
}

// private void CheckForCloseObjects()
// {
//     var hits = RunProximityObjectDetection();
//     foreach (var hit in hits)
//     {
//         if (CurrentTask == UnitTask.Build && hit.gameObject == Target.gameObject)
//         {
//             if (hit.TryGetComponent<StructureUnit>(out var structure))
//             {
//                 StartedBuilding(structure);
//             }
//         }
//     }
// }