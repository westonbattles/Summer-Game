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
        
        if (rb == null)
        {
            Debug.LogError("Rigidbody component not found on " + gameObject.name);
        }
    }

    public void ApplyKnockback(Vector3 source, float strength)
    {
        if (Time.time < immunityUntil)
        {
            return;
        }
        immunityUntil = Time.time + knockbackImmunityDuration;
        Vector3 direction = (transform.position - source);
        direction.y = 0;
        direction.Normalize();
        rb.AddForce(direction * strength / knockbackResistance, ForceMode.Impulse);
    }

    public void ApplyKnockback(KnockbackInfo info)
    {
        if (Time.time < immunityUntil) return;
        if (info.Direction == Vector3.zero) return;
        
        immunityUntil = Time.time + knockbackImmunityDuration;

        float resistance = Mathf.Max(0.001f, knockbackResistance); // no divide by zero
        rb.WakeUp(); // according to the docs, rbs won't always perform physics calculations to save on cpu. for example when it isn't moving
        rb.AddForce(info.Direction * (info.Strength / resistance), ForceMode.Impulse);
    }
}
