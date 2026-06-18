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
    [SerializeField] TPViewSource viewSource;
    [SerializeField] float fov = 90f;
    
    Camera mainCamera;

    void Awake()
    {
        mainCamera = GetComponent<Camera>();
        mainCamera.fieldOfView = fov;
    }

    void LateUpdate()
    {
        ApplyViewSource();
    }

    void setViewSource(TPViewSource newViewSource)
    {
        viewSource = newViewSource;
    }
    
    void ApplyViewSource()
    {
        if (viewSource == null) { return; }
        CameraParams cameraParams = viewSource.GetParams();
        mainCamera.SetParams(cameraParams);
    }
}
