#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

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
    private void OnPreprocessTexture()
    {
        if(!assetPath.StartsWith("Assets/Resources/Characters/Minion1/"))return;
        var importer=(TextureImporter)assetImporter;
        importer.textureType=TextureImporterType.Default;importer.maxTextureSize=2048;
        importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.sRGBTexture=true;importer.alphaSource=TextureImporterAlphaSource.None;
    }
    public static void PrepareForBuild()
    {
        const string texturePath="Assets/Resources/Characters/Minion1/Minion1_BaseColor.png";
        var textureImporter=AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if(textureImporter==null)throw new System.InvalidOperationException("Minion1 texture importer missing");
        textureImporter.textureType=TextureImporterType.Default;textureImporter.maxTextureSize=2048;
        textureImporter.mipmapEnabled=false;textureImporter.textureCompression=TextureImporterCompression.Uncompressed;
        textureImporter.sRGBTexture=true;textureImporter.alphaSource=TextureImporterAlphaSource.None;textureImporter.SaveAndReimport();
        var texture=Resources.Load<Texture2D>("Characters/Minion1/Minion1_BaseColor");
        if(texture==null)throw new System.InvalidOperationException("Minion1 texture resource missing before build");
        Debug.Log("MINION1_TEXTURE_VALIDATED size="+texture.width+"x"+texture.height);

        foreach (string path in new[] { "Assets/Resources/Characters/Player/PlayerModel.fbx",
            "Assets/Resources/Characters/Deongchi/DeongchiModel.fbx",
            "Assets/Resources/Characters/Minion1/Minion1Model.fbx" })
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
