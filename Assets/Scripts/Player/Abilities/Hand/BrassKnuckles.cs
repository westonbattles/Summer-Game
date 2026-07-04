using System;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public class BrassKnuckles : AbilityBehaviour
{
    [Header("Properties")] 
    [SerializeField] public float baseDamage = 10f;
    [Tooltip("Percentage that damage scales with each subsequent combo state")]
    [SerializeField] public float baseComboIncrease = 0.25f;
    [SerializeField] public int baseComboStages = 3;
    [SerializeField] public float baseCooldownTime = 0.65f;
    [SerializeField] public float baseComboResetTime = 2.5f;
    
    // Modifiers
    public int MaxCombo => baseComboStages + Player.Instance.Modifiers.BrassKnucklesExtraComboNumber;
    
    // todo: list of animations for each state?
    
    // https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Physics.OverlapBoxNonAlloc.html properties

    
    class BrassKnucklesState : AbilityState
    {
        public Player Player;
        public PlayerAim PlayerAim;

        public int ComboState = 0;
        // public bool ComboActive => ComboState != ComboStage.First;
        public float CooldownRemaining;         // how much time left on the ability cooldown
        public float ComboResetTimeRemaining;   // how much time left on the combo reset timer

        public BrassKnucklesState(Player player)
        {
            Player = player;
            PlayerAim = player.Aim;
        }
    }

    public override AbilityState CreateState()
    {
        Player player = Player.Instance;
        BrassKnucklesState state = new BrassKnucklesState(player);
        return state;
    }

    public override void Tick(AbilityState state, float deltaTime)
    {
        BrassKnucklesState brassKnuckles = state as BrassKnucklesState;
        if (brassKnuckles == null) return;
        
        // todo: state management
        
        
        brassKnuckles.CooldownRemaining = Mathf.Max(0f, brassKnuckles.CooldownRemaining- deltaTime);
    }

    public override bool TryExecute(AbilityState state)
    {
        // make sure we actually can/should execute
        BrassKnucklesState brassKnuckles = state as BrassKnucklesState;
        if (brassKnuckles == null) return false;
        if (brassKnuckles.CooldownRemaining > 0f) return false;
  
        Debug.Log($"Combo State: {brassKnuckles.ComboState}");

        
        // succesful execution
        
        // boxcast
        
        // update combo state
        if (brassKnuckles.ComboState < MaxCombo) brassKnuckles.ComboState += 1;
        
        brassKnuckles.CooldownRemaining = baseCooldownTime; // start cooldown countdown
        return true;
    }

    public override float GetCooldownFraction(AbilityState state)
    {
        BrassKnucklesState brassKnuckles = state as BrassKnucklesState;
        if (brassKnuckles == null) return 0f;
        
        return brassKnuckles.CooldownRemaining / baseCooldownTime;
    }
    // no cancel since carrying momentum after character swap is kinda cool
    /*public override void Cancel(AbilityState state)
    {
    }*/
}