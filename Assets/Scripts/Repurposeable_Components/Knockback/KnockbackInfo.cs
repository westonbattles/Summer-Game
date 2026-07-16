using UnityEngine;

public struct KnockbackInfo
{
    private const float MinDirectionSqr = 0.0001f;
    
    public Vector3 Direction;
    public float Strength;
    public float Duration; // Optional. Rigidbody-based receivers can ignore it; NavMesh receivers can use a fallback when this is 0.
    public GameObject Source;

    public bool HasDirection => Direction.sqrMagnitude > MinDirectionSqr;
    
    public KnockbackInfo(Vector3 direction, float strength, float duration = 0f, GameObject source = null)
    {
        direction.y = 0f;

        Direction = direction.sqrMagnitude > MinDirectionSqr ? direction.normalized : Vector3.zero;
        Strength = Mathf.Max(0f, strength);
        Duration = Mathf.Max(0f, duration);
        Source = source;
    }

    public static KnockbackInfo AwayFrom(Vector3 sourcePosition, Vector3 targetPosition, float strength, float duration = 0f, GameObject source = null)
    {
        return new KnockbackInfo(targetPosition - sourcePosition, strength, duration, source);
    }
}
