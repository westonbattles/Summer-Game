using System;

[Serializable]
public class TitaniumKnuckles : ItemBehaviour
{
    public override void ApplyModifiers(PlayerModifiers modifiers, int itemCount)
    {
        modifiers.BrassKnucklesComboIncreaseDamageMultiplier.AddValue(1);             // first item        -> add the combo damage multiplier
        modifiers.BrassKnucklesBaseDamageMultiplier.AddValue(0.5f * (itemCount - 1)); // consecutive items -> just increase the base damage multiplier instead
    }
}