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
        rooms.Clear();foreach(var name in new[]{"Physics","Chemistry","Biology","Engineering"})rooms.Enqueue(name);receipt.Clear();finishing=false;
        Directory.CreateDirectory("Evidence/CurtainLighting");Lightmapping.bakeCompleted+=OnComplete;EditorApplication.update+=Update;Next();
    }
    static void ConfigureDaylight(LabShade shade)
    {
        shade.daylight=UnityEngine.Object.FindObjectsByType<Light>().Where(l=>l.type==LightType.Directional||l.name=="Window soft fill").ToArray();
        shade.daylightOpenIntensities=shade.daylight.Select(l=>l.type==LightType.Directional?1.05f:3f).ToArray();
        foreach(var light in shade.daylight)
        {
            light.lightmapBakeType=LightmapBakeType.Realtime;light.enabled=true;
            if(light.name=="Window soft fill"){light.type=LightType.Spot;light.range=6;light.spotAngle=120;light.innerSpotAngle=90;light.transform.position=shade.transform.position+shade.transform.forward*.25f-Vector3.up*shade.fullDrop*.5f;light.transform.rotation=Quaternion.LookRotation(shade.transform.forward-Vector3.up*.3f);light.color=new Color(.96f,.98f,1);}
        }
        shade.RefreshDaylight();
    }
    public static void TuneDaylight()
    {
        foreach(var room in new[]{"Physics","Chemistry","Biology","Engineering"})
        {var scene=EditorSceneManager.OpenScene("Assets/Labs/Scenes/"+room+".unity");var shade=UnityEngine.Object.FindAnyObjectByType<LabShade>();ConfigureDaylight(shade);RoomLighting.Apply();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}
        AssetDatabase.SaveAssets();EditorApplication.Exit(0);
    }
    static void OnComplete(){completed=true;}
    static void Next()
    {
        if(rooms.Count==0){Finish();return;}current=rooms.Dequeue();completed=false;start=EditorApplication.timeSinceStartup;
        try
        {
            string path="Assets/Labs/Scenes/"+current+".unity",backup="Evidence/CurtainLighting/Before/"+current;
            Directory.CreateDirectory(backup);if(!File.Exists(backup+"/"+current+".unity"))File.Copy(path,backup+"/"+current+".unity");
            string maps="Assets/Labs/Scenes/"+current;if(Directory.Exists(maps))foreach(var file in Directory.GetFiles(maps,"*",SearchOption.AllDirectories))
            {string copy=Path.Combine(backup,"Maps",Path.GetRelativePath(maps,file));Directory.CreateDirectory(Path.GetDirectoryName(copy));if(!File.Exists(copy))File.Copy(file,copy);}
            var scene=EditorSceneManager.OpenScene(path);
            foreach(var light in UnityEngine.Object.FindObjectsByType<Light>())
            {
                if(light.name=="Baked ceiling softbox")light.intensity=1.25f;
                if(light.name=="Baked window fill")light.intensity=0;
                if(light.name=="Soft daylight")light.intensity=.9f;
                if(light.name=="Ceiling illumination")light.intensity=.6f;
                EditorUtility.SetDirty(light);
            }
            var shade=UnityEngine.Object.FindAnyObjectByType<LabShade>();if(!shade||!shade.bottomBar)throw new InvalidOperationException("Upgrade the authored shade first.");
            ConfigureDaylight(shade);
            foreach(var light in UnityEngine.Object.FindObjectsByType<Light>().Where(l=>l.name=="Ceiling illumination"))light.lightmapBakeType=LightmapBakeType.Realtime;
            shade.roomReflection=UnityEngine.Object.FindAnyObjectByType<ReflectionProbe>();if(shade.roomReflection){shade.roomReflection.mode=UnityEngine.Rendering.ReflectionProbeMode.Realtime;shade.roomReflection.refreshMode=UnityEngine.Rendering.ReflectionProbeRefreshMode.ViaScripting;shade.roomReflection.timeSlicingMode=UnityEngine.Rendering.ReflectionProbeTimeSlicingMode.IndividualFaces;}
            var control=shade.GetComponentsInChildren<LabFixture>().Single(f=>f.action==LabFixture.Action.Shade);
            foreach(var hit in shade.GetComponentsInChildren<Collider>())if(hit.gameObject!=control.gameObject)hit.enabled=false;
            shade.lowered=true;shade.Step(10);
            RoomLighting.Apply();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            receipt.Add(current+" indoor-only lighting bake started "+DateTime.UtcNow.ToString("O"));Write("IN PROGRESS");
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
            var shade=UnityEngine.Object.FindAnyObjectByType<LabShade>();shade.lowered=false;shade.Step(10);
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
            var camera=UnityEngine.Object.FindObjectsByType<Camera>().First(x=>x.name.Contains("Learner"));var urp=camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();if(urp)urp.renderPostProcessing=false;
            BuildLabs.Capture(camera,"Evidence/CurtainLighting/open-"+current+".png",1400,875);
            shade.lowered=true;shade.Step(10);BuildLabs.Capture(camera,"Evidence/CurtainLighting/closed-"+current+".png",1400,875);shade.lowered=false;shade.Step(10);
            receipt.Add(current+" PASS "+maps.Length+" baked lightmaps; "+(EditorApplication.timeSinceStartup-start).ToString("F1")+" seconds; source geometry/materials unchanged.");Write("IN PROGRESS");Next();
        }
        catch(Exception e){Fail(e);}
    }
    static void Finish(){finishing=true;EditorApplication.update-=Update;Lightmapping.bakeCompleted-=OnComplete;Write("PASS");AssetDatabase.Refresh();EditorApplication.Exit(0);}
    static void Fail(Exception e){finishing=true;EditorApplication.update-=Update;Lightmapping.bakeCompleted-=OnComplete;receipt.Add(e.ToString());Write("FAILED");Debug.LogException(e);EditorApplication.Exit(1);}
    static void Write(string status){File.WriteAllLines("Evidence/CurtainLighting/bakes.txt",new[]{status,"Indoor-only lightmaps and probes; runtime daylight controlled by authored blinds."}.Concat(receipt));}
}
#endif
