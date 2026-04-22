using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerInputReader))]
public class PlayerAttack : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private Hitbox hitbox;

    [Header("Timing")]
    [SerializeField] private float attackActiveTime = 0.12f;
    [SerializeField] private float attackCooldown = 0.2f;

    [Header("Knockback")]
    [SerializeField] private float knockbackHorizontalSpeed = 8f;
    [SerializeField] private float knockbackVerticalSpeed = 2.5f;

    [Header("Hit Detection Fallback")]
    [SerializeField] private float fallbackHitRadius = 1.15f;
    [SerializeField] private float fallbackHitSideOffset = 0.9f;
    [SerializeField] private float fallbackHitHeightOffset = 0.25f;

    [Header("Attack Visual")]
    [SerializeField] private bool showAttackIndicator = true;
    [SerializeField] private float attackIndicatorDuration = 1f;
    [SerializeField] private Color attackIndicatorColor = new(1f, 0.2f, 0.2f, 0.15f);

    private PlayerInputReader inputReader;
    private float attackEndTime = float.NegativeInfinity;
    private float nextAttackTime = float.NegativeInfinity;

    private readonly HashSet<Hurtbox> hitThisAttack = new();
    private readonly Collider[] fallbackHitResults = new Collider[16];
    private GameObject attackIndicator;
    private Renderer attackIndicatorRenderer;
    private float attackIndicatorHideTime = float.NegativeInfinity;

    private void Awake()
    {
        inputReader = GetComponent<PlayerInputReader>();

        // SerializeField はシーン側に保存された値が優先されるため、
        // デフォルトを書き換えても見た目が変わらないことがある。
        // 弱攻撃の「真横」寄りを維持するため、高さは安全側で上限をかける。
        fallbackHitHeightOffset = Mathf.Min(fallbackHitHeightOffset, 0.1f);

        if (hitbox == null)
        {
            hitbox = GetComponentInChildren<Hitbox>();
        }

        SetupAttackIndicator();
    }

    private void Update()
    {
        UpdateAttackIndicatorLifetime();

        if (hitbox == null)
        {
            return;
        }

        if (IsAttacking())
        {
            TryHitByFallbackOverlap();
            UpdateAttackIndicatorTransform();

            if (Time.time >= attackEndTime)
            {
                EndAttack();
            }

            return;
        }

        if (Time.time < nextAttackTime)
        {
            return;
        }

        if (!inputReader.ConsumeAttackPressed())
        {
            return;
        }

        BeginAttack();
    }

    private void OnEnable()
    {
        if (hitbox != null)
        {
            hitbox.Enabled = false;
        }

        SetAttackIndicatorVisible(false);
        attackIndicatorHideTime = float.NegativeInfinity;
    }

    private void BeginAttack()
    {
        hitThisAttack.Clear();
        hitbox.Enabled = true;
        attackEndTime = Time.time + attackActiveTime;
        nextAttackTime = Time.time + attackCooldown;
        UpdateAttackIndicatorTransform();
        SetAttackIndicatorVisible(true);
        attackIndicatorHideTime = Time.time + Mathf.Max(0f, attackIndicatorDuration);
    }

    private void EndAttack()
    {
        hitbox.Enabled = false;
        attackEndTime = float.NegativeInfinity;
        SetAttackIndicatorVisible(false);
    }

    private bool IsAttacking()
    {
        return Time.time < attackEndTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsAttacking())
        {
            return;
        }

        if (hitbox != null && other == hitbox.GetComponent<Collider>())
        {
            return;
        }

        Hurtbox hurtbox = other.GetComponent<Hurtbox>();
        if (hurtbox == null)
        {
            hurtbox = other.GetComponentInParent<Hurtbox>();
        }

        TryApplyHit(hurtbox);
    }

    private void TryHitByFallbackOverlap()
    {
        Vector3 side = GetFlatSideDirection();
        Vector3 center = transform.position + Vector3.up * fallbackHitHeightOffset + side * fallbackHitSideOffset;

        int count = Physics.OverlapSphereNonAlloc(
            center,
            fallbackHitRadius,
            fallbackHitResults,
            ~0,
            QueryTriggerInteraction.Collide
        );

        for (int i = 0; i < count; i++)
        {
            Collider col = fallbackHitResults[i];
            if (col == null)
            {
                continue;
            }

            Hurtbox hurtbox = col.GetComponent<Hurtbox>();
            if (hurtbox == null)
            {
                hurtbox = col.GetComponentInParent<Hurtbox>();
            }

            TryApplyHit(hurtbox);
        }
    }

    private void TryApplyHit(Hurtbox hurtbox)
    {
        if (hurtbox == null)
        {
            return;
        }

        if (hurtbox.IsInvincible)
        {
            return;
        }

        if (!hitThisAttack.Add(hurtbox))
        {
            return;
        }

        Rigidbody targetRb = hurtbox.TargetRigidbody;
        if (targetRb == null)
        {
            return;
        }

        if (targetRb == GetComponent<Rigidbody>())
        {
            return;
        }

        Vector3 attackDirection = GetSideFacingDirection();
        Vector3 knockVelocity = attackDirection * knockbackHorizontalSpeed + Vector3.up * knockbackVerticalSpeed;
        targetRb.linearVelocity = knockVelocity;
    }

    private Vector3 GetFlatForward()
    {
        Vector3 forward = transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = Vector3.forward;
        }

        return forward.normalized;
    }

    private void SetupAttackIndicator()
    {
        if (!showAttackIndicator)
        {
            return;
        }

        attackIndicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        attackIndicator.name = "AttackIndicator";
        attackIndicator.transform.SetParent(transform, true);

        Collider indicatorCollider = attackIndicator.GetComponent<Collider>();
        if (indicatorCollider != null)
        {
            Destroy(indicatorCollider);
        }

        attackIndicatorRenderer = attackIndicator.GetComponent<Renderer>();
        if (attackIndicatorRenderer != null)
        {
            attackIndicatorRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            attackIndicatorRenderer.receiveShadows = false;
            attackIndicatorRenderer.material = CreateAttackIndicatorMaterial();
        }

        SetAttackIndicatorVisible(false);
        UpdateAttackIndicatorTransform();
    }

    private void UpdateAttackIndicatorTransform()
    {
        if (attackIndicator == null)
        {
            return;
        }

        Vector3 center = transform.position
                         + Vector3.up * fallbackHitHeightOffset
                         + GetFlatSideDirection() * fallbackHitSideOffset;
        attackIndicator.transform.position = center;
        attackIndicator.transform.localScale = Vector3.one * (fallbackHitRadius * 2f);
    }

    private void SetAttackIndicatorVisible(bool visible)
    {
        if (attackIndicator != null)
        {
            attackIndicator.SetActive(visible);
        }
    }

    private Material CreateAttackIndicatorMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        Material material;

        if (shader != null)
        {
            material = new Material(shader);
            material.SetFloat("_Surface", 1f); // 1 = Transparent
            material.SetFloat("_Blend", 0f);   // Alpha blend
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            material.SetColor("_BaseColor", attackIndicatorColor);
            return material;
        }

        // URP シェーダーが取れない環境向けフォールバック。
        shader = Shader.Find("Legacy Shaders/Transparent/Diffuse");
        material = shader != null ? new Material(shader) : new Material(Shader.Find("Standard"));
        material.color = attackIndicatorColor;
        return material;
    }

    private void UpdateAttackIndicatorLifetime()
    {
        if (attackIndicator == null)
        {
            return;
        }

        if (Time.time >= attackIndicatorHideTime)
        {
            SetAttackIndicatorVisible(false);
        }
    }

    private Vector3 GetFlatSideDirection()
    {
        return GetSideFacingDirection();
    }

    private Vector3 GetSideFacingDirection()
    {
        // サイドビューでは左右（world X）だけを攻撃方向として扱う。
        // `transform.right` を使うと、向きによって奥行き(Z)側にずれるため明示的に固定する。
        float sign = transform.forward.x >= 0f ? 1f : -1f;
        return Vector3.right * sign;
    }
}

