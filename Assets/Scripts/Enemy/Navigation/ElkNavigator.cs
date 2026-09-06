using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Enemy))]
[RequireComponent(typeof(NavMeshAgent))]
public class ElkNavigator : MonoBehaviour
{
    [Header("Distance Settings")]
    [SerializeField] float retreatDistance = 5f;
    [SerializeField] float approachDistance = 9f;
    [SerializeField] float aggroRange = 14f;

    [Header("Aim Settings")]
    [SerializeField] float facingTolerance = 3f;

    NavMeshAgent agent;
    Transform target;
    Vector3 aimDirection;
    bool hasAimDirection;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.stoppingDistance = 0f;
    }

    void Update()
    {
        if (target == null || !agent.isOnNavMesh) return;

        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;
        float distance = toTarget.magnitude;

        agent.updateRotation = !hasAimDirection;

        if (distance > aggroRange) StopMoving();
        else if (distance < retreatDistance) RetreatFromTarget(toTarget, distance);
        else if (distance > approachDistance) MoveToTarget();
        else StopMoving();

        if (hasAimDirection) FaceAimDirection();
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (target != null) return;

        ClearAimDirection();
        if (agent.isOnNavMesh) StopMoving();
    }

    public void SetAimDirection(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.0001f) return;

        aimDirection = direction.normalized;
        hasAimDirection = true;
    }

    public void ClearAimDirection()
    {
        hasAimDirection = false;
    }

    public bool IsFacingDirection(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.0001f) return false;

        float requiredDot = Mathf.Cos(facingTolerance * Mathf.Deg2Rad);
        return Vector3.Dot(transform.forward, direction.normalized) >= requiredDot;
    }

    void MoveToTarget()
    {
        agent.isStopped = false;
        agent.SetDestination(target.position);
    }

    void RetreatFromTarget(Vector3 toTarget, float distance)
    {
        Vector3 awayFromTarget = distance > 0.001f ? -toTarget / distance : -transform.forward;
        Vector3 destination = transform.position + awayFromTarget * (retreatDistance - distance);

        agent.isStopped = false;
        agent.SetDestination(destination);
    }

    void StopMoving()
    {
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
    }

    void FaceAimDirection()
    {
        Quaternion targetRotation = Quaternion.LookRotation(aimDirection, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, agent.angularSpeed * Time.deltaTime);
    }
}
