using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Unity.Entities;
using RTS.DOTS;

public enum UnitState
{
    Idle, Moving, Attacking, Chopping, Mining, Building, Dead, Collecting
}
public enum UnitTask
{
    None, Build, Chop, Mine, Attack, ReturnResource, Collect
}
public enum DestinationSource
{
    CodeTriggered, PlayerClick
}
public abstract class Unit : MonoBehaviour, IPooledRuntimeObject
{
    [SerializeField] private ActionSO[] m_Actions;
    [SerializeField] private Sprite m_UnitIcon;
    [SerializeField] protected float m_ObjectDetectionRadius = 0.5f;
    [SerializeField] protected float m_UnitDetectionCheckRate = 0.5f;
    [SerializeField] protected float m_AttackRange = 1f;
    [SerializeField] protected float m_AutoAttackFrequency = 1.5f;
    [SerializeField] protected float m_AutoAttackDamageDelay = 0.5f;
    [SerializeField] protected int m_AutoAttackDamage = 10;
    [SerializeField] protected int m_Health = 100;
    [SerializeField] protected Color m_DamageFlashColor = new Color(1f, 0.27f, 0.25f, 1f);
    public bool isTargeted = false;
    protected GameManager m_GameManager;
    protected Animator m_Animator;
    protected AIPawn m_AIPawn;
    protected SpriteRenderer m_SpriteRenderer;
    protected Color m_OriginalColor;
    protected Material m_OriginalMaterial;
    protected Material m_HighlightMaterial;
    protected CapsuleCollider2D m_Collider;
    protected float m_NextUnitDetectionTime = 0f;
    protected float m_NextAutoAttackTime;
    protected int m_CurrentHealth;
    private bool m_IsRegistered;
    protected UnitStance m_CurrentStance = UnitStance.Offensive;
    public UnitState CurrentState { get; protected set; } = UnitState.Idle;
    public UnitTask CurrentTask { get; protected set; } = UnitTask.None;
    public Unit Target { get; protected set; }
    public virtual bool IsPlayer => true;
    public virtual bool IsBuilding => false;
    public virtual bool IsSelectable => CurrentState != UnitState.Dead;
    public virtual bool IsTargetable => CurrentState != UnitState.Dead;
    public ActionSO[] Actions => m_Actions;
    public SpriteRenderer SpriteRenderer => m_SpriteRenderer;
    public bool hasTarget => Target != null;
    public int CurrentHealth => m_CurrentHealth;
    public int MaxHealth => m_Health;
    public int AttackDamage => m_AutoAttackDamage;
    public UnitStance CurrentStance => m_CurrentStance;
    public CapsuleCollider2D Collider => m_Collider;
    public Sprite UnitIcon => m_UnitIcon != null ? m_UnitIcon : (m_SpriteRenderer != null ? m_SpriteRenderer.sprite : null);
    public Entity DotsTarget { get; private set; }
    public Entity DotsEntity { get; private set; }
    public bool IsDotsControlled => DotsEntity != Entity.Null && SwarmGameBridge.Instance != null;
    bool m_ApplyingDotsSnapshot;
    int m_DotsAttackSequence;
    public void BindDots(Entity entity)
    {
        m_AIPawn?.Stop();
        DotsEntity = entity;
        if (m_AIPawn != null) m_AIPawn.enabled = false;
    }
    public void UnbindDots() { DotsEntity = Entity.Null; }
    public void ApplyDotsSnapshot(GameplayActor actor, CombatBody body, Vector2 offset)
    {
        m_ApplyingDotsSnapshot = true;
        transform.position = new Vector3(body.Position.x - offset.x, body.Position.y - offset.y, transform.position.z);
        int previousHealth = m_CurrentHealth;
        m_CurrentHealth = Mathf.Max(0, Mathf.CeilToInt(body.Health));
        if (body.Health <= 0 && CurrentState != UnitState.Dead)
        {
            if (m_Collider != null && this is not SheepUnit) m_Collider.enabled = false;
            Die();
        }
        else if (CurrentState != UnitState.Dead)
        {
            CurrentState = (UnitState)actor.State; CurrentTask = (UnitTask)actor.Task;
            DotsTarget = actor.Target;
            Target = SwarmGameBridge.Instance.ViewFor(actor.Target);
            if (m_SpriteRenderer != null && Mathf.Abs(actor.Facing.x) > 0.001f) m_SpriteRenderer.flipX = actor.Facing.x < 0;
            if (m_Animator != null)
            {
                m_Animator.SetFloat("Speed", actor.Moving != 0 ? Mathf.Clamp01(actor.Speed) : 0);
                if (actor.Kind != GameplayKind.Sheep && actor.AttackSequence != m_DotsAttackSequence) m_Animator.SetTrigger("Attack");
            }
            m_DotsAttackSequence = actor.AttackSequence;
            if (this is WorkerUnit worker) worker.ApplyDotsWorkerPresentation();
            if (this is SheepUnit sheep) sheep.ApplyDotsGrazing(actor.Grazing != 0);
        }
        if (m_CurrentHealth < previousHealth && m_SpriteRenderer != null)
        {
            if (m_FlashCoroutine != null) StopCoroutine(m_FlashCoroutine);
            m_FlashCoroutine = StartCoroutine(FlashEffect(m_DamageFlashColor, 1, 0.2f));
            m_GameManager?.ShowTextPopup((previousHealth - m_CurrentHealth).ToString(), Color.red, GetTopPosition());
        }
        m_ApplyingDotsSnapshot = false;
    }
    private float m_DotsCooldown;
    private float m_DotsWindup = -1;
    public bool HasDotsTarget => DotsTarget != Entity.Null;

