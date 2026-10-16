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
        AuthoredMotionImport.Validate();
        var minion=Resources.Load<TextAsset>("Characters/Minion1/Minion1PoseData");
        if(minion==null)throw new System.InvalidOperationException("Minion1 motions missing");
        AuthoredMotionLibrary.Read(minion.bytes,4,33);
        var minion2=Resources.Load<TextAsset>("Characters/Minion2/Minion2PoseData");
        if(minion2==null)throw new System.InvalidOperationException("Minion2 motions missing");
        AuthoredMotionLibrary.Read(minion2.bytes,5,41);
        var minion3=Resources.Load<TextAsset>("Characters/Minion3/Minion3PoseData");
        if(minion3==null)throw new System.InvalidOperationException("Minion3 motions missing");
        AuthoredMotionLibrary.Read(minion3.bytes,4,41);
        var minion4=Resources.Load<TextAsset>("Characters/Minion4/Minion4PoseData");
        if(minion4==null)throw new System.InvalidOperationException("Minion4 motions missing");
        AuthoredMotionLibrary.Read(minion4.bytes,4,33);
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
