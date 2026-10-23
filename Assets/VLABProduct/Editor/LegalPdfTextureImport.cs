#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
public sealed class LegalPdfTextureImport:AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if(!assetPath.StartsWith("Assets/VLABProduct/Resources/LegalPdf/"))return;
        var importer=(TextureImporter)assetImporter;importer.textureType=TextureImporterType.Default;importer.sRGBTexture=true;importer.mipmapEnabled=false;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;importer.alphaSource=TextureImporterAlphaSource.None;importer.npotScale=TextureImporterNPOTScale.None;importer.isReadable=false;
    }
}
#endif
