using KinematicCharacterController;
using UnityEngine;

public class PlayerOutOfBounds : MonoBehaviour
{
    [SerializeField] private float deathYBoundary = -80f;
    [SerializeField] private Transform spawnPoint;

    private KinematicCharacterMotor motor;

    private void Awake()
    {
        motor = GetComponent<KinematicCharacterMotor>();
    }

    private void Update()
    {
        if (transform.position.y < deathYBoundary)
        {
            Respawn();
        }
    }

    private void Respawn()
    {
        if (motor != null && spawnPoint != null)
        {
            motor.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
            motor.BaseVelocity = Vector3.zero;
        }
    }
}