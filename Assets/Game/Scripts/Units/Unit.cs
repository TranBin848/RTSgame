using UnityEngine;

public enum UnitState
{
    Idle, Moving, Attacking, Chopping, Mining, Building
}
public enum UnitTask
{
    None, Build, Chop, Mine, Attack
}
public abstract class Unit : MonoBehaviour
{
    [SerializeField] private ActionSO[] m_Actions;
    [SerializeField] protected float m_ObjectDetectionRadius = 0.5f;

    public bool isTargeted = false;
    protected GameManager m_GameManager;
    protected Animator m_Animator;
    protected AIPawn m_AIPawn;
    protected SpriteRenderer m_SpriteRenderer;
    protected Material m_OriginalMaterial;
    protected Material m_HighlightMaterial;
    public UnitState CurrentState { get; protected set; } = UnitState.Idle;
    public UnitTask CurrentTask { get; protected set; } = UnitTask.None;
    public Unit Target { get; protected set; }
    public virtual bool IsPlayer => true;
    public virtual bool IsBuilding => false;
    public ActionSO[] Actions => m_Actions;
    public SpriteRenderer SpriteRenderer => m_SpriteRenderer;
    public bool hasTarget => Target != null;
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
        if (m_SpriteRenderer != null)
        {
            m_OriginalMaterial = m_SpriteRenderer.material;
        }
        m_GameManager = GameManager.Get();
        m_HighlightMaterial = Resources.Load<Material>("Materials/Outline");
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
    }
}
