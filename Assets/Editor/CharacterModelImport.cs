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
        if(!assetPath.StartsWith("Assets/Resources/Characters/Minion1/")&&!assetPath.StartsWith("Assets/Resources/Characters/Minion2/")&&!assetPath.StartsWith("Assets/Resources/Characters/Minion3/")&&!assetPath.StartsWith("Assets/Resources/Characters/Minion4/")&&!assetPath.StartsWith("Assets/Resources/Characters/Dojaehak/"))return;
        var importer=(TextureImporter)assetImporter;
        importer.textureType=TextureImporterType.Default;importer.maxTextureSize=2048;
        importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.sRGBTexture=true;importer.alphaSource=TextureImporterAlphaSource.None;
    }
    public static void PrepareForBuild()
    {
        foreach(string kind in new[]{"Minion1","Minion2","Minion3","Minion4","Dojaehak"})
        {
            string texturePath="Assets/Resources/Characters/"+kind+"/"+kind+"_BaseColor.png";
            var textureImporter=AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if(textureImporter==null)throw new System.InvalidOperationException(kind+" texture importer missing");
            textureImporter.textureType=TextureImporterType.Default;textureImporter.maxTextureSize=2048;
            textureImporter.mipmapEnabled=false;textureImporter.textureCompression=TextureImporterCompression.Uncompressed;
            textureImporter.sRGBTexture=true;textureImporter.alphaSource=TextureImporterAlphaSource.None;textureImporter.SaveAndReimport();
            var texture=Resources.Load<Texture2D>("Characters/"+kind+"/"+kind+"_BaseColor");
            if(texture==null)throw new System.InvalidOperationException(kind+" texture resource missing before build");
            Debug.Log(kind.ToUpperInvariant()+"_TEXTURE_VALIDATED size="+texture.width+"x"+texture.height);
        }

        foreach (string path in new[] { "Assets/Resources/Characters/Player/PlayerModel.fbx",
            "Assets/Resources/Characters/Deongchi/DeongchiModel.fbx",
            "Assets/Resources/Characters/Minion1/Minion1Model.fbx",
            "Assets/Resources/Characters/Minion2/Minion2Model.fbx",
            "Assets/Resources/Characters/Minion3/Minion3Model.fbx",
            "Assets/Resources/Characters/Minion4/Minion4Model.fbx",
            "Assets/Resources/Characters/Dojaehak/DojaehakModel.fbx" })
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
