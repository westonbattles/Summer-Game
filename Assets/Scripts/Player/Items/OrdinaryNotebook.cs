using System;
using UnityEngine;

[Serializable]
public class OrdinaryNotebook : ItemBehaviour
{
    [SerializeField] private float overkillHealPercent = 0.1f;


    public override void ApplyModifiers(PlayerModifiers modifiers, int itemCount)
    {
        
    }

    public override ItemRuntime CreateRuntime(Player player)
    {
        return new OrdinaryNotebookRuntime(player, overkillHealPercent);
    }

    private class OrdinaryNotebookRuntime : ItemRuntime
    {
        private readonly Player player;
        private readonly float healPercent;

        public OrdinaryNotebookRuntime(Player player, float healPercent)
        {
            this.player = player;
            this.healPercent = healPercent;
            Health.OnOverkillDamage += HandleOverkill;
        }

        private void HandleOverkill(GameObject source, float overkillAmount)
        {
            if (source != player.gameObject) return; //only player kills

            float healAmount = overkillAmount * healPercent * Count;
            player.GetComponent<PlayerHealth>().Heal(healAmount);
        }

        public override void Tick(float deltaTime)
        {
            
        }

        public override void Dispose()
        {
            Health.OnOverkillDamage -= HandleOverkill;
        }
    }
}