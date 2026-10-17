#if UNITY_EDITOR
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Editor only. uGUI keeps a static list of enabled selectables, and in this project (domain reload off, scenes opened
/// and closed by tools) the indexes the selectables hold can drift away from that list, so closing a scene throws
/// IndexOutOfRangeException in Selectable.OnDisable. This rebuilds the list from the selectables that are actually
/// enabled: before a scene closes, around play mode, and after the first scene loads. Builds never need it.
/// </summary>
internal static class SelectableRegistryRepair
{
    private static readonly FieldInfo ListField = typeof(Selectable).GetField("s_Selectables", BindingFlags.Static | BindingFlags.NonPublic);
    private static readonly FieldInfo CountField = typeof(Selectable).GetField("s_SelectableCount", BindingFlags.Static | BindingFlags.NonPublic);
    private static readonly FieldInfo EnableCalledField = typeof(Selectable).GetField("m_EnableCalled", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo IndexField = typeof(Selectable).GetField("m_CurrentIndex", BindingFlags.Instance | BindingFlags.NonPublic);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void Rebuild()
    {
        if (ListField == null || CountField == null || EnableCalledField == null || IndexField == null) return;

        var all = Resources.FindObjectsOfTypeAll<Selectable>();
        var list = new Selectable[Mathf.Max(10, Mathf.NextPowerOfTwo(all.Length + 1))];
        int count = 0;
        foreach (var selectable in all)
        {
            bool registered = selectable && selectable.isActiveAndEnabled && selectable.gameObject.scene.IsValid() && !UnityEditor.EditorUtility.IsPersistent(selectable);
            EnableCalledField.SetValue(selectable, registered);
            IndexField.SetValue(selectable, registered ? count : -1);
            if (registered) list[count++] = selectable;
        }

        ListField.SetValue(null, list);
        CountField.SetValue(null, count);
    }

    [UnityEditor.InitializeOnLoadMethod]
    private static void Hook()
    {
        UnityEditor.SceneManagement.EditorSceneManager.sceneClosing -= OnSceneClosing;
        UnityEditor.SceneManagement.EditorSceneManager.sceneClosing += OnSceneClosing;
        UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        UnityEditor.EditorApplication.delayCall += Rebuild;
    }

    private static void OnSceneClosing(UnityEngine.SceneManagement.Scene scene, bool removingScene) => Rebuild();

    private static void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange change)
    {
        if (change is UnityEditor.PlayModeStateChange.ExitingEditMode or UnityEditor.PlayModeStateChange.ExitingPlayMode or UnityEditor.PlayModeStateChange.EnteredEditMode) Rebuild();
    }
}
#endif
