using System;
using UnityEngine;

[Serializable]
public class ProteinBar : ItemBehaviour
{
    [SerializeField] private float maxHealthBonusPerStack = 0.1f; // 10% per stack

    public override void ApplyModifiers(PlayerModifiers modifiers, int itemCount)
    {
        // Additive percentage scaling (e.g., 1 stack = +10%, 2 stacks = +20%)
        modifiers.MaxHealthBonus.AddValue(maxHealthBonusPerStack * itemCount);
    }

    public override ItemRuntime CreateRuntime(Player player)
    {
        return new ProteinBarRuntime(player);
    }

    class ProteinBarRuntime : ItemRuntime
    {
        private readonly Health _playerHealth;

        public ProteinBarRuntime(Player player)
        {
            if (player != null)
            {
                _playerHealth = player.GetComponent<Health>();
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