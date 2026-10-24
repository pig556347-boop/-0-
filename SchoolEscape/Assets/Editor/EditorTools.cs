using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SchoolEscape
{
    public static class EditorTools
    {
        const string Scene = "Assets/Scenes/Exploration.unity";
        [MenuItem("School Escape/Open exploration")]
        public static void Open()
        {
            if (!File.Exists(Scene))
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), Scene);
            }
            else EditorSceneManager.OpenScene(Scene);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Scene, true) };
        }
        static void Settings()
        {
            PlayerSettings.companyName = "School Escape Independent";
            PlayerSettings.productName = "폐교탈출 · 탐방 베타";
            PlayerSettings.defaultScreenWidth = 1440;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.runInBackground = true;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = false;
            PlayerSettings.WebGL.memorySize = 256;
            // Include the runtime-created materials' shader in player builds.
            if (!File.Exists("Assets/Resources/BuildShader.mat"))
                AssetDatabase.CreateAsset(new Material(Shader.Find("Standard")), "Assets/Resources/BuildShader.mat");
            AssetDatabase.SaveAssets();
        }
        [MenuItem("School Escape/Build separate WebGL")]
        public static void BuildWebGL()
        {
            Open(); Settings();
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { Scene }, locationPathName = "Builds/WebGL",
                target = BuildTarget.WebGL, options = BuildOptions.None
            });
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new InvalidOperationException("School Escape WebGL build failed: " + report.summary.result);
        }
        public static void Validate()
        {
            Open(); Settings();
            EditorApplication.isPlaying = true;
            // Pass -escapeValidate on the command line. The runtime validator
            // exits the editor after writing a fresh, timestamped result file.
        }
    }
}
