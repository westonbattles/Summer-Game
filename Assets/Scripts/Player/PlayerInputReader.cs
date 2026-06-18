using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-100)]
public class PlayerInputReader : MonoBehaviour
{
    [SerializeField] bool autoBhop;

    public Vector2 Move { get; private set; }
    public Vector2 Look { get; private set; }
    public bool JumpPressed { get; private set; }

    InputAction _moveAction;
    InputAction _lookAction;
    InputAction _jumpAction;

    void Awake()
    {
        _moveAction = FindInputAction("Move");
        _lookAction = FindInputAction("Look");
        _jumpAction = FindInputAction("Jump");
        SetCursorLocked(true);
    }

    void Update()
    {
        ReadInput();
    }

    public void ReadInput()
    {
        Move = _moveAction?.ReadValue<Vector2>() ?? Vector2.zero;
        Look = _lookAction?.ReadValue<Vector2>() ?? Vector2.zero;
        JumpPressed = _jumpAction != null && (autoBhop ? _jumpAction.IsPressed() : _jumpAction.WasPressedThisFrame());
    }

    public void Configure(bool useAutoBhop)
    {
        autoBhop = useAutoBhop;
    }

    InputAction FindInputAction(string actionName)
    {
        return InputSystem.actions != null ? InputSystem.actions.FindAction(actionName, false) : null;
    }

    public void SetCursorLocked(bool locked)
    {
        Cursor.visible = !locked;
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
    }
}
