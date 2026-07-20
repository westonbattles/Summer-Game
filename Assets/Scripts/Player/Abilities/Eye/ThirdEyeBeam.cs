using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

enum BeamState
{
    Inactive,
    Windup,
    Firing
}

[Serializable]
public class ThirdEyeBeam : AbilityBehaviour
{
    [Header("Properties")] 
    [SerializeField] float baseDamagePerSecond = 20f;
    [SerializeField] float baseCooldownTime = 10f;
    [SerializeField] float beamDuration = 1.5f;
    [SerializeField] float beamRange = 20f;
    [SerializeField] float beamRadius = 0.35f; // thickness of the capsule cast (0 would be pure hitscan raycast at cursor)
    [SerializeField] DamageType baseDamageType = DamageType.Magic;

    [Header("Windup")]
    [SerializeField] float windupTime = 0.2f;

    [Header("Target Gathering")]
    [SerializeField] Vector3 castPositionOffset;
    [SerializeField] bool showGizmo = true;

    [Header("Obstruction")]
    [SerializeField] LayerMask obstructionLayers; // objects the beam should stop at (terrain, walls, static props)

    [Header("VFX")]
    [SerializeField] LineRenderer beamLineRenderer; // stretched from origin to hit point each tick
    [SerializeField] PooledVfx beamImpactVfx;
    
    LayerMask HitLayers => Enemy.LayerMask | LayerMask.GetMask("Hittable");
    
    // MODIFIERS
    // to use a stat modifier, simply use it here! like so
    // public ModifiableStat => baseStat | could be turned into the following:
    // public ModifiableStat => baseStat + Player.Instance.Modifiers.StatModifierAddName
    // public ModifiableStat => baseStat * Player.Instance.Modifiers.StatModifierMultiplierName
    // (or any other way u want to use the modifier)
    public float Damage => baseDamagePerSecond;
    public DamageType DamageType => baseDamageType;
    public float CooldownTime => baseCooldownTime;
    public float Range => beamRange;
    
    class ThirdEyeBeamState : AbilityState
    {
        public readonly Player Player;
        public readonly PlayerAim PlayerAim;
        public BeamState State = BeamState.Inactive;

        public float CooldownRemaining;          // how much time left on the ability cooldown
        public float WindupTimer;                // priming time (beam won't fire instantaneously)
        public float FiringTimer;                // counts down over beamDuration while firing

        // scratch buffer reused every tick to avoid per-frame allocations
        public readonly RaycastHit[] HitBuffer = new RaycastHit[32];
        public readonly HashSet<IDamageable> HitThisTick = new();

        public LineRenderer RuntimeLineRenderer; // placeholder until new vfx is made
        public LineRenderer LineRendererInstance; // instantiated once from prefab
        

        public ThirdEyeBeamState(Player player, PlayerAim playerAim)
        {
            Player = player;
            PlayerAim = playerAim;
        }
    }

    public override AbilityState CreateState()
    {
        Player player = Player.Instance;
        PlayerAim playerAim = player.Aim;
        if (playerAim == null)
        {
            Debug.LogError("ThirdEyeBeam couldn't find PlayerAim");
            return null;
        }
        ThirdEyeBeamState state = new ThirdEyeBeamState(player, playerAim);

        // instantiate the beam VFX prefab once and hold onto it for ability lifetime
        if (beamLineRenderer != null)
        {
            LineRenderer instance = UnityEngine.Object.Instantiate(beamLineRenderer, player.transform);
            instance.positionCount = 2;
            instance.enabled = false;
            state.LineRendererInstance = instance;
        }

        return state;
    }

    /// Returns the beam's LineRenderer as the instantiated prefab if one was assigned, otherwise a bare placeholder
    LineRenderer GetOrCreateLineRenderer(ThirdEyeBeamState beam)
    {
        if (beam.LineRendererInstance != null) return beam.LineRendererInstance;

        GameObject go = new GameObject("ThirdEyeBeam_PlaceholderVFX");
        go.transform.SetParent(beam.Player.transform, worldPositionStays: false);

        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.startWidth = beamRadius > 0f ? beamRadius * 2f : 0.1f;
        lr.endWidth = lr.startWidth;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = Color.cyan;
        lr.endColor = new Color(0f, 1f, 1f, 0.3f); // fade out toward the tip
        lr.enabled = false;

        beam.LineRendererInstance = lr;
        return lr;
    }

    void StartWindup(ThirdEyeBeamState beam)
    {
        beam.State = BeamState.Windup;
        beam.WindupTimer = windupTime;
    }

