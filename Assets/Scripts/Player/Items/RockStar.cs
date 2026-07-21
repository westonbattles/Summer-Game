using System;
using UnityEngine;

[Serializable]
public class RockStar : ItemBehaviour
{
    public override void ApplyModifiers(PlayerModifiers modifiers, int itemCount)
    {
        modifiers.DamageMultiplier.SetValue(0.1f * itemCount);
    }
}
