using UnityEngine;

public class CharacterSideFacingVisual : MonoBehaviour
{
    [SerializeField] private float rightYaw = 90f;
    [SerializeField] private float leftYaw = -90f;

    private Transform facingRoot;

    public void Initialize(Transform root)
    {
        facingRoot = root;
        UpdateFacing();
    }

    private void LateUpdate()
    {
        UpdateFacing();
    }

    private void UpdateFacing()
    {
        Transform root = facingRoot != null ? facingRoot : transform.parent;
        if (root == null)
        {
            return;
        }

        float sign = root.forward.x >= 0f ? 1f : -1f;
        float yaw = sign >= 0f ? rightYaw : leftYaw;
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
    }
}
