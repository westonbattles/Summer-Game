using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

enum PipeState
{
    Inactive,
    Windup,
    Active
}

[Serializable]
public class ThinkingPipe : AbilityBehaviour
{
    [Header("Properties")]
    [SerializeField] float baseDamagePerSecond = 20f;
    [SerializeField] float baseCooldownTime = 12f;
    [SerializeField] float smokeDuration = 6f;
    //[SerializeField] float smokeHeight = 5f; // unused atm, may change the cast to utilize this for more of a flat smoke sheet kind of deal
    [SerializeField] float smokeMaxRadius = 10f; // thickness of the capsule cast (0 would be pure hitscan raycast at cursor)
    [SerializeField] DamageType baseDamageType = DamageType.Physical;
    [SerializeField] float useTime = 0f;
    [SerializeField] float useCooldown = 1f;

    [Header("Windup")]
    [SerializeField] float windupTime = 0.2f;
    [SerializeField] float expandDuration = 0.75f;

    [Header("Target Gathering")]
    [SerializeField] Vector3 castPositionOffset;
    [SerializeField] bool showGizmo = true;

    [Header("VFX")]
    [SerializeField] PooledVfx smokeCloudVfx;
    [SerializeField] float vfxBaseRadius = 1f;

    LayerMask HitLayers => Enemy.LayerMask | LayerMask.GetMask("Hittable");

    // MODIFIERS
    // to use a stat modifier, simply use it here! like so
    // public ModifiableStat => baseStat | could be turned into the following:
    // public ModifiableStat => baseStat + Player.Instance.Modifiers.StatModifierAddName
    // public ModifiableStat => baseStat * Player.Instance.Modifiers.StatModifierMultiplierName
    // (or any other way u want to use the modifier)
    public float Damage => baseDamagePerSecond;
    public float CooldownTime => baseCooldownTime
        * Player.Instance.Modifiers.CooldownDurationMultiplier.GetValue();
    public float MaxRadius => smokeMaxRadius;
    public DamageType DamageType => baseDamageType;

    class ThinkingPipeState : AbilityState
    {
        public readonly Player Player;
        public PipeState State = PipeState.Inactive;

        public Vector3 CastPosition;    // fixed at windup start so the cloud does not follow the player. Ideally this will be a short stationary animation so this should be okay
        public float CooldownRemaining;
        public float WindupTimer;
        public float ActiveTimer;       // counts down over cloudDuration while active
        public PooledVfx VfxInstance;

        // scratch buffer reused every tick to avoid per-frame allocations
        public readonly Collider[] HitBuffer = new Collider[64];
        public readonly HashSet<IDamageable> HitThisTick = new();

        public ThinkingPipeState(Player player)
        {
            Player = player;
        }
    }

    public override AbilityState CreateState()
    {
        Player player = Player.Instance;
        return new ThinkingPipeState(player);
    }

    void StartWindup(ThinkingPipeState pipe)
    {
        pipe.State = PipeState.Windup;
        pipe.WindupTimer = windupTime;
        pipe.CastPosition = pipe.Player.transform.position + castPositionOffset; // position locked at this point
    }

    void StartActive(ThinkingPipeState pipe)
    {
        pipe.State = PipeState.Active;
        pipe.ActiveTimer = smokeDuration;

        PooledVfx vfx = VFXPoolManager.Instance.Play(smokeCloudVfx, pipe.CastPosition, Quaternion.identity);
        if (vfx != null && vfxBaseRadius > 0f)
        {
            float scaleRatio = MaxRadius / vfxBaseRadius;
            vfx.transform.localScale = Vector3.one * scaleRatio;
        }
        pipe.VfxInstance = vfx;
    }

    void EndActive(ThinkingPipeState pipe)
    {
        pipe.State = PipeState.Inactive;

        if (pipe.VfxInstance != null)
        {
            VFXPoolManager.Instance.Release(pipe.VfxInstance);
            pipe.VfxInstance = null;
        }
    }

