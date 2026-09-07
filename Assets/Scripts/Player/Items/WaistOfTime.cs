using System;
using UnityEngine;

[Serializable]
public class WaistOfTime : ItemBehaviour
{
    [SerializeField] private float CDRAmount = 0.15f;
    [SerializeField] private float minCDR = 0.15f;

    public override void ApplyModifiers(PlayerModifiers modifiers, int itemCount)
    {
        // (1 - 0.15)^itemCount
        // 1 item  = 0.85
        // 2 items = 0.7225
        // 7 items = 0.3205

        float remainingDurationRatio = Mathf.Pow(1f - CDRAmount, itemCount);
        float finalMultiplier = Mathf.Max(minCDR, remainingDurationRatio);
        modifiers.CooldownDurationMultiplier.SetValue(finalMultiplier);
    }
}