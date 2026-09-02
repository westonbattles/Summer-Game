using System;
using UnityEngine;

[Serializable]
public class ExtraFinger : ItemBehaviour
{
    [SerializeField] private int comboLengthBonusHits = 1;

    public override void ApplyModifiers(PlayerModifiers modifiers, int itemCount)
    {
        modifiers.BrassKnucklesComboStagesBonus.AddValue(itemCount * comboLengthBonusHits);
    }
}