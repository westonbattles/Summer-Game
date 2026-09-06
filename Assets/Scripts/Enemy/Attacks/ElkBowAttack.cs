using System;
using UnityEngine;

enum AttackState
{
    Idle,
    IdleAggro, // player in range but behind a wall or something
    Charging,
    Ready,
    Releasing,
    Cooldown
}

[RequireComponent(typeof(Combat))]
[RequireComponent(typeof(EnemyAttackController))]
public class ElkBowAttack : MonoBehaviour, IEnemyAttackWithIndicator
{
    [SerializeField] DamageType damageType = DamageType.Projectile;
    [SerializeField] Projectile projectilePrefab;
    //[SerializeField] GameObject/Indicator indicatorPrefab;
    [SerializeField] Transform firePoint;
    [SerializeField] float damage = 8f;
    [SerializeField] float chargeTime = 1.5f;
    [SerializeField] float cooldown = 1.5f;
    [SerializeField] float maxRange = 12f;
    [SerializeField] float projectileSpeed = 10f;

    Combat ownerCombat;
    Transform target;
    Player targetPlayer;
    AttackState attackState;

    Vector3 shotVelocity;
    
    float chargeRemaining;
    float cooldownRemaining;
    
    // need to be used eventually
    public event Action<GameObject> showIndicator;
    public event Action hideIndicator;
    
    void Awake()
    {
        attackState = AttackState.Idle;
        
        ownerCombat = GetComponent<Combat>();
        if (ownerCombat == null) Debug.LogError($"Could not find combat script on gameobject: {gameObject}");
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        targetPlayer = newTarget != null ? newTarget.GetComponentInParent<Player>() : null;
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
        else if (state == AttackState.Charging) chargeRemaining = chargeTime;
        else if (state == AttackState.Releasing) ReleaseArrowAnimationEvent(); // this will eventually be triggered in an actual animation just doing it manual for rn until we have an animation
    }

    void ExitState(AttackState state)
    {
        if (state == AttackState.Idle) RaiseBow();
    }

    void Update()
    {
        // if (attackState == AttackState.Idle) IdleUpdate();
        // else if (attackState == AttackState.Charging) ChargingUpdate();
        // else if (attackState == AttackState.Ready) ReadyUpdate();
        // else if (attackState == AttackState.Cooldown) CooldownUpdate();

        switch (attackState)
        {
            case AttackState.Idle: IdleUpdate(); break;
            case AttackState.IdleAggro: IdleAggroUpdate(); break;
            case AttackState.Charging: ChargingUpdate(); break;
            case AttackState.Ready: ReadyUpdate(); break;
            case AttackState.Cooldown: CooldownUpdate(); break;
        }

        if (cooldownRemaining > 0f) cooldownRemaining -= Time.deltaTime;

        // handle state windup charge
    }

    bool HasLineOfSite()
    {
        return true; // todo, impl
    }

    void IdleUpdate()
    {
        // if the player gets back into range while we're idle, we'll go into cooldown state or charging state depending on if the cooldown is still active
        if (Vector3.Distance(transform.position, target.position) <= maxRange)
        {
            if (HasLineOfSite())
            {
                if (cooldownRemaining > 0f) ChangeState(AttackState.Cooldown);
                else ChangeState(AttackState.Charging);
            }
            else
            {
                ChangeState(AttackState.IdleAggro);
            }
        }
    }

    void IdleAggroUpdate()
    {
        if (Vector3.Distance(transform.position, target.position) > maxRange)
        {
            ChangeState(AttackState.Idle);
            return;
        }

        if (HasLineOfSite())
        {
            if (cooldownRemaining > 0f) ChangeState(AttackState.Cooldown);
            else ChangeState(AttackState.Charging);
        }
    }
    
    void ChargingUpdate()
    {
        // cancel the attack if we lose line of site or if player goes out of range
        if (TryCancelAttack()) return;
        
        AimAtPredictedShot();
        
        chargeRemaining -= Time.deltaTime;
        if (chargeRemaining <= 0) ChangeState(AttackState.Ready);
    }

