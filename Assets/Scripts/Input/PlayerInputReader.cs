using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputReader : MonoBehaviour
{
    private PlayerControls controls;

    public Vector2 Move { get; private set; }

    private float jumpPressedTime = float.NegativeInfinity;
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

        controls.Player.Jump.started += OnJumpStarted;
        controls.Player.Attack.performed += OnAttackPerformed;
    }

    private void OnDisable()
    {
        controls.Player.Move.performed -= OnMovePerformed;
        controls.Player.Move.canceled -= OnMoveCanceled;

        controls.Player.Jump.started -= OnJumpStarted;
        controls.Player.Attack.performed -= OnAttackPerformed;

        controls.Player.Disable();
    }

    public bool HasBufferedJump(float bufferTime)
    {
        return Time.time - jumpPressedTime <= bufferTime;
    }

    public void ConsumeJumpPressed()
    {
        jumpPressedTime = float.NegativeInfinity;
    }

    public void ClearJumpPressed()
    {
        jumpPressedTime = float.NegativeInfinity;
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

    private void OnJumpStarted(InputAction.CallbackContext context)
    {
        jumpPressedTime = Time.time;
    }

    private void OnAttackPerformed(InputAction.CallbackContext context)
    {
        attackPressed = true;
    }
}