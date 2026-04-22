using UnityEngine;

public static class RespawnBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureRespawnComponentExists()
    {
        PlayerMovement player = Object.FindAnyObjectByType<PlayerMovement>();
        if (player != null)
        {
            EnsureFallRespawn(player.gameObject);
        }

        Hurtbox[] hurtboxes = Object.FindObjectsByType<Hurtbox>();
        for (int i = 0; i < hurtboxes.Length; i++)
        {
            Hurtbox hurtbox = hurtboxes[i];
            if (hurtbox == null || hurtbox.TargetRigidbody == null)
            {
                continue;
            }

            EnsureFallRespawn(hurtbox.TargetRigidbody.gameObject);
        }
    }

    private static void EnsureFallRespawn(GameObject target)
    {
        if (target == null || target.GetComponent<FallRespawn>() != null)
        {
            return;
        }

        target.AddComponent<FallRespawn>();
    }
}
