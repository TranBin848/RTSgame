using UnityEngine;
using RTS.DOTS;

public class SheepUnit : HumanoidUnit, IResourceNode
{
    [SerializeField] private ResourceType m_ResourceType = ResourceType.Meat;
    [SerializeField] private float m_InteractionRadius = 0.4f;
    [SerializeField] private float m_FleeDistance = 2f;
    [SerializeField] private float m_FleeJitterRadius = 0.75f;
    [SerializeField] private string m_GrazingAnimatorBool = "IsGrazing";
    [SerializeField] private float m_MinIdleBeforeGrazing = 2f;
    [SerializeField] private float m_MaxIdleBeforeGrazing = 5f;
    [SerializeField] private float m_MinGrazingDuration = 1.5f;
    [SerializeField] private float m_MaxGrazingDuration = 4f;

    private bool m_IsClaimed;
    private bool m_IsGrazing;
    private float m_NextGrazingTime;
    private float m_GrazingEndTime;
    private Coroutine m_DeadFreezeCoroutine;

    public override bool IsPlayer => false;
    public ResourceType ResourceType => m_ResourceType;
    public bool IsClaimed => IsDotsControlled && SwarmGameBridge.Instance.TryResourceClaimState(this, out var resource) ? resource.Claims >= resource.Capacity : m_IsClaimed;
    public float InteractionRadius => m_InteractionRadius;
    public void ConfigureDotsActor(ref GameplayActor actor)
    {
        actor.MinIdle = Mathf.Max(0.01f, m_MinIdleBeforeGrazing); actor.MaxIdle = Mathf.Max(actor.MinIdle, m_MaxIdleBeforeGrazing);
        actor.MinGraze = Mathf.Max(0.01f, m_MinGrazingDuration); actor.MaxGraze = Mathf.Max(actor.MinGraze, m_MaxGrazingDuration);
        actor.GrazeTimer = actor.MinIdle; actor.FleeDistance = m_FleeDistance; actor.FleeJitter = m_FleeJitterRadius;
    }
    public void ApplyDotsGrazing(bool grazing) { SetGrazingAnimator(grazing); }

    protected override void Start()
    {
        base.Start();
        ScheduleNextGrazing();
    }

    public bool TryClaim()
    {
        if (IsDotsControlled) return SwarmGameBridge.Instance.ClaimResource(this);
        if (m_IsClaimed)
        {
            return false;
        }

        StopGrazing();
        m_IsClaimed = true;
        return true;
    }

    public void Release()
    {
        if (IsDotsControlled) { SwarmGameBridge.Instance.ReleaseResource(this); return; }
        m_IsClaimed = false;
    }

    public void Hit()
    {
    }

    public Vector3 GetInteractionPoint(Vector3 requesterPosition)
    {
        return Collider != null ? Collider.ClosestPoint(requesterPosition) : transform.position;
    }

    public override void TakeDamage(int dmg, Unit damager)
    {
        if (IsDotsControlled) { base.TakeDamage(dmg, damager); return; }
        if (CurrentState == UnitState.Dead)
        {
            return;
        }

        StopGrazing();
        base.TakeDamage(dmg, damager);
        if (CurrentState != UnitState.Dead)
        {
            SetTarget(null);
            FleeFrom(damager);
        }
    }

    protected override void UpdateBehaviour()
    {
        if (CurrentState == UnitState.Dead)
        {
            return;
        }

        if (CurrentState == UnitState.Attacking)
        {
            SetState(UnitState.Moving);
            return;
        }

        if (CurrentState == UnitState.Moving)
        {
            StopGrazing();
            return;
        }

        if (m_IsClaimed)
        {
            StopGrazing();
            return;
        }

        if (m_IsGrazing)
        {
            if (Time.time >= m_GrazingEndTime)
            {
                StopGrazing();
                ScheduleNextGrazing();
            }

            return;
        }

        if (CurrentState == UnitState.Idle && Time.time >= m_NextGrazingTime)
        {
            StartGrazing();
        }
    }

    protected override void PerformAttackAnimation()
    {
    }

    protected override void RunDeadEffect()
    {
        StopGrazing();
        StopMovement();
        m_Animator.SetFloat("Speed", 0f);
        m_Animator.SetTrigger("Dead");
    }

    protected override void RegisterUnit(Unit unit)
    {
    }

    protected override void UnregisterUnit(Unit unit)
    {
    }

    protected override void OnDestinationReached()
    {
        if (CurrentState != UnitState.Dead)
        {
            StopGrazing();
            ScheduleNextGrazing();
        }
    }

    private void FleeFrom(Unit damager)
    {
        Vector3 fleeDirection = damager != null
            ? (transform.position - damager.transform.position).normalized
            : Random.insideUnitCircle.normalized;

        if (fleeDirection.sqrMagnitude <= 0.001f)
        {
            fleeDirection = Random.insideUnitCircle.normalized;
        }

        Vector2 jitter = Random.insideUnitCircle * m_FleeJitterRadius;
        Vector3 destination = transform.position + fleeDirection * m_FleeDistance + new Vector3(jitter.x, jitter.y, 0f);
        m_SpriteRenderer.flipX = fleeDirection.x < 0f;
        MoveTo(destination);
    }

    private void StartGrazing()
    {
        m_IsGrazing = true;
        m_GrazingEndTime = Time.time + Random.Range(m_MinGrazingDuration, m_MaxGrazingDuration);
        SetGrazingAnimator(true);
    }

    private void StopGrazing()
    {
        if (!m_IsGrazing)
        {
            SetGrazingAnimator(false);
            return;
        }

        m_IsGrazing = false;
        SetGrazingAnimator(false);
    }

    private void ScheduleNextGrazing()
    {
        m_NextGrazingTime = Time.time + Random.Range(m_MinIdleBeforeGrazing, m_MaxIdleBeforeGrazing);
    }

    private void SetGrazingAnimator(bool isGrazing)
    {
        if (!string.IsNullOrWhiteSpace(m_GrazingAnimatorBool))
        {
            m_Animator.SetBool(m_GrazingAnimatorBool, isGrazing);
        }
    }

}
