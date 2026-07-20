using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public class Whirlwind : AbilityBehaviour
{
    [Header("Properties")] 
    [SerializeField] float baseDamage = 10f;
    [SerializeField] float baseCooldownTime = 4f;
    [SerializeField] DamageType baseDamageType = DamageType.Physical;

    [Header("Spin/Animation")]
    [SerializeField, Header("delay for applying the tick of damage")] float spinDamageDelay = 0.5f;
    //[SerializeField] float spinTime = 0.5f; // how long to play the animation for

    [Header("Target Gathering")]
    [SerializeField] float overlapSphereRadius = 3.3f;
    [Tooltip("max buffer size for the overlap sphere aplying damage. more colliders than buffer allows for means some enemies won't be damaged")]
    [SerializeField] uint overlapSphereMaxColliders = 128;
    [SerializeField] bool showGizmo = true;

    //[Header("VFX")]
    //[SerializeField] PooledVfx whirlwindSlashVFX;
    
    LayerMask HitLayers => Enemy.LayerMask | LayerMask.GetMask("Hittable");
    
    // MODIFIERS
    // to use a stat modifier, simply use it here! like so
    // public ModifiableStat => baseStat | could be turned into the following:
    // public ModifiableStat => baseStat + Player.Instance.Modifiers.StatModifierAddName
    // public ModifiableStat => baseStat * Player.Instance.Modifiers.StatModifierMultiplierName
    // (or any other way u want to use the modifier)
    public float Damage => baseDamage;
    public DamageType DamageType => baseDamageType;
    public float DamageRadius => overlapSphereRadius + Player.Instance.Modifiers.WhirlwindRadiusBonus.GetValue();
    public float CooldownTime => baseCooldownTime;
    
    // damage function
    float _sixOverLn2 = 6f / Mathf.Log(2);
    /// in: (enemy count)
    /// out: damage dealt to all enemies
    float DamageFunction(int enemyCount) => Damage + _sixOverLn2 * Mathf.Log(enemyCount);
    
    class WhirlwindState : AbilityState
    {
        public readonly Player Player;
        public HashSet<IDamageable> DamagedTargets = new();

        public bool HitPending;
        public float SpinTimerRemaining;         // how much time left until we should tick spin damage
        public float CooldownRemaining;          // how much time left on the ability cooldown
        

        public WhirlwindState(Player player)
        {
            Player = player;
        }
    }

    public override AbilityState CreateState()
    {
        Player player = Player.Instance;
        WhirlwindState state = new WhirlwindState(player);
        return state;
    }
    
    /// Apply whirlwind damage
    void ApplyDamage(WhirlwindState whirlwind)
    {
        
        Vector3 position = whirlwind.Player.transform.position;
        Collider[] hits = new Collider[overlapSphereMaxColliders];
        int hitCount = Physics.OverlapSphereNonAlloc(position, DamageRadius, hits, HitLayers);
        if (hitCount > 0)
        {
            // de-dupe
            whirlwind.DamagedTargets.Clear();
            for (int i = 0; i < hitCount; i++)
            {
                IDamageable target = hits[i].GetComponentInParent<IDamageable>();
                if (target != null) whirlwind.DamagedTargets.Add(target); // dupes won't be added to hashset
            }

            float damage = DamageFunction(whirlwind.DamagedTargets.Count);
            Debug.Log($"Whirlwind hit {whirlwind.DamagedTargets.Count} enemies! ({damage} damage per)");
            
            // build damageInfo
            DamageInfo damageInfo = new DamageInfo(damage, DamageType, whirlwind.Player.gameObject);
            foreach (IDamageable target in whirlwind.DamagedTargets)
            {
                target.TakeDamage(damageInfo);
            }
            
        }
       
        // todo: vfx
        // PooledVfx vfx = VFXPoolManager.Instance.Play(whirlwindSlashVFX, position, Quaternion.identity);
        // if (vfx != null)
        // {
        //     float increaseRatio = DamageRadius / overlapSphereRadius;
        //     vfx.transform.localScale *= increaseRatio; // gets reset internally after being freed \dont gotta worry abt that
        // }
    }
    
    
    
    public override void Tick(AbilityState state, float deltaTime)
    {
        WhirlwindState whirlwind = state as WhirlwindState;
        if (whirlwind == null) return;


        if (whirlwind.HitPending && whirlwind.SpinTimerRemaining == 0f)
        {
            ApplyDamage(whirlwind);
            whirlwind.HitPending = false;
        }
        
            
        whirlwind.SpinTimerRemaining = Mathf.Max(0f, whirlwind.SpinTimerRemaining - deltaTime); // update spin damage offset timer
        whirlwind.CooldownRemaining = Mathf.Max(0f, whirlwind.CooldownRemaining - deltaTime);   // update ability cooldown
    }

    public override bool TryExecute(AbilityState state)
    {
        // make sure we actually can/should execute the whirlwind
        WhirlwindState whirlwind = state as WhirlwindState;
        if (whirlwind == null) return false;
        if (whirlwind.CooldownRemaining > 0f) return false;
  
        
        // succesful execution
        whirlwind.HitPending = true;                    // tell tick we need to apply a hit
        whirlwind.SpinTimerRemaining = spinDamageDelay; // start spin damage tick cooldown
        whirlwind.CooldownRemaining = CooldownTime;     // start cooldown countdown
        return true;
    }

    public override float GetCooldownFraction(AbilityState state)
    {
        WhirlwindState whirlwind = state as WhirlwindState;
        if (whirlwind == null) return 0f;

        return whirlwind.CooldownRemaining / CooldownTime;
    }

    public override void Cancel(AbilityState state)
    {
        WhirlwindState whirlwind = state as WhirlwindState;
        if (whirlwind == null) return;

        // stop any pending hits from happening
        whirlwind.HitPending = false;
        whirlwind.SpinTimerRemaining = 0f;
    }
    
    
    // gizmo debug for aoe range
    public void DrawGizmos(Transform playerTransform)
    {
        if (!showGizmo) return;

        Gizmos.color = Color.limeGreen;
        Gizmos.DrawWireSphere(playerTransform.position, DamageRadius);
    }
}