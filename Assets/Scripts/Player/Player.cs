using KinematicCharacterController;
using UnityEngine;

[RequireComponent(typeof(KinematicCharacterMotor))]
[RequireComponent(typeof(PlayerInputReader))]
public class Player : MonoBehaviour, ICharacterController
{
    [Header("Input")]
    [SerializeField] bool autoBhop;

    [Header("Movement")]
    [SerializeField] float gravity = 40f;
    [SerializeField] float gravityMultiplier = 1.025f;
    [SerializeField] float groundAccel = 130f;
    [SerializeField] float maxSpeed = 10f;
    [SerializeField] float airAccel = 50f;
    [SerializeField] float airSpeed = 1f;
    [SerializeField] float friction = 12f;
    [SerializeField] float jumpHeight = 12f;
    [SerializeField] float jumpBufferTime = 0.2f;

    KinematicCharacterMotor _motor;
    PlayerInputReader _input;
    TPCameraViewSource _viewSource;
    float _jumpBufferCounter;

    void Awake()
    {
        _motor = GetComponent<KinematicCharacterMotor>();
        _input = GetComponent<PlayerInputReader>();
        _viewSource = GetComponentInChildren<TPCameraViewSource>();
        _input.Configure(autoBhop);

        if (autoBhop)
        {
            jumpBufferTime = 0.01f;
        }
    }

    void Start()
    {
        _motor.CharacterController = this;
    }

    void Update()
    {
        UpdateJumpBuffer();
    }

    void UpdateJumpBuffer()
    {
        if (_input.JumpPressed)
        {
            _jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            _jumpBufferCounter -= Time.deltaTime;
        }
    }

    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        Vector3 forward = Vector3.ProjectOnPlane(GetLookRotation() * Vector3.forward, _motor.CharacterUp);
        if (forward != Vector3.zero)
        {
            currentRotation = Quaternion.LookRotation(forward, _motor.CharacterUp);
        }
    }

    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        Vector3 inputDirection = GetPlanarMoveDirection();

        if (_motor.GroundingStatus.IsStableOnGround)
        {
            ApplyGroundVelocity(ref currentVelocity, inputDirection, deltaTime);
        }
        else
        {
            ApplyAirVelocity(ref currentVelocity, inputDirection, deltaTime);
        }
    }

    Vector3 GetPlanarMoveDirection()
    {
        Quaternion lookRotation = GetLookRotation();
        Vector3 forward = Vector3.ProjectOnPlane(lookRotation * Vector3.forward, _motor.CharacterUp).normalized;
        Vector3 right = Vector3.ProjectOnPlane(lookRotation * Vector3.right, _motor.CharacterUp).normalized;
        Vector3 inputDirection = right * _input.Move.x + forward * _input.Move.y;
        return Vector3.ClampMagnitude(inputDirection, 1f);
    }

    Quaternion GetLookRotation()
    {
        return _viewSource != null
            ? _viewSource.LookRotation
            : transform.rotation;
    }

    void ApplyGroundVelocity(ref Vector3 currentVelocity, Vector3 inputDirection, float deltaTime)
    {
        if (_motor.GroundingStatus.GroundNormal != Vector3.zero)
        {
            currentVelocity = _motor.GetDirectionTangentToSurface(currentVelocity, _motor.GroundingStatus.GroundNormal) * currentVelocity.magnitude;
        }

        Vector3 reorientedInput = _motor.GetDirectionTangentToSurface(inputDirection, _motor.GroundingStatus.GroundNormal);
        Vector2 target = new Vector2(reorientedInput.x, reorientedInput.z);
        Vector2 horizontalVelocity = new Vector2(currentVelocity.x, currentVelocity.z);

        if (_jumpBufferCounter > 0f)
        {
            ApplyJump(ref currentVelocity, ref horizontalVelocity, target, deltaTime);
        }
        else
        {
            horizontalVelocity = MoveGround(target, horizontalVelocity, deltaTime);
            horizontalVelocity = Vector2.ClampMagnitude(horizontalVelocity, maxSpeed);
        }

        currentVelocity = new Vector3(horizontalVelocity.x, currentVelocity.y, horizontalVelocity.y);
    }

    void ApplyJump(ref Vector3 currentVelocity, ref Vector2 horizontalVelocity, Vector2 target, float deltaTime)
    {
        _motor.ForceUnground(0.2f);
        _jumpBufferCounter = 0f;
        currentVelocity = currentVelocity - Vector3.Project(currentVelocity, _motor.CharacterUp);
        currentVelocity += _motor.CharacterUp * jumpHeight;
        horizontalVelocity = MoveAir(target, horizontalVelocity, deltaTime);
    }

    void ApplyAirVelocity(ref Vector3 currentVelocity, Vector3 inputDirection, float deltaTime)
    {
        Vector2 horizontalVelocity = new Vector2(currentVelocity.x, currentVelocity.z);
        Vector2 target = new Vector2(inputDirection.x, inputDirection.z);
        horizontalVelocity = MoveAir(target, horizontalVelocity, deltaTime);

        currentVelocity.y -= gravity * gravityMultiplier * deltaTime;
        currentVelocity = new Vector3(horizontalVelocity.x, currentVelocity.y, horizontalVelocity.y);
    }

    Vector2 MoveGround(Vector2 target, Vector2 horizontalVelocity, float deltaTime)
    {
        float speed = horizontalVelocity.magnitude;
        if (speed > 0f)
        {
            float drop = speed * friction * deltaTime;
            horizontalVelocity *= Mathf.Max(speed - drop, 0f) / speed;
        }

        return Accelerate(target, horizontalVelocity, groundAccel, deltaTime);
    }

    Vector2 MoveAir(Vector2 target, Vector2 horizontalVelocity, float deltaTime)
    {
        return Accelerate(target, horizontalVelocity, airAccel, deltaTime);
    }

    Vector2 Accelerate(Vector2 target, Vector2 horizontalVelocity, float acceleration, float deltaTime)
    {
        float projectedVelocity = Vector2.Dot(target, horizontalVelocity);
        if (!_motor.GroundingStatus.IsStableOnGround && projectedVelocity >= airSpeed)
        {
            return horizontalVelocity;
        }

        return horizontalVelocity + acceleration * deltaTime * target; // floats before vector
    }

    public void BeforeCharacterUpdate(float deltaTime) { }
    public void PostGroundingUpdate(float deltaTime) { }
    public void AfterCharacterUpdate(float deltaTime) { }
    public bool IsColliderValidForCollisions(Collider coll) => true;
    public void OnDiscreteCollisionDetected(Collider hitCollider) { }
    public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }
    public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }
    public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport) { }
}
