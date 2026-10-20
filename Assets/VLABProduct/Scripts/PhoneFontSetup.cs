#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEditor;

public static class VlabPhoneFontSetup
{
    const string Root="Assets/VLABProduct/Resources/UI/";
    public static void Install()
    {
        string glyphs=new string(Enumerable.Range(32,95).Concat(Enumerable.Range(0x00A0,0x0250-0x00A0)).Concat(Enumerable.Range(0x1E00,0x100)).Concat(Enumerable.Range(0x0391,0x03C9-0x0391+1)).Concat(Enumerable.Range(0x2080,10)).Select(value=>(char)value).ToArray())+"−×÷·≈≤≥°√²³πΔθμρλωσ⁄";
        var math=Make("NotoSansMath-Regular.ttf","VlabMath",glyphs+"−√≈≤≥");
        var regular=Make("NotoSans-Regular.ttf","VlabBody",glyphs);var strong=Make("NotoSans-SemiBold.ttf","VlabBodyStrong",glyphs);
        regular.fallbackFontAssetTable=new System.Collections.Generic.List<TMP_FontAsset>{math};strong.fallbackFontAssetTable=new System.Collections.Generic.List<TMP_FontAsset>{math};
        const string required="Tiếng Việt · Chọn thí nghiệm · Ghi số đo · Thử lưu · Vật lí · Hóa học · Sinh học · Kỹ thuật · ăâđêôơưĂÂĐÊÔƠƯ áàảãạ ắằẳẵặ ấầẩẫậ ếềểễệ ốồổỗộ ớờởỡợ ứừửữự ỷỹỵ Δπθμρλωσ ×÷−≈≤≥°²³√⁄₀₁₂₃₄₅₆₇₈₉";
        foreach(var face in new[]{regular,strong})if(!face.HasCharacters(required,out uint[] missing,true,false))throw new InvalidOperationException("Noto UI glyphs missing: "+string.Join(",",missing.Select(value=>"U+"+value.ToString("X4"))));
        regular.fontWeightTable[6]=new TMP_FontWeightPair{regularTypeface=strong,italicTypeface=strong};regular.fontWeightTable[7]=new TMP_FontWeightPair{regularTypeface=strong,italicTypeface=strong};
        EditorUtility.SetDirty(regular);AssetDatabase.SaveAssets();
        Directory.CreateDirectory("Evidence");File.WriteAllText("Evidence/phone-font.txt","PASS\nOfficial SIL-OFL Noto Sans Regular/SemiBold; Vietnamese UI and equation glyphs checked in native TMP.\n"+regular.faceInfo.familyName+" / "+strong.faceInfo.styleName+"\n");
    }
    static TMP_FontAsset Make(string sourceName,string name,string glyphs)
    {
        var source=AssetDatabase.LoadAssetAtPath<Font>(Root+sourceName);if(!source)throw new InvalidOperationException("Licensed Noto Sans source missing: "+sourceName);
        string path=Root+name+".asset";var asset=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if(!asset){asset=TMP_FontAsset.CreateFontAsset(source,48,6,UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,2048,2048,AtlasPopulationMode.Dynamic,true);asset.name="VLAB "+sourceName.Replace(".ttf","");AssetDatabase.CreateAsset(asset,path);AssetDatabase.AddObjectToAsset(asset.material,path);foreach(var texture in asset.atlasTextures)if(texture)AssetDatabase.AddObjectToAsset(texture,path);}
        asset.atlasPopulationMode=AtlasPopulationMode.Dynamic;asset.TryAddCharacters(glyphs,out _);
        asset.atlasPopulationMode=AtlasPopulationMode.Static;EditorUtility.SetDirty(asset);EditorUtility.SetDirty(asset.material);foreach(var texture in asset.atlasTextures)if(texture)EditorUtility.SetDirty(texture);return asset;
    }
}
#endif
