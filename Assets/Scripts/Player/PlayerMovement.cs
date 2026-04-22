using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(PlayerInputReader))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float jumpVelocity = 7f;
    [SerializeField] private float groundCheckExtraDistance = 0.2f;
    [SerializeField] private LayerMask groundLayers = ~0;
    [SerializeField] private bool rotateToMoveDirection = true;
    [SerializeField] private float moveDeadzone = 0.2f;
    [SerializeField] private float jumpBufferTime = 0.05f;

    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;
    private PlayerInputReader inputReader;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        inputReader = GetComponent<PlayerInputReader>();
    }

    private void FixedUpdate()
    {
        Vector2 moveInput = inputReader.Move;

        if (moveInput.magnitude < moveDeadzone)
        {
            moveInput = Vector2.zero;
        }

        Vector3 moveDirection = new Vector3(moveInput.x, 0f, moveInput.y);

        Vector3 velocity = rb.linearVelocity;
        velocity.x = moveDirection.x * moveSpeed;
        velocity.z = moveDirection.z * moveSpeed;

        bool isGrounded = IsGrounded();

        if (isGrounded && inputReader.HasBufferedJump(jumpBufferTime))
        {
            inputReader.ConsumeJumpPressed();
            velocity.y = jumpVelocity;
        }

        rb.linearVelocity = velocity;

        if (rotateToMoveDirection && moveDirection.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(moveDirection, Vector3.up);
        }
    }

    private bool IsGrounded()
    {
        Bounds bounds = capsuleCollider.bounds;

        Vector3 checkPosition = new Vector3(
            bounds.center.x,
            bounds.min.y - 0.02f,
            bounds.center.z
        );

        float checkRadius = bounds.extents.x * 0.8f;

        return Physics.CheckSphere(
            checkPosition,
            checkRadius,
            groundLayers,
            QueryTriggerInteraction.Ignore
        );
    }
}