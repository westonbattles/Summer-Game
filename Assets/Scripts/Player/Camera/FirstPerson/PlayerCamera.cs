using UnityEngine;
using UnityEngine.Rendering.Universal;

public class PlayerCamera : MonoBehaviour
{
    const string ViewModelLayerName = "Player";

    [SerializeField] Transform cameraTarget;
    [SerializeField] float defaultFov = 90f;
    [SerializeField] float mouseSensitivity = 0.4f;
    [Range(0, 90), SerializeField] float maxCameraPitch = 85f;

    public Quaternion LookRotation => transform.rotation;
    
    Vector3 _eulerAngles;
    PlayerInputReader _input;
    Camera _mainCamera;
    Camera _viewModelCamera;
    int _viewModelLayerMask;

    void Awake()
    {
        _mainCamera = GetComponentInChildren<Camera>(true);
        if (_mainCamera == null) _mainCamera = Camera.main;
        if (cameraTarget == null) cameraTarget = FindCameraTarget();

        if (_mainCamera != null)
        {
            _mainCamera.fieldOfView = defaultFov;
            SetupViewModelOverlayCamera();
        }

        if (cameraTarget != null)
        {
            transform.position = cameraTarget.position;
            transform.eulerAngles = _eulerAngles = cameraTarget.eulerAngles;
        }
        _input = GetComponentInParent<PlayerInputReader>(true);
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        if (_input != null) UpdateRotation(_input.Look);
    }

    void LateUpdate()
    {
        if (cameraTarget != null)
        {
            transform.position = cameraTarget.position;
        }

        SyncViewModelOverlayCamera();
    }

    void UpdateRotation(Vector2 look)
    {
        _eulerAngles.x += -look.y * mouseSensitivity;
        _eulerAngles.y += look.x * mouseSensitivity;
        _eulerAngles.x = Mathf.Clamp(_eulerAngles.x, -maxCameraPitch, maxCameraPitch);
        transform.eulerAngles = new Vector3(_eulerAngles.x, _eulerAngles.y, 0f);
    }

    void SetupViewModelOverlayCamera()
    {
        if (_mainCamera == null) return;

        int viewModelLayer = LayerMask.NameToLayer(ViewModelLayerName);
        if (viewModelLayer < 0) return;

        _viewModelLayerMask = 1 << viewModelLayer;
        Transform viewModel = _mainCamera.transform.Find("ViewModel");
        if (viewModel == null)
        {
            viewModel = transform.Find("ViewModel");
        }

        if (viewModel != null)
        {
            SetLayerRecursively(viewModel, viewModelLayer);
        }

        _mainCamera.cullingMask &= ~_viewModelLayerMask;

        GameObject overlayCameraObject = new GameObject("ViewModelOverlayCamera");
        overlayCameraObject.transform.SetParent(_mainCamera.transform, false);
        _viewModelCamera = overlayCameraObject.AddComponent<Camera>();
        SyncViewModelOverlayCamera();

        _viewModelCamera.clearFlags = CameraClearFlags.Depth;
        _viewModelCamera.cullingMask = _viewModelLayerMask;
        _viewModelCamera.depth = _mainCamera.depth + 1f;
        _viewModelCamera.useOcclusionCulling = false;

        UniversalAdditionalCameraData mainCameraData = _mainCamera.GetUniversalAdditionalCameraData();
        UniversalAdditionalCameraData overlayCameraData = _viewModelCamera.GetUniversalAdditionalCameraData();
        overlayCameraData.renderType = CameraRenderType.Overlay;
        overlayCameraData.renderPostProcessing = false;
        overlayCameraData.requiresDepthTexture = false;
        overlayCameraData.requiresColorTexture = false;

        if (!mainCameraData.cameraStack.Contains(_viewModelCamera))
        {
            mainCameraData.cameraStack.Add(_viewModelCamera);
        }
    }

    void SyncViewModelOverlayCamera()
    {
        if (_mainCamera == null || _viewModelCamera == null) return;

        _viewModelCamera.fieldOfView = _mainCamera.fieldOfView;
        _viewModelCamera.nearClipPlane = _mainCamera.nearClipPlane;
        _viewModelCamera.farClipPlane = _mainCamera.farClipPlane;
        _viewModelCamera.rect = _mainCamera.rect;
        _viewModelCamera.allowHDR = _mainCamera.allowHDR;
        _viewModelCamera.allowMSAA = _mainCamera.allowMSAA;
    }

    static void SetLayerRecursively(Transform root, int layer)
    {
        root.gameObject.layer = layer;

        foreach (Transform child in root)
        {
            SetLayerRecursively(child, layer);
        }
    }

    Transform FindCameraTarget()
    {
        Player player = GetComponentInParent<Player>(true);
        if (player == null) return null;

        foreach (string targetName in new[] { "CameraTarget", "CameraFollowTarget", "Camera Target", "ViewTarget" })
        {
            Transform target = player.transform.Find(targetName);
            if (target != null) return target;
        }

        return null;
    }
}
