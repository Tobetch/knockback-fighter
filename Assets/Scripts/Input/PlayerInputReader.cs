using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputReader : MonoBehaviour
{
    private PlayerControls controls;

    public Vector2 Move { get; private set; }

    private bool jumpPressed;
    private bool attackPressed;

    private void Awake()
    {
        controls = new PlayerControls();
    }

    private void OnEnable()
    {
        controls.Player.Enable();

        controls.Player.Move.performed += OnMovePerformed;
        controls.Player.Move.canceled += OnMoveCanceled;

        controls.Player.Jump.performed += OnJumpPerformed;
        controls.Player.Attack.performed += OnAttackPerformed;
    }

    private void OnDisable()
    {
        controls.Player.Move.performed -= OnMovePerformed;
        controls.Player.Move.canceled -= OnMoveCanceled;

        controls.Player.Jump.performed -= OnJumpPerformed;
        controls.Player.Attack.performed -= OnAttackPerformed;

        controls.Player.Disable();
    }

    public bool ConsumeJumpPressed()
    {
        if (!jumpPressed)
        {
            return false;
        }

        jumpPressed = false;
        return true;
    }

    public bool ConsumeAttackPressed()
    {
        if (!attackPressed)
        {
            return false;
        }

        attackPressed = false;
        return true;
    }

    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        Move = context.ReadValue<Vector2>();
    }

    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        Move = Vector2.zero;
    }

    private void OnJumpPerformed(InputAction.CallbackContext context)
    {
        jumpPressed = true;
    }

    private void OnAttackPerformed(InputAction.CallbackContext context)
    {
        attackPressed = true;
    }
}