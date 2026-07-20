using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PsychicPulseRamEffect : MonoBehaviour
{
    public struct RamInfo
    {
        public Vector3 Direction;
        public Vector3 InitialDirection;
        public float Duration;
        public float ContactRadius;
        public float HitHeightOffset;
        public float TargetDamage; // damage that gets applied to the target of psychic pulse, (applied every collision)
        public float ChipDamage; // damage applied to enemies being collided with
        public float MinImpactSpeed;
        public float FullStrengthImpactSpeed;
        public float MinInitialDirectionDot;
        public float ChainKnockbackStrength;
        public float ChainKnockbackDuration;
        public float ChainStrengthMultiplier;
        public float ChainDurationMultiplier;
        public float InitialDirectionBias;
        public int ChainsRemaining;
        public LayerMask TargetLayers;
        public GameObject Source;
        public Enemy PulseTarget;
        public Enemy IgnoreEnemy;
        public HashSet<Enemy> ChipDamagedEnemies;
        public HashSet<Enemy> TargetDamageCreditedEnemies;
    }

    private const float MinDirectionSqr = 0.0001f;

    private readonly HashSet<Enemy> hitEnemies = new();
    private readonly RaycastHit[] castHits = new RaycastHit[16];

    private Enemy owner;
    private Combat sourceCombat;
    private Vector3 direction;
    private Vector3 initialDirection;
    private Vector3 lastPosition;

    private float remaining;
    private float contactRadius;
    private float hitHeightOffset;
    private float targetDamage;
    private float chipDamage;
    private float minImpactSpeed;
    private float fullStrengthImpactSpeed;
    private float minInitialDirectionDot;
    private float chainKnockbackStrength;
    private float chainKnockbackDuration;
    private float chainStrengthMultiplier;
    private float chainDurationMultiplier;
    private float initialDirectionBias;
    private int chainsRemaining;
    private LayerMask targetLayers;
    private GameObject source;
    private Enemy pulseTarget;
    private HashSet<Enemy> chipDamagedEnemies;
    private HashSet<Enemy> targetDamageCreditedEnemies;
    private bool hasMovedFastEnough;

    private void Awake()
    {
        owner = GetComponent<Enemy>();
        enabled = false;
    }

    public void Begin(RamInfo info)
    {
        if (owner == null) owner = GetComponent<Enemy>();

        if (owner == null)
        {
            enabled = false;
            return;
        }

        Vector3 flatDirection = info.Direction;
        flatDirection.y = 0f;

        direction = flatDirection.sqrMagnitude > MinDirectionSqr ? flatDirection.normalized : Vector3.zero;

        Vector3 flatInitialDirection = info.InitialDirection;
        flatInitialDirection.y = 0f;

        initialDirection = flatInitialDirection.sqrMagnitude > MinDirectionSqr ? flatInitialDirection.normalized : direction;

        remaining = Mathf.Max(0f, info.Duration);
        contactRadius = Mathf.Max(0.01f, info.ContactRadius);
        hitHeightOffset = info.HitHeightOffset;
        targetDamage = Mathf.Max(0f, info.TargetDamage);
        chipDamage = Mathf.Max(0f, info.ChipDamage);
        minImpactSpeed = Mathf.Max(0f, info.MinImpactSpeed);
        fullStrengthImpactSpeed = Mathf.Max(minImpactSpeed + 0.01f, info.FullStrengthImpactSpeed);
        minInitialDirectionDot = Mathf.Clamp(info.MinInitialDirectionDot, -1f, 1f);
        chainKnockbackStrength = Mathf.Max(0f, info.ChainKnockbackStrength);
        chainKnockbackDuration = Mathf.Max(0f, info.ChainKnockbackDuration);
        chainStrengthMultiplier = Mathf.Clamp01(info.ChainStrengthMultiplier);
        chainDurationMultiplier = Mathf.Clamp01(info.ChainDurationMultiplier);
        initialDirectionBias = Mathf.Clamp01(info.InitialDirectionBias);
        chainsRemaining = Mathf.Max(0, info.ChainsRemaining);
        targetLayers = info.TargetLayers;
        source = info.Source;
        sourceCombat = source != null ? source.GetComponent<Combat>() : null;
        pulseTarget = info.PulseTarget != null ? info.PulseTarget : owner;
        chipDamagedEnemies = info.ChipDamagedEnemies ?? new HashSet<Enemy>();
        targetDamageCreditedEnemies = info.TargetDamageCreditedEnemies ?? new HashSet<Enemy>();
        hasMovedFastEnough = false;

        hitEnemies.Clear();
        hitEnemies.Add(owner);

        if (info.IgnoreEnemy != null) hitEnemies.Add(info.IgnoreEnemy);

        lastPosition = transform.position;
        enabled = remaining > 0f && direction.sqrMagnitude > MinDirectionSqr;

        RegisterPushedEnemy(owner);
    }

    private void Update()
    {
        Vector3 currentPosition = transform.position;

        if (TryGetRamTravel(lastPosition, currentPosition, out Vector3 travelDirection, out float impactStrengthScale))
        {
            hasMovedFastEnough = true;
            CheckRammedTargets(lastPosition, currentPosition, travelDirection, impactStrengthScale);
        }
        else if (hasMovedFastEnough)
        {
            enabled = false;
        }

        lastPosition = currentPosition;
        remaining -= Time.deltaTime;

        if (remaining <= 0f) enabled = false;
    }

    private bool TryGetRamTravel(Vector3 fromPosition, Vector3 toPosition, out Vector3 travelDirection, out float impactStrengthScale)
    {
        Vector3 delta = toPosition - fromPosition;
        delta.y = 0f;

        float deltaDistance = delta.magnitude;
        if (deltaDistance <= 0.001f)
        {
            travelDirection = Vector3.zero;
            impactStrengthScale = 0f;
            return false;
        }

        travelDirection = delta / deltaDistance;

        if (initialDirection.sqrMagnitude > MinDirectionSqr && Vector3.Dot(travelDirection, initialDirection) < minInitialDirectionDot)
        {
            impactStrengthScale = 0f;
            return false;
        }

        float speed = deltaDistance / Mathf.Max(Time.deltaTime, 0.0001f);
        impactStrengthScale = Mathf.InverseLerp(minImpactSpeed, fullStrengthImpactSpeed, speed);
        return speed >= minImpactSpeed;
    }

    private void CheckRammedTargets(Vector3 fromPosition, Vector3 toPosition, Vector3 travelDirection, float impactStrengthScale)
    {
        Vector3 from = fromPosition + Vector3.up * hitHeightOffset;
        Vector3 to = toPosition + Vector3.up * hitHeightOffset;
        Vector3 delta = to - from;

        if (delta.sqrMagnitude > MinDirectionSqr)
        {
            int count = Physics.SphereCastNonAlloc(
                from,
                contactRadius,
                travelDirection,
                castHits,
                delta.magnitude,
                targetLayers,
                QueryTriggerInteraction.Collide);

            for (int i = 0; i < count; i++) HandleRammedCollider(castHits[i].collider, travelDirection, impactStrengthScale);
        }
    }

    private void HandleRammedCollider(Collider hitCollider, Vector3 travelDirection, float impactStrengthScale)
    {
        if (hitCollider == null) return;

        Enemy hitEnemy = hitCollider.GetComponentInParent<Enemy>();
        if (hitEnemy == null || hitEnemies.Contains(hitEnemy)) return;
        if (!IsInFrontOfRam(hitEnemy, travelDirection)) return;

        hitEnemies.Add(hitEnemy);

        if (chainsRemaining > 0 && chainKnockbackStrength > 0f)
        {
            IKnockbackable knockbackable = hitEnemy.GetComponentInParent<IKnockbackable>();
            if (knockbackable != null)
            {
                Vector3 nextDirection = GetBiasedChainDirection(hitEnemy);
                float scaledKnockbackStrength = chainKnockbackStrength * impactStrengthScale;
                if (scaledKnockbackStrength <= 0f) return;

                KnockbackInfo knockbackInfo = new(
                    nextDirection,
                    scaledKnockbackStrength,
                    chainKnockbackDuration,
                    source);

                if (knockbackable.ApplyKnockback(knockbackInfo))
                {
                    PsychicPulseRamEffect nextRamEffect = hitEnemy.GetComponent<PsychicPulseRamEffect>();
                    if (nextRamEffect == null) nextRamEffect = hitEnemy.gameObject.AddComponent<PsychicPulseRamEffect>();

                    nextRamEffect.Begin(new RamInfo
                    {
                        Direction = knockbackInfo.Direction,
                        InitialDirection = initialDirection,
                        Duration = chainKnockbackDuration,
                        ContactRadius = contactRadius,
                        HitHeightOffset = hitHeightOffset,
                        TargetDamage = targetDamage,
                        ChipDamage = chipDamage,
                        MinImpactSpeed = minImpactSpeed,
                        FullStrengthImpactSpeed = fullStrengthImpactSpeed,
                        MinInitialDirectionDot = minInitialDirectionDot,
                        ChainKnockbackStrength = chainKnockbackStrength * chainStrengthMultiplier,
                        ChainKnockbackDuration = chainKnockbackDuration * chainDurationMultiplier,
                        ChainStrengthMultiplier = chainStrengthMultiplier,
                        ChainDurationMultiplier = chainDurationMultiplier,
                        InitialDirectionBias = initialDirectionBias,
                        ChainsRemaining = chainsRemaining - 1,
                        TargetLayers = targetLayers,
                        Source = source,
                        PulseTarget = pulseTarget,
                        IgnoreEnemy = owner,
                        ChipDamagedEnemies = chipDamagedEnemies,
                        TargetDamageCreditedEnemies = targetDamageCreditedEnemies
                    });
                }
            }
        }
    }

    private bool IsInFrontOfRam(Enemy hitEnemy, Vector3 travelDirection)
    {
        Vector3 toTarget = hitEnemy.GetCenterPoint() - owner.GetCenterPoint();
        toTarget.y = 0f;

        if (toTarget.sqrMagnitude <= MinDirectionSqr) return true;

        return Vector3.Dot(toTarget.normalized, travelDirection) >= -0.05f;
    }

    private Vector3 GetBiasedChainDirection(Enemy hitEnemy)
    {
        Vector3 collisionDirection = hitEnemy.GetCenterPoint() - owner.GetCenterPoint();
        collisionDirection.y = 0f;

        if (collisionDirection.sqrMagnitude <= MinDirectionSqr) collisionDirection = direction;
        if (initialDirection.sqrMagnitude <= MinDirectionSqr) return collisionDirection.normalized;

        Vector3 biasedDirection = Vector3.Lerp(collisionDirection.normalized, initialDirection, initialDirectionBias);
        return biasedDirection.sqrMagnitude > MinDirectionSqr ? biasedDirection.normalized : initialDirection;
    }
    
    /// apply ram damage
    private void Damage(Enemy target, float damage)
    {
        if (damage <= 0f || target == null) return;
        
        IDamageable damageable = target.GetComponentInParent<IDamageable>();
        if (damageable != null && sourceCombat != null) sourceCombat.TryDamage(damageable, new DamageInfo(damage, DamageType.Physical, source));
    }

    private void RegisterPushedEnemy(Enemy pushedEnemy)
    {
        if (pushedEnemy == null || pushedEnemy == pulseTarget) return;

        if (chipDamagedEnemies.Add(pushedEnemy)) Damage(pushedEnemy, chipDamage);

        if (pulseTarget != null && targetDamageCreditedEnemies.Add(pushedEnemy)) Damage(pulseTarget, targetDamage);
    }

    private void OnDisable()
    {
        remaining = 0f;
        hitEnemies.Clear();
        pulseTarget = null;
        chipDamagedEnemies = null;
        targetDamageCreditedEnemies = null;
    }
}
