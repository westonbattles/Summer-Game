using UnityEngine;

public class RangedAttack : AttackBehavior
{
    [SerializeField] private Transform firePoint;
    [SerializeField] private float projectileSpeed = 12f;
    // TODO: wire when pool exists
    // [SerializeField] private ProjectilePool projectilePool;

    public override void OnAttackHit()
    {
        // Re-aim at fire time so the shot leads toward where the player is NOW,
        // not where they were when the cast started.
        Vector3 direction = (target.position - firePoint.position).normalized;

        // TODO: pull a Projectile from the pool, place at firePoint,
        //       init with: direction, projectileSpeed, damage, gameObject (source).
        Debug.DrawRay(firePoint.position, direction * 5f, Color.red, 0.5f);
    }
}
