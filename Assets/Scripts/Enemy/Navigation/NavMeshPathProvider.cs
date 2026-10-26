using UnityEngine;
using UnityEngine.AI;

public class NavMeshPathProvider : MonoBehaviour
{
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private float repathInterval = 0.25f;
    [SerializeField, Min(0f)] private float repathJitter = 0.05f;

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

    public void SetTarget(Vector3 t, bool forceRepath) => SetTarget(t, forceRepath, false);

    public void SetTarget(Vector3 t, bool forceRepath, bool clearPath)
    {
        target = t;
        if (!forceRepath) return;

        if (agent.isOnNavMesh && (clearPath || Vector3.Dot(agent.desiredVelocity, target - transform.position) <= 0f))
            agent.ResetPath();
        nextRepath = 0f;
    }

    private void Update()
    {
        if (agent.isOnNavMesh && !agent.pathPending && Time.time >= nextRepath)
        {
            // stagger by a small amount so it doesn't move so robotically. Lemurians do this when they walk a little diagonally
            nextRepath = Time.time + repathInterval + Random.value * repathJitter;
            agent.SetDestination(target);
        }
    }

    private void LateUpdate()
    {
        if (!agent.isOnNavMesh) return;

        // this keeps the agent glued to the navmesh
        agent.nextPosition = transform.position;
        transform.position = agent.nextPosition;
    }

    public void Move(Vector3 offset)
    {
        if (!agent.isOnNavMesh) return;

        agent.nextPosition = transform.position;
        agent.Move(offset);
        transform.position = agent.nextPosition;
    }

    public Vector3 DesiredVelocity => agent.isOnNavMesh && agent.hasPath
        ? Vector3.ProjectOnPlane(agent.desiredVelocity, Vector3.up)
        : Vector3.zero;

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
