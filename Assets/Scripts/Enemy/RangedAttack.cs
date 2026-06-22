using UnityEngine;
using UnityEngine.Pool;

public class RangedAttack : AttackBehavior
{
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float projectileSpeed = 12f;
    [SerializeField] private int defaultPoolSize = 16;

    private ObjectPool<Projectile> pool;

    protected override void Awake()
    {
        base.Awake();   // load-bearing animator reference, do not change the base awake unless you reach out to me - jake

        pool = new ObjectPool<Projectile>(
            createFunc: () => Instantiate(projectilePrefab),
            actionOnGet: p => p.gameObject.SetActive(true),
            actionOnRelease: p => p.gameObject.SetActive(false),
            actionOnDestroy: p => Destroy(p.gameObject),
            collectionCheck: true,      // catches accidental double-release in editor. we don't want to corrupt the pool!
            defaultCapacity: defaultPoolSize
        );
    }

    public override void OnAttackHit()
    {
        // Re-aim at fire time so the shot leads toward where the player is NOW,
        // not where they were when the cast started.
        Vector3 direction = (target.position - firePoint.position).normalized;

        Projectile proj = pool.Get();
        proj.transform.SetPositionAndRotation(firePoint.position, Quaternion.identity);
        proj.Launch(direction, projectileSpeed, new DamageInfo(damage, gameObject), pool.Release);
        // Debug.DrawRay(firePoint.position, direction * 5f, Color.red, 0.5f);
    }
}
