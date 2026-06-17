using UnityEngine;

public class TPPlayerViewSource : TPCameraViewSource
{

    [SerializeField] Transform target;
    [SerializeField] PlayerInputReader input;
    [SerializeField] Vector2 framing;
    [SerializeField] float distance;
    [SerializeField] float mouseSensitivity = 0.4f;
    
    float pitch;
    float yaw;
    
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

    public override CameraParams GetParams()
    {
        // TODO - Raycast down so camera follows terrain
        Vector3 trackingPosition = target.transform.position;
        
        return new CameraParams
        {
            TrackingPosition = trackingPosition,
            Framing = framing,
            Distance = distance,
            Pitch = pitch,
            Yaw = yaw,
        };
    }
}
