using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(PlayerInputReader))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float jumpVelocity = 7f;
    // 接地チェック距離が短すぎると、コライダー/床の厚み・物理の微小な浮きで grounded が取れず、
    // 「入力は入っているのにジャンプできない」が再発しやすい。まずは厚めのデフォルトで安定優先。
    [SerializeField] private float groundCheckExtraDistance = 0.6f;
    [SerializeField] private float maxGroundAngle = 55f;
    [SerializeField] private LayerMask groundLayers = ~0;
    [SerializeField] private bool rotateToMoveDirection = true;
    [SerializeField] private float moveDeadzone = 0.2f;
    [SerializeField] private float jumpBufferTime = 0.15f;
    [SerializeField] private float coyoteTime = 0.08f;
    [SerializeField] private float jumpMinTimeBetweenJumps = 0.08f;
    [SerializeField] private bool drawGroundCheckGizmos = true;

    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;
    private PlayerInputReader inputReader;

    private float lastGroundedTime = float.NegativeInfinity;
    private bool wasGroundedLastFixed;
    private RaycastHit lastGroundHit;
    private float lastJumpTime = float.NegativeInfinity;
    private readonly Collider[] overlapResults = new Collider[8];
    private readonly RaycastHit[] castHits = new RaycastHit[8];

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        inputReader = GetComponent<PlayerInputReader>();

        // NOTE:
        // `groundCheckExtraDistance` は SerializeField なので、シーン/Prefab 側に古い値が保存されていると
        // コード上のデフォルトを変えても反映されません。短すぎる値だと grounded が永遠に取れず、
        // 「入力は入っているのにジャンプできない」が再発します。最小値にクランプして安全側に倒します。
        groundCheckExtraDistance = Mathf.Max(groundCheckExtraDistance, 0.35f);
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

        bool isGrounded = IsGrounded(out lastGroundHit);
        if (isGrounded)
        {
            lastGroundedTime = Time.time;
        }

        bool canUseCoyote = Time.time - lastGroundedTime <= coyoteTime;
        bool canJumpAgain = Time.time - lastJumpTime >= jumpMinTimeBetweenJumps;

        if (canUseCoyote && canJumpAgain && inputReader.HasBufferedJump(jumpBufferTime))
        {
            inputReader.ConsumeJumpPressed();
            velocity.y = jumpVelocity;
            lastGroundedTime = float.NegativeInfinity;
            lastJumpTime = Time.time;
        }

        rb.linearVelocity = velocity;

        if (rotateToMoveDirection && moveDirection.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(moveDirection, Vector3.up);
        }

        wasGroundedLastFixed = isGrounded;
    }

    private bool IsGrounded(out RaycastHit groundHit)
    {
        groundHit = default;

        float castDistance = Mathf.Max(0.001f, groundCheckExtraDistance);

        GetCapsuleWorldPoints(out Vector3 point1, out Vector3 point2, out float radius);

        // 接触中でも拾えるように、まず OverlapCapsule で足元近傍の接触を確認。
        // その後、少し上から CapsuleCast して「開始時オーバーラップでヒットしない」を回避する。
        float probeRadius = Mathf.Max(0.001f, radius * 0.95f);
        Vector3 overlapP1 = point1 + Vector3.up * 0.02f;
        Vector3 overlapP2 = point2 + Vector3.up * 0.02f;

        int overlapCount = Physics.OverlapCapsuleNonAlloc(
            overlapP1,
            overlapP2,
            probeRadius,
            overlapResults,
            groundLayers,
            QueryTriggerInteraction.Collide
        );

        for (int i = 0; i < overlapCount; i++)
        {
            Collider c = overlapResults[i];
            if (c == null || c == capsuleCollider)
            {
                continue;
            }

            if (c.attachedRigidbody == rb)
            {
                continue;
            }

            return true;
        }

        // キャスト開始点を少し上げて、オーバーラップ状態からでも下方向のヒットを取りやすくする。
        Vector3 castP1 = point1 + Vector3.up * 0.03f;
        Vector3 castP2 = point2 + Vector3.up * 0.03f;

        int hitCount = Physics.CapsuleCastNonAlloc(
            castP1,
            castP2,
            probeRadius,
            Vector3.down,
            castHits,
            castDistance + 0.06f,
            groundLayers,
            QueryTriggerInteraction.Collide
        );

        if (TryPickBestGroundHit(hitCount, out groundHit))
        {
            return true;
        }

        // 最後のフォールバック: コライダー中心から下に Raycast。
        // オーバーラップや床形状によってキャスト系が拾えない環境でも grounded を取りやすい。
        Bounds b = capsuleCollider.bounds;
        float rayDistance = (b.extents.y + castDistance + 0.08f);
        Vector3 rayOrigin = b.center;
        if (Physics.Raycast(
                rayOrigin,
                Vector3.down,
                out RaycastHit rayHit,
                rayDistance,
                groundLayers,
                QueryTriggerInteraction.Collide
            ))
        {
            if (rayHit.collider != null && rayHit.collider != capsuleCollider && rayHit.rigidbody != rb)
            {
                float groundAngle = Vector3.Angle(rayHit.normal, Vector3.up);
                if (groundAngle <= maxGroundAngle)
                {
                    groundHit = rayHit;
                    return true;
                }
            }
        }

        return false;
    }

    private bool TryPickBestGroundHit(int hitCount, out RaycastHit groundHit)
    {
        groundHit = default;

        RaycastHit best = default;
        bool hasBest = false;
        float bestDistance = float.PositiveInfinity;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit h = castHits[i];
            if (h.collider == null || h.collider == capsuleCollider)
            {
                continue;
            }

            if (h.rigidbody == rb)
            {
                continue;
            }

            float groundAngle = Vector3.Angle(h.normal, Vector3.up);
            if (groundAngle > maxGroundAngle)
            {
                continue;
            }

            if (h.distance < bestDistance)
            {
                bestDistance = h.distance;
                best = h;
                hasBest = true;
            }
        }

        if (!hasBest)
        {
            return false;
        }

        groundHit = best;
        return true;
    }

    private void GetCapsuleWorldPoints(out Vector3 point1, out Vector3 point2, out float radius)
    {
        Vector3 center = transform.TransformPoint(capsuleCollider.center);
        Vector3 up = transform.up;

        float maxScale = Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);
        float heightScale = Mathf.Abs(transform.lossyScale.y);

        radius = capsuleCollider.radius * maxScale;
        float height = Mathf.Max(capsuleCollider.height * heightScale, radius * 2f);

        float pointOffset = Mathf.Max(0f, (height * 0.5f) - radius);

        point1 = center + up * pointOffset;
        point2 = center - up * pointOffset;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGroundCheckGizmos)
        {
            return;
        }

        CapsuleCollider cc = capsuleCollider != null ? capsuleCollider : GetComponent<CapsuleCollider>();
        if (cc == null)
        {
            return;
        }

        Vector3 center = transform.TransformPoint(cc.center);
        Vector3 up = transform.up;

        float maxScale = Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);
        float heightScale = Mathf.Abs(transform.lossyScale.y);

        float radius = cc.radius * maxScale;
        float height = Mathf.Max(cc.height * heightScale, radius * 2f);
        float pointOffset = Mathf.Max(0f, (height * 0.5f) - radius);

        Vector3 p1 = center + up * pointOffset;
        Vector3 p2 = center - up * pointOffset;

        Gizmos.color = wasGroundedLastFixed ? new Color(0.2f, 0.9f, 0.2f, 0.8f) : new Color(0.9f, 0.2f, 0.2f, 0.8f);
        DrawGizmoCapsule(p1, p2, radius);

        Gizmos.color = new Color(0.9f, 0.9f, 0.2f, 0.8f);
        Gizmos.DrawLine(p1, p1 + Vector3.down * groundCheckExtraDistance);
        Gizmos.DrawLine(p2, p2 + Vector3.down * groundCheckExtraDistance);
    }

    private static void DrawGizmoCapsule(Vector3 p1, Vector3 p2, float radius)
    {
        Gizmos.DrawWireSphere(p1, radius);
        Gizmos.DrawWireSphere(p2, radius);
        Gizmos.DrawLine(p1 + Vector3.forward * radius, p2 + Vector3.forward * radius);
        Gizmos.DrawLine(p1 - Vector3.forward * radius, p2 - Vector3.forward * radius);
        Gizmos.DrawLine(p1 + Vector3.right * radius, p2 + Vector3.right * radius);
        Gizmos.DrawLine(p1 - Vector3.right * radius, p2 - Vector3.right * radius);
    }
}