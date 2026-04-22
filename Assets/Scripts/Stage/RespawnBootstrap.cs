using UnityEngine;

public static class RespawnBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureRespawnComponentExists()
    {
        PlayerMovement player = Object.FindFirstObjectByType<PlayerMovement>();
        if (player == null)
        {
            return;
        }

        if (player.GetComponent<FallRespawn>() != null)
        {
            return;
        }

        player.gameObject.AddComponent<FallRespawn>();
    }
}
