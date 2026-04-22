using UnityEngine;

public sealed class PenguinAttackFlap : MonoBehaviour
{
    [Header("Wing Bone Names")]
    [SerializeField] private string[] leftWingBoneNames = { "DEF-Wing.L", "DEF-Wing.001.L", "DEF-Wing.002.L" };
    [SerializeField] private string[] rightWingBoneNames = { "DEF-Wing.R", "DEF-Wing.001.R", "DEF-Wing.002.R" };

    [Header("Timing")]
    [SerializeField] private float windupDuration = 0.05f;
    [SerializeField] private float slapDuration = 0.07f;
    [SerializeField] private float recoverDuration = 0.09f;

    [Header("Angles")]
    [SerializeField] private float leftWindupZ = 20f;
    [SerializeField] private float leftSlapZ = -55f;
    [SerializeField] private float rightWindupZ = -20f;
    [SerializeField] private float rightSlapZ = 55f;

    [Header("Forward Reach")]
    [SerializeField] private float wingWindupRetract = -0.009f;
    [SerializeField] private float wingForwardReach = 0.0144f;
    [SerializeField] private float wingRootWeight = 0.25f;
    [SerializeField] private float wingMidWeight = 0.4f;
    [SerializeField] private float wingTipWeight = 0.6f;
    [SerializeField] private float maxPerBoneForwardOffset = 0.0144f;

    private Transform[] leftWings = new Transform[0];
    private Transform[] rightWings = new Transform[0];
    private Quaternion[] leftBaseRotations = new Quaternion[0];
    private Quaternion[] rightBaseRotations = new Quaternion[0];
    private Vector3[] leftBasePositions = new Vector3[0];
    private Vector3[] rightBasePositions = new Vector3[0];
    private float motionUntilTime = float.NegativeInfinity;
    private bool wasAnimating;

    private void Awake()
    {
        // モデルのボーンスケールが大きい場合でも「前方突き出し」が暴れないよう、
        // 位置オフセット量はかなり小さくクランプしておく。
        wingWindupRetract = Mathf.Clamp(wingWindupRetract, -0.02f, 0f);
        wingForwardReach = Mathf.Clamp(wingForwardReach, 0f, 0.02f);
        maxPerBoneForwardOffset = Mathf.Clamp(maxPerBoneForwardOffset, 0.001f, 0.02f);
        wingRootWeight = Mathf.Clamp(wingRootWeight, 0f, 1f);
        wingMidWeight = Mathf.Clamp(wingMidWeight, 0f, 1f);
        wingTipWeight = Mathf.Clamp(wingTipWeight, 0f, 1f);
        CacheWingBones();
    }

