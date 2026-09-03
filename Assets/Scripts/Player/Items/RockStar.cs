using System;
using UnityEngine;

[Serializable]
public class RockStar : ItemBehaviour
{
    [SerializeField] private float chancePerStack = 0.15f;
    [SerializeField] private StatusEffectDefinition burningStatusEffect;
    [SerializeField] private ItemDefinition itemDefinition;

    public override void ApplyModifiers(PlayerModifiers modifiers, int itemCount)
    {
        // none
    }

    public override ItemRuntime CreateRuntime(Player player)
    {
        return new RockStarRuntime(player, this, 1);
    }

    private class RockStarRuntime : ItemRuntime
    {
        private readonly Player player;
        private readonly RockStar data;
        private readonly Combat playerCombat;
        private readonly ItemInventory playerInventory;
        private int currentItemCount = 1;

        public RockStarRuntime(Player player, RockStar data, int initialCount = 1)
        {
            this.player = player;
            this.data = data;
            this.currentItemCount = initialCount;

            if (player != null)
            {
                playerCombat = player.GetComponent<Combat>();
                if (playerCombat != null)
                {
                    playerCombat.AfterDamageDealt += OnAfterDamageDealt;
                }
                else
                {
                    Debug.LogWarning($"[RockStar] - Player combat component missing!");
                }

                playerInventory = player.GetComponent<ItemInventory>();
                if (playerInventory != null)
                {
                    playerInventory.OnItemCountChanged += OnItemCountChanged;
                }
                else
                {
                    Debug.LogWarning($"[RockStar] - Player inventory component missing!");
                }
            }
        }

        private void OnItemCountChanged(ItemDefinition def, int newCount)
        {
            if (data.itemDefinition != null && def == data.itemDefinition)
            {
                currentItemCount = newCount;
            }
        }

        private void OnAfterDamageDealt(Hit hit)
        {
            Debug.Log($"[RockStar] OnAfterDamageDealt fired on target: {hit.Target}");
            if (hit.Target == null || data.burningStatusEffect == null)
            {
                Debug.LogWarning("[RockStar] Null target or missing status effect reference!");
                return;
            }

            if (hit.Info.IsProc) return;

            float procChance = data.chancePerStack * currentItemCount;
            if (UnityEngine.Random.value > procChance) return;

            if (hit.Target is Component targetComponent)
            {
                var statusController = targetComponent.GetComponent<StatusEffectController>()
                                     ?? targetComponent.GetComponentInParent<StatusEffectController>();

                if (statusController != null)
                {
                    Debug.Log($"[RockStar] Applying {data.burningStatusEffect.name} to {targetComponent.name}");
                    statusController.Apply(data.burningStatusEffect, player.gameObject);
                }
                else
                {
                    Debug.LogWarning($"[RockStar] Target {hit.Target} lacks a StatusEffectController.");
                }
            }
        }

        public override void Dispose()
        {
            if (playerCombat != null)
            {
                playerCombat.AfterDamageDealt -= OnAfterDamageDealt;
            }
            if (playerInventory != null)
            {
                playerInventory.OnItemCountChanged -= OnItemCountChanged;
            }

            base.Dispose();
        }
    }
}

