//using UnityEngine;
//using UnityEngine.AI;

//[RequireComponent(typeof(Enemy))]
//public class ChaseNavigator : MonoBehaviour
//{

//    private enum NavigationState
//    {
//        Stationary,
//        Chasing,
//        Retreating
//    }

//    [Header("References")]
//    public Transform player;
//    private NavMeshAgent agent;

//    [Header("Chase Settings")]
//    public float backupDistance = 0f; // the closest the enemy can be without needing to back up (for ranged enemies), default 0 is fine
//    public float stopDistance = 5f;
//    public float resumeDistance = 7f;

//    NavigationState state = NavigationState.Chasing;
//    int movementLockCount;

//    public bool IsMovementLocked => movementLockCount > 0;

//    void Awake()
//    {
//        agent = GetComponent<NavMeshAgent>();
//        agent.stoppingDistance = 0f; // ChaseNavigator controls when the agent should stop
//    }

//    private void OnEnable()
//    {
//        movementLockCount = 0;
//        state = NavigationState.Chasing;
//    }

//    private void OnDisable()
//    {
//        movementLockCount = 0;
//    }

//    // Start is called once before the first execution of Update after the MonoBehaviour is created
//    void Start()
//    {
//        if (player == null)
//        {
//            GameObject p = GameObject.FindGameObjectWithTag("Player");
//            if (p != null ) player = p.transform;
//        }
//    }

//    // Update is called once per frame
//    void Update()
//    {
//        if (IsMovementLocked) return;
//        if (player == null) return;
//        if (agent == null || !agent.isOnNavMesh) return;

//        float distance = Vector3.Distance(transform.position, player.position);

//        if (backupDistance > 0f && distance <= backupDistance) state = NavigationState.Retreating;
//        else if (distance >= resumeDistance) state = NavigationState.Chasing;
//        else if (state == NavigationState.Chasing && distance <= stopDistance) state = NavigationState.Stationary;
//        else if (state == NavigationState.Retreating && distance >= backupDistance) state = NavigationState.Stationary;

//        if (state == NavigationState.Chasing)
//        {
//            agent.updateRotation = true;
//            agent.isStopped = false;
//            agent.SetDestination(player.position);
//        }

//        if (state == NavigationState.Retreating)
//        {
//            agent.updateRotation = false;

//            Vector3 awayFromPlayer = transform.position - player.position;
//            awayFromPlayer.y = 0f;

//            if (awayFromPlayer.sqrMagnitude > 0.001f)
//            {
//                Vector3 retreatPosition = transform.position + awayFromPlayer.normalized * (backupDistance - distance);

//                agent.isStopped = false;
//                agent.SetDestination(retreatPosition);
//            }

//            FacePlayer();
//        }

//        if (state == NavigationState.Stationary)
//        {
//            agent.updateRotation = true;
//            agent.isStopped = true;
//            agent.velocity = Vector3.zero;
//        }
//    }

//    void FacePlayer()
//    {
//        Vector3 directionToPlayer = player.position - transform.position;
//        directionToPlayer.y = 0f;

//        if (directionToPlayer.sqrMagnitude <= 0.001f) return;

//        Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
//        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, agent.angularSpeed * Time.deltaTime);
//    }

//    public void AddMovementLock()
//    {
//        movementLockCount++;

//        if (agent == null || !agent.isOnNavMesh) return;

//        agent.ResetPath();
//        agent.isStopped = false;
//    }

//    public void RemoveMovementLock()
//    {
//        movementLockCount = Mathf.Max(0, movementLockCount - 1);

//        if (movementLockCount == 0)
//            ResumeAfterMovementLock();
//    }

//    private void ResumeAfterMovementLock()
//    {
//        if (agent == null || player == null) return;

//        float distance = Vector3.Distance(transform.position, player.position);
//        if (distance > stopDistance)
//            state = NavigationState.Chasing;
//    }

//}

using UnityEngine;

[RequireComponent(typeof(Enemy))]
[RequireComponent(typeof(NavMeshPathProvider))]
public class ChaseNavigator : MonoBehaviour
{
    private enum NavigationState { Stationary, Chasing, Retreating }

    [Header("References")]
    public Transform player;
    private NavMeshPathProvider path;

    [Header("Chase Settings")]
    public float backupDistance = 0f;
    public float stopDistance = 5f;
    public float resumeDistance = 7f;

    [Header("Movement")]
    [SerializeField] float moveSpeed = 3.5f;   // TODO: pull from Enemy stats
    [SerializeField] float turnSpeed = 360f;

    NavigationState state = NavigationState.Chasing;
    int movementLockCount;

    public bool IsMovementLocked => movementLockCount > 0;

    void Awake() => path = GetComponent<NavMeshPathProvider>();

    void OnEnable()
    {
        movementLockCount = 0;
        state = NavigationState.Chasing;
    }

    void OnDisable() => movementLockCount = 0;

    void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    void Update()
    {
        if (IsMovementLocked || player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (backupDistance > 0f && distance <= backupDistance) state = NavigationState.Retreating;
        else if (distance >= resumeDistance) state = NavigationState.Chasing;
        else if (state == NavigationState.Chasing && distance <= stopDistance) state = NavigationState.Stationary;
        else if (state == NavigationState.Retreating && distance >= backupDistance) state = NavigationState.Stationary;

        switch (state)
        {
            case NavigationState.Chasing:
                path.SetTarget(player.position);
                MoveAlongPath();
                FaceDirection(path.DesiredDirection);
                break;

            case NavigationState.Retreating:
                Vector3 away = transform.position - player.position;
                away.y = 0f;
                if (away.sqrMagnitude > 0.001f)
                {
                    path.SetTarget(transform.position + away.normalized * (backupDistance - distance));
                    MoveAlongPath();
                }
                FaceDirection(player.position - transform.position);
                break;

            case NavigationState.Stationary:
                FaceDirection(player.position - transform.position);
                break;
        }
    }

    void MoveAlongPath()
    {
        transform.position += path.DesiredDirection * moveSpeed * Time.deltaTime;
    }

    void FaceDirection(Vector3 dir)
    {
        dir.y = 0f;
        if (dir.sqrMagnitude <= 0.001f) return;
        Quaternion look = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, look, turnSpeed * Time.deltaTime);
    }

    public void AddMovementLock() => movementLockCount++;

    public void RemoveMovementLock()
    {
        movementLockCount = Mathf.Max(0, movementLockCount - 1);
        if (movementLockCount == 0) ResumeAfterMovementLock();
    }

    void ResumeAfterMovementLock()
    {
        if (player == null) return;
        if (Vector3.Distance(transform.position, player.position) > stopDistance)
            state = NavigationState.Chasing;
    }
}