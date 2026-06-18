using UnityEngine;

public class TPPlayerViewSource : TPViewSource
{
    
    [SerializeField] Transform target;
    [SerializeField] Vector2 framing;
    [SerializeField] float distance;

    [Header("Jump Tracking")]
    [Tooltip("If the Camera targeted y position should be given by the player or by the ground under the player")]
    [SerializeField] bool dontTrackJumps = true;
    [SerializeField] float maxGroundFollowDistance = 10f;
    [SerializeField] float verticalSmoothTime = 0.15f;

    PlayerLook look;
    public override Quaternion LookRotation => look != null ? look.LookRotation : Quaternion.identity;
    
    float smoothTrackingX;
    float smoothTrackingY;
    float smoothTrackingZ;
    float trackingVelocityX;
    float trackingVelocityY;
    float trackingVelocityZ;
    

    // ensures target and input reference
    void Awake()
    {
        // ensure player look component
        look = GetComponentInParent<PlayerLook>();
        if (look == null)
        {
            Debug.LogError($"{nameof(TPPlayerViewSource)} needs a parent PlayerLook.", this);
        }

        // ensure target
        if (target == null)
        {
            // default target to player transform if no target given
            Player player = GetComponentInParent<Player>();
            if (player == null)
            {
                Debug.LogError($"{nameof(TPPlayerViewSource)} needs a target or a parent Player object.", this);
                return;
            }

            target = player.transform;
        }

        // smoothing y value starts equal to y value
        smoothTrackingY = target.position.y;
    }

    Vector3 GetTrackingPosition()
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

            trackingPosition.y = smoothTrackingY;
        }
        
        // TrackingOffset is member variable derived by TPViewSource that lets us offset the target position if we want.
        return trackingPosition + TrackingOffset;
    }
    
    
    public override CameraParams GetParams()
    {
        return new CameraParams
        {
            TrackingPosition = GetTrackingPosition(),
            Framing = framing,
            Distance = distance,
            Pitch = look.Pitch,
            Yaw = look.Yaw,
        };
    }
}
