using System;
using UnityEngine;
using NaughtyAttributes;

[DisallowMultipleComponent]
public class Health : MonoBehaviour, IDamageable, IPoolable
{
    [SerializeField] private float baseMaxHP = 100f;
    public float BaseMaxHP => baseMaxHP;
    public float MaxHP => baseMaxHP * (1f + currentMaxHealthBonus);

    [field: SerializeField]
    [field: ProgressBar("Health", nameof(MaxHP), EColor.Red)]
    public float CurrentHP { get; private set; }
    public bool IsAlive => CurrentHP > 0f;

    /// <summary>Fired on any HP change. (current, max) - for health bars.</summary>
    public event Action<float, float> OnHealthChanged;
    /// <summary>Fired when this dies. Carries the killing blow for kill credit.</summary>
    public event Action<DamageInfo> OnDeath;

    private bool isDead;
    private bool isPlayer;
    private float currentMaxHealthBonus = 0f;

    private void Awake()
    {
        isPlayer = Player.Instance != null && gameObject == Player.Instance.gameObject;
        CurrentHP = MaxHP;
    }

    private void OnEnable()
    {
        if (isPlayer && Player.Instance?.Modifiers != null)
        {
            Player.Instance.Modifiers.MaxHealthBonus.OnValueChanged += HandleMaxHealthBonusChanged;
            currentMaxHealthBonus = Player.Instance.Modifiers.MaxHealthBonus.GetValue();
        }
    }

    private void OnDisable()
    {
        if (isPlayer && Player.Instance?.Modifiers != null)
        {
            Player.Instance.Modifiers.MaxHealthBonus.OnValueChanged -= HandleMaxHealthBonusChanged;
        }
    }

    public void OnSpawn()
    {
        isDead = false;
        CurrentHP = MaxHP;      // MaxHP may have been bumped by an affix; refill to it
        OnHealthChanged?.Invoke(CurrentHP, MaxHP);
    }

    public void OnDespawn()
    {
        // nothing to reset on despawn, but we need this to satisfy IPoolable
    }


    /// <summary>
    /// TakeDamage reduces HP by the given amount, and fires OnHealthChanged. If HP drops to 0 or below, it fires OnDeath. It ignores damage if already dead or if damage is 0 or negative.
    /// </summary>
    /// <param name="info"></param>
    public void TakeDamage(DamageInfo info)
    {
        if (isDead || info.Amount <= 0f)
            return;

        float finalDamage = info.Amount * GetDamageTakenMultiplier();

        CurrentHP = Mathf.Max(CurrentHP - finalDamage, 0f);
        OnHealthChanged?.Invoke(CurrentHP, MaxHP);

        if (CurrentHP <= 0f)
            Die(info);
    }

    /// <summary>
    /// Reacts whenever PlayerModifiers updates MaxHealthBonus (e.g., item equipped/unequipped).
    /// </summary>
    private void HandleMaxHealthBonusChanged(float newBonus)
    {
        currentMaxHealthBonus = newBonus;

        // Clamp CurrentHP to the new MaxHP so player doesn't exceed cap
        CurrentHP = Mathf.Min(CurrentHP, MaxHP);
        OnHealthChanged?.Invoke(CurrentHP, MaxHP);
    }

    /// <summary>
    /// Heal increases HP by the given amount, up to MaxHP. It fires OnHealthChanged. It ignores healing if already dead or if amount is 0 or negative.
    /// </summary>
    /// <param name="amount"></param>
    public void Heal(float amount)
    {
        if (isDead || amount <= 0f)
            return;

        CurrentHP = Mathf.Min(CurrentHP + amount, MaxHP);
        OnHealthChanged?.Invoke(CurrentHP, MaxHP);
    }

    /// <summary>
    /// Call during gameplay (buffs/debuffs/level ups) to change capacity without healing.
    /// </summary>
    /// <param name="newMax"></param>
    public void SetMaxHP(float newMax)
    {
        baseMaxHP = Mathf.Max(newMax, 1f);
        CurrentHP = Mathf.Min(CurrentHP, MaxHP);
        OnHealthChanged?.Invoke(CurrentHP, MaxHP);
    }

    /// <summary>
    /// Call at spawn or before combat to set starting max health and refill fully
    /// </summary>
    /// <param name="newMax"></param>
    public void InitializeMaxHP(float newMax)
    {
        baseMaxHP = Mathf.Max(newMax, 1f);
        CurrentHP = MaxHP;
        OnHealthChanged?.Invoke(CurrentHP, MaxHP);
    }

    /// <summary>
    /// Handles death logic.
    /// </summary>
    /// <param name="killingBlow">Whoever did the killing blow.</param>
    private void Die(DamageInfo killingBlow)
    {
        Debug.Log($"{name} Died: killed by {killingBlow.Source?.name ?? "unknown"}", this);
        if (isDead)
            return;
        isDead = true;
        OnDeath?.Invoke(killingBlow);
    }

    private float GetDamageTakenMultiplier()
    {
        // Check if this entity is the player
        if (Player.Instance != null && gameObject == Player.Instance.gameObject)
        {
            return Player.Instance.Modifiers.DamageTakenPercent.GetValue();
        }

        // Optional: If enemies can also have modifiers (e.g., via a local Modifier component), fetch it here.
        // Otherwise, default to taking full 100% damage.
        return 1f;
    }

#if UNITY_EDITOR
    #region DEBUGGING
    [Header("Debug Settings")]
#pragma warning disable CS0414
    [SerializeField] private bool showDebugButtons = false;
#pragma warning restore CS0414

    // ------  Buttons ------
    [ShowIf(nameof(showDebugButtons))]
    [Button("Take 10 Damage")]
    private void TestTakeDamage()
    {
        TakeDamage(new DamageInfo(10f, DamageType.Physical, gameObject));
    }

    [ShowIf(nameof(showDebugButtons))]
    [Button("Heal 10 Health")]
    private void TestHeal()
    {
        Heal(10f);
    }

    [ShowIf(nameof(showDebugButtons))]
    [Button("Kill")]
    private void TestDie()
    {
        TakeDamage(new DamageInfo(CurrentHP, DamageType.Physical, gameObject));
    }

    [ShowIf(nameof(showDebugButtons))]
    [Button("+10 Max HP (No Heal)")]
    private void TestAddMaxHPNoHeal()
    {
        SetMaxHP(MaxHP + 10f);
    }

    [ShowIf(nameof(showDebugButtons))]
    [Button("-10 Max HP (No Heal)")]
    private void TestReduceMaxHPNoHeal()
    {
        SetMaxHP(MaxHP - 10f);
    }

    [ShowIf(nameof(showDebugButtons))]
    [Button("Re-Initialize +10 Max HP (Refill)")]
    private void TestInitializeMaxHP()
    {
        InitializeMaxHP(MaxHP + 10f);
    }
    #endregion
#endif
}
