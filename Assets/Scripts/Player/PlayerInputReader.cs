using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-100)]
public class PlayerInputReader : MonoBehaviour
{
    [SerializeField] bool autoBhop;

    public Vector2 Move { get; private set; }
    public Vector2 Look { get; private set; }
    public bool JumpPressed { get; private set; }
    public bool SwapPressed { get; private set; }
    
    public bool PrimaryAbilityPressed { get; private set; }
    public bool SecondaryAbilityPressed { get; private set; }
    public bool MovementAbilityPressed { get; private set; }
    public bool SpecialAbilityPressed { get; private set; }

    InputAction _moveAction;
    InputAction _lookAction;
    InputAction _jumpAction;
    InputAction _swapAction;
    InputAction _primaryAbilityAction;
    InputAction _secondaryAbilityAction;
    InputAction _movementAbilityAction;
    InputAction _specialAbilityAction;

    void Awake()
    {
        _moveAction = FindInputAction("Move");
        _lookAction = FindInputAction("Look");
        _jumpAction = FindInputAction("Jump");
        _swapAction = FindInputAction("Swap");
        _primaryAbilityAction = FindInputAction("PrimaryAbility");
        _secondaryAbilityAction = FindInputAction("SecondaryAbility");
        _movementAbilityAction = FindInputAction("MovementAbility");
        _specialAbilityAction = FindInputAction("SpecialAbility");
        SetCursorLocked(true);
    }

    void Update()
    {
        ReadInput();
    }

    public void ReadInput()
    {
        Move = _moveAction?.ReadValue<Vector2>() ?? Vector2.zero; // defaults to zero if the action returns null
        Look = _lookAction?.ReadValue<Vector2>() ?? Vector2.zero; // defaults to zero if the action returns null
        JumpPressed = _jumpAction != null && (autoBhop ? _jumpAction.IsPressed() : _jumpAction.WasPressedThisFrame()); // defaults to false if the action returns null
        SwapPressed = _swapAction != null && _swapAction.WasPressedThisFrame();
        PrimaryAbilityPressed = _primaryAbilityAction != null && _primaryAbilityAction.IsPressed(); // ispressed = hold to fire
        SecondaryAbilityPressed = _secondaryAbilityAction != null && _secondaryAbilityAction.IsPressed(); // ispressed = hold to fire
        MovementAbilityPressed = _movementAbilityAction != null && _movementAbilityAction.IsPressed(); // ispressed = hold to fire
        SpecialAbilityPressed = _specialAbilityAction != null && _specialAbilityAction.IsPressed(); // ispressed = hold to fire
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
