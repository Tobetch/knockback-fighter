using System.Collections;
using UnityEngine;

public static class PenguinVisualBootstrap
{
    private const string UrpPrefabPath = "Assets/Hosh/Stylized Penguin (Free)/Prefabs/URP/Penguen_Emperor_Mesh_Lite.prefab";
    private const string BuiltinPrefabPath = "Assets/Hosh/Stylized Penguin (Free)/Prefabs/Builtin/Penguen_Emperor_Mesh_Builtin_Lite.prefab";
    private const string VisualInstanceName = "PenguinVisualInstance";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        GameObject runnerObj = new GameObject("PenguinVisualBootstrapRunner");
        Object.DontDestroyOnLoad(runnerObj);
        runnerObj.hideFlags = HideFlags.HideAndDontSave;
        runnerObj.AddComponent<PenguinVisualBootstrapRunner>();
    }

    private sealed class PenguinVisualBootstrapRunner : MonoBehaviour
    {
        private IEnumerator Start()
        {
            // 他の RuntimeInitializeOnLoadMethod で生成されるダミーを待つ。
            yield return null;
            yield return null;

            GameObject penguinPrefab = LoadPenguinPrefab();
            if (penguinPrefab == null)
            {
                Destroy(gameObject);
                yield break;
            }

            ApplyToPlayer(penguinPrefab);
            ApplyToDummy(penguinPrefab);

            Destroy(gameObject);
        }

        private static GameObject LoadPenguinPrefab()
        {
#if UNITY_EDITOR
            GameObject urp = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(UrpPrefabPath);
            if (urp != null)
            {
                return urp;
            }

            return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(BuiltinPrefabPath);
#else
            return null;
#endif
        }

        private static void ApplyToPlayer(GameObject prefab)
        {
            PlayerMovement player = Object.FindAnyObjectByType<PlayerMovement>();
            if (player == null)
            {
                return;
            }

            Transform anchor = player.transform.Find("Visual");
            if (anchor == null)
            {
                anchor = player.transform;
            }

            ApplyPenguinVisual(anchor, prefab, new Vector3(0f, -0.5f, 0f), 0.9f);
        }

        private static void ApplyToDummy(GameObject prefab)
        {
            Hurtbox[] hurtboxes = Object.FindObjectsByType<Hurtbox>();
            for (int i = 0; i < hurtboxes.Length; i++)
            {
                Hurtbox hurtbox = hurtboxes[i];
                if (hurtbox == null || hurtbox.TargetRigidbody == null)
                {
                    continue;
                }

                GameObject root = hurtbox.TargetRigidbody.gameObject;
                if (!root.name.Contains("DummyEnemy"))
                {
                    continue;
                }

                ApplyPenguinVisual(root.transform, prefab, new Vector3(0f, -0.5f, 0f), 0.9f);
            }
        }

        private static void ApplyPenguinVisual(Transform anchor, GameObject prefab, Vector3 localOffset, float localScale)
        {
            if (anchor.Find(VisualInstanceName) != null)
            {
                return;
            }

            Renderer[] existingRenderers = anchor.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < existingRenderers.Length; i++)
            {
                Renderer r = existingRenderers[i];
                if (r != null)
                {
                    r.enabled = false;
                }
            }

            GameObject visualInstance = Object.Instantiate(prefab, anchor);
            visualInstance.name = VisualInstanceName;
            visualInstance.transform.localPosition = localOffset;
            visualInstance.transform.localScale = Vector3.one * localScale;

            CharacterSideFacingVisual facing = visualInstance.GetComponent<CharacterSideFacingVisual>();
            if (facing == null)
            {
                facing = visualInstance.AddComponent<CharacterSideFacingVisual>();
            }

            facing.Initialize(anchor);
        }
    }
}
