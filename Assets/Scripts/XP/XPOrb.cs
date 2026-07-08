using UnityEngine;

/// <summary>
/// A single XP pickup. Waits a beat (the particle burst covers this window), then
/// pursues the player with ramping speed and grants XP on arrival.
/// Pursuit, not interpolation: speed is authored, arrival time is emergent.
/// MoveTowards recomputes the full direction every frame (no persistent velocity),
/// so the orb adjusts to player movement in realtime, curves organically, and can
/// never orbit or snap - once orb speed exceeds player speed, the gap only shrinks.
/// </summary>
public class XPOrb : MonoBehaviour, IPoolable
{
    [Header("Flight")]
    [Tooltip("Beat before pursuit starts - the 'loot moment'. The particle burst covers this window.")]
    [SerializeField] private float flightDelay = 0.35f;
    [Tooltip("Speed when pursuit begins. Low = a lazy start that reads as 'noticing' the player.")]
    [SerializeField] private float startSpeed = 2f;
    [Tooltip("Speed gained per second. This is the 'vacuum' feel knob.")]
    [SerializeField] private float acceleration = 30f;
    [Tooltip("Must comfortably exceed player sprint speed, or orbs can be kited forever.")]
    [SerializeField] private float maxSpeed = 30f;

    [Header("Failsafe")]
    [Tooltip("Orbs self-despawn after this long, e.g. if maxSpeed is mis-tuned below player speed.")]
    [SerializeField] private float maxLifeTime = 10f;

    private float value;
    private float speed;
    private float age;
    private bool launched;
    private bool releaseToPoolOnDespawn;
    private PooledVfx pooledVfx;

    private void Awake()
    {
        pooledVfx = GetComponent<PooledVfx>();
    }

    /// <summary>Called by XPDropper immediately after spawn. Stamps the value and arms the pursuit.</summary>
    public void Launch(float xpValue, bool releaseToPool = false)
    {
        value = xpValue;
        age = 0f;
        speed = startSpeed;
        releaseToPoolOnDespawn = releaseToPool;
        launched = true;
    }

    private void Update()
    {
        if (!launched)
            return;

        age += Time.deltaTime;

        if (age < flightDelay)
            return;

        PlayerExperience player = PlayerExperience.Instance;
        if (player == null || age >= maxLifeTime)
        {
            Despawn();
            return;
        }

        speed = Mathf.Min(speed + acceleration * Time.deltaTime, maxSpeed);

        Vector3 target = player.CollectPoint.position;
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

        // MoveTowards clamps exactly onto the target on the final step.
        if ((transform.position - target).sqrMagnitude < 0.0001f)
        {
            player.AddXP(value);
            Despawn();
        }
    }

    private void Despawn()
    {
        launched = false;

        if (releaseToPoolOnDespawn && pooledVfx != null && VFXPoolManager.Instance != null)
        {
            VFXPoolManager.Instance.Release(pooledVfx);
            return;
        }

        Destroy(gameObject);
    }

    public void OnSpawn()
    {
        // Launch() re-stamps all flight state on reuse.
    }

    public void OnDespawn()
    {
        launched = false;
        releaseToPoolOnDespawn = false;
    }
}
