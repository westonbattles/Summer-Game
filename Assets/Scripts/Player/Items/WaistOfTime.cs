using System;
using UnityEngine;

[Serializable]
public class WaistOfTime : ItemBehaviour
{
    [SerializeField] private float CDRAmount = 0.15f;
    [SerializeField] private float minCDR = 0.15f;

    public override void ApplyModifiers(PlayerModifiers modifiers, int itemCount)
    {
        float totalReduction = CDRAmount * itemCount;
        modifiers.CooldownDurationMultiplier.AddValue(-totalReduction);
        
        if (modifiers.CooldownDurationMultiplier.GetValue() < minCDR)
        {
            modifiers.CooldownDurationMultiplier.SetValue(minCDR);
        }
    }
}