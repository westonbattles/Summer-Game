using UnityEngine;

public class MeleeAttack : AttackBehavior
{
    [SerializeField] private float swingReach = 2f;
    [SerializeField] private LayerMask hittableLayers;

    public override void OnAttackHit()
    {
        // Contact frame: sphere around enemy, hit anything damageable in reach. This depends on layers!
        Collider[] hits = Physics.OverlapSphere(transform.position, swingReach, hittableLayers);

        foreach (Collider hit in hits)
        {
            if (hit.TryGetComponent<IDamageable>(out var dmg))
                dmg.TakeDamage(new DamageInfo(damage, gameObject));
        }
    }
}
