#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class PrototypeMenu
{
    [MenuItem("School Rule Zero/Open Prototype")]
    public static void OpenPrototype()
    {
        if (EditorApplication.isPlaying) { Debug.LogWarning("Stop Play Mode first."); return; }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        PrototypeBootstrap.RunSmokeChecks = false;
        SessionState.EraseBool("SchoolRuleZero.RunSmoke");
        EditorSceneManager.OpenScene("Assets/Scenes/Prototype.unity");
    }
    [MenuItem("School Rule Zero/Play Prototype")]
    public static void PlayPrototype()
    {
        if (EditorApplication.isPlaying) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        SessionState.EraseBool("SchoolRuleZero.RunSmoke");
        EditorSceneManager.OpenScene("Assets/Scenes/Prototype.unity");
        EditorApplication.isPlaying = true;
    }
    [MenuItem("School Rule Zero/Build WebGL")]
    public static void BuildWebGL()
    {
        if (EditorApplication.isPlaying) { Debug.LogWarning("Stop Play Mode first."); return; }
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
        {
            Debug.LogError("Install WebGL Build Support for this Unity version through Unity Hub.");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        string output = EditorUtility.SaveFolderPanel("WebGL output folder", "", "SchoolRuleZeroWebGL");
        if (string.IsNullOrEmpty(output)) return;
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/Prototype.unity" },
            locationPathName = output,
            target = BuildTarget.WebGL,
            options = BuildOptions.Development
        });
        if (report.summary.result == BuildResult.Succeeded)
            Debug.Log("WebGL build created at " + output +
                ". Serve this folder over HTTP(S); opening index.html as a local file is insufficient.");
        else Debug.LogError("WebGL build failed: " + report.summary.result);
    }
    [MenuItem("School Rule Zero/Run Smoke Checks")]
    public static void RunSmokeChecks()
    {
        if (EditorApplication.isPlaying) { Debug.LogWarning("Stop Play Mode first."); return; }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        // SessionState survives the default domain reload when entering Play Mode.
        SessionState.SetBool("SchoolRuleZero.RunSmoke", true);
        EditorSceneManager.OpenScene("Assets/Scenes/Prototype.unity");
        EditorApplication.isPlaying = true;
    }
}
#endif
