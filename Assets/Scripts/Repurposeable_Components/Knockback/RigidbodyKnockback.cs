using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RigidbodyKnockback : MonoBehaviour, IKnockbackable
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
}
