using UnityEngine;

public abstract class TPCameraViewSource : MonoBehaviour
{
    
    [SerializeField] Vector3 trackingOffset;
    
    public abstract CameraParams GetParams();
    public abstract Quaternion LookRotation { get; }
    
    protected Vector3 ApplyTrackingOffset(Vector3 position)
    {
        return position + trackingOffset;
    }
}
