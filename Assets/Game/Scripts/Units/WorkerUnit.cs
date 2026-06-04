using System.Collections.Generic;
using UnityEngine;

public class WorkerUnit : HumanoidUnit
{
    [SerializeField] private WorkerUnitDefinition m_Definition;
    [SerializeField] private string m_AttackAnimatorBool = "isAttacking";

    private readonly Dictionary<ResourceType, int> m_CarriedResources = new();

    private IResourceNodeLocator m_ResourceNodeLocator;
    private IResourceDepotLocator m_ResourceDepotLocator;
    private IPlayerResourceWallet m_ResourceWallet;
    private IResourceNode m_AssignedResourceNode;
    private IResourceDepot m_AssignedDepot;
    private WorkerUnitDefinition.GatheringProfile m_ActiveProfile;
    private bool m_HasActiveProfile;
    private float m_GatherTimer;
    private float m_HitTimer;

    public bool IsHoldingResource => TryGetCurrentCarryType(out _);

    public void Inject(
        IResourceNodeLocator resourceNodeLocator,
        IResourceDepotLocator resourceDepotLocator,
        IPlayerResourceWallet resourceWallet)
    {
        m_ResourceNodeLocator = resourceNodeLocator;
        m_ResourceDepotLocator = resourceDepotLocator;
        m_ResourceWallet = resourceWallet;
    }

    protected override void UpdateBehaviour()
    {
        if (!HasGatheringDefinition())
        {
            return;
        }

        if (CurrentTask == UnitTask.Build && hasTarget)
        {
            CheckForConstruction();
        }
        else if (IsInResourceCombat(out var resourceUnit))
        {
            HandleResourceCombat(resourceUnit);
        }
        else if (IsAssignedToResourceNode() && GetCarriedAmount(m_ActiveProfile.ResourceType) < m_ActiveProfile.CarryCapacity)
        {
            HandleGatheringTask();
        }
        else if (CurrentTask == UnitTask.ReturnResource && m_AssignedDepot != null && IsHoldingCurrentResource())
        {
            HandleDepositTask();
        }

        if (m_HasActiveProfile && CurrentState == m_ActiveProfile.GatherState && GetCarriedAmount(m_ActiveProfile.ResourceType) < m_ActiveProfile.CarryCapacity)
        {
            ProcessGathering();
        }

        HandleResourcePlay();
    }

    protected override void OnSetDestination(DestinationSource source)
    {
        SetState(UnitState.Moving);
        SetAttackAnimationActive(false);
        if (source == DestinationSource.PlayerClick)
        {
            CancelActiveWork();
        }
    }

    protected override void Die()
    {
        ReleaseAssignedResourceNode();
        base.Die();
    }

    protected override void PerformAttackAnimation()
    {
        if (Target == null)
        {
            return;
        }

        Vector3 direction = (Target.transform.position - transform.position).normalized;
        m_SpriteRenderer.flipX = direction.x < 0;
    }

    public void OnBuildingFinished()
    {
        CancelActiveWork();
    }

    public void SetWoodStorage(StructureUnit storage)
    {
        m_AssignedDepot = storage;
    }

    public void SendToBuild(StructureUnit structure)
    {
        CancelActiveWork();
        MoveTo(structure.transform.position);
        SetTarget(structure);
        SetTask(UnitTask.Build);
    }

    public bool TryAssignResourceNode(IResourceNode resourceNode)
    {
        if (resourceNode == null || !HasGatheringDefinition())
        {
            return false;
        }

        if (ReferenceEquals(m_AssignedResourceNode, resourceNode))
        {
            return true;
        }

        if (!m_Definition.TryGetProfile(resourceNode.ResourceType, out var profile))
        {
            return false;
        }

        if (!resourceNode.TryClaim())
        {
            return false;
        }

        CancelActiveWork();

        m_AssignedResourceNode = resourceNode;
        m_ActiveProfile = profile;
        m_HasActiveProfile = true;
        m_AssignedDepot = null;
        ResetGatherTimers();

        if (resourceNode is Unit resourceUnit && resourceUnit.CurrentState != UnitState.Dead)
        {
            SetTarget(resourceUnit);
            SetTask(UnitTask.Attack);
            MoveTo(resourceUnit.transform.position);
        }
        else
        {
            MoveTo(resourceNode.GetInteractionPoint());
            SetTask(profile.GatherTask);
        }

        return true;
    }

