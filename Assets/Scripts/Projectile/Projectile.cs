using System;
using UnityEngine;

// TODO: make projectile launch take in a owner combat system or owner object and have it use the combat system

/// <summary>
/// Pool-agnostic projectile. Carries a DamageInfo, flies forward, and on trigger
/// contact deals damage once then releases itself back to whatever pool spawned it.
/// It does NOT know about the pool directly � the spawner injects a release callback.
/// </summary>
public class Projectile : MonoBehaviour, IPoolable
{
    [Header("Core Settings")]
    [Tooltip("The lifetime max duration of a projectile.")]
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private LayerMask hittableLayers;
    [SerializeField] private int poolCapacity = 16;     // of base 2

    [Tooltip("Radius used for collision detection fixing")]
    [SerializeField, Min(0.001f)] private float sphereCastRadius;

    [Tooltip("DO NOT ENABLE THIS UNLESS YOU REALLY NEED TO USE A COLLIDER FOR SOME REASON. " +
        "LIKE FOR SOME ABSURED PROJECTILE SHAPE. SPHERECAST METHOD IS SO MUCH BETTER AND MORE " +
        "EFFECIENT AND MOST PROJECTILES WORK PERFECTLY WITH A SPHERECAST IF THEYRE SMALL")]
    [SerializeField] private bool usePhysicsCollision = false;
    
    [Header("Visuals")]
    [Tooltip("Spawned when hitting a valid target")]
    [SerializeField] private PooledVfx impactVFXPrefab;

    public int PoolCapacity => poolCapacity;

    private Combat _ownerCombat;
    private DamageInfo damageInfo;
    private float speed;
    private Action<Projectile> release;     // injected: "Put yourself back" FORCEEE!!!
    private float lifeTimer;
    private bool isSpent;                   // has it already hit something and dealt damage? I dunno *shrug*

    float launchedAt;
    float damageChangeDelay;
    float laterDamage;
    
    private void Awake()
    {
        Rigidbody body = GetComponent<Rigidbody>();
        Collider projectileCollider = GetComponent<Collider>();

        if (!usePhysicsCollision)
        {
            if (body != null)
            {
                body.isKinematic = true;
                body.useGravity = false;
                body.detectCollisions = false;
            }

            if (projectileCollider != null)
                projectileCollider.enabled = false;

            return;
        }

        if (body == null || projectileCollider == null)
        {
            Debug.LogError("[Projectile.cs] - Projectile physics collision fallback requires a Rigidbody and Collider.", this);
            usePhysicsCollision = false;
            return;
        }

        body.isKinematic = true;
        body.useGravity = false;
        body.detectCollisions = true;

        projectileCollider.enabled = true;
        projectileCollider.isTrigger = true;
    }

    /// <summary>Called once by the pool manager right after Instantiate. Wires the release path for this instance's lifetime.</summary>
    public void Initialize(Action<Projectile> releaseCallback)
    {
        release = releaseCallback;
    }
    
    static readonly Collider[] overlapBuffer = new Collider[8];
    
    /// make sure projectiles can't spawn inside walls
    private void CheckSpawnOverlap()
    {
        int hitCount = Physics.OverlapSphereNonAlloc(
            transform.position,
            sphereCastRadius,
            overlapBuffer,
            hittableLayers,
            QueryTriggerInteraction.Collide);

        for (int i = 0; i < hitCount; i++)
        {
            Collider hitCollider = overlapBuffer[i];

            if (hitCollider == null || ShouldIgnoreCollision(hitCollider))
                continue;

            Vector3 hitPoint = hitCollider.ClosestPoint(transform.position);
            HandleHit(hitCollider, hitPoint);
            return;
        }
    }


    /// <summary>Sets this shot's flight parameters. Pool lifecycle is already handled.</summary>
    public void Launch(Combat ownerCombat, Vector3 origin, Vector3 direction, float speed, DamageInfo info,
        float damageChangeDelay = -1f, float laterDamage = 0f)
    {
        _ownerCombat = ownerCombat;
        transform.position = origin;
        this.speed = speed;
        damageInfo = info;

        launchedAt = Time.time;
        this.damageChangeDelay = damageChangeDelay;
        this.laterDamage = laterDamage;

        if (direction != Vector3.zero)
        {
            transform.forward = direction;
        }

        CheckSpawnOverlap();
    }

    /// <summary>Called by the pool's actionOnGet, before the projectile is fired.</summary>
    public void OnSpawn()
    {
        isSpent = false;
        lifeTimer = lifetime;
    }

    /// <summary>Called by the pool's actionOnRelease.</summary>
    public void OnDespawn()
    {
        damageInfo = default;
        _ownerCombat = null;
    }

    private void Update()
    {
        if (isSpent) return;

        Vector3 direction = transform.forward;
        Vector3 startPosition = transform.position;
        float deltaDistance = speed * Time.deltaTime; // s=d/t

        if (deltaDistance > 0f && Physics.SphereCast(
                startPosition,
                sphereCastRadius,
                direction,
                out RaycastHit hitInfo,
                deltaDistance,
                hittableLayers,
                QueryTriggerInteraction.Collide))
        {
            // ignores collisions with owner/shooter or the projectile itself
            if (ShouldIgnoreCollision(hitInfo.collider))
            {
                transform.position = startPosition + direction * deltaDistance;
            }
            else
            {
                transform.position = startPosition + direction * hitInfo.distance;
                HandleHit(hitInfo.collider, hitInfo.point);
                return;
            }
        }
        else
        {
            // move forward if no hit is detected
            transform.position = startPosition + direction * deltaDistance;
        }
        
        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0f)
            Despawn();
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (!usePhysicsCollision || isSpent) return;
        if (ShouldIgnoreCollision(other)) return;

        Vector3 impactPoint = other.ClosestPoint(transform.position);
        HandleHit(other, impactPoint);
    }

    private void HandleHit(Collider other, Vector3 impactPoint)
    {
        if (isSpent) return;
        if ((hittableLayers.value & (1 << other.gameObject.layer)) == 0) return; // only use collisions with correct layers

        TryApplyDamage(other);
        PlayImpactVFX(impactPoint);

        Despawn();
    }

    private void TryApplyDamage(Collider other)
    {
        IDamageable target = other.GetComponentInParent<IDamageable>();
        if (target == null) return;

        if (_ownerCombat != null)
        {
            DamageInfo hitInfo = damageInfo; 
            float flightTime = Time.time - launchedAt; 
 
            if (damageChangeDelay >= 0f && flightTime >= damageChangeDelay) hitInfo.Amount = laterDamage;
            
            _ownerCombat.TryDamage(target, hitInfo);
        }
        else
        {
            Debug.LogError("[Projectile.cs] - Projectile was launched without an owner Combat.", this);
        }
    }

    private void PlayImpactVFX(Vector3 point)
    {
        if (impactVFXPrefab == null)
        {
            Debug.LogWarning("[Projectile.cs] - Projectile has no impactVFXPrefab assigned.", this);
            return;
        }
        VFXPoolManager.Instance.Play(impactVFXPrefab, point, Quaternion.identity);
    }

    // Helper to filter out shooter, trap spawner, or invalid targets.
    private bool ShouldIgnoreCollision(Collider col)
    {
        if (_ownerCombat != null && col.gameObject == _ownerCombat.gameObject)
            return true;

        if (col.gameObject == gameObject)
            return true;

        return false;
    }

    private void Despawn()
    {
        if (isSpent) return;
        isSpent = true;
        release?.Invoke(this);
    }

    private void OnDrawGizmosSelected()
    {
        if (isSpent) return;
        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(transform.position, sphereCastRadius);
    }
}
