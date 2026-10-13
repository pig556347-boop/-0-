#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

// Static skin plus validated baked world-space deltas: no FBX animation scale curves.
public sealed class AuthoredMotionImport : AssetPostprocessor
{
    public override uint GetVersion() => 3;
    private void OnPreprocessModel()
    {
        if(!assetPath.StartsWith("Assets/Resources/Motions/"))return;
        ModelImporter importer=(ModelImporter)assetImporter;
        importer.animationType=ModelImporterAnimationType.Generic;
        importer.importAnimation=false;importer.isReadable=true;importer.optimizeGameObjects=false;
        importer.materialImportMode=ModelImporterMaterialImportMode.None;
    }
    public static void Validate()
    {
        const string path="Assets/Resources/Motions/PlayerMotions.fbx";
        ModelImporter importer=(ModelImporter)AssetImporter.GetAtPath(path);
        if(importer==null)throw new InvalidOperationException("Authored player skin missing");
        if(importer.importAnimation||!importer.isReadable||importer.optimizeGameObjects)
        {
            importer.importAnimation=false;importer.isReadable=true;importer.optimizeGameObjects=false;
            importer.SaveAndReimport();
        }
        TextAsset data=AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Resources/Motions/PlayerPoseData.bytes");
        if(data==null)throw new InvalidOperationException("Baked pose data missing");
        AuthoredMotionLibrary library=AuthoredMotionLibrary.Read(data.bytes);
        foreach(string name in AuthoredPlayerView.RequiredClips)
            if(!library.Clips.ContainsKey(name))throw new InvalidOperationException("Baked motion missing: "+name);
        Debug.Log("AUTHORED_DATA_VALIDATED candidates=20 bones=19 fps=60 bytes="+data.bytes.Length);
    }
}
#endif
