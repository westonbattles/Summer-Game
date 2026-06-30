using System;
using UnityEngine;

/// <summary>
/// Pool-agnostic projectile. Carries a DamageInfo, flies forward, and on trigger
/// contact deals damage once then releases itself back to whatever pool spawned it.
/// It does NOT know about the pool directly � the spawner injects a release callback.
/// </summary>
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]       // make sure its kinematic
public class Projectile : MonoBehaviour
{
    [Header("Core Settings")]
    [Tooltip("The lifetime max duration of a projectile.")]
    [SerializeField] private float lifetime = 5f;   // it will despawn if it hits nothing
    [SerializeField] private LayerMask hittableLayers;

    [Header("Visuals")]
    [Tooltip("The visual child object containing the LineRenderer or Trail")]
    [SerializeField] private GameObject visualBody;
    [Tooltip("Spawned when hitting a valid target")]
    [SerializeField] private GameObject impactVFXPrefab;

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

        // object pooling will disable the visual body when the projectile is released, so we need to re-enable it here
        if (visualBody != null)
            visualBody.SetActive(true);
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
        if (visualBody != null)
            visualBody.SetActive(false); // hide the visual body before returning to pool
        release?.Invoke(this);
    }
}
