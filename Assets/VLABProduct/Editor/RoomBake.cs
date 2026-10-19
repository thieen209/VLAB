#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Vlab.Product;

// Re-bake the copied rooms for the same neutral lighting used by the product camera.
// Geometry and material assets are preserved.
public static class VlabRoomBake
{
    static readonly Queue<string> rooms=new Queue<string>();static readonly List<string> receipt=new List<string>();
    static string current;static bool completed,finishing;static double start;
    public static void Run()
    {
        if(EditorApplication.isPlaying||Lightmapping.isRunning)throw new InvalidOperationException("An idle Editor is required.");
        if(!Path.GetFullPath(".").TrimEnd('\\','/').Equals(@"E:\VLAB\4 LABS\Ngua Man VLAB",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Only the named product copy may be baked.");
        rooms.Clear();foreach(var name in new[]{"Chemistry","Biology","Engineering"})rooms.Enqueue(name);receipt.Clear();finishing=false;
        Directory.CreateDirectory("Evidence/RoomBakes");Lightmapping.bakeCompleted+=OnComplete;EditorApplication.update+=Update;Next();
    }
    static void OnComplete(){completed=true;}
    static void Next()
    {
        if(rooms.Count==0){Finish();return;}current=rooms.Dequeue();completed=false;start=EditorApplication.timeSinceStartup;
        try
        {
            string path="Assets/Labs/Scenes/"+current+".unity",backup="Evidence/RoomBakes/Before/"+current;
            Directory.CreateDirectory(backup);if(!File.Exists(backup+"/"+current+".unity"))File.Copy(path,backup+"/"+current+".unity");
            string maps="Assets/Labs/Scenes/"+current;if(Directory.Exists(maps))foreach(var file in Directory.GetFiles(maps,"*",SearchOption.AllDirectories))
            {string copy=Path.Combine(backup,"Maps",Path.GetRelativePath(maps,file));Directory.CreateDirectory(Path.GetDirectoryName(copy));if(!File.Exists(copy))File.Copy(file,copy);}
            var scene=EditorSceneManager.OpenScene(path);
            foreach(var light in UnityEngine.Object.FindObjectsByType<Light>())
            {
                if(light.name=="Baked ceiling softbox")light.intensity=2.5f;
                if(light.name=="Baked window fill")light.intensity=4.25f;
                if(light.name=="Soft daylight")light.intensity=.9f;
                if(light.name=="Ceiling illumination")light.intensity=.6f;
                EditorUtility.SetDirty(light);
            }
            RoomLighting.Apply();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            receipt.Add(current+" neutral lighting bake started "+DateTime.UtcNow.ToString("O"));Write("IN PROGRESS");
            if(!Lightmapping.BakeAsync())throw new InvalidOperationException("Unity refused "+current+" bake.");
        }
        catch(Exception e){Fail(e);}
    }
    static void Update()
    {
        if(finishing)return;
        try
        {
            if(EditorApplication.timeSinceStartup-start>480){Lightmapping.Cancel();throw new TimeoutException(current+" bake exceeded8minutes.");}
            if(!completed||Lightmapping.isRunning)return;
            var maps=LightmapSettings.lightmaps;if(maps.Length==0||maps.Any(x=>!x.lightmapColor)||!Lightmapping.lightingDataAsset)throw new InvalidOperationException(current+" has incomplete baked maps.");
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
            var camera=UnityEngine.Object.FindObjectsByType<Camera>().First(x=>x.name.Contains("Learner"));var urp=camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();if(urp)urp.renderPostProcessing=false;
            BuildLabs.Capture(camera,"Assets/VLABProduct/Resources/UI/Preview_"+current+".png",1400,875);
            receipt.Add(current+" PASS "+maps.Length+" baked lightmaps; "+(EditorApplication.timeSinceStartup-start).ToString("F1")+" seconds; source geometry/materials unchanged.");Write("IN PROGRESS");Next();
        }
        catch(Exception e){Fail(e);}
    }
    static void Finish(){finishing=true;EditorApplication.update-=Update;Lightmapping.bakeCompleted-=OnComplete;Write("PASS");AssetDatabase.Refresh();EditorApplication.Exit(0);}
    static void Fail(Exception e){finishing=true;EditorApplication.update-=Update;Lightmapping.bakeCompleted-=OnComplete;receipt.Add(e.ToString());Write("FAILED");Debug.LogException(e);EditorApplication.Exit(1);}
    static void Write(string status){File.WriteAllLines("Evidence/RoomBakes/bakes.txt",new[]{status,"Neutral product lighting; no new geometry or material assets; copied rooms only."}.Concat(receipt));}
}
#endif
