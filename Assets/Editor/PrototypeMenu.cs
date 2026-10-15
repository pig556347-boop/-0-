#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;
using System.IO.Compression;

public static class PrototypeMenu
{
    // Noninteractive entry point for GitHub Actions. Never invoke folder dialogs in CI.
    public static void BuildForCI()
    {
        const string output = "build/WebGL";
        Directory.CreateDirectory(output);
        PlayerSettings.WebGL.template = "PROJECT:SchoolFullscreen";
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.threadsSupport = false;
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/Prototype.unity" },
            locationPathName = output,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        });
        string binaries = Path.Combine(output, "Build");
        if (report.summary.result != BuildResult.Succeeded ||
            !File.Exists(Path.Combine(output, "index.html")) ||
            !Directory.Exists(binaries) ||
            Directory.GetFiles(binaries, "*.wasm").Length == 0 ||
            Directory.GetFiles(binaries, "*.data").Length == 0)
            throw new System.InvalidOperationException("A complete WebGL build was not produced.");
        GameAudioBuildFiles.CopyTo(output);
        File.WriteAllText(Path.Combine(output, ".nojekyll"), "");
    }
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
        if (Directory.GetFileSystemEntries(output).Length != 0)
        {
            EditorUtility.DisplayDialog("WebGL output", "Choose an empty folder so existing files are not mixed into the build.", "OK");
            return;
        }
        // GitHub Pages does not supply Unity-specific Content-Encoding headers.
        PlayerSettings.WebGL.template = "PROJECT:SchoolFullscreen";
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.threadsSupport = false;
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/Prototype.unity" },
            locationPathName = output,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        });
        if (report.summary.result == BuildResult.Succeeded)
        {
            string buildFolder = Path.Combine(output, "Build");
            if (!File.Exists(Path.Combine(output, "index.html")) || !Directory.Exists(buildFolder) ||
                Directory.GetFiles(buildFolder, "*.wasm").Length == 0 ||
                Directory.GetFiles(buildFolder, "*.data").Length == 0)
            {
                Debug.LogError("Unity reported success, but the selected output folder does not contain a complete WebGL game. " +
                    "ZIP export stopped. Selected folder: " + output +
                    ". Unity report output: " + report.summary.outputPath +
                    ". Check the Console and Editor log for the actual build output or errors.");
                EditorUtility.RevealInFinder(output);
                return;
            }
            GameAudioBuildFiles.CopyTo(output);
            File.WriteAllText(Path.Combine(output, ".nojekyll"), "");
            string archive = output.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
                "-" + System.DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + ".zip";
            try
            {
                ZipFile.CreateFromDirectory(output, archive);
                Debug.Log("WebGL build created at " + output +
                    ". Upload this ZIP for deployment: " + archive);
                EditorUtility.RevealInFinder(archive);
            }
            catch (System.Exception exception)
            {
                Debug.LogError("WebGL build succeeded, but ZIP export failed. The build remains at " +
                    output + ". " + exception.Message);
            }
        }
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
