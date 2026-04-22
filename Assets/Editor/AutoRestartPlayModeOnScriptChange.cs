using UnityEditor;
using UnityEditor.Callbacks;

[InitializeOnLoad]
public static class AutoRestartPlayModeOnScriptChange
{
    private const string EnabledPrefKey = "knockbackfighter.autoRestartPlayModeOnScriptChange.enabled";
    private const string WasPlayingBeforeReloadKey = "knockbackfighter.autoRestartPlayModeOnScriptChange.wasPlayingBeforeReload";

    static AutoRestartPlayModeOnScriptChange()
    {
        AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        UpdateMenuCheckmark();
    }

    private static bool Enabled
    {
        get => EditorPrefs.GetBool(EnabledPrefKey, false);
        set
        {
            EditorPrefs.SetBool(EnabledPrefKey, value);
            UpdateMenuCheckmark();
        }
    }

    [MenuItem("Tools/Auto Restart PlayMode On Script Change")]
    private static void Toggle()
    {
        Enabled = !Enabled;
    }

    [MenuItem("Tools/Auto Restart PlayMode On Script Change", true)]
    private static bool ValidateToggle()
    {
        UpdateMenuCheckmark();
        return true;
    }

    private static void UpdateMenuCheckmark()
    {
        Menu.SetChecked("Tools/Auto Restart PlayMode On Script Change", Enabled);
    }

    private static void OnBeforeAssemblyReload()
    {
        if (!Enabled)
        {
            SessionState.SetBool(WasPlayingBeforeReloadKey, false);
            return;
        }

        SessionState.SetBool(WasPlayingBeforeReloadKey, EditorApplication.isPlaying);
    }

    [DidReloadScripts]
    private static void OnDidReloadScripts()
    {
        if (!Enabled)
        {
            SessionState.SetBool(WasPlayingBeforeReloadKey, false);
            return;
        }

        if (!SessionState.GetBool(WasPlayingBeforeReloadKey, false))
        {
            return;
        }

        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
            return;
        }

        // 既に EditMode の場合はそのまま再生し直す。
        EditorApplication.delayCall += () => EditorApplication.isPlaying = true;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (!Enabled)
        {
            return;
        }

        if (state != PlayModeStateChange.EnteredEditMode)
        {
            return;
        }

        if (!SessionState.GetBool(WasPlayingBeforeReloadKey, false))
        {
            return;
        }

        SessionState.SetBool(WasPlayingBeforeReloadKey, false);
        EditorApplication.delayCall += () => EditorApplication.isPlaying = true;
    }
}
