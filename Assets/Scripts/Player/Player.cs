using System;
using KinematicCharacterController;
using UnityEngine;
using UnityEngine.InputSystem;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

[Serializable]
class MovementSettings
{
    [SerializeField] float groundAccel;
    [SerializeField] float maxSpeed;
    [SerializeField] float airAccel;
    [SerializeField] float airSpeed;
    [SerializeField] float jumpHeight;
    public float GroundAccel => groundAccel;
    public float MaxSpeed => maxSpeed;
    public float AirAccel => airAccel;
    public float AirSpeed => airSpeed;
    public float JumpHeight => jumpHeight;
}

[RequireComponent(typeof(KinematicCharacterMotor))]
[RequireComponent(typeof(AbilityController))]
[RequireComponent(typeof(PlayerInputReader))]
[RequireComponent(typeof(PlayerCharacterSwitcher))]
[RequireComponent(typeof(PlayerAim))]
[RequireComponent(typeof(PlayerLook))]
[RequireComponent(typeof(ItemInventory),typeof(PlayerInteractor))]
[RequireComponent(typeof(Combat))]
public class Player : MonoBehaviour, ICharacterController, ICharacterVelocityProjection
{
    public static LayerMask LayerMask => LayerMask.GetMask("Player");
    
    [Header("Input")]
    [SerializeField] bool autoBhop;

    [Header("Movement Settings")]
    [SerializeField] MovementSettings handMovementSettings;
    [SerializeField] MovementSettings eyeMovementSettings; 
    
    [Header("Movement")]
    [SerializeField] float gravity = 31f;
    [SerializeField] float gravityMultiplier = 1f;
    [SerializeField] float baseFriction = 20f;
    [SerializeField] float stopSpeed = 4f; //min speed dropoff when grounded
    [SerializeField] float jumpBufferTime = 0.05f;
    [Tooltip("Outward speed required to override KCC step snapping while leaving a ledge. Independent of the motor's ledge snap setting.")]
    [SerializeField] float ledgeWalkOffVelocityThreshold = 3f;

    [Header("Dash Ramp Movement")]
    [SerializeField] RampSlideSettings rampSlide = new();
    
    [Header("Loadouts")]
    [SerializeField] AbilityLoadout eyeLoadout;
    [SerializeField] AbilityLoadout handLoadout;
    [SerializeField] AbilityLoadout startingLoadout;

    public static Player Instance { get; private set; }
    public PlayerInputReader Input { get; private set; }
    public PlayerModifiers Modifiers { get; private set; } = PlayerModifiers.CreateFresh();
    public PlayerAnimator Animator { get; private set; }
    public PlayerAim Aim { get; private set; }
    public PlayerLook Look { get; private set; }
    public Combat Combat { get; private set; }
    public ItemInventory Inventory { get; private set; }

    public AbilityController AbilityController { get; private set; }
    public PlayerCharacterSwitcher CharacterSwitcher { get; private set; }

    public AbilityLoadout EyeLoadout => eyeLoadout;
    public AbilityLoadout HandLoadout => handLoadout;

    public KinematicCharacterMotor Motor => _motor;
    public bool IsGrounded => _motor.GroundingStatus.IsStableOnGround;
    public bool GroundedRaycast(float rayLength) { return Physics.Raycast(transform.position, Vector3.down, rayLength, LayerMask.GetMask("Ground"));}
    public bool IgnoreEnemiesForCollision = true; // disable player colliding with enemies
    
    KinematicCharacterMotor _motor;
    float _jumpBufferCounter;
    
    PlayerMovement _movement;
    public bool IsDashing => _movement != null && _movement.IsDashing;
    

