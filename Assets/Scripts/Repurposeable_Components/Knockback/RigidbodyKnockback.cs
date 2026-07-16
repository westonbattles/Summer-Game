using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RigidbodyKnockback : MonoBehaviour, IKnockbackable, IPoolable
{
    [Tooltip("Higher values mean less knocback.")]
    [SerializeField] private float knockbackResistance = 1f;
    [SerializeField] private float knockbackImmunityDuration = 0.25f;
    private float immunityUntil;
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public bool ApplyKnockback(KnockbackInfo info)
    {
        if (Time.time < immunityUntil) return false;
        if (!info.HasDirection) return false;
        
        immunityUntil = Time.time + knockbackImmunityDuration;

        float resistance = Mathf.Max(0.001f, knockbackResistance);
        rb.WakeUp();
        rb.AddForce(info.Direction * (info.Strength / resistance), ForceMode.Impulse);
        return true;
    }

    public void OnSpawn()
    {
        ResetKnockbackState();
    }

    public void OnDespawn()
    {
        ResetKnockbackState();
    }

    private void ResetKnockbackState()
    {
        immunityUntil = 0f;

        if (rb == null) return;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }
}
