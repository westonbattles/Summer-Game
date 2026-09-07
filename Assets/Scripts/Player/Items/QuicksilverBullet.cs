using System;
using UnityEngine;

[Serializable]
public class QuicksilverBullet : ItemBehaviour
{
    [SerializeField] private double damageReductionRemainder = .6f;

    public override void ApplyModifiers(PlayerModifiers modifiers, int itemCount)
    {
        float damageReduced = (float)Math.Pow(damageReductionRemainder, (itemCount - 1));
        Debug.Log(damageReduced);
        modifiers.CooldownDamageReduction.AddValue(damageReduced);
    }

    public override ItemRuntime CreateRuntime(Player player)
    {
        return new QuicksilverBulletRuntime(player);
    }

    private class QuicksilverBulletRuntime : ItemRuntime
    {
        public QuicksilverBulletRuntime(Player player)
        {
            
        }

        public override void Tick(float deltaTime)
        {
            
        }

        public override void Dispose()
        {
            
        }
    }
}