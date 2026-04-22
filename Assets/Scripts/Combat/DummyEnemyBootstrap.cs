using UnityEngine;

public static class DummyEnemyBootstrap
{
    private const string DummyName = "DummyEnemy";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureDummyEnemyExists()
    {
        PlayerMovement player = Object.FindFirstObjectByType<PlayerMovement>();
        Hurtbox existingHurtbox = Object.FindFirstObjectByType<Hurtbox>();
        if (existingHurtbox != null)
        {
            AlignPlayerTowards(player, existingHurtbox.transform.position);
            return;
        }

        Vector3 spawnPosition = player != null
            ? new Vector3(player.transform.position.x + 3f, player.transform.position.y, player.transform.position.z)
            : new Vector3(3f, 1f, 0f);

        spawnPosition.y = Mathf.Max(1f, spawnPosition.y);

        GameObject dummy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        dummy.name = DummyName;
        dummy.transform.position = spawnPosition;

        Rigidbody rb = dummy.AddComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        dummy.AddComponent<Hurtbox>();
        AlignPlayerTowards(player, dummy.transform.position);
    }

    private static void AlignPlayerTowards(PlayerMovement player, Vector3 targetPosition)
    {
        if (player == null)
        {
            return;
        }

        float xDelta = targetPosition.x - player.transform.position.x;
        if (Mathf.Abs(xDelta) < 0.0001f)
        {
            return;
        }

        float sign = xDelta >= 0f ? 1f : -1f;
        player.transform.rotation = Quaternion.LookRotation(Vector3.right * sign, Vector3.up);
    }
}
