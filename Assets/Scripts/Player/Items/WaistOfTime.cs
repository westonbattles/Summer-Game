using System;
using UnityEngine;

[Serializable]
public class WaistOfTime : ItemBehaviour
{
    [SerializeField] private float CDRAmount = 15f;

    public override void ApplyModifiers(PlayerModifiers modifiers, int itemCount)
    {
        modifiers.CooldownDurationMultiplier.AddValue(-CDRAmount * itemCount);
    }
}