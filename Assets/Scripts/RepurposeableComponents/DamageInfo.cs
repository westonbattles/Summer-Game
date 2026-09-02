using UnityEngine;

/// <summary>
/// This is the data structure that represents our damage values. Anytime we want to deal damage to an IDamageable, we will create a DamageInfo and pass it to TakeDamage().
/// This is a struct because we want to avoid heap allocations when creating damage values. 
/// This is important because we will be creating a lot of damage values every frame, and we don't want to create garbage that needs to be collected.
/// With object pooling, this can help.
/// </summary>
public struct DamageInfo
{
    public float Amount;
    public DamageType Type;
    public GameObject Source;   // who dealt the damage (e.g. kill credit, thorns, aggro)
    public bool IsCrit;
    public readonly bool IsProc;
    // public DamageType Type;  // an enum for when we add affixes

    public DamageInfo(float amount, DamageType type, GameObject source = null, bool isProc = false, bool isCrit = false)
    {
        Amount = amount;
        Type = type;
        Source = source;
        IsProc = isProc;
        IsCrit = isCrit;
    }
}
