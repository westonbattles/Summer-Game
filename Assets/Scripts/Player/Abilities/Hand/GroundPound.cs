using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;


[Serializable]
public class GroundPound : AbilityBehaviour
{
    [Header("Properties")] 
    [SerializeField] float baseDamage = 50f;
    [SerializeField] float baseCooldownTime = 4f;
    [SerializeField] float baseDownwardSpeed = 13f;
    
    
    // MODIFIERS
    // to use a stat modifier, simply use it here! like so
    // public ModifiableStat => baseStat | could be turned into the following:
    // public ModifiableStat => baseStat + Player.Instance.Modifiers.StatModifierAddName
    // public ModifiableStat => baseStat * Player.Instance.Modifiers.StatModifierMultiplierName
    // (or any other way u want to use the modifier)
    public float Damage => baseDamage;
    public float CooldownTime => baseCooldownTime;
    public float DownwardSpeed => baseDownwardSpeed;
    
    class GroundPoundState : AbilityState
    {
        public Player Player;

        public bool Active;                     // if we are actively falling thru the air about to do a ground pound
        public float CooldownRemaining;         // how much time left on the ability cooldown
        public HashSet<IDamageable> DamagedTargets = new();
        

        public GroundPoundState(Player player)
        {
            Player = player;
        }
    }

    public override AbilityState CreateState()
    {
        Player player = Player.Instance;
        GroundPoundState state = new GroundPoundState(player);
        return state;
    }

    public override void Tick(AbilityState state, float deltaTime)
    {
        GroundPoundState groundPound = state as GroundPoundState;
        if (groundPound == null) return;

        if (groundPound.Active)
        {
            if (groundPound.Player.IsGrounded)
            {
                groundPound.Active = false;
                Debug.Log("Pounded!");
            }
        }
        
        // update cooldown
        groundPound.CooldownRemaining = Mathf.Max(0f, groundPound.CooldownRemaining - deltaTime);
    }

    public override bool TryExecute(AbilityState state)
    {
        
        // make sure we actually can/should execute the pound
        GroundPoundState groundPound = state as GroundPoundState;
        if (groundPound == null) return false;
        if (groundPound.CooldownRemaining > 0f) return false;
        if (groundPound.Active) return false; // alr pounding
  
       
        
        // succesful execution

        groundPound.Active = true;

        // if grounded -> slight upward momentum
        if (groundPound.Player.IsGrounded)
        {
            
        }
        // else -> downward momentum
        else
        {
            groundPound.Player.GroundPound(DownwardSpeed);
        }
        

        // swing vfx
        
        groundPound.CooldownRemaining = CooldownTime; // start cooldown countdown
        return true;
    }

    public override float GetCooldownFraction(AbilityState state)
    {
        GroundPoundState groundPound = state as GroundPoundState;
        if (groundPound == null) return 0f;

        return groundPound.CooldownRemaining / CooldownTime;
    }

    public override void Cancel(AbilityState state)
    {
        GroundPoundState groundPound = state as GroundPoundState;
        if (groundPound == null) return;

        groundPound.Active = false; // make sure we don't do a delayed hit after cancel
    }
    
    
    // gizmo debug for aoe range
    public void DrawGizmos(Transform playerTransform)
    {
        
    }
}