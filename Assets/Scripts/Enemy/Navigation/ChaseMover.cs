using UnityEngine;

public class ChaseMover : GroundMover
{
    private enum NavigationState { Stationary, Chasing, Retreating }

    [Header("References")]
    public Transform player;

    [Header("Chase Settings")]
    public float backupDistance = 0f;
    public float stopDistance = 5f;
    public float resumeDistance = 7f;

    NavigationState state = NavigationState.Chasing;

    protected override void OnEnable()
    {
        base.OnEnable();
        state = NavigationState.Chasing;
    }

    private void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    private void Update()
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
                Path.SetTarget(player.position);
                MoveAlongPath();
                FaceDirection(Path.DesiredDirection);
                break;

            case NavigationState.Retreating:
                Vector3 away = transform.position - player.position;
                away.y = 0f;
                if (away.sqrMagnitude > 0.001f)
                {
                    Path.SetTarget(transform.position + away.normalized * (backupDistance - distance));
                    MoveAlongPath();
                }
                FaceDirection(player.position - transform.position);
                break;

            case NavigationState.Stationary:
                FaceDirection(player.position - transform.position);
                break;
        }
    }

    // Return to chasing the player
    protected override void OnMovementUnlocked()
    {
        if (player == null) return;
        if (Vector3.Distance(transform.position, player.position) > stopDistance)
            state = NavigationState.Chasing;
    }
}
