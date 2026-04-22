using UnityEngine;

public class Hurtbox : MonoBehaviour
{
    [SerializeField] private Rigidbody targetRigidbody;

    public Rigidbody TargetRigidbody => targetRigidbody;

    private void Awake()
    {
        if (targetRigidbody == null)
        {
            targetRigidbody = GetComponentInParent<Rigidbody>();
        }
    }
}

