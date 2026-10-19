#if UNITY_EDITOR
using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

public static class VlabTitleFont
{
    public static void Install()
    {
        const string root="Assets/VLABProduct/Resources/UI/",path=root+"VlabTitle.asset";
        var source=AssetDatabase.LoadAssetAtPath<Font>(root+"ChakraPetch-Bold.ttf");
        if(!source)throw new InvalidOperationException("The licensed Chakra Petch Bold source is missing.");
        var asset=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if(!asset)
        {
            asset=TMP_FontAsset.CreateFontAsset(source,80,8,UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,2048,2048,AtlasPopulationMode.Dynamic,true);
            asset.name="VLAB Chakra Petch Bold";AssetDatabase.CreateAsset(asset,path);AssetDatabase.AddObjectToAsset(asset.material,path);
            foreach(var texture in asset.atlasTextures)if(texture)AssetDatabase.AddObjectToAsset(texture,path);
        }
        asset.atlasPopulationMode=AtlasPopulationMode.Dynamic;
        const string titles="PHÒNG VẬT LÍPHÒNG HÓA HỌCPHÒNG SINH HỌCPHÒNG KỸ THUẬTVật líHóa họcSinh họcKỹ thuậtPhysicsChemistryBiologyEngineeringABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        asset.TryAddCharacters(titles,out var missing);
        if(!string.IsNullOrEmpty(missing)||asset.faceInfo.styleName.IndexOf("Bold",StringComparison.OrdinalIgnoreCase)<0)
            throw new InvalidOperationException("Home title font validation failed: "+missing+" / "+asset.faceInfo.styleName);
        // Home titles are a finite bilingual set; bake them without runtime glyph generation.
        asset.atlasPopulationMode=AtlasPopulationMode.Static;
        foreach(var texture in asset.atlasTextures)if(texture)EditorUtility.SetDirty(texture);
        EditorUtility.SetDirty(asset.material);EditorUtility.SetDirty(asset);AssetDatabase.SaveAssets();
        Directory.CreateDirectory("Evidence");File.WriteAllText("Evidence/title-font.txt","PASS\nOfficial Chakra Petch Bold source; Vietnamese and English Home titles have native TMP glyphs.\n"+asset.faceInfo.familyName+" / "+asset.faceInfo.styleName);
        Debug.Log("VLAB title font installed and native glyph coverage verified.");
    }
    public static void ReviewRooms(){Install();VlabRuntimeSmoke.Polish();}
    public static void Review(){Install();VlabRuntimeSmoke.HomeReview();}
}
#endif
