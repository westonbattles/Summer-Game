using UnityEngine;

public class NavMeshKnockback : MonoBehaviour, IKnockbackable, IPoolable
{
    [Tooltip("Higher values mean less knockback.")]
    [SerializeField] private float knockbackResistance = 1f;
    [SerializeField] private float knockbackImmunityDuration = 0.25f;
    [SerializeField] private float defaultKnockbackDuration = 0.25f;
    [SerializeField] private bool decayVelocityOverDuration = true;

    private float immunityUntil;
    private float knockbackRemaining;
    private float knockbackDuration;
    private Vector3 knockbackVelocity;
    private bool hasMovementLock;

    private GroundMover chaseNavigator;

    private void Awake()
    {
        chaseNavigator = GetComponent<GroundMover>();
    }

    private void Update()
    {
        if (knockbackRemaining <= 0f) return;

        float fraction = decayVelocityOverDuration ? Mathf.Clamp01(knockbackRemaining / knockbackDuration) : 1f;
        transform.position += knockbackVelocity * fraction * Time.deltaTime;

        knockbackRemaining -= Time.deltaTime;
        if (knockbackRemaining <= 0f) EndKnockback();
    }

    public bool ApplyKnockback(KnockbackInfo info)
    {
        if (Time.time < immunityUntil) return false;
        if (!info.HasDirection) return false;

        immunityUntil = Time.time + knockbackImmunityDuration;

        float resistance = Mathf.Max(0.001f, knockbackResistance);
        float duration = info.Duration > 0f ? info.Duration : defaultKnockbackDuration;

        knockbackDuration = Mathf.Max(0.01f, duration);
        knockbackRemaining = knockbackDuration;
        knockbackVelocity = info.Direction * (info.Strength / resistance);

        BeginKnockback();
        return true;
    }

    private void BeginKnockback()
    {
        if (hasMovementLock) return;
        chaseNavigator?.AddMovementLock();
        hasMovementLock = true;
    }

    private void EndKnockback()
    {
        knockbackRemaining = 0f;
        knockbackVelocity = Vector3.zero;

        if (!hasMovementLock) return;
        chaseNavigator?.RemoveMovementLock();
        hasMovementLock = false;
    }

    private void OnDisable() => ResetKnockbackState();
    public void OnSpawn() => ResetKnockbackState();
    public void OnDespawn() => ResetKnockbackState();

    private void ResetKnockbackState()
    {
        immunityUntil = 0f;
        EndKnockback();
    }
}