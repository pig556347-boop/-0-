#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

// Legacy sampled clips only drive this visual hierarchy. No Animator root motion.
public sealed class AuthoredMotionImport : AssetPostprocessor
{
    public override uint GetVersion() => 2;
    private void OnPreprocessModel()
    {
        if (!assetPath.StartsWith("Assets/Resources/Motions/")) return;
        ModelImporter importer = (ModelImporter)assetImporter;
        importer.animationType = ModelImporterAnimationType.Legacy;
        importer.importAnimation = true;
        importer.isReadable = true;
        importer.optimizeGameObjects = false;
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
        importer.animationCompression = ModelImporterAnimationCompression.Off;
        ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
        foreach (ModelImporterClipAnimation clip in clips)
        {
            string name = clip.name;
            int separator = name.LastIndexOf('|');
            if (separator >= 0) name = name.Substring(separator + 1);
            clip.name = name;
            clip.loopTime = name == "FightingIdle" || name == "Walking" ||
                name == "WalkingBackwards" || name == "Running" ||
                name == "LeftStrafe" || name == "RightStrafe";
        }
        importer.clipAnimations = clips;
    }
    private void OnPostprocessAnimation(GameObject root, AnimationClip clip)
    {
        if (!assetPath.StartsWith("Assets/Resources/Motions/")) return;
        string name=AuthoredPlayerView.ClipKey(clip.name);
        if(name!=null)clip.name=name;
    }
    public static void Validate()
    {
        const string path = "Assets/Resources/Motions/PlayerMotions.fbx";
        ModelImporter importer = (ModelImporter)AssetImporter.GetAtPath(path);
        if (importer == null) throw new InvalidOperationException("Authored player FBX missing");
        if (importer.animationType != ModelImporterAnimationType.Legacy || !importer.importAnimation ||
            !importer.isReadable || importer.optimizeGameObjects ||
            importer.animationCompression != ModelImporterAnimationCompression.Off)
        {
            importer.animationType = ModelImporterAnimationType.Legacy;
            importer.importAnimation = true; importer.isReadable = true;
            importer.optimizeGameObjects = false;
            importer.animationCompression = ModelImporterAnimationCompression.Off;
            importer.SaveAndReimport();
        }
        int count = 0;
        var names=new System.Collections.Generic.HashSet<string>();
        foreach (UnityEngine.Object item in AssetDatabase.LoadAllAssetsAtPath(path))
            if (item is AnimationClip clip && !clip.name.StartsWith("__preview__"))
            { count++; string name=AuthoredPlayerView.ClipKey(clip.name); if(name!=null)names.Add(name);
                Debug.Log("AUTHORED_CLIP_IMPORT " + clip.name + " seconds=" + clip.length); }
        if(names.Count!=20)throw new InvalidOperationException("Missing expected motion names: "+string.Join(",",names));
        if (count != 20) throw new InvalidOperationException("Expected 20 authored candidates, got " + count);
    }
}
#endif
