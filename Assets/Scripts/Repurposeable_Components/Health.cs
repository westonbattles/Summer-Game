using System;
using UnityEngine;

[DisallowMultipleComponent]
public class Health : MonoBehaviour, IDamageable
{
    [SerializeField] private float baseMaxHP = 100f;

    public float MaxHP { get; private set; }
    public float CurrentHP { get; private set; }
    public bool IsAlive => CurrentHP > 0f;

    /// <summary>Fired on any HP change. (current, max) - for health bars.</summary>
    public event Action<float, float> OnHealthChanged;
    /// <summary>Fired when this dies. Carries the killing blow for kill credit.</summary>
    public event Action<DamageInfo> OnDeath;

    private bool isDead;

    private void Awake()
    {
        MaxHP = baseMaxHP;
        CurrentHP = MaxHP;
    }

    /// <summary>
    /// TakeDamage reduces HP by the given amount, and fires OnHealthChanged. If HP drops to 0 or below, it fires OnDeath. It ignores damage if already dead or if damage is 0 or negative.
    /// </summary>
    /// <param name="info"></param>
    public void TakeDamage(DamageInfo info)
    {
        if (isDead || info.Amount <= 0f)
            return;

        CurrentHP = Mathf.Max(CurrentHP - info.Amount, 0f);
        OnHealthChanged?.Invoke(CurrentHP, MaxHP);

        if (CurrentHP <= 0f)
            Die(info);
    }

    /// <summary>
    /// SetMaxHP sets the max HP and refills. Affixes call this at spawn (e.g. elite x2.5 HP). It's called b4 combat starts, so a full refill is intended.
    /// </summary>
    /// <param name="newMax"></param>
    public void SetMaxHP(float newMax)
    {
        MaxHP = MathF.Max(newMax, 1f);
        CurrentHP = MaxHP;
        OnHealthChanged?.Invoke(CurrentHP, MaxHP);
    }

    /// <summary>
    /// Handles death logic.
    /// </summary>
    /// <param name="killingBlow">Whoever did the killing blow.</param>
    private void Die(DamageInfo killingBlow)
    {
        if (isDead)
            return;
        isDead = true;
        OnDeath?.Invoke(killingBlow);
    }
}
