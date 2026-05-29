using System.Diagnostics.Tracing;
using NUnit.Framework.Constraints;
using UnityEngine;

public class WorkerUnit : HumanoidUnit
{
    [SerializeField] private float m_WoodGatherTickTime = 1f;
    [SerializeField] private int m_WoodPerTick = 1;
    private float m_ChoppingTimer;
    private int m_WoodCollected;
    private int m_GoldCollected;
    private int m_WoodCapacity = 5;
    private int m_GoldCapacity = 10;
    public Tree m_AssignedTree;
    protected override void UpdateBehaviour()
    {
        if (CurrentTask == UnitTask.Build && hasTarget)
        {
            CheckForConstruction();
        }
        else if (CurrentTask == UnitTask.Chop && m_AssignedTree != null && m_WoodCollected < m_WoodCapacity)
        {
            HandleChoppingTask();
        }

        if (CurrentState == UnitState.Chopping && m_WoodCollected < m_WoodCapacity)
        {
            StartChopping();
        }
        Debug.Log(m_WoodCollected);
    }
    protected override void OnSetDestination(DestinationSource source)
    {
        SetState(UnitState.Moving);
        ResetState();
    }


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
        m_ChoppingTimer += Time.deltaTime;

        if (m_ChoppingTimer >= m_WoodGatherTickTime)
        {
            m_ChoppingTimer = 0f;
            m_WoodCollected += m_WoodPerTick;

            if (m_WoodCollected >= m_WoodCapacity)
            {
                m_WoodCollected = m_WoodCapacity;
                m_Animator.SetBool("isChopping", false);
                SetState(UnitState.Idle);
                Debug.Log($"Worker {name} has reached wood capacity. Total wood collected: {m_WoodCollected}/{m_WoodCapacity}");
            }
            Debug.Log($"Worker {name} gathered wood. Total wood collected: {m_WoodCollected}/{m_WoodCapacity}");
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
        m_Animator.SetBool("isChopping", false);

        m_ChoppingTimer = 0f;

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
