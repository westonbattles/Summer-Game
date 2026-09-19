using UnityEngine;

public class ElkMover : GroundMover
{
    [Header("Distance Settings")]
    [SerializeField] private float retreatDistance = 5f;

    [Header("Aim Settings")]
    [SerializeField] private float facingTolerance = 3f;

    private Transform target;
    private Vector3 aimDirection;
    private bool hasAimDirection;

    void Update()
    {
        if (IsMovementLocked || target == null) return;

        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;
        float distance = toTarget.magnitude;

        if (distance < retreatDistance)
        {
            Vector3 away = distance > 0.001f ? -toTarget / distance : -transform.forward;
            Path.SetTarget(transform.position + away * (retreatDistance - distance));
            MoveAlongPath();
        }

        FaceDirection(hasAimDirection ? aimDirection : Path.DesiredDirection);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (target == null) ClearAimDirection();
    }

    public void SetAimDirection(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.0001f) return;
        aimDirection = direction.normalized;
        hasAimDirection = true;
    }

    public void ClearAimDirection() => hasAimDirection = false;

    public bool IsFacingDirection(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.0001f) return false;

        float requiredDot = Mathf.Cos(facingTolerance * Mathf.Deg2Rad);
        return Vector3.Dot(transform.forward, direction.normalized) >= requiredDot;
    }
}
