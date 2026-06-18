using System;
using UnityEngine;


enum ViewMode
{
    ThirdPerson,
    Dialogue,
    FirstPerson
}

[RequireComponent(typeof(Camera))]
public class CameraRig : MonoBehaviour
{
    [SerializeField] ViewMode viewMode;
    
    [SerializeField] TPViewSource viewSource;
    //[SerializeField] TPViewSource thirdPersonViewSource;
    //[SerializeField] TPViewSource dialogueViewSource;
    // first person view source? 
    [SerializeField] float fov = 90f;
    
    Camera mainCamera;

    void Awake()
    {
        mainCamera = GetComponent<Camera>();
        mainCamera.fieldOfView = fov;
    }

    void LateUpdate()
    {
        // if (transitioning) return; -> if we want to manually control blend transitions for random things and dont want manual assignments of camera stuff
        
        switch (viewMode)
        {
            case ViewMode.ThirdPerson:
                ApplyThirdPersonCameraParams(viewSource); // thirdPersonViewSource
                // ApplyThirdPersonCameraParams(thirdPersonViewSource);
                break;
            
            case ViewMode.Dialogue:
                throw new NotImplementedException("Dialogue camera not implemented");
                // ApplyThirdPersonCameraParams(dialgoueViewSource);
                break;
            
            case ViewMode.FirstPerson:
                throw new NotImplementedException("First person camera not implemented");
                break;
        }
    }

    void SetViewMode(ViewMode newViewMode)
    {
        viewMode = newViewMode;
    }
    
    void ApplyThirdPersonCameraParams(TPViewSource source)
    {
        if (source == null) { return; }
        
        CameraParams cameraParams = source.GetParams();
        
        mainCamera.CalculateCameraPlacement(cameraParams, out Vector3 position, out Quaternion rotation);
        
        // ENSURE objects block target from the cameras view
        float halfHeight = Mathf.Tan(0.5f * Mathf.Deg2Rad * mainCamera.fieldOfView) * cameraParams.Distance;
        float halfWidth = halfHeight * mainCamera.aspect;
        var localOffset = new Vector3(
            halfWidth * cameraParams.Framing.x,
            halfHeight * cameraParams.Framing.y,
            cameraParams.Distance
        );

        // Vector pointing from framed tracking position toward the camera,)
        Vector3 trackingToCamera = -(rotation * localOffset);
        float rayDistance = trackingToCamera.magnitude;
        Vector3 normalizedDirection = trackingToCamera / rayDistance;
        
        // avoid obstacles that intersect camera
        if (Physics.Raycast(cameraParams.TrackingPosition, trackingToCamera, out RaycastHit hit, rayDistance))
        {
            float padding = 0.2f;
            float finalDistance = Mathf.Max(0f, hit.distance - padding);
            // this instead of hit.point so we can control how far away 
            
            // UPDATE THE POSITION, TODO: easing function? shi probably
            position = cameraParams.TrackingPosition + normalizedDirection * finalDistance;
        }
        
        mainCamera.SetParams(position, rotation);
    }
}
