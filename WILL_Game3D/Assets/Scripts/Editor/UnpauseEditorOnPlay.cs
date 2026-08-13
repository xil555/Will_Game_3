using UnityEditor;

/// <summary>
/// The Console "Error Pause" option freezes the editor when any error logs.
/// This unpauses Play Mode after entering it so a physics warning cannot freeze the session.
/// </summary>
[InitializeOnLoad]
static class UnpauseEditorOnPlay
{
    static UnpauseEditorOnPlay()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode)
            return;

        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlaying)
                EditorApplication.isPaused = false;
        };
    }
}