    void ReadyUpdate()
    {
        // cancel the attack if we lose line of site or if player goes out of range
        if (TryCancelAttack()) return;
        
        AimAtPredictedShot();
    }

    void CooldownUpdate()
    {
        // cancel the attack if we lose line of site or if player goes out of range
        if (TryCancelAttack()) return;
        
        
        AimAtPredictedShot();
        
        // start charging when cooldown ends
        if (cooldownRemaining <= 0) ChangeState(AttackState.Charging);
    }

    bool TryCancelAttack()
    {
        if (Vector3.Distance(transform.position, target.position) > maxRange)
        {
            ChangeState(AttackState.Idle);
            return true;
        }
        if (!HasLineOfSite())
        {
            ChangeState(AttackState.IdleAggro);
            return true;
        }

        return false;
    }
    
    void AimAtPredictedShot()
    {
        
    }
    
    void RaiseBow()
    {
        // raise bow animation
    }

    void LowerBow()
    {
        // lower bow animation
    }

    bool TryAimAtPredictedShot(out Vector3 launchVelocity)
    {
        launchVelocity = Vector3.zero;
        
        // if a valid shot exists, we're going to rotate to align ourselves with that shot
        if (!TryCalculateShot(out launchVelocity)) return false;
        navigator.SetAimDirection(launchVelocity);
        if (!navigator.IsFacingDirection(launchVelocity)) return false;
        
        return true;
    }

    bool TryCalculateShot(out Vector3 launchVelocity)
    {

        launchVelocity = Vector3.zero;
        
        Vector3 targetPosition = target.position + Vector3.up; // manuall offset for chest, we could have Player.getcenter function later maybe

        Vector3 targetVelocity = Vector3.zero;

        if (targetPlayer != null)
        {
            // downward projectiong to ignore jumping leading calculations since jumps arent gonna last that long anyways
            targetVelocity = Vector3.ProjectOnPlane(targetPlayer.Motor.Velocity, Vector3.up);
        }

        Vector3 firePointToTarget = targetPosition - firePoint.position;
        float speedSquared = projectileSpeed * projectileSpeed;

        float a = targetVelocity.sqrMagnitude - speedSquared;
        float b = 2f * Vector3.Dot(firePointToTarget, targetVelocity);
        float c = firePointToTarget.sqrMagnitude;

        float interceptTime;

        float epsilon = 0.0001f;
        if (Math.Abs(a) < epsilon)
        {
            if (Math.Abs(b) < epsilon) return false; // player is moving away faster than the shot can reach
            interceptTime = -c / b;
            if (interceptTime <= epsilon) return false;
        }
        else
        {
            // discriminant - https://en.wikipedia.org/wiki/Discriminant
            float discriminant = b * b - 4f * a * c;
            if (discriminant < 0f) return false;

            float squareRoot = Mathf.Sqrt(discriminant);
            float denominator = 2f * a;
            float timeA = (-b - squareRoot) / denominator;
            float timeB = (-b + squareRoot) / denominator;

            // use the smaller (valid) time
            interceptTime = Mathf.Infinity;
            if (timeA > 0 && timeB > 0) interceptTime = Mathf.Min(timeA, timeB);
            else if (timeA > 0) interceptTime = timeA;
            else if (timeB > 0) interceptTime = timeB;
            
            if (float.IsPositiveInfinity(interceptTime)) return false;
        }

        Vector3 predictedPosition = targetPosition + targetVelocity * interceptTime;
        Vector3 direction = predictedPosition - firePoint.position;

        if (direction.sqrMagnitude <= epsilon) return false;
        
        launchVelocity = direction.normalized * projectileSpeed;
        
        return true;
    }
    
    
    // this will eventually be the animation event for our shoot animation to call to shoot our arrow:
    public void ReleaseArrowAnimationEvent()
    {
        if (attackState != AttackState.Releasing) return;
        ShootArrow(shotVelocity);
        ChangeState(AttackState.Cooldown);
        cooldownRemaining = cooldown;
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
    
    // do not need cancel.
}
