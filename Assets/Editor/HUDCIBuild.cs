#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

// Build-time test fixture. Production scene and gameplay code are not edited.
public static class HUDCIBuild
{
    public static void Build()
    {
        CharacterModelImport.PrepareForBuild();
        ClassroomBuildCheck.Validate();
        PrototypeMenu.BuildForCI();
        var test=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        new GameObject("HUD Playtest").AddComponent<HUDPlaytestRunner>();
        const string scene="Assets/Tests/HUDPlaytest.generated.unity";
        EditorSceneManager.SaveScene(test,scene);
        BuildReport result=BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes=new[]{scene},locationPathName="build/HUDTest",target=BuildTarget.WebGL,options=BuildOptions.None
        });
        AssetDatabase.DeleteAsset(scene);
        if(result.summary.result!=BuildResult.Succeeded)
            throw new System.InvalidOperationException("HUD browser test build failed");
    }
}
#endif
