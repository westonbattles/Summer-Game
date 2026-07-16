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
        public float ChipDamage;
        public float ChainKnockbackStrength;
        public float ChainKnockbackDuration;
        public float ChainStrengthMultiplier;
        public float ChainDurationMultiplier;
        public float InitialDirectionBias;
        public int ChainsRemaining;
        public LayerMask TargetLayers;
        public GameObject Source;
        public Enemy InitialRammer;
        public Enemy IgnoreEnemy;
    }

    private const float MinDirectionSqr = 0.0001f;

    private readonly HashSet<Enemy> hitEnemies = new();
    private readonly RaycastHit[] castHits = new RaycastHit[16];
    private readonly Collider[] overlapHits = new Collider[16];

    private Enemy owner;
    private Vector3 direction;
    private Vector3 initialDirection;
    private Vector3 lastPosition;

    private float remaining;
    private float contactRadius;
    private float hitHeightOffset;
    private float chipDamage;
    private float chainKnockbackStrength;
    private float chainKnockbackDuration;
    private float chainStrengthMultiplier;
    private float chainDurationMultiplier;
    private float initialDirectionBias;
    private int chainsRemaining;
    private LayerMask targetLayers;
    private GameObject source;
    private Enemy initialRammer;

    private void Awake()
    {
        owner = GetComponent<Enemy>();
        enabled = false;
    }

    public void Begin(RamInfo info)
    {
        if (owner == null)
            owner = GetComponent<Enemy>();

        if (owner == null)
        {
            enabled = false;
            return;
        }

        Vector3 flatDirection = info.Direction;
        flatDirection.y = 0f;

        direction = flatDirection.sqrMagnitude > MinDirectionSqr
            ? flatDirection.normalized
            : Vector3.zero;

        Vector3 flatInitialDirection = info.InitialDirection;
        flatInitialDirection.y = 0f;

        initialDirection = flatInitialDirection.sqrMagnitude > MinDirectionSqr
            ? flatInitialDirection.normalized
            : direction;

        remaining = Mathf.Max(0f, info.Duration);
        contactRadius = Mathf.Max(0.01f, info.ContactRadius);
        hitHeightOffset = info.HitHeightOffset;
        chipDamage = Mathf.Max(0f, info.ChipDamage);
        chainKnockbackStrength = Mathf.Max(0f, info.ChainKnockbackStrength);
        chainKnockbackDuration = Mathf.Max(0f, info.ChainKnockbackDuration);
        chainStrengthMultiplier = Mathf.Clamp01(info.ChainStrengthMultiplier);
        chainDurationMultiplier = Mathf.Clamp01(info.ChainDurationMultiplier);
        initialDirectionBias = Mathf.Clamp01(info.InitialDirectionBias);
        chainsRemaining = Mathf.Max(0, info.ChainsRemaining);
        targetLayers = info.TargetLayers;
        source = info.Source;
        initialRammer = info.InitialRammer != null ? info.InitialRammer : owner;

        hitEnemies.Clear();
        hitEnemies.Add(owner);

        if (info.IgnoreEnemy != null)
            hitEnemies.Add(info.IgnoreEnemy);

        lastPosition = transform.position;
        enabled = remaining > 0f && direction.sqrMagnitude > MinDirectionSqr;
    }

    private void Update()
    {
        Vector3 currentPosition = transform.position;

        CheckRammedTargets(lastPosition, currentPosition);

        lastPosition = currentPosition;
        remaining -= Time.deltaTime;

        if (remaining <= 0f)
            enabled = false;
    }

    private void CheckRammedTargets(Vector3 fromPosition, Vector3 toPosition)
    {
        Vector3 from = fromPosition + Vector3.up * hitHeightOffset;
        Vector3 to = toPosition + Vector3.up * hitHeightOffset;
        Vector3 delta = to - from;

        if (delta.sqrMagnitude > MinDirectionSqr)
        {
            int count = Physics.SphereCastNonAlloc(
                from,
                contactRadius,
                delta.normalized,
                castHits,
                delta.magnitude + contactRadius,
                targetLayers,
                QueryTriggerInteraction.Collide);

            for (int i = 0; i < count; i++)
                HandleRammedCollider(castHits[i].collider);

            return;
        }

        int overlapCount = Physics.OverlapSphereNonAlloc(
            to,
            contactRadius,
            overlapHits,
            targetLayers,
            QueryTriggerInteraction.Collide);

        for (int i = 0; i < overlapCount; i++)
            HandleRammedCollider(overlapHits[i]);
    }

    private void HandleRammedCollider(Collider hitCollider)
    {
        if (hitCollider == null) return;

        Enemy hitEnemy = hitCollider.GetComponentInParent<Enemy>();
        if (hitEnemy == null || !hitEnemies.Add(hitEnemy)) return;

        if (chainsRemaining > 0 && chainKnockbackStrength > 0f)
        {
            IKnockbackable knockbackable = hitEnemy.GetComponentInParent<IKnockbackable>();
            if (knockbackable != null)
            {
                Vector3 nextDirection = GetBiasedChainDirection(hitEnemy);

                KnockbackInfo knockbackInfo = new(
                    nextDirection,
                    chainKnockbackStrength,
                    chainKnockbackDuration,
                    source);

                knockbackable.ApplyKnockback(knockbackInfo);

                PsychicPulseRamEffect nextRamEffect = hitEnemy.GetComponent<PsychicPulseRamEffect>();
                if (nextRamEffect == null)
                    nextRamEffect = hitEnemy.gameObject.AddComponent<PsychicPulseRamEffect>();

                nextRamEffect.Begin(new RamInfo
                {
                    Direction = knockbackInfo.Direction,
                    InitialDirection = initialDirection,
                    Duration = chainKnockbackDuration,
                    ContactRadius = contactRadius,
                    HitHeightOffset = hitHeightOffset,
                    ChipDamage = chipDamage,
                    ChainKnockbackStrength = chainKnockbackStrength * chainStrengthMultiplier,
                    ChainKnockbackDuration = chainKnockbackDuration * chainDurationMultiplier,
                    ChainStrengthMultiplier = chainStrengthMultiplier,
                    ChainDurationMultiplier = chainDurationMultiplier,
                    InitialDirectionBias = initialDirectionBias,
                    ChainsRemaining = chainsRemaining - 1,
                    TargetLayers = targetLayers,
                    Source = source,
                    InitialRammer = initialRammer,
                    IgnoreEnemy = owner
                });
            }
        }

        if (owner == initialRammer)
        {
            DealChipDamage(hitEnemy);
            DealChipDamage(owner);
        }
    }

    private Vector3 GetBiasedChainDirection(Enemy hitEnemy)
    {
        Vector3 collisionDirection = hitEnemy.GetCenterPoint() - owner.GetCenterPoint();
        collisionDirection.y = 0f;

        if (collisionDirection.sqrMagnitude <= MinDirectionSqr)
            collisionDirection = direction;

        if (initialDirection.sqrMagnitude <= MinDirectionSqr)
            return collisionDirection.normalized;

        Vector3 biasedDirection = Vector3.Lerp(collisionDirection.normalized, initialDirection, initialDirectionBias);
        return biasedDirection.sqrMagnitude > MinDirectionSqr
            ? biasedDirection.normalized
            : initialDirection;
    }

    private void DealChipDamage(Enemy target)
    {
        if (chipDamage <= 0f || target == null) return;

        IDamageable damageable = target.GetComponentInParent<IDamageable>();
        if (damageable != null && damageable.IsAlive)
            damageable.TakeDamage(new DamageInfo(chipDamage, source));
    }

    private void OnDisable()
    {
        remaining = 0f;
        hitEnemies.Clear();
    }
}
