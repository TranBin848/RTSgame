using System.Collections;
using UnityEngine;

public enum UnitState
{
    Idle, Moving, Attacking, Chopping, Mining, Building, Dead
}
public enum UnitTask
{
    None, Build, Chop, Mine, Attack
}
public abstract class Unit : MonoBehaviour
{
    [SerializeField] private ActionSO[] m_Actions;
    [SerializeField] protected float m_ObjectDetectionRadius = 0.5f;
    [SerializeField] protected float m_UnitDetectionCheckRate = 0.5f;
    [SerializeField] protected float m_AttackRange = 1f;
    [SerializeField] protected float m_AutoAttackFrequency = 1.5f;
    [SerializeField] protected float m_AutoAttackDamageDelay = 0.5f;
    [SerializeField] protected int m_AutoAttackDamage = 10;
    [SerializeField] protected int m_Health = 100;
    public bool isTargeted = false;
    protected GameManager m_GameManager;
    protected Animator m_Animator;
    protected AIPawn m_AIPawn;
    protected SpriteRenderer m_SpriteRenderer;
    protected Material m_OriginalMaterial;
    protected Material m_HighlightMaterial;
    protected CapsuleCollider2D m_Collider;
    protected float m_NextUnitDetectionTime = 0f;
    protected float m_NextAutoAttackTime;
    protected int m_CurrentHealth;
    public UnitState CurrentState { get; protected set; } = UnitState.Idle;
    public UnitTask CurrentTask { get; protected set; } = UnitTask.None;
    public Unit Target { get; protected set; }
    public virtual bool IsPlayer => true;
    public virtual bool IsBuilding => false;
    public ActionSO[] Actions => m_Actions;
    public SpriteRenderer SpriteRenderer => m_SpriteRenderer;
    public bool hasTarget => Target != null;
    public int CurrentHealth => m_CurrentHealth;
    protected void Awake()
    {
        if (TryGetComponent<Animator>(out var animator))
        {
            m_Animator = animator;
        }
        if (TryGetComponent<AIPawn>(out var aiPawn))
        {
            m_AIPawn = aiPawn;
            m_AIPawn.OnNewPositionSelected += TurnToPosition;
        }
        if (TryGetComponent<SpriteRenderer>(out var spriteRenderer))
        {
            m_SpriteRenderer = spriteRenderer;
        }
        if (TryGetComponent<CapsuleCollider2D>(out var collider))
        {
            m_Collider = collider;
        }
        if (m_SpriteRenderer != null)
        {
            m_OriginalMaterial = m_SpriteRenderer.material;
        }
        m_GameManager = GameManager.Get();
        m_HighlightMaterial = Resources.Load<Material>("Materials/Outline");
        m_CurrentHealth = m_Health;
    }
    protected virtual void Start()
    {
        RegisterUnit(this);
    }
    void OnDestroy()
    {
        if (m_AIPawn != null)
        {
            m_AIPawn.OnNewPositionSelected -= TurnToPosition;
        }
        UnregisterUnit(this);
    }

    public void SetTask(UnitTask task)
    {
        OnSetTask(CurrentTask, task);
    }
    public void SetState(UnitState state)
    {
        OnSetState(CurrentState, state);
    }
    public void SetTarget(Unit target)
    {
        Target = target;
    }
    public void MoveTo(Vector3 destination)
    {
        if (m_AIPawn != null)
        {
            m_AIPawn.SetDestination(destination);
        }
        OnSetDestination();
    }
    public void Select()
    {
        HighLight();
        isTargeted = true;
    }
    public void Deselect()
    {
        UnHighlight();
        isTargeted = false;
    }
    public void StopMovement()
    {
        m_AIPawn?.Stop();
    }
    public Vector3 GetTopPosition()
    {
        return transform.position + Vector3.up * m_Collider.size.y / 2;
    }
    protected virtual void OnSetDestination()
    {

    }
    protected virtual void OnSetTask(UnitTask oldTask, UnitTask newTask)
    {
        CurrentTask = newTask;
    }
    protected virtual void OnSetState(UnitState oldState, UnitState newState)
    {
        CurrentState = newState;
    }
    protected virtual void RegisterUnit(Unit unit)
    {
        m_GameManager.RegisterUnit(unit);
    }
    protected virtual void UnregisterUnit(Unit unit)
    {
        m_GameManager.UnregisterUnit(unit);
    }
    protected virtual bool TryFindClosetFoe(out Unit foe)
    {
        if (Time.time >= m_NextUnitDetectionTime)
        {
            m_NextUnitDetectionTime = Time.time + m_UnitDetectionCheckRate;
            foe = m_GameManager.FindClosetUnit(transform.position, m_ObjectDetectionRadius, !IsPlayer);
            Debug.Log($"Found closet foe: {foe?.name ?? "None"}");
            return foe != null;
        }
        else
        {
            foe = null;
            return false;
        }
    }
    protected virtual bool TryAttackCurrentTarget()
    {
        if (Time.time >= m_NextAutoAttackTime)
        {
            m_NextAutoAttackTime = Time.time + m_AutoAttackFrequency;
            PerformAttackAnimation();
            StartCoroutine(DelayDamage(m_AutoAttackDamageDelay, m_AutoAttackDamage, Target));
            return true;
        }

        Debug.Log("Attack is on CD");
        return false;
    }
    protected virtual void PerformAttackAnimation()
    {

    }
    protected virtual void Die()
    {
        SetState(UnitState.Dead);

        if (isTargeted)
        {
            Deselect();
        }
    }
    protected virtual void TakeDamage(int dmg, Unit damager)
    {
        if (CurrentState == UnitState.Dead)
        {
            return;
        }
        m_CurrentHealth -= dmg;
        Debug.Log($"{name} took {dmg} damage from {damager.name}");
        m_GameManager.ShowTextPopup(dmg.ToString(), Color.red, GetTopPosition());
        if (m_CurrentHealth <= 0)
        {
            Die();
        }
    }
    protected IEnumerator DelayDamage(float delay, int damage, Unit target)
    {
        yield return new WaitForSeconds(delay);
        if (target != null)
        {
            target.TakeDamage(damage, this);
        }
    }

    protected bool IsTargetInRange(Transform targetTransform)
    {
        return Vector3.Distance(transform.position, targetTransform.position) <= m_AttackRange;
    }
    protected Collider2D[] RunProximityObjectDetection()
    {
        return Physics2D.OverlapCircleAll(transform.position, m_ObjectDetectionRadius);
    }
    void TurnToPosition(Vector3 position)
    {
        var direction = (position - transform.position).normalized;
        m_SpriteRenderer.flipX = direction.x < 0;
    }
    void HighLight()
    {
        if (m_SpriteRenderer != null && m_HighlightMaterial != null)
        {
            m_SpriteRenderer.material = m_HighlightMaterial;
        }
    }
    void UnHighlight()
    {
        if (m_SpriteRenderer != null && m_OriginalMaterial != null)
        {
            m_SpriteRenderer.material = m_OriginalMaterial;
        }
    }
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, m_ObjectDetectionRadius);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, m_AttackRange);
    }
}