    void HandleGatheringTask()
    {
        Vector3 interactionPoint = m_AssignedResourceNode.GetInteractionPoint();
        Vector3 workerClosestPoint = Collider.ClosestPoint(interactionPoint);
        float distance = Vector3.Distance(workerClosestPoint, interactionPoint);

        if (distance <= m_AssignedResourceNode.InteractionRadius)
        {
            StopMovement();
            SetState(m_ActiveProfile.GatherState);
        }
    }

    void HandleResourceCombat(Unit resourceUnit)
    {
        if (resourceUnit.CurrentState == UnitState.Dead)
        {
            SetAttackAnimationActive(false);
            SetTarget(null);
            MoveTo(m_AssignedResourceNode.GetInteractionPoint());
            SetTask(m_ActiveProfile.GatherTask);
            SetState(m_ActiveProfile.GatherState);
            return;
        }

        if (Target != resourceUnit)
        {
            SetTarget(resourceUnit);
        }

        if (IsTargetInRange(resourceUnit))
        {
            StopMovement();
            SetState(UnitState.Attacking);
            SetAttackAnimationActive(true);
            TryAttackCurrentTarget();
        }
        else
        {
            SetAttackAnimationActive(false);
            MoveTo(resourceUnit.transform.position);
        }
    }

    void HandleDepositTask()
    {
        Vector3 deliveryPoint = m_AssignedDepot.GetDeliveryPoint(transform.position);
        float distance = Vector3.Distance(transform.position, deliveryPoint);

        if (distance > 0.5f)
        {
            return;
        }

        int amount = GetCarriedAmount(m_ActiveProfile.ResourceType);
        if (amount <= 0)
        {
            return;
        }

        m_GameManager.ShowTextPopup(amount.ToString(), m_ActiveProfile.DeliveryPopupColor, GetTopPosition());
        m_ResourceWallet?.AddResource(m_ActiveProfile.ResourceType, amount);
        SetCarriedAmount(m_ActiveProfile.ResourceType, 0);

        if (!TryAssignClosestResourceNode(m_ActiveProfile.ResourceType))
        {
            m_AssignedDepot = null;
            SetTask(UnitTask.None);
            SetState(UnitState.Idle);
        }
    }

    void ProcessGathering()
    {
        SetGatherAnimationActive(true);
        m_GatherTimer += Time.deltaTime;
        m_HitTimer += Time.deltaTime;

        if (m_HitTimer >= m_ActiveProfile.HitFrequency)
        {
            m_HitTimer = 0f;
            m_AssignedResourceNode?.Hit();
        }

        if (m_GatherTimer < m_ActiveProfile.GatherTickTime)
        {
            return;
        }

        m_GatherTimer = 0f;
        int updatedAmount = Mathf.Min(
            GetCarriedAmount(m_ActiveProfile.ResourceType) + m_ActiveProfile.ResourcePerTick,
            m_ActiveProfile.CarryCapacity);
        SetCarriedAmount(m_ActiveProfile.ResourceType, updatedAmount);

        if (updatedAmount >= m_ActiveProfile.CarryCapacity)
        {
            HandleGatheringFinished();
        }
    }

    void HandleGatheringFinished()
    {
        SetGatherAnimationActive(false);

        if (m_ResourceDepotLocator != null &&
            m_ResourceDepotLocator.TryFindClosestDepot(transform.position, m_ActiveProfile.ResourceType, out var depot))
        {
            m_AssignedDepot = depot;
            MoveTo(depot.GetDeliveryPoint(transform.position));
            SetState(UnitState.Idle);
            SetTask(UnitTask.ReturnResource);
            return;
        }

        m_AssignedDepot = null;
        SetState(UnitState.Idle);
        SetTask(UnitTask.None);
    }

