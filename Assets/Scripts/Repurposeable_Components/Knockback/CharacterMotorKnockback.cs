using KinematicCharacterController;
using UnityEngine;

[RequireComponent(typeof(KinematicCharacterMotor))]
public class CharacterMotorKnockback : MonoBehaviour, IKnockbackable
{
    [Tooltip("Higher values mean less knockback.")]
    [SerializeField] private float knockbackResistance = 1f;
    [SerializeField] private float knockbackImmunityDuration = 0.25f;
    private float immunityUntil;

    private KinematicCharacterMotor motor;

    private void Awake()
    {
        motor = GetComponent<KinematicCharacterMotor>();
        if (motor == null)
        {
            Debug.LogError("KinematicCharacterMotor component not found on " + gameObject.name);
        }
    }

    public void ApplyKnockback(Vector3 source, float strength)
    {
        if (Time.time < immunityUntil)
        {
            return;
        }
        immunityUntil = Time.time + knockbackImmunityDuration;
        Vector3 direction = transform.position - source;
        direction.y = 3f;
        direction.Normalize();

        motor.ForceUnground();
        motor.BaseVelocity += direction * (strength / knockbackResistance);
    }
}
