using System;
using UnityEngine;

enum SwingStage
{
    NotSwinging,
    First,
    Second,
    Third
}

[Serializable]
public class BrassKnuckles : AbilityBehaviour
{
    [Header("Properties")] 
    [SerializeField] public float baseDamage = 10f;
    [SerializeField] public float cooldownTime = 2.3f;
    
    // todo: list of animations for each state?
    
    
    // https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Physics.OverlapBoxNonAlloc.html properties

    
    class BrassKnucklesState : AbilityState
    {
        public Player Player;
        public PlayerAim PlayerAim;

        public SwingStage SwingState = SwingStage.NotSwinging;
        public bool IsSwinging => SwingState == SwingStage.NotSwinging;
        public float CooldownRemaining;         // how much time left on the ability cooldown

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
        if (brassKnuckles.IsSwinging) return false;
        if (brassKnuckles.CooldownRemaining > 0f) return false;
  

        

        brassKnuckles.CooldownRemaining = cooldownTime; // start cooldown countdown
        return true;
    }

    public override float GetCooldownFraction(AbilityState state)
    {
        BrassKnucklesState brassKnuckles = state as BrassKnucklesState;
        // todo
    }
    // no cancel since carrying momentum after character swap is kinda cool
    /*public override void Cancel(AbilityState state)
    {
    }*/
}