using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NavMeshKnockback : MonoBehaviour, IKnockbackable
{
    [Tooltip("Higher values mean less knockback.")]
    [SerializeField] private float knockbackResistance = 1f;
    [SerializeField] private float knockbackImmunityDuration = 0.25f;
    private float immunityUntil;
    private NavMeshAgent agent;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError("NavMeshAgent component not found on " + gameObject.name);
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
        agent.velocity = direction * (strength / knockbackResistance);
    }
}
