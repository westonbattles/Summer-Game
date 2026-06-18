using UnityEngine;


public class PlayerLook : MonoBehaviour
{
    [SerializeField] float mouseSensitivity = 0.4f;

    PlayerInputReader input;
    float pitch;
    float yaw;

    public float Pitch => pitch;
    public float Yaw => yaw;
    public Quaternion LookRotation => Quaternion.Euler(pitch, yaw, 0f);

    void Awake()
    {
        input = GetComponent<PlayerInputReader>();

        Vector3 euler = transform.eulerAngles;
        pitch = euler.x;
        yaw = euler.y;
    }
    
    void Update()
    {
        
        if (input == null) return;

        Vector2 look = input.Look;
        
        pitch -= look.y * mouseSensitivity;
        yaw += look.x * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, -85f, 85f);
        yaw = Mathf.Repeat(yaw + 180f, 360f) - 180f;
    }
    
}
