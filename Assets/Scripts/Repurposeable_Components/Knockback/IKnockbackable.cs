using UnityEngine;

public interface IKnockbackable
{
    //void ApplyKnockback(Vector3 source, float strength);
    void ApplyKnockback(KnockbackInfo info);
}
