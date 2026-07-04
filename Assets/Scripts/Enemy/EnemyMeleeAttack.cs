using System.Collections.Generic;
using UnityEngine;

public class EnemyMeleeAttack : MonoBehaviour, IEnemyAttack
{
    [SerializeField] private float damage = 10f;
    [SerializeField] private float cooldown = 1f;
    [SerializeField] private float range = 1.5f;
    [SerializeField] private float windup = 0.3f;
    [SerializeField] private float hitRadius = 1f;
    [Tooltip("Where the hit detection is centered. Leave empty to use this enemy's root position.")]
    [SerializeField] private Transform hitOrigin;
    [SerializeField] private LayerMask hittableLayers;

    [Header("Knockback")]
    [SerializeField] private bool applyKnockback = true;
    [SerializeField] private float knockbackStrength = 5f;

    private Transform target;
    private Animator animator;

    private float cooldownRemaining;
    private float windupRemaining;
    private bool isWindingUp;

    private readonly HashSet<Transform> hitRoots = new();

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        if (hitOrigin == null)
            hitOrigin = transform;      // this is the fallback to root if nothing was assigned.
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    private void Update()
    {
        if (cooldownRemaining > 0f)
            cooldownRemaining -= Time.deltaTime;
        if (!isWindingUp) return;

        windupRemaining -= Time.deltaTime;
        if (windupRemaining <= 0f)
            LandHit();
    }

    public bool TryExecute()
    {
        if (isWindingUp || cooldownRemaining > 0f)
        {
            return false;
        }
        if (target == null)
        {
            Debug.Log($"EnemyMeleeAttack (TryExecute): {name}: blocked — target is null");
            return false;
        }

        if (Vector3.Distance(transform.position, target.position) > range)
        {
            return false;
        }

        isWindingUp = true;
        windupRemaining = windup;
        animator?.SetTrigger("Attack");
        return true;
    }

    private void LandHit()
    {
        isWindingUp = false;
        cooldownRemaining = cooldown;

        Collider[] hits = Physics.OverlapSphere(hitOrigin.position, hitRadius, hittableLayers);
        hitRoots.Clear();

        foreach (var hit in hits)
        {
            if (!hitRoots.Add(hit.transform.root))  // prevents duplicate hits
            {
                continue;
            }

            //Debug.Log($"{name}: hit collider {hit.name}, has IDamageable: {hit.TryGetComponent<IDamageable>(out _)}");
            // useful debug if you want to see what the enemy hit.
            IDamageable damageable = hit.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(new DamageInfo(damage, gameObject));
            }

            if (applyKnockback)
            {
                IKnockbackable knockbackable = hit.GetComponentInParent<IKnockbackable>();
                knockbackable?.ApplyKnockback(transform.position, knockbackStrength);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Engagement range — how close the target needs to be before TryExecute() succeeds.
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, range);

        // Hit detection — where LandHit()'s OverlapSphere actually checks, which may
        // differ from engagement range if hitOrigin is offset from root.
        Transform origin = hitOrigin != null ? hitOrigin : transform;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(origin.position, hitRadius);

        // Line to current target, only meaningful in Play mode once SetTarget has run.
        if (target != null)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawLine(transform.position, target.position);
        }
    }
}
