using System.Diagnostics.Tracing;
using NUnit.Framework.Constraints;
using UnityEngine;

public class WorkerUnit : HumanoidUnit
{
    public Tree m_AssignedTree;
    protected override void UpdateBehaviour()
    {
        if (CurrentTask == UnitTask.Build && hasTarget)
        {
            CheckForConstruction();
        }
    }
    protected override void OnSetDestination(DestinationSource source) => ResetState();

    public void OnBuildingFinished() => ResetState();

    public void SendToBuild(StructureUnit structure)
    {
        MoveTo(structure.transform.position);
        SetTarget(structure);
        SetTask(UnitTask.Build);
    }
    public void SendToChop(Tree tree)
    {
        if (tree.TryOccupy())
        {
            MoveTo(tree.GetBottomPosition());
            SetTask(UnitTask.Chop);
            m_AssignedTree = tree;
            Debug.Log($"Worker {name} assigned to chop tree {tree.name}");
        }
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
        SetState(UnitState.Building);
        m_Animator.SetBool("isBuilding", true);
        structure.AssignWorkerToBuildProcess(this);
    }
    void ResetState()
    {
        SetTask(UnitTask.None);
        if (hasTarget)
        {
            CleanUpTarget();
        }
        m_Animator.SetBool("isBuilding", false);

        if (m_AssignedTree != null)
        {
            m_AssignedTree.Unoccupy();
            m_AssignedTree = null;
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