    void OnDrawGizmos()
    {
        if (Instance == null) return;
        
        AbilityBehaviour ab = HandLoadout.GetAbility(AbilitySlot.Primary).Behaviour;
        if (ab is BrassKnuckles bk)  bk.DrawGizmos(transform);

        ab = HandLoadout.GetAbility(AbilitySlot.Secondary).Behaviour;
        if (ab is Whirlwind ww) ww.DrawGizmos(transform);
        
        ab = HandLoadout.GetAbility(AbilitySlot.Movement).Behaviour;
        if (ab is GroundPound gp) gp.DrawGizmos(transform);
    }

    
    void Awake()
    {
        Instance = this;
        
        Input               = GetComponent<PlayerInputReader>();
        Aim                 = GetComponent<PlayerAim>();
        Look                = GetComponent<PlayerLook>();
        AbilityController   = GetComponent<AbilityController>();
        CharacterSwitcher   = GetComponent<PlayerCharacterSwitcher>();
        Combat              = GetComponent<Combat>();
        Inventory           = GetComponent<ItemInventory>();
        
        Animator = new PlayerAnimator(CharacterSwitcher);
        
        _motor = GetComponent<KinematicCharacterMotor>();
        _movement = new PlayerMovement(_motor, rampSlide);
        
        Input.Configure(autoBhop);
        if (autoBhop) jumpBufferTime = 0.01f;
        
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
        
        if (Keyboard.current.periodKey.wasPressedThisFrame) EnemySpawner.Instance.TestSpawn();
        
        // DEBUG SPAWN ITEM
        if (Input.DebugSpawnItemPressed) 
        {
            SecretsManager.Instance.AwardSecret();
            ItemDefinition randomItem = ItemSpawner.Instance.ItemCatalog.GetRandomItem(ItemRarityWeightDistribution.Default);
            ItemSpawner.Instance.Spawn(randomItem, transform.position+Vector3.up, Vector3.up * 13);
        }
        
        
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
        if (_movement.UpdateVelocity(ref currentVelocity, GetPlanarMoveDirection(), _jumpBufferCounter > 0f, GetMovementParameters(), deltaTime))
        {
            _jumpBufferCounter = 0f;
            Animator.PlayJumpAnimation();
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

    MovementParameters GetMovementParameters()
    {
        return new MovementParameters
        {
            GroundAcceleration = GetCurrentGroundAccel(),
            GroundSpeed = GetCurrentMaxSpeed(),
            AirAcceleration = GetCurrentAirAccel(),
            AirSpeed = GetCurrentAirSpeed(),
            JumpSpeed = GetCurrentJumpHeight(),
            Gravity = gravity * gravityMultiplier,
            Friction = baseFriction,
            StopSpeed = stopSpeed,
            BhopCarry = Modifiers.BhopMomentumCarryPercent.GetValue()
        };
    }

    
    /*
     MOVEMENT SETTINGS -
     
     movement player modifiers will be applied here
     */
    
    float GetCurrentGroundAccel()
    {
        if (CharacterSwitcher.CurrentCharacter == PlayerCharacter.Hand)
        {
            return handMovementSettings.GroundAccel;
        }
        return eyeMovementSettings.GroundAccel;
    }

    float GetCurrentMaxSpeed()
    {
        if (CharacterSwitcher.CurrentCharacter == PlayerCharacter.Hand)
        {
            return handMovementSettings.MaxSpeed;
        }
        return eyeMovementSettings.MaxSpeed;
    }
    
    float GetCurrentAirAccel()
    {
        if (CharacterSwitcher.CurrentCharacter == PlayerCharacter.Hand)
        {
            return handMovementSettings.AirAccel;
        }
        return eyeMovementSettings.AirAccel;
    }

    float GetCurrentAirSpeed()
    {
        if (CharacterSwitcher.CurrentCharacter == PlayerCharacter.Hand)
        {
            return handMovementSettings.AirSpeed;
        }
        return eyeMovementSettings.AirSpeed;
    }
    
    float GetCurrentJumpHeight()
    {
        if (CharacterSwitcher.CurrentCharacter == PlayerCharacter.Hand)
        {
            return handMovementSettings.JumpHeight;
        }
        return eyeMovementSettings.JumpHeight;
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
        Dash dash = EyeLoadout.GetAbility(AbilitySlot.Movement)?.Behaviour as Dash;
        float damp = dash?.VerticalDashDampPercent ?? 1f;
        Vector3 squishedForwardVector = new Vector3(forwardVector.x, forwardVector.y * damp, forwardVector.z);
        Dash(squishedForwardVector, speed);
    }
    
    /// requests a minimum speed along the dash direction to preserve perpendicular momentum
    public void Dash(Vector3 directionNormalized, float speed)
    {
        Dash dash = EyeLoadout.GetAbility(AbilitySlot.Movement)?.Behaviour as Dash;
        _movement.QueueDash(directionNormalized * speed, dash?.SurfaceDashDuration ?? 0.25f, dash?.SurfaceDashFriction ?? 1f);
    }

    public void GroundPoundDash(Vector3 directionNormalized, float speed, bool forceUnground)
    {
        _movement.QueueGroundPound(directionNormalized * speed, forceUnground);
    }

    public void BeforeCharacterUpdate(float deltaTime) => _movement.BeforeCharacterUpdate();

    public void PostGroundingUpdate(float deltaTime)
    {
        _movement.PostGroundingUpdate(_jumpBufferCounter > 0f);
    }
    public void AfterCharacterUpdate(float deltaTime) => _movement.AfterCharacterUpdate(deltaTime, GetCurrentMaxSpeed());

    // custom projection for ramp boosts/sliding
    public bool ProjectVelocity(ref Vector3 velocity, Vector3 obstructionNormal, bool stableOnHit) => _movement.ProjectVelocity(ref velocity, obstructionNormal);

    public bool IsColliderValidForCollisions(Collider coll)
    {
        if (!IgnoreEnemiesForCollision) return true;

        bool isEnemyLayer = ((1 << coll.gameObject.layer) & Enemy.LayerMask.value) > 0f;
        return !isEnemyLayer;
    }
    public void OnDiscreteCollisionDetected(Collider hitCollider) { }
    public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }
    public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }
    public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
    {
        _movement.ProcessHitStability(hitNormal, ref hitStabilityReport);

        // override ledge snapping when current movement input shows that the player intends to move toward the empty side
        if (hitStabilityReport.LedgeDetected && hitStabilityReport.IsMovingTowardsEmptySideOfLedge)
        {
            Vector3 moveInput = Input != null ? GetPlanarMoveDirection() : Vector3.zero;
            float outwardInput = Vector3.Dot(moveInput, hitStabilityReport.LedgeFacingDirection);
            float outwardVelocity = _motor != null ? Vector3.Dot(_motor.BaseVelocity, hitStabilityReport.LedgeFacingDirection) : 0f;
            if (outwardInput > 0.01f && outwardVelocity >= ledgeWalkOffVelocityThreshold) hitStabilityReport.IsStable = false;
        }
    }
}
