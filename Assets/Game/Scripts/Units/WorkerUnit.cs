using System.Diagnostics.Tracing;
using NUnit.Framework.Constraints;
using UnityEngine;

public class WorkerUnit : HumanoidUnit
{
    [SerializeField] private float m_WoodGatherTickTime = 1f;
    [SerializeField] private float m_GoldGatherTickTime = 1f;
    [SerializeField] private float m_FoodGatherTickTime = 1f;
    [SerializeField] private int m_WoodPerTick = 1;
    [SerializeField] private int m_GoldPerTick = 1;
    [SerializeField] private int m_MeatPerTick = 1;
    [SerializeField] private float m_HitTreeFrequency = 0.3f;
    [SerializeField] private float m_HitGoldStoneFrequency = 0.3f;
    [SerializeField] private float m_HitAnimalFrequency = 0.3f;

    private float m_ChoppingTimer;
    private float m_HitTreeTimer;
    private float m_MiningTimer;
    private float m_HitGoldStoneTimer;
    private float m_FoodingTimer;
    private float m_HitAnimalTimer;
    private int m_WoodCollected;
    private int m_GoldCollected;
    private int m_MeatCollected;
    private int m_WoodCapacity = 5;
    private int m_GoldCapacity = 10;
    private int m_MeatCapacity = 5;
    public Tree m_AssignedTree;
    public GoldStone m_AssignedGoldStone;
    private StructureUnit m_AssignedStoragePit;
    public bool IsHoldingWood => m_WoodCollected > 0;
    public bool IsHoldingGold => m_GoldCollected > 0;
    public bool IsHoldingMeat => m_MeatCollected > 0;
    public bool IsHoldingResource => IsHoldingWood || IsHoldingGold || IsHoldingMeat;
    public enum CarryType
    {
        None = 0,
        Wood = 1,
        Gold = 2,
        Meat = 3
    }
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
        else if (CurrentTask == UnitTask.Mine && m_AssignedGoldStone != null & m_GoldCollected < m_GoldCapacity)
        {
            HandleMiningTask();
        }
        else if (CurrentTask == UnitTask.ReturnResource && m_AssignedStoragePit != null
        && (IsHoldingWood || IsHoldingGold || IsHoldingMeat))
        {
            var closetPointOnStorge = m_AssignedStoragePit.Collider.ClosestPoint(transform.position);
            var distance = Vector3.Distance(transform.position, closetPointOnStorge);
            if (distance <= 0.5f)
            {
                if (IsHoldingWood)
                {
                    m_GameManager.ShowTextPopup(m_WoodCollected.ToString(), Color.green, GetTopPosition());
                    m_GameManager.AddResources(0, m_WoodCollected, 0);
                    m_WoodCollected = 0;
                    TryMoveToClosetTree();
                }
                else if (IsHoldingGold)
                {
                    m_GameManager.ShowTextPopup(m_GoldCollected.ToString(), Color.yellow, GetTopPosition());
                    m_GameManager.AddResources(m_GoldCollected, 0, 0);
                    m_GoldCollected = 0;
                    TryMoveToClosetGoldStone();
                }
                else if (IsHoldingMeat)
                {
                    m_GameManager.ShowTextPopup(m_MeatCollected.ToString(), Color.red, GetTopPosition());
                    m_GameManager.AddResources(0, 0, m_MeatCollected);
                    m_MeatCollected = 0;
                    // No need to move to closet animal after delivering meat
                }
                //Debug.Log($"Worker {name} delivered wood to storage {m_AssignedStoragePit.name}. Wood collected reset to 0.");
            }
        }

