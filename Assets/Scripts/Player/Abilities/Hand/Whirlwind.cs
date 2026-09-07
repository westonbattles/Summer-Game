using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Whirlwind : AbilityBehaviour
{
    [Header("Properties")] 
    [SerializeField, Min(0f)] float baseDamage = 10f;
    [SerializeField, Min(0f)] float baseCooldownTime = 4f;
    [SerializeField] DamageType baseDamageType = DamageType.Physical;
    [SerializeField] float useTime = 0f;
    [SerializeField] float useCooldown = 1f;

    [Header("Spin/Animation")]
    [SerializeField, Tooltip("delay for applying the tick of damage")] 
    float spinDamageDelay = 0.5f;
    //[SerializeField] float spinTime = 0.5f; // how long to play the animation for

    [Header("Target Gathering")]
    [SerializeField, Min(0f)] float overlapSphereRadius = 3.3f;
    [Tooltip("max buffer size for the overlap sphere aplying damage. more colliders than buffer allows for means some enemies won't be damaged")]
    [SerializeField, Min(1)] uint overlapSphereMaxColliders = 128;
    [SerializeField] bool showGizmo = true;

    [Header("VFX")]
    [SerializeField] GameObject whirlwindSlashVFX;
    [SerializeField, Min(0f)] float vfxDuration = 1.0f;
    [SerializeField] float vfxHeightOffset = 2.2f;

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
    public float CooldownTime => baseCooldownTime
        - Player.Instance.Modifiers.CooldownDurationMultiplier.GetValue();
    
    // damage function
    float _sixOverLn2 = 6f / Mathf.Log(2);
    /// in: (enemy count)
    /// out: damage dealt to all enemies
    float DamageFunction(int enemyCount) => Damage + _sixOverLn2 * Mathf.Log(enemyCount);
    
    class WhirlwindState : AbilityState
    {
        public readonly Player Player;
        public readonly Collider[] HitBuffer;
        public HashSet<IDamageable> DamagedTargets = new();

        public bool HitPending;
        public float SpinTimerRemaining;         // how much time left until we should tick spin damage
        public float CooldownRemaining;          // how much time left on the ability cooldown

        public GameObject ActiveVfxInstance;

        public WhirlwindState(Player player, int bufferSize)
        {
            Player = player;
            HitBuffer = new Collider[bufferSize];
        }
    }

    public override AbilityState CreateState()
    {
        Player player = Player.Instance;
        int bufferSize = (int)overlapSphereMaxColliders;
        WhirlwindState state = new WhirlwindState(player, bufferSize);
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
                whirlwind.Player.Combat.TryDamage(target, damageInfo);
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
        if (state is not WhirlwindState whirlwind) return;

        whirlwind.SpinTimerRemaining = Mathf.Max(0f, whirlwind.SpinTimerRemaining - deltaTime); // update spin damage offset timer
        whirlwind.CooldownRemaining = Mathf.Max(0f, whirlwind.CooldownRemaining - deltaTime);   // update ability cooldown
        useTime = Mathf.Max(0f, useTime - deltaTime);

        if (whirlwind.HitPending && whirlwind.SpinTimerRemaining == 0f)
        {
            ApplyDamage(whirlwind);
            whirlwind.HitPending = false;
        }
    }

    public override bool TryExecute(AbilityState state)
    {
        // make sure we actually can/should execute the whirlwind
        WhirlwindState whirlwind = state as WhirlwindState;
        if (whirlwind == null) return false;
        if (useTime > 0.01f) return false;
        Debug.Log($"{useTime} is the useTime delay");
        if (whirlwind.CooldownRemaining > 0.1f) // cooldown eval (for QS bullet)
        {
            Debug.Log($"{whirlwind.CooldownRemaining} seconds left of cooldown");
            Player Player = whirlwind.Player;
            if (Player.Modifiers.CooldownDamageReduction.GetValue() != 0f)
            {
                Debug.Log("Quicksilver bullet used");
                // NOTE: This is on all abilities currently, refactoring for a broadcast done in the item runtime would be better but idc about that rn
                float playerHealth = Player.GetComponent<PlayerHealth>().MaxHP;
                DamageInfo selfDamage = new DamageInfo(playerHealth * whirlwind.CooldownRemaining * .01f * Player.Modifiers.CooldownDamageReduction.GetValue(), DamageType.Physical);
                Player.GetComponent<PlayerHealth>().TakeDamage(selfDamage);
            }
            else
            {
                Debug.Log("No Quicksilver Bullet");
                return false;
            }
        }


        // succesful execution
        whirlwind.HitPending = true;                    // tell tick we need to apply a hit
        whirlwind.SpinTimerRemaining = spinDamageDelay; // start spin damage tick cooldown
        whirlwind.CooldownRemaining = CooldownTime;     // start cooldown countdown
        useTime = useCooldown;

        if (whirlwindSlashVFX != null)
        {
            if (whirlwind.ActiveVfxInstance != null) UnityEngine.Object.Destroy(whirlwind.ActiveVfxInstance);

            // 1. Instantiate with prefab's native rotation
            whirlwind.ActiveVfxInstance = UnityEngine.Object.Instantiate(
                whirlwindSlashVFX,
                whirlwind.Player.transform.position,
                whirlwindSlashVFX.transform.rotation
            );

            // 2. Parent to player (worldPositionStays: false lets us set local transforms directly)
            whirlwind.ActiveVfxInstance.transform.SetParent(whirlwind.Player.transform, false);

            // 3. Set local offset (e.g., center height around 1.0f - 1.2f up from feet)
            whirlwind.ActiveVfxInstance.transform.localPosition = new Vector3(0f, 2.2f, 0f);

            // 4. Scale VFX dynamically
            float scaleRatio = DamageRadius / overlapSphereRadius;
            whirlwind.ActiveVfxInstance.transform.localScale *= scaleRatio;

            // 5. Cleanup
            UnityEngine.Object.Destroy(whirlwind.ActiveVfxInstance, vfxDuration);
        }

        return true;
    }

    public override float GetCooldownFraction(AbilityState state) =>
        state is WhirlwindState w && CooldownTime > 0f
            ? Mathf.Clamp01(w.CooldownRemaining / CooldownTime)
            : 0f;

    public override void Cancel(AbilityState state)
    {
        WhirlwindState whirlwind = state as WhirlwindState;
        if (whirlwind == null) return;

        // stop any pending hits from happening
        whirlwind.HitPending = false;
        whirlwind.SpinTimerRemaining = 0f;

        if (whirlwind.ActiveVfxInstance != null)
        {
            UnityEngine.Object.Destroy(whirlwind.ActiveVfxInstance);
        }
    }
    
    
    // gizmo debug for aoe range
    public void DrawGizmos(Transform playerTransform)
    {
        if (!showGizmo) return;

        Gizmos.color = Color.limeGreen;
        Gizmos.DrawWireSphere(playerTransform.position, DamageRadius);
    }
}