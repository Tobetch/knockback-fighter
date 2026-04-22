using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Hitbox : MonoBehaviour
{
    [SerializeField] private bool disableOnStart = true;

    private Collider hitCollider;

    public bool Enabled
    {
        get => hitCollider != null && hitCollider.enabled;
        set
        {
            if (hitCollider != null)
            {
                hitCollider.enabled = value;
            }
        }
    }

    private void Awake()
    {
        hitCollider = GetComponent<Collider>();
        hitCollider.isTrigger = true;

        if (disableOnStart)
        {
            hitCollider.enabled = false;
        }
    }
}

