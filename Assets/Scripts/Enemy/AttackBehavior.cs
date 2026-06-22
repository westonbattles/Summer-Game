using UnityEngine;

/// <summary>
/// Shared logic for all enemy attacks. Owns target tracking, the engagement gate,
/// and cooldown. The attack is split in two: StartAttack() triggers the swing/cast
/// (called from Update when ready), and OnAttackHit() lands the damage (called by an
/// Animation Event on the contact frame). Subclasses implement OnAttackHit().
/// Target is injected by the spawner via SetTarget (pooling-friendly).
/// </summary>
[RequireComponent(typeof(Animator))]
public abstract class AttackBehavior : MonoBehaviour
{
    [Header("Debug / no-animation testing")]
    [SerializeField] private bool useAnimationEvents = true;
    [SerializeField] private float debugWindupSeconds = 0.3f;

    [Tooltip("How close the target needs to be to attack")]
    [SerializeField] protected float damage = 10f;
    [Tooltip("Seconds between attacks")]
    [SerializeField] protected float attackCooldown = 1f;
    [Tooltip("How close the target needs to be to start attacking. Tl;dr engagement distance.")]
    [SerializeField] protected float proximityToAttack = 1.5f;

    protected Transform target;
    protected Animator animator;

    protected float cooldownTimer;
    private static readonly int AttackTrigger = Animator.StringToHash("Attack");

    /// <summary>Spawner calls this. Pass null to clear (e.g. on return to pool).</summary>
    public void SetTarget(Transform newTarget) => target = newTarget;

    protected virtual void Awake()
    {
        animator = GetComponent<Animator>();
    }

    protected virtual void OnEnable()
    {
        cooldownTimer = 0f; // resets on cooldown & respawn
    }

    protected virtual void Update()
    {
        if (target == null) return;

        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer > 0f) return;

        float distance = Vector3.Distance(transform.position, target.position);
        if (distance > proximityToAttack) return;

        StartAttack();
    }

    /// <summary>
    /// Decision moment: fire the animation and start the cooldown. The actual hit
    /// comes later, when the animation's contact frame fires OnAttackHit().
    /// </summary>
    //protected virtual void StartAttack()
    //{
    //    animator.SetTrigger(AttackTrigger);
    //    cooldownTimer = attackCooldown;
    //}

    // debug: allow subclasses to override StartAttack() to add debug windup if not using animation events
    protected virtual void StartAttack()
    {
        cooldownTimer = attackCooldown;

        if (useAnimationEvents)
        {
            animator.SetTrigger(AttackTrigger);
        }
        else
        {
            Invoke(nameof(OnAttackHit), debugWindupSeconds);
        }
    }

    /// <summary>
    /// Called by an Animation Event on the swing/cast's contact frame.
    /// Must be public so the Animator can invoke it by name. Subclass lands the hit.
    /// </summary>
    public abstract void OnAttackHit();

    protected virtual void OnDrawGizmosSelected()
    {
        // Engagement range — how close the target must be before the attack triggers.
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, proximityToAttack);

        // Line to the current target (only meaningful in Play mode once injected).
        if (target != null)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawLine(transform.position, target.position);
        }
    }
}
