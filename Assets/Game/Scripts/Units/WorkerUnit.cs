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
        else if (CurrentTask == UnitTask.Chop && m_AssignedTree != null)
        {
            HandleChoppingTask();
        }

        if (CurrentState == UnitState.Chopping)
        {
            StartChopping();
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
        if (tree.TryToClaim())
        {
            MoveTo(tree.GetBottomPosition());
            SetTask(UnitTask.Chop);
            m_AssignedTree = tree;
            Debug.Log($"Worker {name} assigned to chop tree {tree.name}");
        }
    }
    protected override void Die()
    {
        base.Die();
        if (m_AssignedTree != null)
        {
            m_AssignedTree.Release();
        }
    }
    void HandleChoppingTask()
    {
        var treeBottomPosition = m_AssignedTree.GetBottomPosition();
        var workerClosetPoint = Collider.ClosestPoint(treeBottomPosition);

        var distance = Vector3.Distance(workerClosetPoint, treeBottomPosition);

        if (distance <= 0.1f)
        {
            StopMovement();
            SetState(UnitState.Chopping);
        }
    }
    void StartChopping()
    {
        m_Animator.SetBool("isChopping", true);
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
        m_Animator.SetBool("isChopping", false);

        if (m_AssignedTree != null)
        {
            m_AssignedTree.Release();
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
