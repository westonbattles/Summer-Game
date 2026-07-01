using System;
using UnityEngine;

/// <summary>
/// Pool-agnostic projectile. Carries a DamageInfo, flies forward, and on trigger
/// contact deals damage once then releases itself back to whatever pool spawned it.
/// It does NOT know about the pool directly � the spawner injects a release callback.
/// </summary>
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]       // make sure its kinematic
public class Projectile : MonoBehaviour, IPoolable
{
    [Header("Core Settings")]
    [Tooltip("The lifetime max duration of a projectile.")]
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private LayerMask hittableLayers;
    [SerializeField] private int poolCapacity = 16;     // of base 2

    [Header("Visuals")]
    [Tooltip("Spawned when hitting a valid target")]
    [SerializeField] private GameObject impactVFXPrefab;

    public int PoolCapacity => poolCapacity;

    private DamageInfo damageInfo;
    private float speed;
    private Action<Projectile> release;     // injected: "Put yourself back" FORCEEE!!!
    private float lifeTimer;
    private bool isSpent;                   // has it already hit something and dealt damage? I dunno *shrug*

    void Awake()
    {
        Rigidbody body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false; // redundant 

        GetComponent<Collider>().isTrigger = true;
    }

    /// <summary>Called once by the pool manager right after Instantiate. Wires the release path for this instance's lifetime.</summary>
    public void Initialize(Action<Projectile> releaseCallback)
    {
        release = releaseCallback;
    }

    /// <summary>Sets this shot's flight parameters. Pool lifecycle is already handled.</summary>
    public void Launch(Vector3 origin, Vector3 direction, float speed, DamageInfo info)
    {
        transform.position = origin;
        this.speed = speed;
        damageInfo = info;
        transform.forward = direction;
    }
    
    /// <summary>Sets this shot's flight parameters. Pool lifecycle is already handled.</summary>
    public void Launch(Vector3 direction, float speed, DamageInfo info)
    {
        this.speed = speed;
        damageInfo = info;
        transform.forward = direction;
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
    }

    private void Update()
    {
        transform.position += transform.forward * (speed * Time.deltaTime);

        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0f)
            Despawn();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isSpent) return;
        if ((hittableLayers.value & (1 << other.gameObject.layer)) == 0) return; // only use collisions with correct layers

        if (other.TryGetComponent<IDamageable>(out var target))
            target.TakeDamage(damageInfo);

        // spawn the hit projectiles. Instantiate isn't great but it's fine for our V-slice
        if (impactVFXPrefab == null)
            Debug.LogWarning("Projectile has no impactVFXPrefab assigned.");
        else
            Instantiate(impactVFXPrefab, transform.position, Quaternion.identity);

        Despawn();
    }

    /// <summary>
    /// Marks this projectile as spent and invokes the release callback to return it to the pool.
    /// </summary>
    private void Despawn()
    {
        if (isSpent) return;
        isSpent = true;
        release?.Invoke(this);
    }
}
