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
    [SerializeField] float baseDamage = 40f;
    [SerializeField] float baseCooldownTime = 4f;
    [SerializeField] float baseDamageScalingFactor = 0.5f; // y = 1/2(x+1)
    [SerializeField] float baseDownwardSpeed = 13f;

    [Header("Windup")]
    [SerializeField] float windupTime = 0.2f;
    [SerializeField] float windupSpeed = 10f;

    [Header("Target Gathering")]
    [SerializeField] Vector3 sphereCastPositionOffset;
    [SerializeField] float sphereCastRadius = 3.3f;
    [SerializeField] bool showGizmo = true; 
    
    LayerMask HitLayers => Enemy.LayerMask | LayerMask.GetMask("Hittable");
    
    // MODIFIERS
    // to use a stat modifier, simply use it here! like so
    // public ModifiableStat => baseStat | could be turned into the following:
    // public ModifiableStat => baseStat + Player.Instance.Modifiers.StatModifierAddName
    // public ModifiableStat => baseStat * Player.Instance.Modifiers.StatModifierMultiplierName
    // (or any other way u want to use the modifier)
    public float Damage => baseDamage;
    public float DamageRadius => sphereCastRadius + Player.Instance.Modifiers.GroundPoundRadiusBonus;
    public float DamageScalingFactor => baseDamageScalingFactor * Player.Instance.Modifiers.GroundPoundDamageScalingMultiplier;
    public float CooldownTime => baseCooldownTime;
    public float DownwardSpeed => baseDownwardSpeed;
    
    class GroundPoundState : AbilityState
    {
        public readonly Player Player;
        public GPState State = GPState.Inactive;

        public float MaxHeight; // maximum height we have reached in the current pound (for damage multiplier calculation)
        public float CooldownRemaining;          // how much time left on the ability cooldown
        public float WindupTimer;                // when to transition from initial upward boost to downward groundpound
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

    // height is distance from max height during gronudpound to landing height
    float DamageScalingFunction(float height)
    {
        return Damage * Mathf.Max(1f, DamageScalingFactor * (height + 1)); // max(1, 1/2[x+1])
        // could do a logarithmic scaling instead?
    }
    
    void StartWindup(GroundPoundState groundPound)
    {
        groundPound.State = GPState.Windup;
        groundPound.MaxHeight = float.MinValue;
        groundPound.WindupTimer = windupTime;
        groundPound.Player.GroundPoundDash(Vector3.up, windupSpeed); // dash up for a sec
    }

    void EndWindup(GroundPoundState groundPound)
    {
        groundPound.Player.IgnoreEnemiesForCollision = true;
        groundPound.State = GPState.Falling;
        groundPound.Player.GroundPoundDash(Vector3.down, DownwardSpeed);
    }

    /// Pound The Ground
    void Pound(GroundPoundState groundPound, float deltaTime)
    {
        groundPound.Player.IgnoreEnemiesForCollision = false;
        
        Vector3 position = groundPound.Player.transform.position + sphereCastPositionOffset;
        Collider[] hits = new Collider[64];
        int hitCount = Physics.OverlapSphereNonAlloc(position, sphereCastRadius, hits, HitLayers);
        if (hitCount > 0)
        {
            float distanceFallen = groundPound.MaxHeight - groundPound.Player.transform.position.y;
            float scaledDamage = DamageScalingFunction(distanceFallen);
            
            Debug.Log($"Pounded, Distance Fallen: {distanceFallen}, Damage dealt: {scaledDamage} ({Damage} * {Mathf.Max(1f, DamageScalingFactor * (distanceFallen + 1))})");
            
            // build damageInfo
            DamageInfo damageInfo = new DamageInfo(scaledDamage, groundPound.Player.gameObject, false);
            
            // de-dupe
            groundPound.DamagedTargets.Clear();
            for (int i = 0; i < hitCount; i++)
            {
                IDamageable target = hits[i].GetComponentInParent<IDamageable>();
                if (target != null && groundPound.DamagedTargets.Add(target)) target.TakeDamage(damageInfo);
            }
        }
        
        // todo: vfx circle thing (lowkey wanna take as tab at this - Weston)
        
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
        
        // update max height 
        if (groundPound.State != GPState.Inactive) groundPound.MaxHeight = Mathf.Max(groundPound.MaxHeight, groundPound.Player.transform.position.y);
        
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

        groundPound.Player.IgnoreEnemiesForCollision = false;
        groundPound.State = GPState.Inactive; // make sure we don't do a delayed hit after cancel
    }
    
    
    // gizmo debug for aoe range
    public void DrawGizmos(Transform playerTransform)
    {
        if (!showGizmo) return;

        Gizmos.color = Color.limeGreen;
        Gizmos.DrawWireSphere(playerTransform.position + sphereCastPositionOffset, DamageRadius);
    }
}