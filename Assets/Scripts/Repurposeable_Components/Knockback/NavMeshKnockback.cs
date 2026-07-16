using System;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NavMeshKnockback : MonoBehaviour, IKnockbackable
{
    [Tooltip("Higher values mean less knockback.")]
    [SerializeField] private float knockbackResistance = 1f;
    [SerializeField] private float knockbackImmunityDuration = 0.25f;
    [SerializeField] private bool decayVelocityOverDuration = true;
    private float immunityUntil;
    private float knockbackRemaining;
    private float knockbackDuration;
    private Vector3 knockbackVelocity;

    private bool hasMovementLock;
    private ChaseNavigator chaseNavigator;
    private NavMeshAgent agent;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        chaseNavigator = GetComponent<ChaseNavigator>();
        if (agent == null) Debug.LogError("NavMeshAgent component not found on " + gameObject.name);
        if (chaseNavigator == null) Debug.LogError("ChaseNavigator component not found on " + gameObject.name);
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
    
    public void ApplyKnockback(KnockbackInfo info)
    {
        if (Time.time < immunityUntil) return;
        if (info.Direction == Vector3.zero) return;
        
        immunityUntil = Time.time + knockbackImmunityDuration;

        float resistance = Mathf.Max(0.001f, knockbackResistance);

        knockbackDuration = Mathf.Max(0.01f, info.Duration);
        knockbackRemaining = knockbackDuration;
        knockbackVelocity = info.Direction * (info.Strength / resistance);

        BeginKnockback();
    }

    private void BeginKnockback()
    {
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
