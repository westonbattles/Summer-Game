using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PsychicPulse : AbilityBehaviour
{
    [Header("Properties")]
    [SerializeField, Tooltip("daamage that gets applied to psychic pulse target each collision")] float targetDamage = 2f;
    [SerializeField, Tooltip("chip damage that gets applied to enemies collidants with psychic pulse target")] float chipDamage = 5f;
    [SerializeField] float baseCooldownTime = 2f;
    
    [Header("Targetting")]
    [SerializeField] float maxTargetDistance = 15f;
    [SerializeField, Min(0.01f)] float selectionRadius = 1.25f; // world space max radius around camera forward ray enemies are allowed to be in to be targeted
    [SerializeField] float distanceScoreWeight = 0.05f;
    [SerializeField] float targetHighlightCheckInterval = 0.12f;
    [SerializeField] int maxPossibleTargets = 128;
    [SerializeField, Range(-20f, 0f), Tooltip("More negative values = enemies further behind the player relative to the camera are allowed to be targeted")] float backPruneThreshold = 0f;

    [Header("VFX")]
    [SerializeField] PooledVfx highlightVFXPrefab;

    [Header("Knockback")]
    [SerializeField] float pulseKnockbackStrength = 18f;
    [SerializeField] float pulseKnockbackDuration = 0.35f;

    [Header("KB Chain")]
    [SerializeField, Tooltip("how close pushed enemy can be to the next enemies to apply chain kb ")] float ramContactRadius = 0.2f;
    [SerializeField] float ramHitHeightOffset = 0.25f;
    [SerializeField] float ramMinImpactSpeed = 3f;
    [SerializeField] float ramFullStrengthImpactSpeed = 10f;
    [SerializeField, Range(-1f, 1f)] float ramMinInitialDirectionDot = 0f;
    [SerializeField] int maxRamChains = 4;
    [SerializeField, Range(0f, 1f)] float chainKnockbackStrengthMultiplier = 0.65f;
    [SerializeField, Range(0f, 1f)] float chainDurationMultiplier = 0.85f;
    [SerializeField, Range(0f, 1f)] float chainInitialDirectionBias = 0.8f; // how aligned with inital push direction the kb directions will be

    [Header("Gizmos")]
    
    LayerMask TargetLayers => Enemy.LayerMask;


    // MODIFIERS
    // to use a stat modifier, simply use it here! like so
    // public ModifiableStat => baseStat | could be turned into the following:
    // public ModifiableStat => baseStat + Player.Instance.Modifiers.StatModifierAddName
    // public ModifiableStat => baseStat * Player.Instance.Modifiers.StatModifierMultiplierName
    // (or any other way u want to use the modifier)
    public float CooldownTime => baseCooldownTime 
        - Player.Instance.Modifiers.CooldownDurationMultiplier.GetValue();

    class PsychicPulseState : AbilityState
    {
        public readonly Player Player;
        public readonly PlayerAim PlayerAim;
        public HashSet<Targetable> Targets = new(); // hashmap to de-dupe targets selected for finding which enemy to highlight

        public Targetable CurrentTarget;
        public PooledVfx ActiveHighlightVFX;
        
        public float CooldownRemaining;
        public float HighlightCheckRemaining;
        
        public Collider[] CandidateBuffer;
        //Enemy HighlightedEnemy;

        public PsychicPulseState(Player player, int maxPossibleTargets)
        {
            Player = player;
            PlayerAim = player.Aim;
            CandidateBuffer = new Collider[maxPossibleTargets];
        }
    }

    public override AbilityState CreateState()
    {
        Player player = Player.Instance;
        return new PsychicPulseState(player, maxPossibleTargets);
    }

    public override void Tick(AbilityState state, float deltaTime)
    {
        if (state is not PsychicPulseState psychicPulse) return;

        psychicPulse.CooldownRemaining = Mathf.Max(0f, psychicPulse.CooldownRemaining - deltaTime);

        bool canTarget = psychicPulse.CooldownRemaining <= 0f
                      && psychicPulse.Player.AbilityController.CurrentLoadout == psychicPulse.Player.EyeLoadout;

        if (!canTarget)
        {
            ClearTarget(psychicPulse); // no-op if nothing is targeted
            return;
        }

        if (psychicPulse.CurrentTarget != null && psychicPulse.ActiveHighlightVFX != null)
            psychicPulse.ActiveHighlightVFX.transform.position = psychicPulse.CurrentTarget.TargetPosition;

        psychicPulse.HighlightCheckRemaining -= deltaTime;
        if (psychicPulse.HighlightCheckRemaining > 0f) return;
        psychicPulse.HighlightCheckRemaining = targetHighlightCheckInterval;

        SetTarget(psychicPulse, FindBestTarget(psychicPulse));
    }

    public override bool TryExecute(AbilityState state)
    {
        if (state is not PsychicPulseState psychicPulse) return false;

        Targetable targetable = psychicPulse.CurrentTarget;
        if (targetable == null) return false;

        Enemy target = targetable.GetComponentInParent<Enemy>();
        if (target == null)
        {
            Debug.LogWarning($"[PsychicPulse.cs] {targetable.name} has no Enemy in its hierarchy.", targetable);
            return false;
        }

        // Enemy and IKnockbackable are almost certainly on the same object,
        // so look it up there instead of walking the hierarchy again.
        if (!target.TryGetComponent(out IKnockbackable knockbackable))
        {
            Debug.LogWarning($"[PsychicPulse.cs] - {target.name} has no IKnockbackable component.", target);
            return false;
        }
        if (psychicPulse.CooldownRemaining > 0.1f) // cooldown eval (for QS bullet)
        {
            Debug.Log($"{psychicPulse.CooldownRemaining} seconds left of cooldown");
            Player Player = psychicPulse.Player;
            if (Player.Modifiers.CooldownDamageReduction.GetValue() != 0f)
            {
                Debug.Log("Quicksilver bullet used");
                // NOTE: This is on all abilities currently, refactoring for a broadcast done in the item runtime would be better but idc about that rn
                float playerHealth = Player.GetComponent<PlayerHealth>().MaxHP;
                DamageInfo selfDamage = new DamageInfo(playerHealth * psychicPulse.CooldownRemaining * .01f * Player.Modifiers.CooldownDamageReduction.GetValue(), DamageType.Physical);
                Player.GetComponent<PlayerHealth>().TakeDamage(selfDamage);
            }
            else
            {
                Debug.Log("No Quicksilver Bullet");
                return false;
            }
        }

        Vector3 pulseDirection = GetPulseDirection(psychicPulse, target);
        GameObject source = psychicPulse.Player.gameObject;

        bool started = knockbackable.ApplyKnockback(
            new KnockbackInfo(pulseDirection, pulseKnockbackStrength, pulseKnockbackDuration, source));
        if (!started) return false;

        StartRamEffect(target, pulseDirection, source);
        ClearTarget(psychicPulse);
        psychicPulse.CooldownRemaining = CooldownTime;
        return true;
    }

    public override void Cancel(AbilityState state)
    {
        PsychicPulseState psychicPulse = state as PsychicPulseState;
        if (psychicPulse == null) return;
        
        // remove highlighted target
        ClearTarget(psychicPulse);
    }

    Targetable FindBestTarget(PsychicPulseState psychicPulse)
    {
        Vector3 playerPosition = psychicPulse.Player.transform.position;
        Vector3 cameraOrigin = psychicPulse.PlayerAim.AimOrigin;
        Vector3 cameraForward = psychicPulse.PlayerAim.AimDirection;

        int hitCount = Physics.OverlapSphereNonAlloc(
            playerPosition,
            maxTargetDistance,
            psychicPulse.CandidateBuffer,
            TargetLayers,
            QueryTriggerInteraction.Collide);

        if (hitCount == psychicPulse.CandidateBuffer.Length)
            Debug.LogWarning("PsychicPulse candidate buffer full.");

        Targetable bestTarget = null;
        float bestScore = Mathf.Infinity;
        psychicPulse.Targets.Clear();

        for (int i = 0; i < hitCount; i++)
        {
            Collider collider = psychicPulse.CandidateBuffer[i];
            if (collider == null) continue;

            Targetable targetable = collider.GetComponentInParent<Targetable>();
            if (targetable == null || !psychicPulse.Targets.Add(targetable)) continue;

            Vector3 targetPoint = targetable.TargetPosition;
            Vector3 cameraToTarget = targetPoint - cameraOrigin;

            float targetDistanceAlongAimRay = Vector3.Dot(cameraForward, cameraToTarget);

            if (targetDistanceAlongAimRay <= 0f) continue;
            if (Vector3.Dot(cameraForward, targetPoint - playerPosition) < backPruneThreshold) continue;

            Vector3 targetProjectionOnCameraForward = cameraOrigin + cameraForward * targetDistanceAlongAimRay;

            float distanceFromAimRaySqr = (targetPoint - targetProjectionOnCameraForward).sqrMagnitude;
            float selectionRadiusSqr = selectionRadius * selectionRadius;
            if (distanceFromAimRaySqr > selectionRadiusSqr) continue;

            float normalizedSideDistanceSqr = distanceFromAimRaySqr / selectionRadiusSqr;
            float score = normalizedSideDistanceSqr + targetDistanceAlongAimRay * distanceScoreWeight;

            if (score < bestScore)
            {
                bestScore = score;
                bestTarget = targetable;
            }
        }

        return bestTarget;
    }

    Vector3 GetPulseDirection(PsychicPulseState psychicPulse, Enemy target)
    {
        Vector3 direction = psychicPulse.PlayerAim.AimDirection;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = target.GetCenterPoint() - psychicPulse.Player.transform.position;
            direction.y = 0f;
        }

        return direction.sqrMagnitude > 0.0001f
            ? direction.normalized
            : psychicPulse.Player.transform.forward;
    }

    void StartRamEffect(Enemy target, Vector3 direction, GameObject source)
    {
        PsychicPulseRamEffect ramEffect = target.GetComponent<PsychicPulseRamEffect>();
        if (ramEffect == null) ramEffect = target.gameObject.AddComponent<PsychicPulseRamEffect>();

        ramEffect.Begin(new PsychicPulseRamEffect.RamInfo
        {
            Direction = direction,
            InitialDirection = direction,
            Duration = pulseKnockbackDuration,
            ContactRadius = ramContactRadius,
            HitHeightOffset = ramHitHeightOffset,
            TargetDamage = targetDamage,
            ChipDamage = chipDamage,
            MinImpactSpeed = ramMinImpactSpeed,
            FullStrengthImpactSpeed = ramFullStrengthImpactSpeed,
            MinInitialDirectionDot = ramMinInitialDirectionDot,
            ChainKnockbackStrength = pulseKnockbackStrength * chainKnockbackStrengthMultiplier,
            ChainKnockbackDuration = pulseKnockbackDuration * chainDurationMultiplier,
            ChainStrengthMultiplier = chainKnockbackStrengthMultiplier,
            ChainDurationMultiplier = chainDurationMultiplier,
            InitialDirectionBias = chainInitialDirectionBias,
            ChainsRemaining = maxRamChains,
            TargetLayers = TargetLayers,
            Source = source,
            PulseTarget = target
        });
    }

    /// sets and highlights target
    void SetTarget(PsychicPulseState psychicPulse, Targetable target)
    {
        if (psychicPulse.CurrentTarget == target) return;
        ClearTarget(psychicPulse);
        if (target == null) return;

        psychicPulse.CurrentTarget = target;
        target.SetHighlighted(true);
        if (highlightVFXPrefab != null)
        {
            psychicPulse.ActiveHighlightVFX = VFXPoolManager.Instance.Play(highlightVFXPrefab, target.TargetPosition, Quaternion.identity);
        }
    }

    // stops vfx on current target and sets current target to null
    void ClearTarget(PsychicPulseState psychicPulse)
    {
        if (psychicPulse.CurrentTarget == null) return;

        psychicPulse.CurrentTarget.SetHighlighted(false);
        psychicPulse.CurrentTarget = null;

        if (psychicPulse.ActiveHighlightVFX != null)
        {
            VFXPoolManager.Instance.Release(psychicPulse.ActiveHighlightVFX);
            psychicPulse.ActiveHighlightVFX = null;
        }
    }

    public override float GetCooldownFraction(AbilityState state)
    {
        if (state is not PsychicPulseState pp) return 0f;
        if (pp.CooldownRemaining > 0f) return Mathf.Clamp01(pp.CooldownRemaining / CooldownTime);
        return 0f;
    }
    
}
