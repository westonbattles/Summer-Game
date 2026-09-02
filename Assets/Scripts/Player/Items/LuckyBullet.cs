using UnityEngine;
using System;

[Serializable]
public class LuckyBullet : ItemBehaviour
{
    [SerializeField] private float damageMultiplier = 2f;
    public override void ApplyModifiers(PlayerModifiers modifiers, int itemCount)
    {
        modifiers.RevolverLastBulletDamageMultiplier.MultiplyValue(damageMultiplier * itemCount); 
    }
}
