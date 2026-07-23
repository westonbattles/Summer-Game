using System;
using UnityEngine;

[Serializable]
public class ExampleItem : ItemBehaviour
{
    // modifier example
    [SerializeField, Min(0f)] float damageMultiplierBonusPerItem = 0.1f;

    // runtime examples
    [SerializeField, Min(0f)] float healingPerStack = 1f;
    [SerializeField, Min(0.01f)] float healingInterval = 5f;

    public override void ApplyModifiers(PlayerModifiers modifiers, int itemCount)
    {
        // applyModifiers is called whenever the inventory recalculates its
        // modifiers, so this line adds the configured damage bonus for every item in our inventory (itemCount is the amount in our inventory)
        modifiers.DamageMultiplier.AddValue(damageMultiplierBonusPerItem * itemCount);
        
        // this function allows us to do custom stat scaling based on itemCount (like how in ror2, some items have different changes when u get more than 1
    }

    public override ItemRuntime CreateRuntime(Player player)
    {
        // createRuntime is called the first time this item enters an inventory.
        // it creates one state-d runtime instance for this item and player.
        return new ExampleItemRuntime(player, healingPerStack, healingInterval);
    }

    class ExampleItemRuntime : ItemRuntime
    {
        /*
           ItemRuntime gives us access to two member functions

           public int Count { get; private set; } // count of the item in our inventory
           public void SetCount(int count) { Count = count; }

           so if we need access to the item count during runtime functionality (anything runtime behaviour that scales on count, like auto-healing)
           we just use Count for it.
         */
        
        float healingPerStack;
        float healingInterval;

        Health playerHealth;
        float healingTimer;
        
        public ExampleItemRuntime(Player player, float healingPerStack, float healingInterval)
        {
            // The constructor receives the player and copies the definition's
            // settings so this runtime can maintain its own per-player state
            this.healingPerStack = healingPerStack;
            this.healingInterval = healingInterval;
        }

        public override void Tick(float deltaTime)
        {
            // tick is called once per frame by ItemInventory. count is updated
            // by ItemInventory, so the healing automatically scales per stack with this code (since we have access to Count)
            if (playerHealth == null || Count <= 0)  return;

            healingTimer += deltaTime;
            if (healingTimer < healingInterval) return;

            healingTimer -= healingInterval;
            playerHealth.Heal(healingPerStack * Count);
        }

        public override void Dispose()
        {
            // dispose is called when the inventory is destroyed. unsubscribing
            // prevents code in the rest of the codebase from keeping access to this runtime instance
            
            // so if we subscribed to an action, this is where we would unsubscribe for example
        }
    }
}
