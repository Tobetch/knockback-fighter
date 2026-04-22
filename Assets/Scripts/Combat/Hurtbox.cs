using UnityEngine;

public class Hurtbox : MonoBehaviour
{
    [SerializeField] private Rigidbody targetRigidbody;
    private RespawnInvincibility respawnInvincibility;

    public Rigidbody TargetRigidbody => targetRigidbody;
    public bool IsInvincible
    {
        get
        {
            if (respawnInvincibility == null)
            {
                respawnInvincibility = GetComponentInParent<RespawnInvincibility>();
            }

            return respawnInvincibility != null && respawnInvincibility.IsInvincible;
        }
    }

    private void Awake()
    {
        if (targetRigidbody == null)
        {
            targetRigidbody = GetComponentInParent<Rigidbody>();
        }

        respawnInvincibility = GetComponentInParent<RespawnInvincibility>();
    }
}

