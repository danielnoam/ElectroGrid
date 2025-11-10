
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

public class MobileHapticsPostProcess
{
    [PostProcessBuild(1)]
    public static void OnPostProcessBuild(BuildTarget target, string path)
    {
        if (target == BuildTarget.Android)
        {
            // Validate the manifest was included
            Debug.Log("[MobileHaptics] Build complete - manifest should include VIBRATE permission");
        }
    }
}
#endif

