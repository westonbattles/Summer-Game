using System;
using UnityEngine;

[Serializable]
public class ProteinBar : ItemBehaviour
{
    [SerializeField] private float maxHealthBonusPerStack = 0.1f; // 10% per stack

    public override void ApplyModifiers(PlayerModifiers modifiers, int itemCount)
    {
        float multiplier = 1f + maxHealthBonusPerStack;
        float totalBonus = Mathf.Pow(multiplier, itemCount) - 1f;
        modifiers.MaxHealthBonus.AddValue(totalBonus);
    }

    public override ItemRuntime CreateRuntime(Player player)
    {
        return new ProteinBarRuntime(player);
    }

    class ProteinBarRuntime : ItemRuntime
    {
        private readonly PlayerHealth health;

        public ProteinBarRuntime(Player player)
        {
            if (player != null)
            {
                health = player.GetComponent<PlayerHealth>();
            }
        }

        public override void Tick(float deltaTime)
        {
            // Pure passive stat item — no per-frame tick logic needed
        }

        public override void Dispose()
        {
            // Cleanup references if necessary
        }
    }
}