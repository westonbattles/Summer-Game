using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Enemy))]
public class ChaseNavigator : MonoBehaviour
{

    [Header("References")]
    public Transform player;
    private NavMeshAgent agent;

    [Header("Chase Settings")]
    public float stopDistance = 5f;
    public float resumeDistance = 7f;
    private bool isChasing = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

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

        if (isChasing)
        {
            if (distance <= stopDistance)
            {
                isChasing = false;
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }
        }
        else
        {
            if (distance >= resumeDistance)
            {
                isChasing = true;
                agent.isStopped = false;
            }
        }

        if (isChasing)
        {
            agent.SetDestination(player.position);
        }
    }
}
