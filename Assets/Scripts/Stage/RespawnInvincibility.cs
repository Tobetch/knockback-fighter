using System.Collections.Generic;
using UnityEngine;

public class RespawnInvincibility : MonoBehaviour
{
    [SerializeField] private float invincibilityDuration = 3f;
    [SerializeField] private float blinkInterval = 0.12f;

    private readonly List<Renderer> renderers = new();
    private readonly List<bool> rendererInitialEnabled = new();
    private float invincibleUntilTime = float.NegativeInfinity;
    private float nextBlinkTime = float.NegativeInfinity;
    private bool blinkVisible = true;

    public bool IsInvincible => Time.time < invincibleUntilTime;

    private void Awake()
    {
        CacheRenderers();
    }

    private void Update()
    {
        if (!IsInvincible)
        {
            if (!blinkVisible)
            {
                blinkVisible = true;
                SetBlinkVisible(true);
            }

            return;
        }

        if (Time.time < nextBlinkTime)
        {
            return;
        }

        blinkVisible = !blinkVisible;
        SetBlinkVisible(blinkVisible);
        nextBlinkTime = Time.time + Mathf.Max(0.02f, blinkInterval);
    }

    public void Activate()
    {
        // リスポーン時点の可視状態を基準に取り直す。
        // これにより、後から差し替えた見た目（ペンギン）だけを点滅対象にできる。
        CacheRenderers();

        invincibleUntilTime = Time.time + Mathf.Max(0f, invincibilityDuration);
        blinkVisible = false;
        nextBlinkTime = Time.time;
    }

    private void CacheRenderers()
    {
        renderers.Clear();
        rendererInitialEnabled.Clear();

        Renderer[] found = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < found.Length; i++)
        {
            Renderer r = found[i];
            if (r == null)
            {
                continue;
            }

            renderers.Add(r);
            rendererInitialEnabled.Add(r.enabled);
        }
    }

    private void SetBlinkVisible(bool visible)
    {
        for (int i = 0; i < renderers.Count; i++)
        {
            Renderer r = renderers[i];
            if (r == null)
            {
                continue;
            }

            bool baseEnabled = i < rendererInitialEnabled.Count && rendererInitialEnabled[i];
            r.enabled = baseEnabled && visible;
        }
    }
}
