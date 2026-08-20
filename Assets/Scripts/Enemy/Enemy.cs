using System;
using UnityEngine;

/// <summary>
/// Base for all enemies (including bosses). Composition-first: this holds only
/// what every enemy shares for pooling/generalization. HP lives on a Health
/// component; attacks/affixes are separate components added per-enemy.
/// </summary>
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(Combat))]
[RequireComponent(typeof(XPDropper))]
public class Enemy : MonoBehaviour
{
    public static LayerMask LayerMask => LayerMask.GetMask("Enemy");
    
    [SerializeField] private float movementSpeed;
    [field: SerializeField] public bool AffixEligible { get; private set; }

    [Tooltip("The value added to the kill tracker on kill. Elites may have more. Tune later.")]
    [SerializeField] private float killValue = 1f;  // TUNE LATER
    private Health health;
    private XPDropper xpDropper;
    private Collider[] cachedHitboxColliders;
    private Animator animator;

    public bool CountsTowardSpawnCap { get; set; }

    /// <summary>Raised after death logic runs. Spawner listens and releases to pool.</summary>
    public event Action<Enemy> OnDied;

    /// <summary>
    /// Static so any progression system can subscribe once without holding refs to
    /// individual enemies. Carries the XP amount and who gets credit (the killer).
    /// </summary>
    public static event Action<int, GameObject> OnAnyEnemyDied;

    /// <summary>
    /// Per-enemy drop hook. Fires at the death position so a future loot system can
    /// spawn pickups there. Carries position + whether this enemy was affix-eligible
    /// (elites usually drop better). Wire a listener when the pickup system exists.
    /// </summary>
    public event Action<Vector3, bool> OnDropLoot;

    protected virtual void Awake()
    {
        health = GetComponent<Health>();
        xpDropper = GetComponent<XPDropper>();
        cachedHitboxColliders = GetComponentsInChildren<Collider>();
        animator = GetComponentInChildren<Animator>();
    }

    private void OnEnable()
    {
        health.OnDeath += HandleDeath;
    }

    private void OnDisable()
    {
        health.OnDeath -= HandleDeath;
    }

    private void HandleDeath(DamageInfo killingBlow)
    {
        // Enemy-specific death work happens here, BEFORE we hand back to the pool:
        // added by orion: death animation feel free to move this
        animator?.SetTrigger("Dead");

        // 1. XP spawn the orb.
        xpDropper.Drop();

        OnAnyEnemyDied?.Invoke(xpDropper.Value, killingBlow.Source);

        StageKills.Instance?.Tracker.ReportKill(killValue);     // BIG NOTE. IF IT FAILS, DROP THE ? AND MAKE IT NRE!

        // 2. Drops announce at this position, while still active/placed.
        OnDropLoot?.Invoke(transform.position, AffixEligible);

        // 3. Release last, after death work is done.
        OnDied?.Invoke(this);   // tells the spawner: I'm done, release me fool! :)
    }

    /// <summary>
    /// Combines the hitboxes of the enemy on awake.
    /// </summary>
    /// <returns>Returns the location as a Vector3.</returns>
    /// 
    // Possible TODO: Depending on needs, we might need an override to allow us to search a "Hitboxes" container gameobject instead of all the children of the root.
    public Vector3 GetCenterPoint()
    {
        // failsafe incase it doesn't have a collider or breaks.
        if (cachedHitboxColliders == null || cachedHitboxColliders.Length == 0)
        {
            Debug.LogWarning($"[Enemy] - GetCenterPoint: {name} is missing a hitbox or the cache failed to allocate. Using failsafe function. This may cause inconsistent collisions.", this);
            return transform.position + Vector3.up;
        }

        Bounds combinedBounds = default;
        bool init = false;

        for (int i = 0; i < cachedHitboxColliders.Length; i++)
        {
            Collider col = cachedHitboxColliders[i];

            if (col == null || !col.enabled)
            {
                continue;
            }

            // Ignores non hitbox trigger colliders (e.g. aggro radius or wtv)
            if (((1 << col.gameObject.layer) & LayerMask.value) == 0)
            {
                continue;
            }
            
            if (!init)
            {
                combinedBounds = col.bounds;
                init = true;
            }
            else
            {
                combinedBounds.Encapsulate(col.bounds);
            }
        }

        return init ? combinedBounds.center : (transform.position + Vector3.up);
    }
}
