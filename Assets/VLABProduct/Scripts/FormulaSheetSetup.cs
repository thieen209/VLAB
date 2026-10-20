#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Vlab.Product;

public static class FormulaSheetSetup
{
    public static void Configure()
    {
        const string root="Assets/VLABProduct/Resources/UI/Formulas/";
        var expected=FormulaSheet.All.Values.SelectMany(item=>Enumerable.Range(0,item.pages).Select(page=>item.id+(page==0?"":"_"+(page+1))+".png")).OrderBy(name=>name).ToArray();
        var actual=Directory.GetFiles(root,"*.png").Select(Path.GetFileName).OrderBy(name=>name).ToArray();
        if(expected.Length!=35||!actual.SequenceEqual(expected))throw new InvalidOperationException("Formula PNG set differs from the35 approved manifest pages");
        foreach(string name in expected)
        {
            string path=root+name;var bytes=File.ReadAllBytes(path);if(bytes.Length<100||bytes[0]!=137||bytes[1]!=80||bytes[2]!=78||bytes[3]!=71)throw new InvalidOperationException("Invalid formula PNG: "+name);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);if(!importer)throw new InvalidOperationException("Formula texture not imported: "+name);
            importer.textureType=TextureImporterType.Default;importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=2048;importer.mipmapEnabled=false;importer.isReadable=false;importer.filterMode=FilterMode.Bilinear;importer.textureCompression=TextureImporterCompression.Compressed;
            var android=importer.GetPlatformTextureSettings("Android");android.overridden=true;android.maxTextureSize=2048;android.format=TextureImporterFormat.ETC2_RGB4;android.compressionQuality=75;importer.SetPlatformTextureSettings(android);importer.SaveAndReimport();
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);if(!texture||texture.width!=1800||texture.height!=600)throw new InvalidOperationException("Formula dimensions changed: "+name);
        }
        Directory.CreateDirectory("Evidence");File.WriteAllText("Evidence/formula-import.txt","PASS\n35 PNGs /32 approved lessons;1800x600; NPOTNone;max2048;no mipmaps/readwrite;AndroidETC2_RGB4.\n");
    }
}
#endif
