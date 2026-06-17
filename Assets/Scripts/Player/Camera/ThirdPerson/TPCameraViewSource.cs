using UnityEngine;

public abstract class TPCameraViewSource : MonoBehaviour
{
    public abstract CameraParams GetParams();
    public abstract Quaternion LookRotation { get; }
}
