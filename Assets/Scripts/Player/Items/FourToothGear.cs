using System;
using UnityEngine;

[Serializable]
public class FourToothGear : ItemBehaviour
{
    [SerializeField] private float attackSpeedMult = .2f;

    public override void ApplyModifiers(PlayerModifiers modifiers, int itemCount)
    {
        modifiers.AttackSpeedMultiplier.MultiplyValue(1 + attackSpeedMult * itemCount);
    }

    public override ItemRuntime CreateRuntime(Player player)
    {
        return new FourToothGearRuntime(player);
    }

    private class FourToothGearRuntime : ItemRuntime
    {
        public FourToothGearRuntime(Player player)
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