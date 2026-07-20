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
[RequireComponent(typeof(PsychicPulseHighlighter))]
public class Enemy : MonoBehaviour
{
    public static LayerMask LayerMask => LayerMask.GetMask("Enemy");
    
    [SerializeField] private float movementSpeed;
    [field: SerializeField] public bool AffixEligible { get; private set; }

    [Tooltip("The value added to the kill tracker on kill. Elites may have more. Tune later.")]
    [SerializeField] private float killValue = 1f;  // TUNE LATER
    private Health health;
    private XPDropper xpDropper;
    public PsychicPulseHighlighter Highlighter { get; private set; }

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
        Highlighter = GetComponent<PsychicPulseHighlighter>();

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

        // 1. XP spawn the orb.
        xpDropper.Drop();

        OnAnyEnemyDied?.Invoke(xpDropper.Value, killingBlow.Source);

        StageKills.Instance?.Tracker.ReportKill(killValue);     // BIG NOTE. IF IT FAILS, DROP THE ? AND MAKE IT NRE!

        // 2. Drops announce at this position, while still active/placed.
        OnDropLoot?.Invoke(transform.position, AffixEligible);

        // 3. Release last, after death work is done.
        OnDied?.Invoke(this);   // tells the spawner: I'm done, release me fool! :)
    }

    public Vector3 GetCenterPoint()
    {
        // TODO: use hit colliders in children or wtv to calculate y bounds. maybe even x/z as well (if we dont think they should be centered x/z at transform.position x/z)
        return transform.position + Vector3.up;
    }
}
