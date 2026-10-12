#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
public sealed class ClassroomModelImport : AssetPostprocessor
{
    public override uint GetVersion() { return 2; }
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
        bool floor = assetPath.StartsWith("Assets/Resources/Classroom/floor/");
        importer.mipmapEnabled = floor;
        importer.filterMode = FilterMode.Bilinear;
        if (!floor) importer.textureCompression = TextureImporterCompression.Uncompressed;
    }
}
#endif
