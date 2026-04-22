using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class FallRespawn : MonoBehaviour
{
    [SerializeField] private float fallY = -10f;
    [SerializeField] private Transform respawnPoint;
    [SerializeField] private float fallbackHeightAboveStage = 1.5f;
    [SerializeField] private Vector3 fallbackRespawnPosition = new(0f, 2f, 0f);

    private Rigidbody rb;
    private Vector3 resolvedFallbackRespawnPosition;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        resolvedFallbackRespawnPosition = ResolveFallbackRespawnPosition();
    }

    private void Update()
    {
        if (transform.position.y >= fallY)
        {
            return;
        }

        Respawn();
    }

    public void Respawn()
    {
        Vector3 respawnPosition = resolvedFallbackRespawnPosition;
        Quaternion respawnRotation = transform.rotation;

        if (respawnPoint != null)
        {
            respawnPosition = respawnPoint.position;
            respawnRotation = respawnPoint.rotation;
        }

        transform.SetPositionAndRotation(respawnPosition, respawnRotation);
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    private Vector3 ResolveFallbackRespawnPosition()
    {
        GameObject ground = GameObject.Find("Ground");
        if (ground == null)
        {
            return fallbackRespawnPosition;
        }

        Collider groundCollider = ground.GetComponent<Collider>();
        if (groundCollider != null)
        {
            Bounds b = groundCollider.bounds;
            return new Vector3(b.center.x, b.max.y + fallbackHeightAboveStage, b.center.z);
        }

        Renderer groundRenderer = ground.GetComponent<Renderer>();
        if (groundRenderer != null)
        {
            Bounds b = groundRenderer.bounds;
            return new Vector3(b.center.x, b.max.y + fallbackHeightAboveStage, b.center.z);
        }

        return fallbackRespawnPosition;
    }
}

