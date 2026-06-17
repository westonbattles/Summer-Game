using UnityEngine;

// Third Person Player Camera

// owns camera state
// asks a framing/composition object to fill CameraParams
// applies CameraParams with CameraExtensions

[RequireComponent(typeof(Camera))]
public class TPPlayerCamera : MonoBehaviour
{
    [SerializeField] TPCameraViewSource viewSource;
    [SerializeField] float fov = 90f;
    
    Camera mainCamera;
    

    void Awake()
    {
        mainCamera = GetComponent<Camera>();
        mainCamera.fieldOfView = fov;
    }

    void Start()
    {
        // camera params
        ApplyViewSource();
    }

    void LateUpdate()
    {
        ApplyViewSource();
    }
    
    void ApplyViewSource()
    {
        if (viewSource == null) { return; }
        CameraParams cameraParams = viewSource.GetParams();
        mainCamera.SetParams(cameraParams);
    }
}
