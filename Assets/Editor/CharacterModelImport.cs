#if UNITY_EDITOR
using UnityEditor;

// Keep supplied skeletons accessible to the visual driver and real mesh tests.
public sealed class CharacterModelImport : AssetPostprocessor
{
    private void OnPreprocessModel()
    {
        if (!assetPath.StartsWith("Assets/Resources/Characters/")) return;
        ModelImporter importer = (ModelImporter)assetImporter;
        importer.isReadable = true;
        importer.optimizeGameObjects = false;
        importer.importAnimation = false;
    }
    public static void PrepareForBuild()
    {
        foreach (string path in new[] { "Assets/Resources/Characters/Player/PlayerModel.fbx",
            "Assets/Resources/Characters/Deongchi/DeongchiModel.fbx" })
        {
            ModelImporter importer = (ModelImporter)AssetImporter.GetAtPath(path);
            if (importer == null) throw new System.InvalidOperationException("Character FBX missing: " + path);
            if (!importer.isReadable || importer.optimizeGameObjects || importer.importAnimation)
            {
                importer.isReadable = true; importer.optimizeGameObjects = false; importer.importAnimation = false;
                importer.SaveAndReimport();
            }
        }
    }
}
#endif
