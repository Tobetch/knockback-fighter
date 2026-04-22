using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    private enum FollowMode
    {
        OffsetFollow = 0,
        SideViewLockZ = 1,
        SideViewLockX = 2,
    }

    [SerializeField] private Transform target;
    [SerializeField] private FollowMode mode = FollowMode.SideViewLockZ;

    [Header("Offset Follow")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 6f, -8f);

    [Header("Side View")]
    [SerializeField] private float sideDistance = 12f;
    [SerializeField] private float sideHeight = 6f;
    [SerializeField] private float lockedAxisValue = -12f;
    [SerializeField] private bool lockRotationInSideView = true;
    [SerializeField] private float sideViewPitchDegrees = 0f;

    [SerializeField] private float positionSmoothTime = 0.12f;
    [SerializeField] private bool lookAtTarget = true;
    [SerializeField] private Vector3 lookAtOffset = new Vector3(0f, 1.2f, 0f);

    private Vector3 positionVelocity;

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 desired = GetDesiredPosition();
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref positionVelocity, positionSmoothTime);

        if (IsSideViewMode() && lockRotationInSideView)
        {
            transform.rotation = GetSideViewRotation();
        }
        else if (lookAtTarget)
        {
            transform.LookAt(target.position + lookAtOffset, Vector3.up);
        }
    }

    private bool IsSideViewMode()
    {
        return mode == FollowMode.SideViewLockZ || mode == FollowMode.SideViewLockX;
    }

    private Quaternion GetSideViewRotation()
    {
        // 「斜め上」になる主因は LookAt による上下角度。
        // サイドビューでは向きを固定し、必要なら pitch だけ任意で付ける。
        switch (mode)
        {
            case FollowMode.SideViewLockX:
                // +X 方向から見る（カメラ forward が -X を向く）
                return Quaternion.Euler(sideViewPitchDegrees, -90f, 0f);
            case FollowMode.SideViewLockZ:
            default:
                // -Z から +Z 方向へ見る（Unityのidentity forwardは+Z）
                return Quaternion.Euler(sideViewPitchDegrees, 0f, 0f);
        }
    }

    private Vector3 GetDesiredPosition()
    {
        Vector3 t = target.position;

        switch (mode)
        {
            case FollowMode.OffsetFollow:
                return t + offset;

            case FollowMode.SideViewLockX:
                // カメラは +X 側（or 固定X）から真横に見る想定
                return new Vector3(lockedAxisValue, t.y + sideHeight, t.z);

            case FollowMode.SideViewLockZ:
            default:
                // カメラは -Z 側（or 固定Z）から真横に見る想定
                // X/Y を追従し、Z は固定して「斜め」にならないようにする
                return new Vector3(t.x, t.y + sideHeight, lockedAxisValue);
        }
    }
}

