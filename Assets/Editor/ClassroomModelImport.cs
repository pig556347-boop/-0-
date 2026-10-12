#if UNITY_EDITOR
using UnityEditor;
public sealed class ClassroomModelImport : AssetPostprocessor
{
    private void OnPreprocessModel()
    {
        if (!assetPath.StartsWith("Assets/Resources/Classroom/")) return;
        ModelImporter importer = (ModelImporter)assetImporter;
        importer.importAnimation = false;
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
        importer.isReadable = true;
    }
    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Resources/Classroom/")) return;
        TextureImporter importer = (TextureImporter)assetImporter;
        importer.maxTextureSize = 2048;
        importer.mipmapEnabled = true;
    }
}
#endif
