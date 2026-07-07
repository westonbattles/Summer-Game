using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

enum GPState
{
    Inactive,
    Windup,
    Falling
}

[Serializable]
public class GroundPound : AbilityBehaviour
{
    [Header("Properties")] 
    [SerializeField] float baseDamage = 50f;
    [SerializeField] float baseCritChance = 0.1f;
    [SerializeField] float baseCooldownTime = 4f;
    [SerializeField] float baseDownwardSpeed = 13f;

    [Header("Windup")]
    [SerializeField] float windupTime = 0.2f;
    [SerializeField] float windupSpeed = 10f;

    [Header("Target Gathering")]
    [SerializeField] LayerMask hitLayers = Enemy.LayerMask | LayerMask.GetMask("Hittable");
    [SerializeField] Vector3 sphereCastPositionOffset;
    [SerializeField] float sphereCastRadius = 0f;
    
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
        public readonly Player Player;
        public GPState State = GPState.Inactive;

        //public bool Active => State != GPState.Inactive;    // if we are actively doing a ground pound
        public float CooldownRemaining;                     // how much time left on the ability cooldown
        public float WindupTimer;                           // when to transition from initial upward boost to downward groundpound
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

    void StartWindup(GroundPoundState groundPound)
    {
        groundPound.State = GPState.Windup;
        groundPound.WindupTimer = windupTime;
        groundPound.Player.GroundPoundDash(Vector3.up, windupSpeed);
    }

    void EndWindup(GroundPoundState groundPound)
    {
        groundPound.State = GPState.Falling;
        groundPound.Player.GroundPoundDash(Vector3.down, DownwardSpeed);
    }

    /// Pound The Ground
    void Pound(GroundPoundState groundPound, float deltaTime)
    {
        // todo: do some pounding

        Vector3 position = groundPound.Player.transform.position + sphereCastPositionOffset;
        Collider[] hits = new Collider[32];
        //Physics.OverlapSphereNonAlloc(position, sphereCastRadius, hits, hitLayers);
        
        Debug.Log("Pounded");
        
        groundPound.State = GPState.Inactive;
    }
    
    public override void Tick(AbilityState state, float deltaTime)
    {
        GroundPoundState groundPound = state as GroundPoundState;
        if (groundPound == null) return;

        switch (groundPound.State)
        {
            case GPState.Windup:
                if (groundPound.WindupTimer == 0f) EndWindup(groundPound);
                else groundPound.WindupTimer = Mathf.Max(0f, groundPound.WindupTimer - deltaTime);
                break;
            case GPState.Falling:
                if (groundPound.Player.IsGrounded) Pound(groundPound, deltaTime);
                break;
        }
        
        
        // update ability cooldown
        groundPound.CooldownRemaining = Mathf.Max(0f, groundPound.CooldownRemaining - deltaTime);
    }

    public override bool TryExecute(AbilityState state)
    {
        
        // make sure we actually can/should execute the pound
        GroundPoundState groundPound = state as GroundPoundState;
        if (groundPound == null) return false;
        if (groundPound.CooldownRemaining > 0f) return false;
        if (groundPound.State != GPState.Inactive) return false; // alr pounding
  
        
        // succesful execution
        StartWindup(groundPound);
        
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

        groundPound.State = GPState.Inactive; // make sure we don't do a delayed hit after cancel
    }
    
    
    // gizmo debug for aoe range
    public void DrawGizmos(Transform playerTransform)
    {
        
    }
}