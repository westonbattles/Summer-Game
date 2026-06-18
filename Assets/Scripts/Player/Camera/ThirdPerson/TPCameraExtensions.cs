// little polygon my goat: https://blog.littlepolygon.com/posts/cameras/
using UnityEngine;

public struct CameraParams
{
    public Vector3 TrackingPosition; // 3D world position of the player
    public Vector2 Framing; // 2D screen position of the player [(-1,1), (1,1)]
    public float Distance; // offset between the camera and the player
    public float Pitch; // vertical camera tilt
    public float Yaw; // horizontal camera pan
}

public struct CameraBlend {
    public Vector2 Framing;
    public float Pitch;
    public float Yaw;
    public float Distance;
}

public static class TPCameraExtensions
{

    public static void CalculateCameraPlacement(this Camera camera, in CameraParams cameraParams, out Vector3 position, out Quaternion rotation)
    {
        // (UNITY DEFAULTS TO VERTICAL FOV!!!)
        // tan(fov/2) is the tan (opposite/adj) of the triangle created by half our vertical fov
        // let t = the point that lies cameraParams.distance away from the center of the camera
        // then tan(fov/2) = the distance (world distance!) from s to the top of the screen / cameraParams.distance
        // "the distance from s to the top of the screen" in this case represents the magnitude of a vector starting at s, and pointing "up" perpendicular to the camera (so not straight up like y axis up)
        float halfHeight = Mathf.Tan(0.5f * Mathf.Deg2Rad * camera.fieldOfView) * cameraParams.Distance;
        float halfWidth = halfHeight * camera.aspect;
        
        // this vector points from the camera, to the offset "framing" coordinates on the view plane that is 'distance' away from the camera. keep in mind localoffset magnitude can be greater than distance (since distance is dist to the view plane, but not every point on the view plane
        // framing ranges from (-1,-1) to (1,1) so we can just multiply here
        var localOffset = new Vector3(
            halfWidth * cameraParams.Framing.x,
            halfHeight * cameraParams.Framing.y,
            cameraParams.Distance
        );

        // rotation is a quaternion simply represeting the rotation of our camera. multiplying it by any forward facing vector (aka (0,0,1)) rotates that vector to be facing the direction our camera is facing
        rotation = Quaternion.Euler(cameraParams.Pitch, cameraParams.Yaw, 0);
        
        // remember, localOffset (assuming no framing!) is a forward facing vector with magnitude cameraParams.Distance. with framing, it gets moved left/right/up/down a bit and the magnitude is a bit larger, but it doesn't matter. the thing to note here is that this vector is oriented 'forward'
        // because the vector is 'oriented forward', when we multiply it with our rotation quaternion (our camera's rotation), this vector will now point in the same direction as our camera (with any framing offsets applied as well)
        // so when we subtract this from trackingposition, we're essentially moving straight backwards (based on our orientation) with a distance of cameraParams.distance
        // this means our camera will be positioned behind the trackingposition cameraParams.distance away (relative to the player, meaning as a result the camera will be looking at the player)
        position = cameraParams.TrackingPosition - rotation * localOffset; 
    }
    
    public static void SetParams(this Camera camera, in CameraParams cameraParams)
    {
        camera.CalculateCameraPlacement(cameraParams, out Vector3 position, out Quaternion rotation);
        camera.transform.SetPositionAndRotation(position, rotation);
    }
    
    public static void SetParams(this Camera camera, in Vector3 position, in Quaternion rotation)
    {
        camera.transform.SetPositionAndRotation(position, rotation);
    }
    

    public static void GetParams(
        this Camera camera,
        out CameraParams result,
        in Vector3 trackingPosition
    )
    {
        // Tracking Position
        result.TrackingPosition = trackingPosition;

        // Pitch/Yaw
        Vector3 cameraEulerAngles = camera.transform.eulerAngles;
        result.Pitch = cameraEulerAngles.x;
        result.Yaw = cameraEulerAngles.y;
        
        
        Vector3 position = camera.transform.position;
        Quaternion rotation = camera.transform.rotation;
        
        // Distance
        // you cant just do Vector3.Distance(trackingPosition, camera.transform.position); because this doesn't account for the player offset,
        // distance is supposed to be the distance from the camera to the center of the viewplane that the player is on. if they have different framing offsets, using Vector3.Distance will make distance grow when it should remain constant
        Vector3 cameraToTarget = trackingPosition - position;
        Vector3 forward = rotation * Vector3.forward; // camera forward in world space
        result.Distance = Vector3.Dot(cameraToTarget, forward); // the vector pointing from the camera to the trackingposition guy gets projected along the unit vector (facing directly toward the center of the viewplane), and the magnitudes of the projection and the unit vector get multiplied. unit vector mag is 1, so this returns distance from camera to the center of the viewplane bangg
        
        // Framing
        // "undoes" the orientation of the vector pointing from the camera to the target
        // this makes the vector, instead align with the direction of the z+ axis (keeping its offset).
        // note: the z component of this vector is garunteed to be result.Distance and now (x, y) will correspond to our offset
        // so essentially: after this, z is forward depth, x is camera-right offset, and y is camera-up offset.
        Vector3 localTarget = Quaternion.Inverse(rotation) * cameraToTarget;
        
        float halfHeight = Mathf.Tan(0.5f * Mathf.Deg2Rad * camera.fieldOfView) * result.Distance;
        float halfWidth = halfHeight * camera.aspect;
        // screen space [(-1,-1), (1,1)]
        result.Framing = new Vector2(
            localTarget.x / halfWidth,
            localTarget.y / halfHeight
        );
    }
    
    public static void ComputeBlendout(
        this Camera camera,
        out CameraBlend result,
        in CameraParams newParams
    ) {
        camera.GetParams(out CameraParams oldParams, in newParams.TrackingPosition);

        // offset from the new target to the current view
        result.Framing = oldParams.Framing - newParams.Framing;
        result.Pitch = oldParams.Pitch - newParams.Pitch;
        result.Yaw = oldParams.Yaw - newParams.Yaw;
        result.Distance = oldParams.Distance - newParams.Distance;

        // yaw can wrap-around
        if (result.Yaw > 180f) { result.Yaw -= 360f; }
        else if (result.Yaw < -180f) { result.Yaw += 360f; }
    }

    public static void SetParamsBlended(
        this Camera camera,
        in CameraParams camParams,
        in CameraBlend blendout,
        float progress
    ) {
        var multi = 1f - Mathf.Clamp01(progress);

        var result = camParams;
        result.Framing += multi * blendout.Framing;
        result.Pitch += multi * blendout.Pitch;
        result.Yaw += multi * blendout.Yaw;
        result.Distance += multi * blendout.Distance;

        camera.SetParams(in result);
    }
}