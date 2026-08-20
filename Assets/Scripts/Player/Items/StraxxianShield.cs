using UnityEngine;
using System;
using NaughtyAttributes;

// N stacks of damage reduction: (1 - reduction)^N damage
[Serializable]
public class StraxxianShield : ItemBehaviour
{
    [InfoBox("This cannot be 0! It will cause a divide by zero error in the damage calculation.", EInfoBoxType.Warning)]
    [SerializeField] private float ReductionPerStack = 0.02f;

    public override void ApplyModifiers(PlayerModifiers modifiers, int itemCount)
    {
        float damageRetentionRatio = Mathf.Pow(1f - ReductionPerStack, itemCount); // .98 -> .88 -> .86 -> .85
        modifiers.DamageTakenMultiplier.MultiplyValue(damageRetentionRatio);
    }
}

/*
Inventory:
100 Straxxian Sheilds
- DamageTakenMultipler = 0.1326


Item
- Reduces DamageTakenMultipler by .2




*/