    public EnemySpawnSettings GetDotsSettings()
    {
        var pawn = GetComponent<AIPawn>();
        var collider = GetComponent<CapsuleCollider2D>();
        var projectile = this is EnemyRangerUnit ranged ? ranged.DotsProjectilePrefab : null;
        return new EnemySpawnSettings
        {
            Speed = pawn != null ? pawn.Speed : 2,
            Radius = collider != null ? Mathf.Max(0.15f, collider.size.x * transform.lossyScale.x * 0.5f) : 0.25f,
            Health = m_Health, AttackRange = m_AttackRange, DetectionRange = m_ObjectDetectionRadius,
            AttackInterval = m_AutoAttackFrequency, AttackDelay = m_AutoAttackDamageDelay,
            Damage = m_AutoAttackDamage, Ranged = this is EnemyRangerUnit, VisualScale = Mathf.Abs(transform.lossyScale.x),
            Projectile = projectile != null ? SwarmGameBridge.ProjectileSettings(projectile) : EnemySpawnSettings.Default.Projectile
        };
    }
    public void SetDotsTarget(Entity entity)
    {
        if (IsDotsControlled) { SwarmGameBridge.Instance.Gameplay.Attack(DotsEntity, entity); return; }
        if (this is not WarriorUnit && this is not TowerUnit) return;
        Target = null; DotsTarget = entity; m_DotsWindup = -1;
        SetTask(UnitTask.Attack);
        if (this is HumanoidUnit && SwarmGameBridge.Instance != null && SwarmGameBridge.Instance.TryGetEnemy(entity, out var position, out _)) MoveTo(position);
    }
    protected void ClearDotsTarget() { DotsTarget = Entity.Null; m_DotsWindup = -1; }
    protected bool UpdateDotsCombat(bool acquire, bool chase)
    {
        var bridge = SwarmGameBridge.Instance;
        if (bridge == null || CurrentState == UnitState.Dead) return false;
        m_DotsCooldown = Mathf.Max(0, m_DotsCooldown - Time.deltaTime);
        if (DotsTarget == Entity.Null && acquire && Target == null && Time.time >= m_NextUnitDetectionTime)
        {
            if (bridge.TryFindEnemy(transform.position, m_ObjectDetectionRadius, out var enemy)) DotsTarget = enemy;
            // Preserve the legacy detector's opportunity to find a GameObject when ECS has no target.
            if (DotsTarget != Entity.Null) m_NextUnitDetectionTime = Time.time + m_UnitDetectionCheckRate;
        }
        if (DotsTarget == Entity.Null) return false;
        if (!bridge.TryGetEnemy(DotsTarget, out var position, out float radius))
        { ClearDotsTarget(); SetTask(UnitTask.None); SetState(UnitState.Idle); return false; }
        bool inRange = Vector2.Distance(transform.position, position) - radius <= m_AttackRange;
        if (m_DotsWindup >= 0)
        {
            m_DotsWindup -= Time.deltaTime;
            if (m_DotsWindup <= 0)
            {
                m_DotsWindup = -1;
                if (inRange)
                {
                    if (this is RangerUnit archer) bridge.Fire(this, DotsTarget, m_AutoAttackDamage, archer.DotsProjectileSpawnPosition, archer.DotsProjectilePrefab);
                    else if (this is TowerUnit tower) bridge.Fire(this, DotsTarget, m_AutoAttackDamage, transform.position, tower.DotsProjectilePrefab);
                    else bridge.Simulation.QueueDamage(DotsTarget, m_AutoAttackDamage, bridge.ProxyFor(this));
                }
            }
        }
        if (inRange)
        {
            StopMovement();
            if (CurrentState != UnitState.Attacking) SetState(UnitState.Attacking);
            SetTask(UnitTask.Attack);
            if (m_DotsCooldown <= 0 && m_DotsWindup < 0)
            {
                m_DotsCooldown = m_AutoAttackFrequency; m_DotsWindup = m_AutoAttackDamageDelay;
                if (m_SpriteRenderer != null) m_SpriteRenderer.flipX = position.x < transform.position.x;
                if (m_Animator != null) m_Animator.SetTrigger("Attack");
            }
        }
        else if (chase && this is HumanoidUnit) { MoveTo(position); SetState(UnitState.Moving); }
        else { ClearDotsTarget(); SetState(UnitState.Idle); }
        return true;
    }

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
            m_AIPawn.OnDestinationReached += OnDestinationReached;
        }
        if (TryGetComponent<SpriteRenderer>(out var spriteRenderer))
        {
            m_SpriteRenderer = spriteRenderer;
            m_OriginalColor = m_SpriteRenderer.color;
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
        if (SwarmGameBridge.Instance != null)
        {
            if (SwarmGameBridge.Instance.TryConvertEnemy(this)) return;
            SwarmGameBridge.Instance.Register(this);
        }
        RegisterUnit(this);
    }
    protected virtual void OnDestroy()
    {
        SwarmGameBridge.Instance?.Unregister(this);
        if (m_AIPawn != null)
        {
            m_AIPawn.OnNewPositionSelected -= TurnToPosition;
            m_AIPawn.OnDestinationReached -= OnDestinationReached;
        }
    }

    public void SetTask(UnitTask task)
    {
        if (IsDotsControlled && !m_ApplyingDotsSnapshot)
        {
            var em = SwarmGameBridge.Instance.Gameplay.EntityManager;
            var actor = em.GetComponentData<GameplayActor>(DotsEntity); actor.Task = (GameplayTask)task; em.SetComponentData(DotsEntity, actor); return;
        }
        OnSetTask(CurrentTask, task);
    }
    public void SetState(UnitState state)
    {
        if (IsDotsControlled && !m_ApplyingDotsSnapshot) return;
        OnSetState(CurrentState, state);
    }
    public void SetTarget(Unit target)
    {
        if (IsDotsControlled && !m_ApplyingDotsSnapshot)
        { SwarmGameBridge.Instance.Gameplay.Attack(DotsEntity, SwarmGameBridge.Instance.ProxyFor(target)); return; }
        ClearDotsTarget();
        Target = target;
    }
    public virtual void SetStance(UnitStanceActionSO stance)
    {
        m_CurrentStance = stance.UnitStance;
        if (IsDotsControlled) SwarmGameBridge.Instance.Gameplay.SetStance(DotsEntity, m_CurrentStance == UnitStance.Offensive);

        for (int i = 0; i < m_Actions.Length; i++)
        {
            if (m_Actions[i] == stance)
            {
                m_GameManager.FocusActionUI(i);
                return;
            }
        }
    }
    public void MoveTo(Vector3 destination, DestinationSource source = DestinationSource.CodeTriggered)
    {
        if (IsDotsControlled && !m_ApplyingDotsSnapshot)
        { SwarmGameBridge.Instance.MoveActor(this, destination, source == DestinationSource.PlayerClick); return; }
        if (m_AIPawn != null)
        {
            m_AIPawn.SetDestination(destination);
        }
        OnSetDestination(source);
    }
    public void Select()
    {
        HighLight();
        isTargeted = true;

        for (int i = 0; i < m_Actions.Length; i++)
        {
            if (m_Actions[i] is UnitStanceActionSO stanceAction && stanceAction.UnitStance == m_CurrentStance)
            {
                m_GameManager.FocusActionUI(i);
                return;
            }
        }
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
    protected virtual void OnSetDestination(DestinationSource source)
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
        if (SwarmGameBridge.Instance != null && SwarmGameBridge.Instance.TryConvertEnemy(unit)) return;
        if (m_IsRegistered)
        {
            return;
        }

        if (m_GameManager == null)
        {
            m_GameManager = GameManager.Get();
        }

        m_GameManager.RegisterUnit(unit);
        m_IsRegistered = true;
    }
    protected virtual void UnregisterUnit(Unit unit)
    {
        if (!m_IsRegistered)
        {
            return;
        }

        m_GameManager.UnregisterUnit(unit);
        m_IsRegistered = false;
    }
    protected virtual bool TryFindClosetFoe(out Unit foe)
    {
        if (Time.time >= m_NextUnitDetectionTime)
        {
            m_NextUnitDetectionTime = Time.time + m_UnitDetectionCheckRate;
            foe = m_GameManager.FindClosetUnit(transform.position, m_ObjectDetectionRadius, !IsPlayer);
            //Debug.Log($"Found closet foe: {foe?.name ?? "None"}");
            return foe != null;
        }
        else
        {
            foe = null;
            return false;
        }
    }
    protected virtual void OnAttackReady(Unit target)
    {
        PerformAttackAnimation();
        StartCoroutine(DelayDamage(m_AutoAttackDamageDelay, m_AutoAttackDamage, Target));
    }
    protected virtual bool TryAttackCurrentTarget()
    {
        if (Target == null || !Target.IsTargetable)
        {
            SetTarget(null);
            return false;
        }
        if (Time.time >= m_NextAutoAttackTime)
        {
            m_NextAutoAttackTime = Time.time + m_AutoAttackFrequency;
            OnAttackReady(Target);
            return true;
        }

        //Debug.Log("Attack is on CD");
        return false;
    }
    protected virtual void PerformAttackAnimation()
    {

    }
    protected virtual void RunDeadEffect()
    {

    }
    protected virtual void Die()
    {
        SetState(UnitState.Dead);

        if (m_AIPawn != null)
        {
            StopMovement();
        }
        RunDeadEffect();
        UnregisterUnit(this);
    }

    public virtual void OnSpawnedFromPool(bool isReused)
    {
        ResetRuntimeState();
        if (isReused)
        {
            RegisterUnit(this);
        }
    }

    public virtual void OnReturnedToPool()
    {
        if (IsDotsControlled)
        { UnregisterUnit(this); SwarmGameBridge.Instance?.Unregister(this); }
        StopMovement();
        SetTarget(null);
        CurrentTask = UnitTask.None;
        CurrentState = UnitState.Idle;

        if (m_FlashCoroutine != null)
        {
            StopCoroutine(m_FlashCoroutine);
            m_FlashCoroutine = null;
        }

        if (m_SpriteRenderer != null)
        {
            m_SpriteRenderer.color = m_OriginalColor;
            m_SpriteRenderer.enabled = true;
        }

        if (m_Collider != null)
        {
            m_Collider.enabled = true;
        }
    }

    public void DespawnToPool()
    {
        UnregisterUnit(this);
        gameObject.SetActive(false);
        RuntimeObjectPool.Release(gameObject);
    }

    protected virtual void ResetRuntimeState()
    {
        ClearDotsTarget();
        CurrentState = UnitState.Idle;
        CurrentTask = UnitTask.None;
        Target = null;
        m_CurrentHealth = m_Health;
        m_NextAutoAttackTime = 0f;
        isTargeted = false;

        if (m_SpriteRenderer != null)
        {
            m_SpriteRenderer.enabled = true;
            Color restoredColor = m_OriginalColor;
            restoredColor.a = m_SpriteRenderer.color.a;
            m_SpriteRenderer.color = restoredColor;
        }

        if (m_Collider != null)
        {
            m_Collider.enabled = true;
        }
    }
    private Coroutine m_FlashCoroutine;
    public virtual void TakeDamage(int dmg, Unit damager)
    {
        if (IsDotsControlled && !m_ApplyingDotsSnapshot)
        { SwarmGameBridge.Instance.Simulation.QueueDamage(DotsEntity, dmg, SwarmGameBridge.Instance.ProxyFor(damager)); return; }
        if (!gameObject.activeInHierarchy || !IsTargetable)
        {
            return;
        }
        m_CurrentHealth -= dmg;

        if (!hasTarget)
        {
            SetTarget(damager);
        }

        m_GameManager.ShowTextPopup(dmg.ToString(), Color.red, GetTopPosition());

        if (m_FlashCoroutine != null)
        {
            StopCoroutine(m_FlashCoroutine);
            Color resetColor = m_OriginalColor;
            resetColor.a = m_SpriteRenderer.color.a;
            m_SpriteRenderer.color = resetColor;
        }
        m_FlashCoroutine = StartCoroutine(FlashEffect(m_DamageFlashColor, 1, 0.2f));

        if (m_CurrentHealth <= 0)
        {
            Die();
        }
    }

    protected IEnumerator FlashEffect(Color color, int flashCount, float duration)
    {
        Color originalColor = m_SpriteRenderer.color;
        for (int i = 0; i < flashCount; i++)
        {
            color.a = m_SpriteRenderer.color.a;
            m_SpriteRenderer.color = color;
            yield return new WaitForSeconds(duration / 2f);

            originalColor.a = m_SpriteRenderer.color.a;
            m_SpriteRenderer.color = originalColor;
            yield return new WaitForSeconds(duration / 2f);
        }

        Color finalColor = m_OriginalColor;
        finalColor.a = m_SpriteRenderer.color.a;
        m_SpriteRenderer.color = finalColor;
        m_FlashCoroutine = null;
    }

    protected IEnumerator DelayDamage(float delay, int damage, Unit target)
    {
        yield return new WaitForSeconds(delay);
        if (target != null)
        {
            if (target.CurrentState == UnitState.Dead)
            {
                SetTarget(null);
            }
            else
            {
                target.TakeDamage(damage, this);
            }
        }
    }

    protected bool IsTargetInRange(Unit target)
    {
        if (target == null || !target.IsTargetable || target.Collider == null)
        {
            return false;
        }

        var targetCollider = target.Collider;
        var targetClosetPoint = targetCollider.ClosestPoint(transform.position);

        return Vector3.Distance(targetClosetPoint, transform.position) <= m_AttackRange;
    }
    protected Collider2D[] RunProximityObjectDetection()
    {
        return Physics2D.OverlapCircleAll(transform.position, m_ObjectDetectionRadius);
    }
    void TurnToPosition(Vector3 position)
    {
        var direction = (position - transform.position).normalized;
        if (Mathf.Abs(direction.x) <= 0.001f)
        {
            return;
        }

        m_SpriteRenderer.flipX = direction.x < 0;
    }
    protected virtual void OnDestinationReached()
    {
        // This can be overridden by child classes to implement behavior when reaching destination
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
