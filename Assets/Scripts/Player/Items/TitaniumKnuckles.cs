using System;
using UnityEngine;

[Serializable]
public class TitaniumKnuckles : ItemBehaviour
{
    [SerializeField] private float scalar = 2f;
    public override void ApplyModifiers(PlayerModifiers modifiers, int itemCount)
    {
        modifiers.BrassKnucklesComboIncreaseDamageMultiplier.MultiplyValue(scalar * itemCount);
    }
}