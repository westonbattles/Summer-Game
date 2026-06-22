using UnityEngine;

public class MeleeAttack : AttackBehavior
{
    [SerializeField] private float swingReach = 2f;
    [SerializeField] private LayerMask hittableLayers;
    [SerializeField] private int maxHits = 8;   // buffer size for array, max targets one swing can hit

    private Collider[] hitBuffer;
    // I want to make a quick note here.
    // Using Physics.OverlapSphere is bad compared to Physics.OverlapSphereNonAlloc because it allocates memory on the heap every time it's called, which can lead to garbage collection and performance issues.

    protected override void Awake()
    {
        base.Awake();       // we need to keep animator references, so do not change this unless you reach out to me - jake
        hitBuffer = new Collider[maxHits];
    }

    public override void OnAttackHit()
    {
        // Contact frame: sphere around enemy, hit anything damageable in reach. This depends on layers!
        int count = Physics.OverlapSphereNonAlloc(
            transform.position, swingReach, hitBuffer, hittableLayers);

        for (int i = 0; i < count; i++)
        {
            if (hitBuffer[i].TryGetComponent<IDamageable>(out var target))
                target.TakeDamage(new DamageInfo(damage, gameObject));
        }
    }
}
