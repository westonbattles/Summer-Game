using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Enemy))]
public class ChaseNavigator : MonoBehaviour
{

    private enum NavigationState
    {
        Stationary,
        Chasing,
        Retreating
    }

    [Header("References")]
    public Transform player;
    private NavMeshAgent agent;

    [Header("Chase Settings")]
    public float backupDistance = 0f; // the closest the enemy can be without needing to back up (for ranged enemies), default 0 is fine
    public float stopDistance = 5f;
    public float resumeDistance = 7f;
    private NavigationState state = NavigationState.Stationary;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.stoppingDistance = 0f; // ChaseNavigator controls when the agent should stop

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null ) player = p.transform;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (backupDistance > 0f && distance <= backupDistance)
        {
            state = NavigationState.Retreating;
        }
        else if (distance >= resumeDistance)
        {
            state = NavigationState.Chasing;
        }
        else if (state == NavigationState.Chasing && distance <= stopDistance)
        {
            state = NavigationState.Stationary;
        }
        else if (state == NavigationState.Retreating && distance >= backupDistance)
        {
            state = NavigationState.Stationary;
        }

        if (state == NavigationState.Chasing)
        {
            agent.updateRotation = true;
            agent.isStopped = false;
            agent.SetDestination(player.position);
        }

        if (state == NavigationState.Retreating)
        {
            agent.updateRotation = false;

            Vector3 awayFromPlayer = transform.position - player.position;
            awayFromPlayer.y = 0f;

            if (awayFromPlayer.sqrMagnitude > 0.001f)
            {
                Vector3 retreatPosition = transform.position + awayFromPlayer.normalized * (backupDistance - distance);

                agent.isStopped = false;
                agent.SetDestination(retreatPosition);
            }

            FacePlayer();
        }

        if (state == NavigationState.Stationary)
        {
            agent.updateRotation = true;
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }
    }

    void FacePlayer()
    {
        Vector3 directionToPlayer = player.position - transform.position;
        directionToPlayer.y = 0f;

        if (directionToPlayer.sqrMagnitude <= 0.001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, agent.angularSpeed * Time.deltaTime);
    }
}
