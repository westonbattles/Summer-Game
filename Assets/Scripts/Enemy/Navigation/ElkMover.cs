using UnityEngine;

public class ElkMover : GroundMover
{
    [Header("Retreat Settings")]
    [SerializeField] float retreatDistance = 18f;
    [SerializeField] float maxRetreatAngle = 20f;
    [SerializeField, Min(0f)] float retreatAngleSpeed = 20f;
    [SerializeField, Min(0f)] float retreatDistanceBuffer = 0.5f;
    [SerializeField, Min(0.01f)] float movementSmoothTime = 0.12f;

    [Header("Aim Settings")]
    [SerializeField] float facingTolerance = 3f;
    [SerializeField, Range(0f, 180f)] float retreatStopFacingTolerance = 15f;

    Transform target;
    Vector3 aimDirection;
    bool hasAimDirection;

    float retreatAngle;
    float currentRetreatAngle;
    float lastTurnDirection;
    bool isRetreating;
    Vector3 movementVelocity;
    Vector3 velocityDamping;

    protected override void OnEnable()
    {
        base.OnEnable();
        isRetreating = false;
        retreatAngle = 0f;
        currentRetreatAngle = 0f;
        lastTurnDirection = 0f;
        movementVelocity = Vector3.zero;
        velocityDamping = Vector3.zero;
    }

    void Update()
    {
        if (IsMovementLocked || target == null)
        {
            isRetreating = false;
            currentRetreatAngle = 0f;
            movementVelocity = Vector3.zero;
            velocityDamping = Vector3.zero;
            return;
        }

        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;
        float distance = toTarget.magnitude;

        Vector3 facingDirection = hasAimDirection ? aimDirection : toTarget;
        Vector3 previousForward = transform.forward;
        FaceDirection(facingDirection);

        if (!isRetreating || retreatAngle == 0f)
        {
            float turn = Vector3.SignedAngle(previousForward, transform.forward, Vector3.up);
            if (Mathf.Abs(turn) > 0.001f) lastTurnDirection = Mathf.Sign(turn);
        }
        
        // only move backwards if we are facing the player and we are too close
        float stopDistance = retreatDistance + (isRetreating ? retreatDistanceBuffer : 0f);
        if (distance >= stopDistance)
        {
            isRetreating = false;
            currentRetreatAngle = 0f;
            MoveSmoothly(Vector3.zero);
            return;
        }
        
        float movementFacingTolerance = isRetreating ? Mathf.Max(facingTolerance, retreatStopFacingTolerance) : facingTolerance;
        if (!IsFacingDirection(facingDirection, movementFacingTolerance))
        {
            isRetreating = false;
            MoveSmoothly(Vector3.zero);
            return;
        }
 
        bool startingRetreat = !isRetreating;
        bool changingRetreatSide = false;
        if (startingRetreat || retreatAngle == 0f)
        {
            float nextRetreatAngle = maxRetreatAngle * (lastTurnDirection == 0f ? 1f : lastTurnDirection);
            changingRetreatSide = retreatAngle * nextRetreatAngle < 0f;
            if (changingRetreatSide || Mathf.Abs(currentRetreatAngle) < 0.001f) currentRetreatAngle = nextRetreatAngle * 0.25f;
            retreatAngle = nextRetreatAngle;
            isRetreating = true;
        }

 
        // recalculate away from the player's current position every frame
        Vector3 away = distance > 0.001f ? -toTarget / distance : -transform.forward; 
        // keep the saved angle relative to that moving vector
        Vector3 retreatDirection = Quaternion.AngleAxis(currentRetreatAngle, Vector3.up) * away;
 
        Path.SetTarget(transform.position + retreatDirection * Mathf.Max(2f, stopDistance - distance), forceRepath: startingRetreat, clearPath: changingRetreatSide);
        
        // only move when the path actually points away from the player (nav mesh set target takes a bit)
        Vector3 pathVelocity = Vector3.ClampMagnitude(Path.DesiredVelocity, moveSpeed);
        float alignment = Mathf.InverseLerp(Mathf.Max(facingTolerance + 0.01f, retreatStopFacingTolerance), facingTolerance, Vector3.Angle(transform.forward, facingDirection));
        pathVelocity *= Mathf.SmoothStep(0f, 1f, alignment);
        if (Vector3.Dot(pathVelocity, away) <= 0f) pathVelocity = Vector3.zero;

        
        if (MoveSmoothly(pathVelocity)) currentRetreatAngle = Mathf.MoveTowards(currentRetreatAngle, retreatAngle, retreatAngleSpeed * Time.deltaTime);
    }

    bool MoveSmoothly(Vector3 desiredVelocity)
    {
        if (Time.deltaTime <= 0f) return false;

        movementVelocity = Vector3.SmoothDamp(movementVelocity, desiredVelocity, ref velocityDamping, movementSmoothTime, Mathf.Infinity, Time.deltaTime);
        Vector3 previousPosition = transform.position;
        Path.Move(movementVelocity * Time.deltaTime);
        return (transform.position - previousPosition).sqrMagnitude > 0f;
    }

    public void SetTarget(Transform newTarget)
    {
        if (target != newTarget)
        {
            isRetreating = false;
            currentRetreatAngle = 0f;
            retreatAngle = 0f;
            lastTurnDirection = 0f;
            movementVelocity = Vector3.zero;
            velocityDamping = Vector3.zero;
        } 
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
        return IsFacingDirection(direction, facingTolerance);
    }

    bool IsFacingDirection(Vector3 direction, float tolerance)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.0001f) return false;

        float requiredDot = Mathf.Cos(tolerance * Mathf.Deg2Rad);
        return Vector3.Dot(transform.forward, direction.normalized) >= requiredDot;
    }
}
