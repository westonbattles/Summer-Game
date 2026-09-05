using System;
using UnityEngine;

[Serializable]
public class VialOfBlackBile : ItemBehaviour
{
    [SerializeField] private float chancePerStack = 0.30f;
    [SerializeField] private StatusEffectDefinition corrosion;
    [SerializeField] private ItemDefinition itemDefinition;

    public override ItemRuntime CreateRuntime(Player player)
    {
        return new VialOfBlackBileRuntime(player, this, 1);
    }

    private class VialOfBlackBileRuntime : ItemRuntime
    {
        private readonly Player player;
        private readonly VialOfBlackBile data;
        private readonly Combat playerCombat;
        private readonly ItemInventory inventory;
        private int currentItemCount = 1;

        public VialOfBlackBileRuntime(Player player, VialOfBlackBile data, int initialCount = 1)
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
                    Debug.LogWarning($"[VialOfBlackBile] - Player combat component missing!");
                }

                inventory = player.GetComponent<ItemInventory>();
                if (inventory != null)
                {
                    inventory.OnItemCountChanged += OnItemCountChanged;
                }
                else
                {
                    Debug.LogWarning($"[VileOfBlackBile] - Player inventory component missing!");
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
            Debug.Log($"[VialOfBlackBile] OnAfterDamageDealt fired on target: {hit.Target}");
            if (hit.Target == null || data.corrosion == null)
            {
                Debug.LogWarning("[VialOfBlackBile] Null target or missing status effect reference!");
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
                    Debug.Log($"[VialOfBlackBile] Applying {data.corrosion.name} to {targetComponent.name}");
                    statusController.Apply(data.corrosion, player.gameObject);
                }
                else
                {
                    Debug.LogWarning($"[VialOfBlackBile] Target {hit.Target} lacks a StatusEffectController.");
                }
            }
        }

        public override void Dispose()
        {
            if (playerCombat != null)
            {
                playerCombat.AfterDamageDealt -= OnAfterDamageDealt;
            }
            if (inventory != null)
            {
                inventory.OnItemCountChanged -= OnItemCountChanged;
            }
            base.Dispose();
        }
    }
}