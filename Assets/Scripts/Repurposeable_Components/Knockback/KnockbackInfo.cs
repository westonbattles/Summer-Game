using UnityEngine;

public struct KnockbackInfo
{
    const float DefaultDuration = 0.15f;
    
    public Vector3 Direction;
    public float Strength;
    public float Duration; // duration for ai pathing to be ignored for. doesn't do anything to rb or character motor
    public GameObject Source;
    
    public KnockbackInfo(Vector3 direction, float strength, float duration = DefaultDuration, GameObject source = null)
    {
        Direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.zero;
        Strength = strength;
        Duration = duration;
        Source = source;
    }

    public static KnockbackInfo AwayFrom(Vector3 sourcePosition, Vector3 targetPosition, float strength, float duration = DefaultDuration, GameObject source = null)
    {
        return new KnockbackInfo(targetPosition - sourcePosition, strength, duration, source);
    }
}