    // Applies one tick of damage to everything currently inside the cloud's current radius
    void TickDamage(ThinkingPipeState pipe, float deltaTime)
    {
        float elapsed = smokeDuration - pipe.ActiveTimer;
        float currentRadius = expandDuration > 0f
            ? MaxRadius * Mathf.Clamp01(elapsed / expandDuration)
            : MaxRadius;

        int hitCount = Physics.OverlapSphereNonAlloc(pipe.CastPosition, currentRadius, pipe.HitBuffer, HitLayers);
        if (hitCount > 0)
        {
            float damageThisTick = Damage * deltaTime;
            DamageInfo damageInfo = new DamageInfo(damageThisTick, DamageType, pipe.Player.gameObject, false);

            pipe.HitThisTick.Clear();
            for (int i = 0; i < hitCount; i++)
            {
                IDamageable target = pipe.HitBuffer[i].GetComponentInParent<IDamageable>();
                if (target != null && pipe.HitThisTick.Add(target)) target.TakeDamage(damageInfo);
            }
        }
    }

    public override void Tick(AbilityState state, float deltaTime)
    {
        ThinkingPipeState pipe = state as ThinkingPipeState;
        if (pipe == null) return;

        switch (pipe.State)
        {
            case PipeState.Windup:
                pipe.WindupTimer = Mathf.Max(0f, pipe.WindupTimer - deltaTime);
                if (pipe.WindupTimer == 0f) StartActive(pipe);
                break;

            case PipeState.Active:
                TickDamage(pipe, deltaTime);
                pipe.ActiveTimer = Mathf.Max(0f, pipe.ActiveTimer - deltaTime);
                if (pipe.ActiveTimer == 0f) EndActive(pipe);
                break;
        }

        useTime = Mathf.Max(0f, useTime - deltaTime);
        pipe.CooldownRemaining = Mathf.Max(0f, pipe.CooldownRemaining - deltaTime);
    }

    public override bool TryExecute(AbilityState state)
    {
        ThinkingPipeState pipe = state as ThinkingPipeState;
        if (pipe == null) return false;
        if (pipe.State != PipeState.Inactive) return false;
        if (useTime > 0.01f) return false;
        if (pipe.CooldownRemaining > 0.1f) // cooldown eval (for QS bullet)
        {
            Debug.Log($"{pipe.CooldownRemaining} seconds left of cooldown");
            Player Player = pipe.Player;
            if (Player.Modifiers.CooldownDamageReduction.GetValue() != 0f)
            {
                Debug.Log("Quicksilver bullet used");
                // NOTE: This is on all abilities currently, refactoring for a broadcast done in the item runtime would be better but idc about that rn
                float playerHealth = Player.GetComponent<PlayerHealth>().MaxHP;
                DamageInfo selfDamage = new DamageInfo(playerHealth * pipe.CooldownRemaining * .01f * Player.Modifiers.CooldownDamageReduction.GetValue(), DamageType.Physical);
                Player.GetComponent<PlayerHealth>().TakeDamage(selfDamage);
            }
            else
            {
                Debug.Log("No Quicksilver Bullet");
                return false;
            }
        }

        StartWindup(pipe);

        pipe.CooldownRemaining = CooldownTime;
        useTime = useCooldown;
        return true;
    }

    public override float GetCooldownFraction(AbilityState state)
    {
        ThinkingPipeState pipe = state as ThinkingPipeState;
        if (pipe == null) return 0f;

        return pipe.CooldownRemaining / CooldownTime;
    }

    public override void Cancel(AbilityState state)
    {
        ThinkingPipeState pipe = state as ThinkingPipeState;
        if (pipe == null) return;

        EndActive(pipe); 
    }

    public void DrawGizmos(Transform playerTransform)
    {
        if (!showGizmo) return;

        Gizmos.color = new Color(0.6f, 0.9f, 0.6f, 1f);
        Gizmos.DrawWireSphere(playerTransform.position + castPositionOffset, MaxRadius);
    }
}
