using System;
using UnityEngine;

[Serializable]
public class NewbieGains : ItemBehaviour
{
    [SerializeField] private float firstStackMult = 1.5f;
    [SerializeField] private float secondStackMult = .25f;

    public override void ApplyModifiers(PlayerModifiers modifiers, int itemCount)
    {
        modifiers.DamageAfterSwap.MultiplyValue(firstStackMult + (secondStackMult * (itemCount-1)));
        Debug.Log("Newbie Gains modifier set");
    }

    public override ItemRuntime CreateRuntime(Player player)
    {
        return new NewbieGainsRuntime(player);
    }

    private class NewbieGainsRuntime : ItemRuntime
    {
        public NewbieGainsRuntime(Player player)
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