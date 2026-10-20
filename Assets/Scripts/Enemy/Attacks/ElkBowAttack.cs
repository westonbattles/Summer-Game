using System;
using UnityEngine;

enum AttackState
{
    Idle,
    IdleAggro,
    Charging,
    Ready,
    Releasing,
    Cooldown
}

[RequireComponent(typeof(Combat))]
[RequireComponent(typeof(EnemyAttackController))]
[RequireComponent(typeof(ElkMover))]
public class ElkBowAttack : MonoBehaviour, IEnemyAttackWithIndicator
{
    [SerializeField] DamageType damageType = DamageType.Projectile;
    [SerializeField] Projectile projectilePrefab;
    //[SerializeField] GameObject/Indicator indicatorPrefab;
    [SerializeField] Transform firePoint;
    [SerializeField] LayerMask sightBlockingLayers;
    [SerializeField] float damageCloseRange = 3f;
    [SerializeField] float damageLongRange = 17f;
    [SerializeField, Tooltip("Time in air it takes from arrow to swap from close range damage to long range damage")] float rangeTime = 0.4f;
    [SerializeField] float chargeTime = 1.5f;
    [SerializeField] float cooldown = 1.5f;
    [SerializeField] float maxRange = 30;
    [SerializeField] float projectileSpeed = 30;

    Combat ownerCombat;
    ElkMover mover;
    Transform target;
    Player targetPlayer;
    AttackState attackState;

    Vector3 shotVelocity;
    float chargeRemaining;
    float cooldownRemaining;

    // need to be used eventually
    public event Action<GameObject> showIndicator;
    public event Action hideIndicator;

    Vector3 TargetPoint => target.position + Vector3.up;

    void Awake()
    {
        ownerCombat = GetComponent<Combat>();
        mover = GetComponent<ElkMover>();
        attackState = AttackState.Idle;
        LowerBow();
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        targetPlayer = newTarget != null ? newTarget.GetComponentInParent<Player>() : null;
        mover.SetTarget(newTarget);
        if (target != null) return;

        shotVelocity = Vector3.zero;
        chargeRemaining = 0f;
        cooldownRemaining = 0f;
        ChangeState(AttackState.Idle);
    }

    void Update()
    {
        if (target == null) return;

        if (cooldownRemaining > 0f) cooldownRemaining -= Time.deltaTime;

        switch (attackState)
        {
            case AttackState.Idle: IdleUpdate(); break;
            case AttackState.IdleAggro: IdleAggroUpdate(); break;
            case AttackState.Charging: ChargingUpdate(); break;
            case AttackState.Ready: break;
            case AttackState.Releasing: break;
            case AttackState.Cooldown: CooldownUpdate(); break;
        }
    }

    void ChangeState(AttackState newState)
    {
        if (attackState == newState) return;
        ExitState(attackState);
        attackState = newState;
        EnterState(attackState);
    }

    void EnterState(AttackState state)
    {
        if (state == AttackState.Idle) LowerBow();
        else if (state == AttackState.IdleAggro) mover.ClearAimDirection();
        else if (state == AttackState.Charging) chargeRemaining = chargeTime;
        else if (state == AttackState.Releasing) ReleaseArrowAnimationEvent(); // replace with the shoot animation trigger later
    }

    void ExitState(AttackState state)
    {
        if (state == AttackState.Idle) RaiseBow();
    }

    void IdleUpdate()
    {
        if (Vector3.Distance(transform.position, target.position) > maxRange) return;
        if (!HasLineOfSight(TargetPoint))
        {
            ChangeState(AttackState.IdleAggro);
            return;
        }

        ChangeState(cooldownRemaining > 0f ? AttackState.Cooldown : AttackState.Charging);
    }

    void IdleAggroUpdate()
    {
        if (Vector3.Distance(transform.position, target.position) > maxRange)
        {
            ChangeState(AttackState.Idle);
            return;
        }

        if (!HasLineOfSight(TargetPoint)) return;
        ChangeState(cooldownRemaining > 0f ? AttackState.Cooldown : AttackState.Charging);
    }

    void ChargingUpdate()
    {
        if (TryCancelAttack()) return;
        TryAimAtPredictedShot(out _);

        chargeRemaining -= Time.deltaTime;
        if (chargeRemaining <= 0f) ChangeState(AttackState.Ready);
    }

