using UnityEngine;

public abstract class TPViewSource : MonoBehaviour
{
    
    [SerializeField] Vector3 trackingOffset;
    protected Vector3 TrackingOffset => trackingOffset;
    
    public abstract CameraParams GetParams();
    public abstract Quaternion LookRotation { get; }
    
    // protected Vector3 ApplyTrackingOffset(Vector3 position)
    // {
    //     return position + trackingOffset;
    // }
}
