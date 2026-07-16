using KinematicCharacterController;
using UnityEngine;

[RequireComponent(typeof(KinematicCharacterMotor))]
public class CharacterMotorKnockback : MonoBehaviour, IKnockbackable, IPoolable
{
    [Tooltip("Higher values mean less knockback.")]
    [SerializeField] private float knockbackResistance = 1f;
    [SerializeField] private float knockbackImmunityDuration = 0.25f;
    [SerializeField] private float verticalLift = 3f;
    private float immunityUntil;

    private KinematicCharacterMotor motor;

    private void Awake()
    {
        motor = GetComponent<KinematicCharacterMotor>();
    }

    public bool ApplyKnockback(KnockbackInfo info)
    {
        if (Time.time < immunityUntil) return false;
        if (!info.HasDirection) return false;

        immunityUntil = Time.time + knockbackImmunityDuration;

        Vector3 direction = info.Direction;
        direction.y = verticalLift;
        direction.Normalize();

        float resistance = Mathf.Max(0.001f, knockbackResistance);
        motor.ForceUnground();
        motor.BaseVelocity += direction * (info.Strength / resistance);
        return true;
    }

    public void OnSpawn()
    {
        ResetKnockbackState();
    }

    public void OnDespawn()
    {
        ResetKnockbackState();
    }

    private void ResetKnockbackState()
    {
        immunityUntil = 0f;

        if (motor == null) return;

        motor.BaseVelocity = Vector3.zero;
    }
}
