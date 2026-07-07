using System;
using UnityEngine;

/// <summary>
/// Base for all enemies (including bosses). Composition-first: this holds only
/// what every enemy shares for pooling/generalization. HP lives on a Health
/// component; attacks/affixes are separate components added per-enemy.
/// </summary>
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(XPDropper))]
public class Enemy : MonoBehaviour
{
    public static LayerMask LayerMask => LayerMask.GetMask("Enemy");
    
    [SerializeField] private float movementSpeed;
    [field: SerializeField] public bool AffixEligible { get; private set; }

    private Health health;
    private XPDropper xpDropper;

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

        // 1. XP � spawn the orb.
        xpDropper.Drop();

        OnAnyEnemyDied?.Invoke(xpDropper.Value, killingBlow.Source);

        // 2. Drops � announce at this position, while still active/placed.
        OnDropLoot?.Invoke(transform.position, AffixEligible);

        // 3. Release � last, after death work is done.
        OnDied?.Invoke(this);   // tells the spawner: I'm done, release me fool!
    }
}