    private void LateUpdate()
    {
        if (leftWings.Length == 0 || rightWings.Length == 0)
        {
            return;
        }

        if (Time.time >= motionUntilTime)
        {
            if (wasAnimating)
            {
                ApplyPose(0f, 0f, 0f);
                wasAnimating = false;
            }
            return;
        }

        float timeLeft = motionUntilTime - Time.time;
        float total = Mathf.Max(0.0001f, windupDuration + slapDuration + recoverDuration);
        float elapsed = total - Mathf.Clamp(timeLeft, 0f, total);

        if (elapsed < windupDuration)
        {
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, windupDuration));
            ApplyPose(
                Mathf.Lerp(0f, leftWindupZ, t),
                Mathf.Lerp(0f, rightWindupZ, t),
                Mathf.Lerp(0f, wingWindupRetract, t)
            );
            return;
        }

        elapsed -= windupDuration;
        if (elapsed < slapDuration)
        {
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, slapDuration));
            ApplyPose(
                Mathf.Lerp(leftWindupZ, leftSlapZ, t),
                Mathf.Lerp(rightWindupZ, rightSlapZ, t),
                Mathf.Lerp(wingWindupRetract, wingForwardReach, t)
            );
            return;
        }

        elapsed -= slapDuration;
        float recoverT = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, recoverDuration));
        ApplyPose(
            Mathf.Lerp(leftSlapZ, 0f, recoverT),
            Mathf.Lerp(rightSlapZ, 0f, recoverT),
            Mathf.Lerp(wingForwardReach, 0f, recoverT)
        );
    }

    public void PlaySlap()
    {
        if (leftWings.Length == 0 || rightWings.Length == 0)
        {
            CacheWingBones();
        }

        if (leftWings.Length == 0 || rightWings.Length == 0)
        {
            return;
        }

        float total = Mathf.Max(0.01f, windupDuration + slapDuration + recoverDuration);
        motionUntilTime = Time.time + total;
        wasAnimating = true;
    }

    private void CacheWingBones()
    {
        leftWings = FindBones(leftWingBoneNames);
        rightWings = FindBones(rightWingBoneNames);
        leftBaseRotations = CaptureBaseRotations(leftWings);
        rightBaseRotations = CaptureBaseRotations(rightWings);
        leftBasePositions = CaptureBasePositions(leftWings);
        rightBasePositions = CaptureBasePositions(rightWings);
    }

    private void ApplyPose(float leftZ, float rightZ, float forwardOffset)
    {
        Vector3 worldAttackDir = ResolveWorldAttackDirection();
        ApplyWingChain(leftWings, leftBaseRotations, leftBasePositions, leftZ, forwardOffset, worldAttackDir);
        ApplyWingChain(rightWings, rightBaseRotations, rightBasePositions, rightZ, forwardOffset, worldAttackDir);
    }

    private void ApplyWingChain(
        Transform[] chain,
        Quaternion[] baseRotations,
        Vector3[] basePositions,
        float z,
        float forwardOffset,
        Vector3 worldAttackDir
    )
    {
        if (chain.Length == 0 || baseRotations.Length != chain.Length || basePositions.Length != chain.Length)
        {
            return;
        }

        for (int i = 0; i < chain.Length; i++)
        {
            float rotationWeight = GetChainWeight(i, chain.Length);
            chain[i].localRotation = baseRotations[i] * Quaternion.Euler(0f, 0f, z * Mathf.Max(0.4f, rotationWeight));

            Transform parent = chain[i].parent;
            if (parent == null)
            {
                chain[i].localPosition = basePositions[i];
                continue;
            }

            Vector3 localAttackDir = parent.InverseTransformDirection(worldAttackDir).normalized;
            float positionWeight = GetPositionWeight(i, chain.Length);
            float offset = Mathf.Clamp(forwardOffset * positionWeight, -maxPerBoneForwardOffset, maxPerBoneForwardOffset);
            chain[i].localPosition = basePositions[i] + localAttackDir * offset;
        }
    }

    private float GetChainWeight(int index, int chainLength)
    {
        if (chainLength <= 1)
        {
            return wingTipWeight;
        }

        float t = (float)index / (chainLength - 1);
        if (t <= 0.5f)
        {
            float midT = t / 0.5f;
            return Mathf.Lerp(wingRootWeight, wingMidWeight, midT);
        }

        float tipT = (t - 0.5f) / 0.5f;
        return Mathf.Lerp(wingMidWeight, wingTipWeight, tipT);
    }

    private float GetPositionWeight(int index, int chainLength)
    {
        if (chainLength <= 1)
        {
            return 0f;
        }

        if (index == 0)
        {
            // 根元は位置をほぼ固定しないと「翼が伸びる」見え方が強く出る。
            return 0f;
        }

        if (index == 1)
        {
            return 0.2f;
        }

        return 0.35f;
    }

    private void OnDisable()
    {
        if (leftWings.Length == 0 || rightWings.Length == 0)
        {
            return;
        }

        ApplyPose(0f, 0f, 0f);
        wasAnimating = false;
    }

    private Vector3 ResolveWorldAttackDirection()
    {
        Transform anchor = transform.parent;
        if (anchor == null)
        {
            return Vector3.right;
        }

        return anchor.forward.x >= 0f ? Vector3.right : Vector3.left;
    }

    private Transform[] FindBones(string[] names)
    {
        Transform[] bones = new Transform[names.Length];
        int count = 0;

        for (int i = 0; i < names.Length; i++)
        {
            Transform found = FindChildRecursive(transform, names[i]);
            if (found == null)
            {
                continue;
            }

            bones[count] = found;
            count++;
        }

        if (count == bones.Length)
        {
            return bones;
        }

        Transform[] trimmed = new Transform[count];
        for (int i = 0; i < count; i++)
        {
            trimmed[i] = bones[i];
        }

        return trimmed;
    }

    private static Quaternion[] CaptureBaseRotations(Transform[] bones)
    {
        Quaternion[] rotations = new Quaternion[bones.Length];
        for (int i = 0; i < bones.Length; i++)
        {
            rotations[i] = bones[i].localRotation;
        }

        return rotations;
    }

    private static Vector3[] CaptureBasePositions(Transform[] bones)
    {
        Vector3[] positions = new Vector3[bones.Length];
        for (int i = 0; i < bones.Length; i++)
        {
            positions[i] = bones[i].localPosition;
        }

        return positions;
    }

    private static Transform FindChildRecursive(Transform root, string name)
    {
        if (root.name == name)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildRecursive(root.GetChild(i), name);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
