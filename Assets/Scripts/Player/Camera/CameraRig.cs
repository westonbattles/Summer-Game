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
        
        // do this only if view source isthird person or dialogue
        CameraParams cameraParams = source.GetParams();
        mainCamera.SetParams(cameraParams);
    }
}
