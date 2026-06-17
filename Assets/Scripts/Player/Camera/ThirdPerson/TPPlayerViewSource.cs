using UnityEngine;

public class TPPlayerViewSource : TPCameraViewSource
{
    
    [SerializeField] Transform target;
    [SerializeField] PlayerInputReader input;
    [SerializeField] Vector2 framing;
    [SerializeField] float distance;
    [SerializeField] float mouseSensitivity = 0.4f;

    [Header("Jump Tracking")]
    [SerializeField] bool trackJumps = false;
    [SerializeField] float maxGroundFollowDistance = 10f;
    [SerializeField] float verticalSmoothTime = 0.15f;
    
    float pitch;
    float yaw;
    
    float smoothedTrackingY;
    float trackingYVelocity;
    bool hasSmoothedTrackingY;
    
    public override Quaternion LookRotation => Quaternion.Euler(pitch, yaw, 0f);

    void Awake()
    {
        if (target == null)
        {
            Player player = GetComponentInParent<Player>();
            if (player != null)
            {
                target = player.transform;
            }
        }

        if (input == null)
        {
            input = GetComponentInParent<PlayerInputReader>();
        }
    }
    
    void Update()
    {
        Vector2 look = input.Look;

        pitch = Mathf.Clamp(
            pitch - look.y * mouseSensitivity,
            -85f,
            85f
        );
            
        yaw += look.x * mouseSensitivity;
        yaw = Mathf.Repeat(yaw + 180f, 360f) - 180f;
    }

    Vector3 GetTrackingPosition()
    {
        Vector3 trackingPosition = target.position;
        
        // TODO: apply low-pass filter or critically damped spring?
        // ...
        
        if (trackJumps) return ApplyTrackingOffset(trackingPosition);

        // dy = distance to ground within maxGroundFollowDistance, or maxGroundFollowDistance if no ground was found
        Vector3 rayOrigin = target.position + new Vector3(0f, 0.1f, 0f);
        float desiredY = Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, maxGroundFollowDistance)
            ? hit.point.y
            : target.position.y - maxGroundFollowDistance;
        
        if (!hasSmoothedTrackingY)
        {
            smoothedTrackingY = desiredY;
            hasSmoothedTrackingY = true;
        }

        smoothedTrackingY = Mathf.SmoothDamp(
            smoothedTrackingY,
            desiredY,
            ref trackingYVelocity,
            verticalSmoothTime
        );

        trackingPosition.y = smoothedTrackingY;
        return ApplyTrackingOffset(trackingPosition);
    }
    
    public override CameraParams GetParams()
    {
        return new CameraParams
        {
            TrackingPosition = GetTrackingPosition(),
            Framing = framing,
            Distance = distance,
            Pitch = pitch,
            Yaw = yaw,
        };
    }
}
