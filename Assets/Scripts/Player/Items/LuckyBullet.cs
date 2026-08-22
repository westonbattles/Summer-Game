using Unity;
using System;

[Serializable]
public class LuckyBullet : ItemBehaviour
{
    public override void ApplyModifiers(PlayerModifiers modifiers, int itemCount)
    {
        modifiers.RevolverLastBulletDamageMultiplier.MultiplyValue(1f * itemCount); 
    }
}
