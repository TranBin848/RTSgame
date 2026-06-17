using UnityEngine;

public class Projectile : MonoBehaviour, IPooledRuntimeObject
{
    [Header("Linear")]
    [SerializeField] private float m_Speed = 10f;

    [Header("Visual")]
    [SerializeField] private bool m_ProjectileRotatable = false;

    [Header("Arc")]
    [SerializeField] private bool m_UseArcTrajectory = false;
    [SerializeField] private float m_ArcHeight = 1.5f;
    [SerializeField] private float m_MinTravelDuration = 0.2f;
    [SerializeField] private float m_MaxTravelDuration = 0.75f;
    [SerializeField] private float m_DistanceToDurationMultiplier = 0.08f;

    [SerializeField] private int m_Damage = 10;
    private Unit m_Target;
    private Unit m_Owner;
    private Vector3 m_StartPosition;
    private Vector3 m_TargetPosition;
    private float m_TravelDuration;
    private float m_TravelTimer;
    private bool m_HasAppliedDamage;

    public void Initialize(Unit owner, Unit target, int damage)
    {
        Initialize(owner, target, damage, transform.position);
    }

    public void Initialize(Unit owner, Unit target, int damage, Vector3 launchPosition)
    {
        m_Owner = owner;
        m_Target = target;
        m_Damage = damage;
        m_StartPosition = launchPosition;
        m_TravelTimer = 0f;
        m_HasAppliedDamage = false;
        transform.position = launchPosition;

        if (m_Target == null)
        {
            return;
        }

        m_TargetPosition = GetTargetAimPosition();
        float distance = Vector3.Distance(m_StartPosition, m_TargetPosition);
        m_TravelDuration = Mathf.Clamp(distance * m_DistanceToDurationMultiplier, m_MinTravelDuration, m_MaxTravelDuration);

        RotateTowards(m_TargetPosition - m_StartPosition);
    }

    void Update()
    {
        if (m_Target == null || !m_Target.IsTargetable)
        {
            ReleaseProjectile();
            return;
        }

        if (m_UseArcTrajectory)
        {
            UpdateArcTrajectory();
            return;
        }

        Vector3 direction = (GetTargetAimPosition() - transform.position).normalized;
        RotateTowards(direction);
        transform.position += direction * m_Speed * Time.deltaTime;
    }

    void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.TryGetComponent<Unit>(out var targetUnit))
        {
            if (targetUnit == m_Target)
            {
                ApplyDamageAndDestroy();
            }
        }
    }

    private void UpdateArcTrajectory()
    {
        m_TravelTimer += Time.deltaTime;
        float normalizedTime = m_TravelDuration <= 0.0001f ? 1f : Mathf.Clamp01(m_TravelTimer / m_TravelDuration);
        m_TargetPosition = GetTargetAimPosition();

        Vector3 linearPosition = Vector3.Lerp(m_StartPosition, m_TargetPosition, normalizedTime);
        float arcOffset = 4f * m_ArcHeight * normalizedTime * (1f - normalizedTime);
        Vector3 nextPosition = linearPosition + Vector3.up * arcOffset;
        Vector3 frameDirection = nextPosition - transform.position;

        if (frameDirection.sqrMagnitude > 0.0001f)
        {
            RotateTowards(frameDirection);
        }

        transform.position = nextPosition;

        if (normalizedTime >= 1f)
        {
            ApplyDamageAndDestroy();
        }
    }

    private void ApplyDamageAndDestroy()
    {
        if (m_HasAppliedDamage)
        {
            return;
        }

        m_HasAppliedDamage = true;

        if (m_Target != null && m_Target.IsTargetable)
        {
            m_Target.TakeDamage(m_Damage, m_Owner);
        }

        ReleaseProjectile();
    }

    private Vector3 GetTargetAimPosition()
    {
        if (m_Target == null)
        {
            return m_TargetPosition;
        }

        return m_Target.GetTopPosition();
    }

    private void RotateTowards(Vector3 direction)
    {
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        float angle;
        if (m_ProjectileRotatable)
        {
            float currentRotation = transform.eulerAngles.z;
            angle = currentRotation + 720f * Time.deltaTime;
        }
        else
        {
            angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        }

        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    public void OnSpawnedFromPool(bool isReused)
    {
        m_Target = null;
        m_Owner = null;
        m_TravelTimer = 0f;
        m_HasAppliedDamage = false;
    }

    public void OnReturnedToPool()
    {
        m_Target = null;
        m_Owner = null;
        m_TravelTimer = 0f;
        m_HasAppliedDamage = false;
    }

    private void ReleaseProjectile()
    {
        RuntimeObjectPool.Release(gameObject);
    }
}
