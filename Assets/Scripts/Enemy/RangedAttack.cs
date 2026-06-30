using UnityEngine;

public class RangedAttack : AttackBehavior
{
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float projectileSpeed = 12f;

    public override void OnAttackHit()
    {
        Vector3 direction = (target.position - firePoint.position).normalized;

        Projectile proj = ProjectilePoolManager.Instance.Get(projectilePrefab);
        proj.transform.SetPositionAndRotation(firePoint.position, Quaternion.identity);
        proj.Launch(direction, projectileSpeed, new DamageInfo(damage, gameObject));
        // Debug.DrawRay(firePoint.position, direction * 5f, Color.red, 0.5f);
    }
}
