using System;
using System.Numerics;
using KinematicCharacterController;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

[RequireComponent(typeof(KinematicCharacterMotor))]
[RequireComponent(typeof(AbilityController))]
[RequireComponent(typeof(PlayerInputReader))]
[RequireComponent(typeof(PlayerCharacterSwitcher))]
[RequireComponent(typeof(PlayerAim))]
[RequireComponent(typeof(PlayerLook))]
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

    [Header("Loadouts")]
    [SerializeField] AbilityLoadout eyeLoadout;
    [SerializeField] AbilityLoadout handLoadout;
    [SerializeField] AbilityLoadout startingLoadout;

    public static Player Instance { get; private set; }
    public PlayerModifiers Modifiers { get; private set; } = new(); // items will change stuff from here
    public PlayerAim Aim { get; private set; }
    public PlayerLook Look { get; private set; }
    public AbilityController AbilityController { get; private set; }
    public PlayerCharacterSwitcher CharacterSwitcher { get; private set; }

    public AbilityLoadout EyeLoadout => eyeLoadout;
    public AbilityLoadout HandLoadout => handLoadout;
    
    KinematicCharacterMotor _motor;
    PlayerInputReader _input;
    PlayerLook _look;
    float _jumpBufferCounter;
    
    AbilityController _abilityController;
    PlayerCharacterSwitcher _characterSwitcher;
    PlayerAnimator _playerAnimator;
    
    // dash variables
    Vector3 _dashVelocity;
    bool _shouldDash;
    bool _isDashing;
    public bool IsDashing => _isDashing;

    void OnDrawGizmos()
    {
        AbilityBehaviour ab = HandLoadout.GetAbility(AbilitySlot.Primary).Behaviour;
        if (ab is BrassKnuckles bk)  bk.DrawGizmos(transform);
    }

    void Awake()
    {
        Instance = this;
        Aim = GetComponent<PlayerAim>();
        Look = GetComponent<PlayerLook>();
        AbilityController = GetComponent<AbilityController>();
        CharacterSwitcher = GetComponent<PlayerCharacterSwitcher>();
        
        _motor = GetComponent<KinematicCharacterMotor>();
        _input = GetComponent<PlayerInputReader>();
        _look = GetComponentInChildren<PlayerLook>();
        _input.Configure(autoBhop);

        _abilityController = GetComponent<AbilityController>();
        _characterSwitcher = GetComponent<PlayerCharacterSwitcher>();
        
        _playerAnimator = new PlayerAnimator(_characterSwitcher);

        if (autoBhop)
        {
            jumpBufferTime = 0.01f;
        }
        
        _abilityController.SetLoadout(startingLoadout);
    }

    void Start()
    {
        _motor.CharacterController = this;
    }

    void Update()
    {
        
        
        UpdateJumpBuffer();
        
        // handle ability input
        if (_input.PrimaryAbilityPressed)
        {
            if (_abilityController.GetCurrentAbility(AbilitySlot.Primary) == null) Debug.Log("Can't execute Primary ability: ability has not been set");
            else _abilityController.ExecuteAbility(AbilitySlot.Primary);
        }
        if (_input.SecondaryAbilityPressed)
        {
            if (_abilityController.GetCurrentAbility(AbilitySlot.Secondary) == null) Debug.Log("Can't execute Secondary ability: ability has not been set");
            else _abilityController.ExecuteAbility(AbilitySlot.Secondary);
        }
        if (_input.MovementAbilityPressed)
        {
            if (_abilityController.GetCurrentAbility(AbilitySlot.Movement) == null) Debug.Log("Can't execute Movement ability: ability has not been set");
            else _abilityController.ExecuteAbility(AbilitySlot.Movement);
        }
        if (_input.SpecialAbilityPressed)
        {
            if (_abilityController.GetCurrentAbility(AbilitySlot.Special) == null) Debug.Log("Can't execute Special ability: ability has not been set");
            else _abilityController.ExecuteAbility(AbilitySlot.Special);
        }
        
        // handle swap
        if (_input.SwapPressed) _characterSwitcher.Swap();

        Vector3 planarVelocity = Vector3.ProjectOnPlane(_motor.Velocity, _motor.CharacterUp);
        Vector3 localVelocity3 = transform.InverseTransformDirection(planarVelocity);
        Vector2 localVelocity = new Vector2(localVelocity3.x, localVelocity3.z);
        
        _playerAnimator.HandleMovementAnimation(
            _input.Move,
            localVelocity,
            _motor.GroundingStatus.IsStableOnGround);
        

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
        // perform dash
        if (_shouldDash)
        {
            currentVelocity = _dashVelocity;
            _shouldDash = false;
            return;
        }
        
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
        return _look != null
            ? _look.LookRotation
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
            _playerAnimator.PlayJumpAnimation();
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

    /// Dashes in move input direction only, no up/down
    public void DashAlongPlane(float speed)
    {
        Vector2 moveInput = _input.Move == Vector2.zero ? Vector2.up : _input.Move;
        
        float yaw = _look.Yaw;
        Vector3 dashDirection = Quaternion.AngleAxis(yaw, Vector3.up) * new Vector3(moveInput.x, 0.2f, moveInput.y).normalized; // rotate move input to match look rotation - https://stackoverflow.com/questions/74894102/unity-rotating-a-vector3-along-an-axis
                                                                                                                                // y=0.2 is for testing purposes - but kinda feels good? might leave it tbh
        Dash(dashDirection, speed);
    }
    
    /// Performs dash in a given direction with given speed
    public void Dash(Vector3 directionNormalized, float speed)
    {
        _dashVelocity = directionNormalized * speed;
        _shouldDash = true;
        _isDashing = true;
        //PlayRandomSound(launchSounds, launchPitchMin, launchPitchMax);
        
        _motor.ForceUnground(.25f); // lets you dash along objects without insta stopping you   
    }

    public void BeforeCharacterUpdate(float deltaTime) { }

    public void PostGroundingUpdate(float deltaTime)
    {
        if (!_motor.LastGroundingStatus.IsStableOnGround && _motor.GroundingStatus.IsStableOnGround)
        {
            _isDashing = false;
        }
    }
    public void AfterCharacterUpdate(float deltaTime) { }
    public bool IsColliderValidForCollisions(Collider coll) => true;
    public void OnDiscreteCollisionDetected(Collider hitCollider) { }
    public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }
    public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }
    public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport) { }
}
