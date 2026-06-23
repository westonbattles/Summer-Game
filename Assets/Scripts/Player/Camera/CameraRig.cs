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
    //[SerializeField] float obstructionPadding = 0.2f;
    [SerializeField] float obstructionPadding = 0.25f;
    [SerializeField] float obstructionSmoothTime = 0.12f;
    [SerializeField] [Tooltip("distance threshold for applying smoothing (to avoid applying on smooth surface changes")] float minSmoothingChange = 0.005f;
    [SerializeField] LayerMask obstructionLayers = ~0;
    [SerializeField] float obstructionSphereCastRadius = 0.2f;

    [Header("Player Fade Paramteres")]
    [SerializeField] [Range(0,1)] float minFade = 0.2f;
    [SerializeField] [Range(0,10)] float fadeHiddenDistance = 0.5f;
    [SerializeField] [Range(0,10)] float fadeVisibleDistance = 2.0f;
    [SerializeField] float fadeSmoothTime = 0.08f;
    
    Camera mainCamera;
    Player owningPlayer; // so we can detach camera at runtime for smoother camera
    RendererFader playerFader;
    Collider playerFadeCollider;

    float currentObstructionDistance = Mathf.Infinity;
    float obstructionDistanceVelocity;

    void Awake()
    {
        mainCamera = GetComponent<Camera>();
        mainCamera.fieldOfView = fov;

        owningPlayer = viewSource != null ? viewSource.GetComponentInParent<Player>() : null;
        
        if (viewMode == ViewMode.ThirdPerson)
        {
            EnsurePlayerFader();
        }
    }

    void Start()
    {
        if (viewMode == ViewMode.ThirdPerson) // might be able to change for other viewmodes
        {
            transform.SetParent(null, true); // detach camera from player so we can move it independantly
        }
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
                //break;
            
            case ViewMode.FirstPerson:
                throw new NotImplementedException("First person camera not implemented");
                //break;
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
        if (Physics.SphereCast(cameraParams.TrackingPosition, obstructionSphereCastRadius, targetToCameraNormalized, out RaycastHit hit, targetToCameraDistance, obstructionLayers, QueryTriggerInteraction.Ignore))
        {
            maxDistance = Mathf.Max(0f, hit.distance - obstructionPadding);
        }

        float delta = maxDistance - currentObstructionDistance;
        if (maxDistance < currentObstructionDistance || Mathf.Abs(delta) < minSmoothingChange)
        {
            // wall got closer, snap
            currentObstructionDistance = maxDistance;
            obstructionDistanceVelocity = 0f;
        }
        else
        {
            // wall got further, smooth
            currentObstructionDistance = Mathf.SmoothDamp(
                currentObstructionDistance,
                maxDistance,
                ref obstructionDistanceVelocity,
                obstructionSmoothTime
            );
        }
        
        // UPDATE THE POSITION, TODO: easing function? shi probably
        position = cameraParams.TrackingPosition + targetToCameraNormalized * currentObstructionDistance;
        
        
        // fade player if we're too close
        if (viewMode is ViewMode.ThirdPerson)
        {
            EnsurePlayerFader();
            
            Vector3 closestPoint = playerFadeCollider != null
                ? playerFadeCollider.ClosestPoint(position)
                : cameraParams.TrackingPosition;
            
            float distance = Vector3.Distance(position, closestPoint); // dist from camera to player hitbox or (tracking target if hitbox is null) 
            
            // smooth damp the fade
            float targetFade = Mathf.InverseLerp(fadeHiddenDistance, fadeVisibleDistance, distance);
            float fade = Mathf.Max(minFade, targetFade); // don't go below min fade
            //fade = Mathf.Max(minFade, Mathf.Round(fade * 17f) / 17f); weird smoothing bs - ignore for now
            playerFader?.SetFade(fade);
        }
        
        mainCamera.SetParams(position, rotation);
    }
    
    void EnsurePlayerFader()
    {
        if (playerFader != null) return;
        if (owningPlayer == null) return;
        
        playerFader = new RendererFader(owningPlayer.gameObject);
        playerFadeCollider = owningPlayer.GetComponent<Collider>();
    }
}
