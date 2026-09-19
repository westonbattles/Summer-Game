using UnityEngine;
using UnityEngine.AI;

public class NavMeshPathProvider : MonoBehaviour
{
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private float repathInterval = 0.25f;

    private Vector3 target;
    private float nextRepath;

    private void Awake()
    {
        agent.updatePosition = false;
        agent.updateRotation = false;
    }

    private void OnEnable()
    {
        // prevents snapping when the entity is recylced from a pool.
        agent.Warp(transform.position);
        nextRepath = 0f;
    }

    public void SetTarget(Vector3 t) => target = t;

    private void Update()
    {
        if (Time.time >= nextRepath)
        {
            // stagger by a small amount so it doesn't move so robotically. Lemurians do this when they walk a little diagonally
            nextRepath = Time.time + repathInterval + Random.value * 0.05f;
            agent.SetDestination(target);
        }
        // this keeps the agent glued to the navmesh
        agent.nextPosition = transform.position;
        transform.position = agent.nextPosition;
    }

    // gets the desired direction of the target from the agent as a Vector3. Zero is no path.
    public Vector3 DesiredDirection
    {
        get
        {
            if (!agent.isOnNavMesh || !agent.hasPath) return Vector3.zero;

            Vector3 v = agent.desiredVelocity;
            v.y = 0f;
            return v.sqrMagnitude > 0.0001f ? v.normalized : Vector3.zero;
        }
    }

    public bool HasArrived =>
        agent.hasPath && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance;
}
