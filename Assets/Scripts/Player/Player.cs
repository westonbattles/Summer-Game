using System;
using System.Numerics;
using KinematicCharacterController;
using UnityEngine;
using UnityEngine.InputSystem;
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
    public static LayerMask LayerMask => LayerMask.GetMask("Player");
    
    [Header("Input")]
    [SerializeField] bool autoBhop;

    [Header("Movement")]
    [SerializeField] float gravity = 31f;
    [SerializeField] float gravityMultiplier = 1f;
    [SerializeField] float groundAccel = 140f;
    [SerializeField] float maxSpeed = 8f;
    [SerializeField] float airAccel = 50f;
    [SerializeField] float airSpeed = 1f;
    [SerializeField] float baseFriction = 12f;
    [SerializeField] float jumpHeight = 10f;
    [SerializeField] float jumpBufferTime = 0.05f;

    //[SerializeField] float dashFriction = 6f;
    //[SerializeField] float dashLandMomentumDecayTime = 0.6f; // time to apply updated friction for
    //[SerializeField] float verticalDashDampPercent = 0.6f; // good feeling horizontal dashes lead to too strong vertical dashes, this value squishes down the vertical component for vert dashes to account for that
    
    [Header("Loadouts")]
    [SerializeField] AbilityLoadout eyeLoadout;
    [SerializeField] AbilityLoadout handLoadout;
    [SerializeField] AbilityLoadout startingLoadout;

    public static Player Instance { get; private set; }
    public PlayerInputReader Input { get; private set; }
    public PlayerModifiers Modifiers { get; private set; } = new(); // items will change stuff from here
    public PlayerAnimator Animator { get; private set; }
    public PlayerAim Aim { get; private set; }
    public PlayerLook Look { get; private set; }

    public AbilityController AbilityController { get; private set; }
    public PlayerCharacterSwitcher CharacterSwitcher { get; private set; }

    public AbilityLoadout EyeLoadout => eyeLoadout;
    public AbilityLoadout HandLoadout => handLoadout;

    public bool IsGrounded => _motor.GroundingStatus.IsStableOnGround;
    public bool GroundedRaycast(float rayLength) { return Physics.Raycast(transform.position, Vector3.down, rayLength, LayerMask.GetMask("Ground"));}
    public bool IgnoreEnemiesForCollision = true; // disable player colliding with enemies
    
    KinematicCharacterMotor _motor;
    float _jumpBufferCounter;
    
    float _friction;
    
    // dash variables
    Vector3 _dashVelocity;
    bool _shouldDash;
    bool _isDashing;
    float _dashLandMomentumDecayTimer;
    public bool IsDashing => _isDashing;
    

    void OnDrawGizmos()
    {
        if (Instance == null) return;
        
        AbilityBehaviour ab = HandLoadout.GetAbility(AbilitySlot.Primary).Behaviour;
        if (ab is BrassKnuckles bk)  bk.DrawGizmos(transform);

        ab = HandLoadout.GetAbility(AbilitySlot.Secondary).Behaviour;
        if (ab is Whirlwind ww) ww.DrawGizmos(transform);
        
        ab = HandLoadout.GetAbility(AbilitySlot.Movement).Behaviour;
        if (ab is GroundPound gp) gp.DrawGizmos(transform);

        ab = EyeLoadout.GetAbility(AbilitySlot.Secondary).Behaviour;
        if (ab is PsychicPulse pp) pp.DrawGizmos();
    }

    void Awake()
    {
        Instance = this;
        Input = GetComponent<PlayerInputReader>();
        Aim = GetComponent<PlayerAim>();
        Look = GetComponentInChildren<PlayerLook>();
        AbilityController = GetComponent<AbilityController>();
        CharacterSwitcher = GetComponent<PlayerCharacterSwitcher>();
        Animator = new PlayerAnimator(CharacterSwitcher);
        
        _motor = GetComponent<KinematicCharacterMotor>();
        Input.Configure(autoBhop);

        _friction = baseFriction;
        

        if (autoBhop)
        {
            jumpBufferTime = 0.01f;
        }
        
        AbilityController.SetLoadout(startingLoadout);
    }

    void Start()
    {
        _motor.CharacterController = this;
    }

    void Update()
    {
        // update jump buffer
        if (Input.JumpPressed) _jumpBufferCounter = jumpBufferTime;
        else _jumpBufferCounter -= Time.deltaTime;
        
        // update dash momentum timer
        _dashLandMomentumDecayTimer = Mathf.Max(0f, _dashLandMomentumDecayTimer - Time.deltaTime);
        
        HandleInput();
        
        Vector3 planarVelocity = Vector3.ProjectOnPlane(_motor.Velocity, _motor.CharacterUp);
        Vector3 localVelocity3 = transform.InverseTransformDirection(planarVelocity);
        Vector2 localVelocity = new Vector2(localVelocity3.x, localVelocity3.z);
        
        Animator.HandleMovementAnimation(
            Input.Move,
            localVelocity,
            _motor.GroundingStatus.IsStableOnGround);
    }

    void HandleInput()
    {
        // handle ability input
        if (Input.PrimaryAbilityPressed)
        {
            if (AbilityController.GetCurrentAbility(AbilitySlot.Primary) == null) Debug.Log("Can't execute Primary ability: ability has not been set");
            else AbilityController.ExecuteAbility(AbilitySlot.Primary);
            //{
            //bool executed = AbilityController.ExecuteAbility(AbilitySlot.Primary);
            //if (executed && _characterSwitcher.CurrentCharacter == PlayerCharacter.Hand) _playerAnimator.PlayPrimaryAbility(); // eye uses IK not animator controller
            //}
        }
        if (Input.SecondaryAbilityPressed)
        {
            if (AbilityController.GetCurrentAbility(AbilitySlot.Secondary) == null) Debug.Log("Can't execute Secondary ability: ability has not been set");
            else AbilityController.ExecuteAbility(AbilitySlot.Secondary);
        }
        if (Input.MovementAbilityPressed)
        {
            if (AbilityController.GetCurrentAbility(AbilitySlot.Movement) == null) Debug.Log("Can't execute Movement ability: ability has not been set");
            else AbilityController.ExecuteAbility(AbilitySlot.Movement);
        }
        if (Input.SpecialAbilityPressed)
        {
            if (AbilityController.GetCurrentAbility(AbilitySlot.Special) == null) Debug.Log("Can't execute Special ability: ability has not been set");
            else AbilityController.ExecuteAbility(AbilitySlot.Special);
        }
        
        // handle swap
        if (Input.SwapPressed) CharacterSwitcher.Swap();
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
        Vector3 inputDirection = right * Input.Move.x + forward * Input.Move.y;
        return Vector3.ClampMagnitude(inputDirection, 1f);
    }

    Quaternion GetLookRotation()
    {
        return Look != null
            ? Look.LookRotation
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
            Animator.PlayJumpAnimation();
        }
        else
        {
            horizontalVelocity = MoveGround(target, horizontalVelocity, deltaTime);
            if (_dashLandMomentumDecayTimer <= 0f) horizontalVelocity = Vector2.ClampMagnitude(horizontalVelocity, maxSpeed);
        }

        currentVelocity = new Vector3(horizontalVelocity.x, currentVelocity.y, horizontalVelocity.y);
    }

    void ApplyJump(ref Vector3 currentVelocity, ref Vector2 horizontalVelocity, Vector2 target, float deltaTime)
    {
        _motor.ForceUnground(0.2f);
        _jumpBufferCounter = 0f;
        currentVelocity = currentVelocity - Vector3.Project(currentVelocity, _motor.CharacterUp);
        currentVelocity += _motor.CharacterUp * jumpHeight;

        float speed = horizontalVelocity.magnitude;
        float carry = speed;
        if (carry > maxSpeed) carry = (speed - maxSpeed) * Modifiers.BhopMomentumCarryPercent + maxSpeed; // same as Mathf.Lerp(maxSpeed, speed, Modifiers.BhopMomentumCarryPercent); without the 0-1 clamping
        
        horizontalVelocity = (horizontalVelocity / speed) * carry;
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
            float drop = speed * _friction * deltaTime;
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
    public void DashAlongInputAxis(float speed)
    {
        Vector2 moveInput = Input.Move == Vector2.zero ? Vector2.up : Input.Move;
        
        float yaw = Look.Yaw;
        float yOffset = 0f;
        Vector3 dashDirection = Quaternion.AngleAxis(yaw, Vector3.up) * new Vector3(moveInput.x, yOffset, moveInput.y).normalized; // rotate move input to match look rotation - https://stackoverflow.com/questions/74894102/unity-rotating-a-vector3-along-an-axis
        
        Dash(dashDirection, speed);
    }
    
    // dash based on player look direction, but dashes downward are forced forward
    public void DashVertically(float speed)
    {
        Vector3 forwardVector = Look.LookRotation * Vector3.forward;
        // project downward look directions onto the flat y plane, and normalize (this will dash us along the ground)
        if (forwardVector.y < 0)
        {
            forwardVector.y = 0;
            forwardVector.Normalize();
        }
        
        // apply vertical damping
        Dash dash = HandLoadout.GetAbility(AbilitySlot.Movement)?.Behaviour as Dash;
        float damp = dash?.VerticalDashDampPercent ?? 1f;
        Vector3 squishedForwardVector = new Vector3(forwardVector.x, forwardVector.y * damp, forwardVector.z);
        Dash(squishedForwardVector, speed);
    }
    
    /// Performs dash in a given direction with given speed
    public void Dash(Vector3 directionNormalized, float speed)
    {
        _dashVelocity = directionNormalized * speed;
        _shouldDash = true;
        _isDashing = true;

        Dash dash = EyeLoadout.GetAbility(AbilitySlot.Movement).Behaviour as Dash;
        _dashLandMomentumDecayTimer = dash?.DashLandMomentumDecayTime ?? 0f;
        //PlayRandomSound(launchSounds, launchPitchMin, launchPitchMax);
        
        _motor.ForceUnground(.25f); // lets you dash along objects without insta stopping you   
    }

    public void GroundPoundDash(Vector3 directionNormalized, float speed, bool forceUnground)
    {
        _dashVelocity = directionNormalized * speed;
        _shouldDash = true;
        _isDashing = true;
        //PlayRandomSound(launchSounds, launchPitchMin, launchPitchMax);
        
        if (forceUnground) _motor.ForceUnground(.25f); // lets you dash along objects without insta stopping you 
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

    public bool IsColliderValidForCollisions(Collider coll)
    {
        if (!IgnoreEnemiesForCollision) return true;

        bool isEnemyLayer = ((1 << coll.gameObject.layer) & Enemy.LayerMask.value) > 0f;
        return !isEnemyLayer;
    }
    public void OnDiscreteCollisionDetected(Collider hitCollider) { }
    public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }
    public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }
    public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport) { }
}
