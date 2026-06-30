using System;
using UnityEngine;

/// <summary>
/// Pool-agnostic projectile. Carries a DamageInfo, flies forward, and on trigger
/// contact deals damage once then releases itself back to whatever pool spawned it.
/// It does NOT know about the pool directly � the spawner injects a release callback.
/// </summary>
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    [Tooltip("The lifetime max duration of a projectile.")]
    [SerializeField] private float lifetime = 5f;   // it will despawn if it hits nothing
    [SerializeField] private LayerMask hittableLayers;

    private DamageInfo damageInfo;
    private float speed;
    private Action<Projectile> release;     // injected: "Put yourself back" FORCEEE!!!
    private float lifeTimer;
    private bool isSpent;                   // has it already hit something and dealt damage? I dunno *shrug*

    /// <summary>
    /// Called by the spawner right after the pool hands out this projectile.
    /// </summary>
    public void Launch(Vector3 direction, float speed, DamageInfo info, Action<Projectile> releaseCallback)
    {
        this.speed = speed;
        damageInfo = info;
        release = releaseCallback;
        lifeTimer = lifetime;
        isSpent = false;

        transform.forward = direction;
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

        // Layer filter: ignore anything not on a hittable layer.
        // not sure if walls should be handled separately? maybe we want different SFX or something. for now, just ignore them.
        if ((hittableLayers.value & (1 << other.gameObject.layer)) == 0)
            return;

        if (other.TryGetComponent<IDamageable>(out var target))
            target.TakeDamage(damageInfo);

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
