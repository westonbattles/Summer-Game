using System;
using UnityEngine;

public enum DamageType
{
    Physical,   // punches and stuff
    Projectile, // projectiles
    Magic,      // magic like the eye special abilities
    Fire,       // ticking burn damage
}

public struct Hit
{
    public IDamageable Target;
    public DamageInfo Info;
}

[DisallowMultipleComponent]
public class Combat : MonoBehaviour
{
    // we need the delegate so subscribers have a runtime reference to the current hit that is going on (explained more in the combat system md in ./Docs)
    public delegate void ModifyDamageHandler(ref Hit hit);
    public event ModifyDamageHandler BeforeDamageDealt; // gets invoked before applying damage
    public event Action<Hit> AfterDamageDealt;
    

    /// try apply damage
    public bool TryDamage(IDamageable target, DamageInfo info)
    {
        return ResolveDamage(target, info,
            notifyItems: true);
    }
    

    // doesn't allow items to apply effects / afflictions for this damage type
    public bool TryDamageWithoutEffects(IDamageable target, DamageInfo info)
    {
        return ResolveDamage(target, info,
            notifyItems: false);
    }

    bool ResolveDamage(IDamageable target, DamageInfo info, bool notifyItems)
    {
        if (target == null || !target.IsAlive) return false; // don't damage things that are dead

        if (info.Source == null) info.Source = gameObject; // automatically ensures correct source
        Hit hit = new()
        {
            Target = target,
            Info = info 
        };

        // notifies items before damage in case an item is subscribed and needs to do something
        if (notifyItems) BeforeDamageDealt?.Invoke(ref hit);
        
        // deal damage
        hit.Info.Amount = Mathf.Max(0f, hit.Info.Amount);
        target.TakeDamage(hit.Info);
        
        // notifies items after damage in case an item is subscribed and needs to do something (like inflecting a status effect!)
        if (notifyItems) AfterDamageDealt?.Invoke(hit);
        
        return true;
    }
}
