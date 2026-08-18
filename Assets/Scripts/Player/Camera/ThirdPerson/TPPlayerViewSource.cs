using UnityEngine;

public class TPPlayerViewSource : TPViewSource
{
    
    [SerializeField] Transform target;
    [SerializeField] Vector2 defaultFraming = new Vector2(0f, -0.43f);
    [SerializeField] float distance = 4.3f;

    [Header("Dashing Distance")]
    [SerializeField] float dashingDistanceIncrease = 2f;
    [SerializeField] float dashingDistanceIncreaseTime = 0.1f;
    [SerializeField] float dashingDistanceDecreaseTime = 0.4f;
    
    [Header("Jump Tracking")]
    [Tooltip("If the Camera targeted y position should be given by the player or by the ground under the player")]
    [SerializeField] bool dontTrackJumps = true;
    [SerializeField] float maxGroundFollowDistance = 0.3f;
    [SerializeField] float maxFallCameraLag = 2f;
    [SerializeField] float verticalSmoothTime = 0.15f;

    [Header("Vertical Orbit Easing")] 
    [SerializeField] [Range(0f,85f)] float VOE_start = 5.1f;
    [SerializeField] [Range(0f,85f)] float VOE_end = 85f;
    [SerializeField] [Range(-1f,1f)] float straightDownFraming = -0.08f;
    [SerializeField] [Range(0f,50f)] float straightDownDistance = 5f;


    Player player;
    PlayerLook look;
    public override Quaternion LookRotation => look != null ? look.LookRotation : Quaternion.identity;
    
    float smoothTrackingX;
    float smoothTrackingY;
    float smoothTrackingZ;
    float smoothDashingDistance;
    
    float trackingVelocityX;
    float trackingVelocityY;
    float trackingVelocityZ;
    float dashingDistanceVelocity;
    

    // ensures target and input reference
    void Start()
    {
        player = Player.Instance;
        // ensure player look component
        look = Player.Instance.Look;
        if (look == null) Debug.LogError($"{nameof(TPPlayerViewSource)} needs a parent PlayerLook.", this);

        // ensure target
        if (target == null)
        {
            // default target to player transform if no target given
            target = player.transform;
        }

        // smoothing y value starts equal to y value
        smoothTrackingY = target.position.y;
    }

    Vector3 CalculateTrackingPosition()
    {
        Vector3 trackingPosition = target.position;
        
        // TODO: apply low-pass filter or critically damped spring?
        // ...
        
        // if camera
        if (dontTrackJumps)
        {
            Vector3 rayOrigin = target.position;
            Vector3 Offset = new Vector3(0f, 0.1f, 0f);
            float desiredY = Physics.Raycast(rayOrigin + Offset, Vector3.down, out RaycastHit hit, maxGroundFollowDistance)
                ? hit.point.y
                : target.position.y - maxGroundFollowDistance;

            smoothTrackingY = Mathf.SmoothDamp(
                smoothTrackingY,
                desiredY,
                ref trackingVelocityY,
                verticalSmoothTime
            );

            float highestAllowedY = target.position.y + maxFallCameraLag;
            if (smoothTrackingY > highestAllowedY)
            {
                smoothTrackingY = highestAllowedY;
                trackingVelocityY = 0; // stop the smoothing
            }
            
            trackingPosition.y = smoothTrackingY;
        }
        
        // TrackingOffset is member variable derived by TPViewSource that lets us offset the target position if we want.
        return trackingPosition + TrackingOffset;
    }

    Vector2 CalculateFraming()
    {
        Vector2 framing = defaultFraming;
        
        // blends framing from default framing to straightdown framing based on where our pitch lands between 0f,85f (according to VOE_start and VOE_finish)
        float factor = Mathf.InverseLerp(VOE_start, VOE_end, look.Pitch);
        factor = Mathf.SmoothStep(0f, 1f, factor); // easing function
        framing.y = Mathf.Lerp(framing.y, straightDownFraming, factor);
        
        return framing;
    }
    
    float CalculateDistance()
    {
        float adjustedDistance = distance;
        
        // want: y framing blends between default framing y and 0 based on pitch i guess
        // blend between look.Pitch: 0-85
        float factor = Mathf.InverseLerp(VOE_start, VOE_end, look.Pitch);
        factor = Mathf.SmoothStep(0f, 1f, factor); // easing function
        adjustedDistance = Mathf.Lerp(adjustedDistance, straightDownDistance, factor);

        // get planar dash alignment with where the camera is facing
        bool isPlanarDashing = player.IsDashing;
        
        float directionalAmount = 1f;
        float horizontalLookAmount = 1f;
        
        // ground pound does full distance change always, eye dash scales based on vertical alignment
        if (player.CharacterSwitcher.CurrentCharacter == PlayerCharacter.Eye)
        {
            Vector3 flatForward = Quaternion.Euler(0f, look.Yaw, 0f) * Vector3.forward;
            Vector3 planarVelocity = Vector3.ProjectOnPlane(player.Motor.Velocity, Vector3.up);
            Vector3 dashDirection = isPlanarDashing ? planarVelocity.normalized : Vector3.zero;
            
            // front dash = 1, side dash = .25, back dash = 0
            directionalAmount = (Vector3.Dot(dashDirection, flatForward) + 1f) * 0.5f;
            directionalAmount *= directionalAmount;
            
            // fade the distance when looking vertical so the camera doesnt move up/down
            horizontalLookAmount = Mathf.Cos(Mathf.Abs(look.Pitch) * Mathf.Deg2Rad);
            horizontalLookAmount *= horizontalLookAmount;
        }
        

        // smooth the camera in/out based on the current dash angle
        float targetDashingDistance = isPlanarDashing ? dashingDistanceIncrease * directionalAmount * horizontalLookAmount : 0f;
        float dashingDistanceSmoothTime = targetDashingDistance > smoothDashingDistance ? dashingDistanceIncreaseTime : dashingDistanceDecreaseTime;
        smoothDashingDistance = Mathf.SmoothDamp(smoothDashingDistance, targetDashingDistance, ref dashingDistanceVelocity, dashingDistanceSmoothTime);

        return adjustedDistance + smoothDashingDistance;
    }
    
    public override CameraParams GetParams()
    {
        return new CameraParams
        {
            TrackingPosition = CalculateTrackingPosition(),
            Framing = CalculateFraming(),
            Distance = CalculateDistance(),
            Pitch = look.Pitch,
            Yaw = look.Yaw,
        };
    }
}
