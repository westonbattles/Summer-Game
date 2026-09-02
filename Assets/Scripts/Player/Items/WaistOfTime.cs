using System;
using UnityEngine;

[Serializable]
public class WaistOfTime : ItemBehaviour
{
    [SerializeField] private float CDRAmount = 1f;

    public override void ApplyModifiers(PlayerModifiers modifiers, int itemCount)
    {
        modifiers.CooldownDurationMultiplier.AddValue(-CDRAmount * itemCount);
    }

    public override ItemRuntime CreateRuntime(Player player)
    {
        return new WaistOfTimeRuntime(player);
    }

    private class WaistOfTimeRuntime : ItemRuntime
    {
        public WaistOfTimeRuntime(Player player)
        {
            // none
        }

        public override void Tick(float deltaTime)
        {
            // none
        }

        public override void Dispose()
        {
            // none
        }
    }
}