        if (CurrentState == UnitState.Chopping && m_WoodCollected < m_WoodCapacity)
        {
            StartChopping();
        }
        else if (CurrentState == UnitState.Mining && m_GoldCollected < m_GoldCapacity)
        {
            StartMining();
        }
        //Debug.Log(m_WoodCollected);
        HandleResourcePlay();
    }
    protected override void OnSetDestination(DestinationSource source)
    {
        SetState(UnitState.Moving);
        ResetState();
    }


    public void OnBuildingFinished() => ResetState();
    public void SetWoodStorage(StructureUnit storage)
    {
        m_AssignedStoragePit = storage;
    }

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
            //Debug.Log($"Worker {name} assigned to chop tree {tree.name}");
        }
    }
    public void SendToMine(GoldStone goldStone)
    {
        // if (goldStone.TryToClaim())
        // {
        MoveTo(goldStone.GetBottomPosition());
        SetTask(UnitTask.Mine);
        m_AssignedGoldStone = goldStone;
        //Debug.Log($"Worker {name} assigned to mine gold stone {goldStone.name}");
        //}
    }

    protected override void Die()
    {
        base.Die();
        if (m_AssignedTree != null)
        {
            m_AssignedTree.Release();
        }
    }
    void HandleResourcePlay()
    {
        if (IsHoldingResource)
        {
            if (IsHoldingWood)
            {
                m_Animator.SetFloat("CarryType", (float)CarryType.Wood);
            }
            else if (IsHoldingGold)
            {
                m_Animator.SetFloat("CarryType", (float)CarryType.Gold);
            }
            else if (IsHoldingMeat)
            {
                m_Animator.SetFloat("CarryType", (float)CarryType.Meat);
            }
        }
        else
        {
            m_Animator.SetFloat("CarryType", (float)CarryType.None);
        }
    }
    void HandleMiningTask()
    {
        var goldStoneBottomPosition = m_AssignedGoldStone.GetBottomPosition();
        var workerClosetPoint = Collider.ClosestPoint(goldStoneBottomPosition);

        var distance = Vector3.Distance(workerClosetPoint, goldStoneBottomPosition);

        if (distance <= m_AssignedGoldStone.ColliderRadius)
        {
            StopMovement();
            SetState(UnitState.Mining);
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
    void StartMining()
    {
        m_Animator.SetBool("isMining", true);
        m_MiningTimer += Time.deltaTime;
        m_HitGoldStoneTimer += Time.deltaTime;

        if (m_HitGoldStoneTimer >= m_HitGoldStoneFrequency)
        {
            m_HitGoldStoneTimer = 0f;
            // Here you can add code to play a mining sound or trigger a hit effect on the gold stone
            m_AssignedGoldStone.Hit();
        }


        if (m_MiningTimer >= m_GoldGatherTickTime)
        {
            m_MiningTimer = 0f;
            m_GoldCollected += m_GoldPerTick;

            if (m_GoldCollected >= m_GoldCapacity)
            {
                m_GoldCollected = m_GoldCapacity;
                HandleMiningFinished();
            }
            // Debug.Log($"Worker {name} gathered gold. Total gold collected: {m_GoldCollected}/{m_GoldCapacity}");
        }
    }
    void HandleMiningFinished()
    {
        m_Animator.SetBool("isMining", false);

        m_AssignedStoragePit = m_GameManager.FindClosetStoragePit(transform.position);
        if (m_AssignedStoragePit != null && m_AssignedStoragePit.CanStoreGold)
        {
            var closetPointOnStorge = m_AssignedStoragePit.Collider.ClosestPoint(m_AssignedStoragePit.transform.position);
            MoveTo(closetPointOnStorge);
            //Debug.Log($"Worker {name} is returning wood to storage {m_AssignedStoragePit.name}");
        }
        SetState(UnitState.Idle);
        SetTask(UnitTask.ReturnResource);
    }
    void StartChopping()
    {
        m_Animator.SetBool("isChopping", true);
        m_ChoppingTimer += Time.deltaTime;
        m_HitTreeTimer += Time.deltaTime;

        if (m_HitTreeTimer >= m_HitTreeFrequency)
        {
            m_HitTreeTimer = 0f;
            // Here you can add code to play a chopping sound or trigger a hit effect on the tree
            m_AssignedTree.Hit();
        }


        if (m_ChoppingTimer >= m_WoodGatherTickTime)
        {
            m_ChoppingTimer = 0f;
            m_WoodCollected += m_WoodPerTick;

            if (m_WoodCollected >= m_WoodCapacity)
            {
                m_WoodCollected = m_WoodCapacity;
                HandleChoppingFinished();
            }
            Debug.Log($"Worker {name} gathered wood. Total wood collected: {m_WoodCollected}/{m_WoodCapacity}");
        }
    }
    void HandleChoppingFinished()
    {
        m_Animator.SetBool("isChopping", false);

        m_AssignedStoragePit = m_GameManager.FindClosetStoragePit(transform.position);
        if (m_AssignedStoragePit != null && m_AssignedStoragePit.CanStoreWood)
        {
            var closetPointOnStorge = m_AssignedStoragePit.Collider.ClosestPoint(m_AssignedStoragePit.transform.position);
            MoveTo(closetPointOnStorge);
            //Debug.Log($"Worker {name} is returning wood to storage {m_AssignedStoragePit.name}");
        }
        SetState(UnitState.Idle);
        SetTask(UnitTask.ReturnResource);
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
    void TryMoveToClosetTree()
    {
        var closetTree = m_GameManager.FindClosetUnclaimedTree(transform.position);
        if (closetTree != null)
        {
            SendToChop(closetTree);
        }
    }
    void TryMoveToClosetGoldStone()
    {
        var closetGoldStone = m_GameManager.FindClosetUnclaimedGoldStone(transform.position);
        if (closetGoldStone != null)
        {
            SendToMine(closetGoldStone);
        }
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
