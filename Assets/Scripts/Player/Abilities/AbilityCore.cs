using UnityEngine;

/// <summary>
/// Identifies which of the four ability-bar slots an ability occupies.
/// An ability declares its slot on its AbilityDefinition; the controller maps each
/// slot to one input and one runtime AbilityInstance.
/// </summary>
public enum AbilitySlot
{
    Primary,
    Secondary,
    Movement,
    Special
}

public readonly struct AbilityContext
{
    public readonly GameObject Caster;      // will become DamageInfo.Source
    public readonly Vector3 AimOrigin;
    public readonly Vector3 AimDirection;
    public readonly float BaseDamage;

    public AbilityContext(GameObject caster, Vector3 origin, Vector3 dir, float baseDamage)
    {
        Caster = caster;
        AimOrigin = origin;
        AimDirection = dir;
        BaseDamage = baseDamage;
    }

    // The one place a DamageInfo is built. Source/crit filled in here at runtime,
    // which is why the asset stores only a number. This is important because we don't want to store a reference to the caster in the asset,
    // which would break prefab instancing.
    // uses factory design :)
    public DamageInfo BuildDamage(float multiplier = 1f, bool isCrit = false)
    {
        return new DamageInfo(BaseDamage * multiplier, Caster, isCrit);
    }

    // Hi Weston! o/
    // TODO: when effects need to spawn projectiles or move the player, add the handles they need.
    // e.g. a spawner, a motor as fields here.
}

/// <summary>
/// This is t he composition seam. Write subclasses of this.
/// It's also stateless. It reads the context, acts, stores nothing per-cast.
/// </summary>
[System.Serializable]
public abstract class AbilityEffect
{
    public abstract void Execute(in AbilityContext ctx);
}

// the runtime state. One per entity per slot.
// Charges generalize cooldowns: a normal ability is just maxCharges = 1.
public class AbilityInstance
{
    public AbilityDefinition Definition { get; }
    public int CurrentCharges { get; private set; }
    public float RechargeRemaining { get; private set; }

    public bool IsReady => CurrentCharges > 0;
    public float RechargeProgress01
    {
        get
        {
            if (Definition.cooldown <= 0f) return 1f;

            float progress = RechargeRemaining / Definition.cooldown;
            return Mathf.Clamp01(1f - progress);
        }
    }

    // Constructor
    public AbilityInstance(AbilityDefinition def)
    {
        Definition = def;
        CurrentCharges = def.maxCharges;
    }

    /// <summary>
    /// Ticks the ability instance, updating its recharge state based on the elapsed time (dt).
    /// </summary>
    /// <param name="dt"></param>
    public void Tick(float dt)
    {
        if (CurrentCharges >= Definition.maxCharges)
            return;

        if (Definition.cooldown <= 0f)
        {
            CurrentCharges = Definition.maxCharges;
            return;
        }

        RechargeRemaining -= dt;
        if (RechargeRemaining <= 0f)
        {
            CurrentCharges++;
            if (CurrentCharges < Definition.maxCharges)
            {
                RechargeRemaining += Definition.cooldown;
            }
            else
            {
                RechargeRemaining = 0f;
            }
        }
    }

    /// <summary>
    /// Attempts to activate the ability using the provided AbilityContext. 
    /// If the ability is ready (has charges), it executes the ability's effects, decrements the current charges, and starts the cooldown if it was previously full.
    /// </summary>
    /// <param name="ctx"></param>
    /// <returns></returns>
    public bool TryActivate(in AbilityContext ctx)
    {
        if (!IsReady) return false;
        bool wasFull = CurrentCharges >= Definition.maxCharges;
        Definition.Execute(in ctx);
        CurrentCharges--;
        if (wasFull) RechargeRemaining = Definition.cooldown;
        return true;
    }
}
