using UnityEngine;

/// <summary>
/// Shared logic for all enemy attacks. Owns target tracking, range gating, and
/// cooldown. Subclasses implement only PerformAttack() — the actual hit.
/// Target is injected by the spawner via SetTarget (works with pooling).
/// </summary>
public abstract class AttackBehavior : MonoBehaviour
{
    [Tooltip("How close the target needs to be to attack")]
    [SerializeField] protected float damage = 10f;
    [Tooltip("Attacks per second")]
    [SerializeField] protected float attackFrequency = 1f;
    [Tooltip("How far can the actual attack reach. Do not confuse it with proximity to attack.")]
    [SerializeField] protected float range = 2f;    // might be for raycast or overlap, depending on whats more performant. Makes HUGE difference long run
    // Physics.OverlapSphereNonAlloc is the best bet for melee attacks, and raycasts for ranged attacks. We can even mix them in the same class if we want to do something like a shotgun blast.
    [Tooltip("How close the target needs to be to start attacking")]
    [SerializeField] protected float proximityToAttack = 1.5f;

    protected Transform target;
    protected float cooldownTimer;

    /// <summary>Spawner calls this. Pass null to clear (e.g. on return to pool).</summary>
    public void SetTarget(Transform newTarget) => target = newTarget;

    protected virtual void OnEnable()
    {
        cooldownTimer = 0f; // resets on cooldown
    }

    protected virtual void Update()
    {
        if (target == null) return;

        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer > 0f) return;

        float distance = Vector3.Distance(transform.position, target.position);
        if (distance > proximityToAttack) return;

        PerformAttack();
        cooldownTimer = attackFrequency;   // this means seconds between attacks. Do not confuse with attacks per second.
    }

    /// <summary>The actual hit. Melee = overlap + TakeDamage; Ranged = spawn projectile.</summary>
    protected abstract void PerformAttack();
}
