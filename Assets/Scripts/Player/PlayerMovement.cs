using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerInputReader))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private bool rotateToMoveDirection = true;

    private Rigidbody rb;
    private PlayerInputReader inputReader;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        inputReader = GetComponent<PlayerInputReader>();
    }

    private void FixedUpdate()
    {
        Vector2 moveInput = inputReader.Move;
        Vector3 moveDirection = new Vector3(moveInput.x, 0f, moveInput.y);

        Vector3 velocity = rb.linearVelocity;
        velocity.x = moveDirection.x * moveSpeed;
        velocity.z = moveDirection.z * moveSpeed;
        rb.linearVelocity = velocity;

        if (rotateToMoveDirection && moveDirection.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(moveDirection, Vector3.up);
        }
    }
}