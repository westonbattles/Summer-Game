using UnityEngine;

[RequireComponent(typeof(EnemyAttackController))]
public class EnemyRangedAttack : MonoBehaviour, IEnemyAttack
{
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float damage = 8f;
    [SerializeField] private float cooldown = 1.5f;
    [SerializeField] private float maxRange = 12f;
    [SerializeField] private float projectileSpeed = 10f;

    private Transform target;
    private float cooldownRemaining;

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    private void Update()
    {
        if (cooldownRemaining > 0f)
            cooldownRemaining -= Time.deltaTime;
    }

    public bool TryExecute()
    {
        if (cooldownRemaining > 0f)
        {
            return false;
        }
        if (target == null)
        {
            return false;
        }
        if (Vector3.Distance(transform.position, target.position) > maxRange)
        {
            return false;
        }

        Vector3 direction = (target.position - firePoint.position).normalized;
        Projectile proj = ProjectilePoolManager.Instance.Get(projectilePrefab);
        proj.transform.SetPositionAndRotation(firePoint.position, Quaternion.identity);
        proj.Launch(direction, projectileSpeed, new DamageInfo(damage, gameObject));

        cooldownRemaining = cooldown;
        return true;
    }
    
    // do not need cancel.
}
