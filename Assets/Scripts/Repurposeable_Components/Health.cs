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

    public void TakeDamage(DamageInfo info)
    {
        if (isDead || info.Amount <= 0f)
            return;

        CurrentHP = Mathf.Max(CurrentHP - info.Amount, 0f);
        OnHealthChanged?.Invoke(CurrentHP, MaxHP);

        if (CurrentHP <= 0f)
            Die(info);
    }

    /* TODO's
     * public void Heal(float amount)
     * public void SetMaxHP(float newMax)
     * private void Die(DamageInfo killingBlow)
     * 
     * NOTE: SetMaxHP sets the max HP and refills. Affixes call this at spawn (e.g. elite x2.5 HP). It's called b4 combat starts, so a full refill is intended.
     * 
     */
    
}
