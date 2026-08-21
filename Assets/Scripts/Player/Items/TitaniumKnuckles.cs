using System;

[Serializable]
public class TitaniumKnuckles : ItemBehaviour
{
    public override void ApplyModifiers(PlayerModifiers modifiers, int itemCount)
    {
        modifiers.BrassKnucklesComboIncreaseDamageMultiplier.MultiplyValue(1 + itemCount);
    }
}