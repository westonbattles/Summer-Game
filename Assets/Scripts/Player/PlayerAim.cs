using UnityEngine;

// helper functions to get us the direction that the player is aiming relative to the camera
public class PlayerAim : MonoBehaviour
{
    [SerializeField] Camera aimCamera;

    public Camera AimCamera => aimCamera;
    public Vector3 AimOrigin => aimCamera.transform.position;
    public Vector3 AimDirection => aimCamera.transform.forward;
    public Quaternion AimRotation => aimCamera.transform.rotation;
    
    

    void Awake()
    {
        if (aimCamera == null) aimCamera = Camera.main;
    }

    // get the point in space that the camera's crosshair is aiming at
    public Vector3 GetTargetPoint(float maxDistance, LayerMask hitLayers)
    {
        if (Physics.Raycast(
                AimOrigin,
                AimDirection,
                out RaycastHit hit,
                maxDistance,
                hitLayers,
                QueryTriggerInteraction.Ignore)) // ignore isTrigger colliders
        {
            return hit.point;
        }
        
        // nothing was hit, so aim at the point maxDistance away
        return AimOrigin + AimDirection * maxDistance;
    }

    // get the direction to the target point from the player / shot origin
    public Vector3 GetShotDirection(Vector3 shotOrigin, Vector3 aimPoint)
    {
        return (aimPoint - shotOrigin).normalized;
    }
}
