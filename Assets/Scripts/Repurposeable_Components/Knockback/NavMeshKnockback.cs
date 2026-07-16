using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NavMeshKnockback : MonoBehaviour, IKnockbackable
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
    private bool hasStoredUpdateRotation;
    private bool previousUpdateRotation;
    private ChaseNavigator chaseNavigator;
    private NavMeshAgent agent;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        chaseNavigator = GetComponent<ChaseNavigator>();
    }

    public void Update()
    {
        if (knockbackRemaining <= 0f) return;

        float fraction = decayVelocityOverDuration ? Mathf.Clamp01(knockbackRemaining / knockbackDuration) : 1f;
        agent.isStopped = false;
        agent.velocity = knockbackVelocity * fraction; // decay velocity

        knockbackRemaining -= Time.deltaTime;

        if (knockbackRemaining <= 0f) EndKnockback();
    }
    
    public bool ApplyKnockback(KnockbackInfo info)
    {
        if (Time.time < immunityUntil) return false;
        if (!info.HasDirection) return false;
        if (agent == null || !agent.isOnNavMesh) return false;
        
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
        if (!hasStoredUpdateRotation)
        {
            previousUpdateRotation = agent.updateRotation;
            hasStoredUpdateRotation = true;
        }

        agent.updateRotation = false;

        if (!hasMovementLock)
        {
            chaseNavigator?.AddMovementLock();
            hasMovementLock = true;
        }
        
        agent.ResetPath();
        agent.isStopped = false;
    }

    private void EndKnockback()
    {
        knockbackRemaining = 0f;
        knockbackVelocity = Vector3.zero;

        if (agent != null) agent.velocity = Vector3.zero;
        if (agent != null && hasStoredUpdateRotation)
        {
            agent.updateRotation = previousUpdateRotation;
            hasStoredUpdateRotation = false;
        }

        if (hasMovementLock)
        {
            chaseNavigator?.RemoveMovementLock();
            hasMovementLock = false;
        }
    }

    private void OnDisable()
    {
        EndKnockback();
    }
}
