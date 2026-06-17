using UnityEngine;
using System.Collections;
public class EnemyRangerUnit : EnemyUnit
{
    [SerializeField] private Projectile m_ProjectilePrefab;
    [SerializeField] private Transform m_ProjectileSpawnPoint;
    [SerializeField] private bool m_MirrorSpawnPointWithFacing = true;
    [SerializeField] private bool m_FireByAnimationEvent = false;
    [SerializeField] private float m_FallbackShootDelay = 0.48f;
    private Unit m_PendingShotTarget;
    private int m_PendingShotDamage;
    private bool m_HasPendingShot;
    private Vector3 m_ProjectileSpawnLocalOffset;
    private bool m_HasProjectileSpawnOffset;

    protected override void Start()
    {
        base.Start();
        CacheProjectileSpawnOffset();
    }

    protected override void OnAttackReady(Unit target)
    {
        m_PendingShotTarget = target;
        m_PendingShotDamage = m_AutoAttackDamage;
        m_HasPendingShot = true;
        PerformAttackAnimation();

        if (!m_FireByAnimationEvent)
        {
            StartCoroutine(ShootProjectile(m_FallbackShootDelay));
        }
    }

    public void FireArrowFromAnimationEvent()
    {
        if (!m_FireByAnimationEvent)
        {
            return;
        }

        TryFirePendingShot();
    }

    private IEnumerator ShootProjectile(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (CurrentState == UnitState.Dead)
        {
            yield break;
        }

        TryFirePendingShot();
    }

    private void TryFirePendingShot()
    {
        if (!m_HasPendingShot)
        {
            return;
        }

        Unit target = m_PendingShotTarget;
        int damage = m_PendingShotDamage;
        m_HasPendingShot = false;
        m_PendingShotTarget = null;

        if (target != null && target.IsTargetable)
        {
            Vector3 spawnPosition = GetProjectileSpawnPosition();
            var projectile = RuntimeObjectPool.Spawn(m_ProjectilePrefab, spawnPosition, Quaternion.identity);
            projectile?.Initialize(this, target, damage, spawnPosition);
        }
    }

    protected override void OnRetreatStarted()
    {
        m_HasPendingShot = false;
        m_PendingShotTarget = null;
    }

    private void CacheProjectileSpawnOffset()
    {
        if (m_ProjectileSpawnPoint == null)
        {
            return;
        }

        m_ProjectileSpawnLocalOffset = transform.InverseTransformPoint(m_ProjectileSpawnPoint.position);
        m_HasProjectileSpawnOffset = true;
    }

    private Vector3 GetProjectileSpawnPosition()
    {
        if (m_ProjectileSpawnPoint == null)
        {
            return transform.position;
        }

        if (!m_MirrorSpawnPointWithFacing || !m_HasProjectileSpawnOffset || m_SpriteRenderer == null)
        {
            return m_ProjectileSpawnPoint.position;
        }

        Vector3 localOffset = m_ProjectileSpawnLocalOffset;
        localOffset.x = Mathf.Abs(localOffset.x) * (m_SpriteRenderer.flipX ? -1f : 1f);
        return transform.TransformPoint(localOffset);
    }
}