    void StartFiring(ThirdEyeBeamState beam)
    {
        beam.State = BeamState.Firing;
        beam.FiringTimer = beamDuration;
        GetOrCreateLineRenderer(beam).enabled = true;
    }

    void StopFiring(ThirdEyeBeamState beam)
    {
        beam.State = BeamState.Inactive;
        GetOrCreateLineRenderer(beam).enabled = false;
    }

    void FireTick(ThirdEyeBeamState beam, float deltaTime)
    {
        Debug.Log(beam.Player.transform.position);
        Vector3 origin = beam.Player.transform.position + castPositionOffset;
        Vector3 targetPoint = beam.PlayerAim.GetTargetPoint(beamRange, HitLayers);
        Vector3 direction = beam.PlayerAim.GetShotDirection(origin, targetPoint);

        // find the closest obstruction so the beam doesn't pierce through walls
        float maxDistance = beamRange;
        if (Physics.Raycast(origin, direction, out RaycastHit obstructionHit, beamRange, obstructionLayers))
        {
            maxDistance = obstructionHit.distance;
        }

        int hitCount = beamRadius > 0f
            ? Physics.CapsuleCastNonAlloc(
                origin, origin + direction * 0.01f, beamRadius,
                direction, beam.HitBuffer, maxDistance, HitLayers)
            : Physics.RaycastNonAlloc(
                origin, direction, beam.HitBuffer, maxDistance, HitLayers);

        if (hitCount > 0)
        {
            float damageThisTick = Damage * deltaTime;
            DamageInfo damageInfo = new DamageInfo(damageThisTick, DamageType, beam.Player.gameObject, false);

            beam.HitThisTick.Clear();
            for (int i = 0; i < hitCount; i++)
            {
                IDamageable target = beam.HitBuffer[i].collider.GetComponentInParent<IDamageable>();
                if (target != null && beam.HitThisTick.Add(target)) target.TakeDamage(damageInfo);
            }
        }

        // beam impact vfx placeholder, test further once actual asset is made for it
        if (beamImpactVfx != null)
        {
            VFXPoolManager.Instance.Play(beamImpactVfx, origin + direction * maxDistance, Quaternion.identity);
        }

        // update VFX beam line every tick regardless of whether it hit anything
        LineRenderer lr = GetOrCreateLineRenderer(beam);
        lr.SetPosition(0, origin);
        lr.SetPosition(1, origin + direction * maxDistance);

        // test line
        Debug.DrawLine(origin, origin + direction * maxDistance, Color.cyan);
    }
    
    public override void Tick(AbilityState state, float deltaTime)
    {
        ThirdEyeBeamState beam = state as ThirdEyeBeamState;
        if (beam == null) return;

        switch (beam.State)
        {
            case BeamState.Windup:
                beam.WindupTimer = Mathf.Max(0f, beam.WindupTimer - deltaTime);
                if (beam.WindupTimer == 0f) StartFiring(beam);
                break;

            case BeamState.Firing:
                FireTick(beam, deltaTime);
                beam.FiringTimer = Mathf.Max(0f, beam.FiringTimer - deltaTime);
                if (beam.FiringTimer == 0f) StopFiring(beam);
                break;
        }
        
        // update ability cooldown
        beam.CooldownRemaining = Mathf.Max(0f, beam.CooldownRemaining - deltaTime);
    }

    public override bool TryExecute(AbilityState state)
    {
        
        // make sure we actually can/should execute the pound
        ThirdEyeBeamState beam = state as ThirdEyeBeamState;
        if (beam == null) return false;
        if (beam.CooldownRemaining > 0f) return false;
        if (beam.State != BeamState.Inactive) return false; // already firing
  
        
        // succesful execution
        StartWindup(beam);
        
        beam.CooldownRemaining = CooldownTime; // start cooldown countdown
        return true;
    }

    public override float GetCooldownFraction(AbilityState state)
    {
        ThirdEyeBeamState beam = state as ThirdEyeBeamState;
        if (beam == null) return 0f;

        return beam.CooldownRemaining / CooldownTime;
    }

    public override void Cancel(AbilityState state)
    {
        ThirdEyeBeamState beam = state as ThirdEyeBeamState;
        if (beam == null) return;

        StopFiring(beam);
    }
    
    
    // gizmo debug for beam direction and range
    public void DrawGizmos(Transform playerTransform)
    {
        if (!showGizmo) return;

        Gizmos.color = Color.magenta;
        Vector3 origin = playerTransform.position + castPositionOffset;
        Gizmos.DrawLine(origin, origin + playerTransform.forward * beamRange);
    }
}