    void CooldownUpdate()
    {
        if (TryCancelAttack()) return;
        TryAimAtPredictedShot(out _);
        if (cooldownRemaining <= 0f) ChangeState(AttackState.Charging);
    }

    bool TryCancelAttack()
    {
        if (Vector3.Distance(transform.position, target.position) > maxRange)
        {
            ChangeState(AttackState.Idle);
            return true;
        }

        if (!HasLineOfSight(TargetPoint))
        {
            ChangeState(AttackState.IdleAggro);
            return true;
        }

        return false;
    }

    bool HasLineOfSight(Vector3 point)
    {
        return !Physics.Linecast(firePoint.position, point, sightBlockingLayers, QueryTriggerInteraction.Ignore);
    }

    bool TryAimAtPredictedShot(out Vector3 launchVelocity)
    {
        if (!TryCalculateShot(out launchVelocity)) return false;

        Vector3 aimDirection = Vector3.ProjectOnPlane(launchVelocity, Vector3.up);
        if (aimDirection.sqrMagnitude <= 0.0001f) return false;

        mover.SetAimDirection(aimDirection);
        return mover.IsFacingDirection(aimDirection);
    }

    bool TryCalculateShot(out Vector3 launchVelocity)
    {
        launchVelocity = Vector3.zero;
        if (projectileSpeed <= 0f) return false;

        Vector3 targetVelocity = Vector3.zero;
        if (targetPlayer != null) targetVelocity = Vector3.ProjectOnPlane(targetPlayer.Motor.Velocity, Vector3.up);

        Vector3 firePointToTarget = TargetPoint - firePoint.position;
        float speedSquared = projectileSpeed * projectileSpeed;
        float a = targetVelocity.sqrMagnitude - speedSquared;
        float b = 2f * Vector3.Dot(firePointToTarget, targetVelocity);
        float c = firePointToTarget.sqrMagnitude;
        float interceptTime;
        const float epsilon = 0.0001f;

        if (Mathf.Abs(a) < epsilon)
        {
            if (Mathf.Abs(b) < epsilon) return false;
            interceptTime = -c / b;
            if (interceptTime <= epsilon) return false;
        }
        else
        {
            float discriminant = b * b - 4f * a * c;
            if (discriminant < 0f) return false;

            float squareRoot = Mathf.Sqrt(discriminant);
            float denominator = 2f * a;
            float timeA = (-b - squareRoot) / denominator;
            float timeB = (-b + squareRoot) / denominator;

            interceptTime = Mathf.Infinity;
            if (timeA > 0f && timeB > 0f) interceptTime = Mathf.Min(timeA, timeB);
            else if (timeA > 0f) interceptTime = timeA;
            else if (timeB > 0f) interceptTime = timeB;
            if (float.IsPositiveInfinity(interceptTime)) return false;
        }

        if (interceptTime > maxRange / projectileSpeed) return false;

        Vector3 predictedPosition = TargetPoint + targetVelocity * interceptTime;
        Vector3 direction = predictedPosition - firePoint.position;
        if (direction.sqrMagnitude <= epsilon || !HasLineOfSight(predictedPosition)) return false;

        launchVelocity = direction.normalized * projectileSpeed;
        return true;
    }

    void ShootArrow(Vector3 launchVelocity)
    {
        Projectile projectile = ProjectilePoolManager.Instance.Get(projectilePrefab);
        projectile.Launch(
            ownerCombat,
            firePoint.position,
            launchVelocity.normalized,
            launchVelocity.magnitude,
            new DamageInfo(damageCloseRange, damageType, gameObject), 
            rangeTime,
            damageLongRange);
    }

    void RaiseBow()
    {
        // raise bow animation
    }

    void LowerBow()
    {
        mover.ClearAimDirection();
        // lower bow animation
    }

    // This will eventually be called by the shoot animation.
    public void ReleaseArrowAnimationEvent()
    {
        if (attackState != AttackState.Releasing) return;
        ShootArrow(shotVelocity);
        cooldownRemaining = cooldown;
        ChangeState(AttackState.Cooldown);
    }

    public bool TryExecute()
    {
        if (attackState != AttackState.Ready) return false;
        if (TryCancelAttack()) return false;
        if (!TryAimAtPredictedShot(out Vector3 launchVelocity)) return false;

        shotVelocity = launchVelocity;
        ChangeState(AttackState.Releasing);
        return true;
    }
}
