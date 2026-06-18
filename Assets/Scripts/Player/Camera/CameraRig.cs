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
    
    [Header("Camera Obstruction Parameters")]
    [SerializeField] float obstructionPadding = 0.2f;
    [SerializeField] float obstructionSmoothTime = 0.12f;
    [SerializeField] [Tooltip("the min delta value for camera snap smoothing to apply (to avoid applying on smooth surface changes")] float minSmoothingChange = 0.12f;
    [SerializeField] LayerMask obstructionLayers = ~0;
    
    Camera mainCamera;

    float currentObstructionDistance = Mathf.Infinity;
    float obstructionDistanceVelocity;

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
        
        
        Vector3 targetToCamera = position - cameraParams.TrackingPosition;
        float targetToCameraDistance = targetToCamera.magnitude;
        
        // prevents divide by 0
        if (targetToCameraDistance <= 0.001f)
        {
            mainCamera.SetParams(position, rotation);
            return;
        }
        Vector3 targetToCameraNormalized = targetToCamera / targetToCameraDistance;

        float maxDistance = targetToCameraDistance;
        
        // avoid obstacles that intersect camera
        if (Physics.Raycast(cameraParams.TrackingPosition, targetToCameraNormalized, out RaycastHit hit, targetToCameraDistance, obstructionLayers, QueryTriggerInteraction.Ignore))
        {
            maxDistance = Mathf.Max(0f, hit.distance - obstructionPadding);
        }

        float delta = maxDistance - currentObstructionDistance;
        if (delta > minSmoothingChange)
        {
            // wall got further, smooth
            currentObstructionDistance = Mathf.SmoothDamp(
                currentObstructionDistance,
                maxDistance,
                ref obstructionDistanceVelocity,
                obstructionSmoothTime
            );
        }
        else
        {
            // snap
            currentObstructionDistance = maxDistance;
            obstructionDistanceVelocity = 0f;
        }
        
        // UPDATE THE POSITION, TODO: easing function? shi probably
        position = cameraParams.TrackingPosition + targetToCameraNormalized * currentObstructionDistance;;
        
        mainCamera.SetParams(position, rotation);
    }
}