    void CheckForConstruction()
    {
        if (Target is not StructureUnit structure)
        {
            return;
        }

        float distanceToTarget = Vector2.Distance(transform.position, Target.transform.position);
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

    bool TryAssignClosestResourceNode(ResourceType resourceType)
    {
        return m_ResourceNodeLocator != null
            && m_ResourceNodeLocator.TryFindClosestAvailable(transform.position, resourceType, out var resourceNode)
            && TryAssignResourceNode(resourceNode);
    }

    bool HasGatheringDefinition()
    {
        if (m_Definition != null)
        {
            return true;
        }

        Debug.LogError($"WorkerUnit '{name}' is missing a WorkerUnitDefinition reference.");
        return false;
    }

    bool IsAssignedToResourceNode()
    {
        return m_HasActiveProfile
            && m_AssignedResourceNode != null
            && CurrentTask == m_ActiveProfile.GatherTask;
    }

    bool IsInResourceCombat(out Unit resourceUnit)
    {
        resourceUnit = m_AssignedResourceNode as Unit;
        return m_HasActiveProfile
            && resourceUnit != null
            && CurrentTask == UnitTask.Attack;
    }

    bool IsHoldingCurrentResource()
    {
        return m_HasActiveProfile && GetCarriedAmount(m_ActiveProfile.ResourceType) > 0;
    }

    int GetCarriedAmount(ResourceType resourceType)
    {
        return m_CarriedResources.TryGetValue(resourceType, out var amount) ? amount : 0;
    }

    void SetCarriedAmount(ResourceType resourceType, int amount)
    {
        if (amount <= 0)
        {
            m_CarriedResources.Remove(resourceType);
            return;
        }

        m_CarriedResources[resourceType] = amount;
    }

    void ResetGatherTimers()
    {
        m_GatherTimer = 0f;
        m_HitTimer = 0f;
    }

    void SetGatherAnimationActive(bool isActive)
    {
        if (!string.IsNullOrWhiteSpace(m_ActiveProfile.GatherAnimationBool))
        {
            m_Animator.SetBool(m_ActiveProfile.GatherAnimationBool, isActive);
        }
    }

    void SetAttackAnimationActive(bool isActive)
    {
        if (!string.IsNullOrWhiteSpace(m_AttackAnimatorBool))
        {
            m_Animator.SetBool(m_AttackAnimatorBool, isActive);
        }
    }

    void HandleResourcePlay()
    {
        if (TryGetCurrentCarryType(out var carryType) && m_Definition.TryGetProfile(carryType, out var profile))
        {
            m_Animator.SetFloat("CarryType", profile.CarryAnimatorValue);
            return;
        }

        m_Animator.SetFloat("CarryType", 0f);
    }

    bool TryGetCurrentCarryType(out ResourceType resourceType)
    {
        if (m_HasActiveProfile && GetCarriedAmount(m_ActiveProfile.ResourceType) > 0)
        {
            resourceType = m_ActiveProfile.ResourceType;
            return true;
        }

        foreach (var resourceEntry in m_CarriedResources)
        {
            if (resourceEntry.Value > 0)
            {
                resourceType = resourceEntry.Key;
                return true;
            }
        }

        resourceType = default;
        return false;
    }

    void CancelActiveWork()
    {
        SetTask(UnitTask.None);
        SetState(UnitState.Idle);
        m_Animator.SetBool("isBuilding", false);
        SetAttackAnimationActive(false);
        SetGatherAnimationActive(false);
        ResetGatherTimers();
        ReleaseAssignedResourceNode();
        m_AssignedDepot = null;
        CleanUpTarget();
    }

    void ReleaseAssignedResourceNode()
    {
        if (m_AssignedResourceNode != null)
        {
            m_AssignedResourceNode.Release();
            m_AssignedResourceNode = null;
        }

        m_HasActiveProfile = false;
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
