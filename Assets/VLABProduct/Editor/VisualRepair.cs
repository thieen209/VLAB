#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Vlab.Product;
using Object=UnityEngine.Object;

// Run in the copied product only, without -quit/-nographics. This method exits after the bake.
public static class VlabVisualRepair
{
    const string Project="E:/VLAB/4 LABS/Ngua Man VLAB";
    const string PhysicsPath="Assets/Labs/Scenes/Physics.unity";
    const string BootPath="Assets/VLABProduct/Scenes/VLAB_Boot.unity";
    const string Receipt="Evidence/visual-repair.txt";
    static readonly string[] TopNames={"PHY-TOP-L1","PHY-TOP-L2","PHY-TOP-L3","PHY-TOP-L4","PHY-RAIL-TOP","PHY-LAUNCHER-INFILL"};
    static readonly List<string> evidence=new List<string>(),errors=new List<string>();
    static Stopwatch elapsed;static bool completed,started,finishing;static double startedAt;

    public static void Physics()
    {
        try
        {
            string actual=Path.GetFullPath(".").Replace('\\','/').TrimEnd('/');
            if(!actual.Equals(Project,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Refusing to repair any checkout except the named product copy: "+actual);
            if(EditorApplication.isPlaying||Lightmapping.isRunning)throw new InvalidOperationException("An idle Editor is required.");
            evidence.Clear();errors.Clear();completed=started=finishing=false;elapsed=Stopwatch.StartNew();
            Directory.CreateDirectory("Evidence");Backup();
            var scene=EditorSceneManager.OpenScene(PhysicsPath,OpenSceneMode.Single);
            if(Object.FindObjectsByType<VlabApp>(FindObjectsSortMode.None).Length!=0)throw new InvalidOperationException("Physics must remain the room scene, without a product application root.");
            var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Labs/Upgrade/Materials/Top.mat");
            if(!material)throw new InvalidOperationException("The supplied upgraded Top material is missing.");
            var renderers=Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
            var tops=renderers.Where(r=>TopNames.Contains(r.name)).ToArray();
            if(tops.Length!=6||TopNames.Any(name=>tops.Count(r=>r.name==name)!=1))throw new InvalidOperationException("Expected exactly one of each of the six known Physics worktops; found "+tops.Length);
            foreach(var renderer in tops){renderer.sharedMaterial=material;EditorUtility.SetDirty(renderer);}
            evidence.Add("Worktops: 6/6 remapped to existing "+AssetDatabase.GetAssetPath(material));
            evidence.Add("Worktop names: "+string.Join(", ",TopNames));
            var lights=Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            SetLights(lights,"Baked ceiling softbox",2,2.5f,LightmapBakeType.Baked);
            SetLights(lights,"Baked window fill",1,4.25f,LightmapBakeType.Baked);
            SetLights(lights,"Soft daylight",1,.9f,LightmapBakeType.Mixed);
            SetLights(lights,"Ceiling illumination",2,.6f,LightmapBakeType.Realtime);
            RoomLighting.Apply();
            evidence.Add("Applied the product's neutral ambient/light settings before baking; material assets and postprocessing unchanged.");
            var settings=Lightmapping.lightingSettings;
            if(!settings)throw new InvalidOperationException("Physics has no existing LightingSettings asset.");
            evidence.Add("Existing LightingSettings: "+AssetDatabase.GetAssetPath(settings));
            evidence.Add("Bake configuration: lightmapper="+settings.lightmapper+", resolution="+settings.lightmapResolution+", direct="+settings.directSampleCount+", indirect="+settings.indirectSampleCount+", environment="+settings.environmentSampleCount);
            EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene))throw new IOException("Could not save the repaired Physics scene before baking.");
            AssetDatabase.SaveAssets();
            Write("IN PROGRESS");
            Application.logMessageReceived+=Log;
            Lightmapping.bakeCompleted+=BakeCompleted;
            EditorApplication.update+=Update;
            startedAt=EditorApplication.timeSinceStartup;
            started=Lightmapping.BakeAsync();
            if(!started)throw new InvalidOperationException("Unity refused BakeAsync. Inspect the Editor log and graphics/lightmapper availability.");
            UnityEngine.Debug.Log("VLAB PHYSICS VISUAL REPAIR: asynchronous bake started; 8 minute limit.");
        }
        catch(Exception error){Fail(error);}
    }

    static void SetLights(Light[] lights,string name,int expected,float intensity,LightmapBakeType kind)
    {
        var selected=lights.Where(light=>light.name==name).ToArray();
        if(selected.Length!=expected)throw new InvalidOperationException("Expected "+expected+" lights named "+name+"; found "+selected.Length);
        foreach(var light in selected){light.intensity=intensity;light.lightmapBakeType=kind;EditorUtility.SetDirty(light);}
        evidence.Add(name+": "+selected.Length+" × "+intensity.ToString("0.##",System.Globalization.CultureInfo.InvariantCulture)+" ("+kind+")");
    }
    static void BakeCompleted(){completed=true;}
    static void Log(string message,string stack,LogType type)
    {if(type==LogType.Error||type==LogType.Exception)errors.Add(message+"\n"+stack);}
    static void Update()
    {
        if(finishing||!started)return;
        try
        {
            double seconds=EditorApplication.timeSinceStartup-startedAt;
            if(seconds>480){Lightmapping.Cancel();throw new TimeoutException("Physics bake exceeded 8 minutes. Earlier scene/lightmap backup is retained.");}
            if(completed&&!Lightmapping.isRunning){Complete();return;}
            if(seconds>5&&!Lightmapping.isRunning&&!completed)throw new InvalidOperationException("The bake stopped without a completion event. Inspect the Editor log.");
        }
        catch(Exception error){Fail(error);}
    }
    static void Complete()
    {
        finishing=true;
        if(errors.Count>0)throw new InvalidOperationException("Unity reported errors during the bake; see receipt.");
        var maps=LightmapSettings.lightmaps;
        if(!Lightmapping.lightingDataAsset||maps.Length==0||maps.Any(map=>!map.lightmapColor))throw new InvalidOperationException("The bake completed without valid color lightmaps and a LightingDataAsset.");
        int mapped=Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Count(r=>r.lightmapIndex>=0&&r.lightmapIndex<maps.Length);
        if(mapped<6)throw new InvalidOperationException("Too few renderers reference the completed bake: "+mapped);
        evidence.Add("Baked lightmaps: "+maps.Length+"; mapped renderers: "+mapped);
        foreach(var map in maps)evidence.Add("Lightmap: "+AssetDatabase.GetAssetPath(map.lightmapColor)+" "+map.lightmapColor.width+"×"+map.lightmapColor.height+"; shadowmask="+(map.shadowMask?AssetDatabase.GetAssetPath(map.shadowMask):"none"));
        evidence.Add("LightingDataAsset: "+AssetDatabase.GetAssetPath(Lightmapping.lightingDataAsset));
        var scene=SceneManager.GetActiveScene();EditorSceneManager.MarkSceneDirty(scene);
        if(!EditorSceneManager.SaveScene(scene,PhysicsPath))throw new IOException("Could not save baked Physics scene.");
        AssetDatabase.SaveAssets();
        new GameObject("VLAB Application",typeof(VlabApp));
        Directory.CreateDirectory("Assets/VLABProduct/Scenes");
        if(!EditorSceneManager.SaveScene(scene,BootPath))throw new IOException("Could not regenerate VLAB_Boot from the baked Physics scene.");
        evidence.Add("Saved Physics and regenerated VLAB_Boot using the existing ProductBuild scene-clone pattern.");
        evidence.Add("Other room scenes, original FourLabs and source Delivery were not modified.");
        evidence.Add("Visual quality, device comfort and stereo readability require the subsequent runtime render review.");
        AssetDatabase.SaveAssets();Write("PASS");Unsubscribe();UnityEngine.Debug.Log("VLAB PHYSICS VISUAL REPAIR COMPLETE");EditorApplication.Exit(0);
    }
    static void Fail(Exception error)
    {
        finishing=true;Unsubscribe();errors.Add(error.ToString());
        try{Directory.CreateDirectory("Evidence");Write("FAILED");}catch(Exception receiptError){UnityEngine.Debug.LogException(receiptError);}
        UnityEngine.Debug.LogException(error);EditorApplication.Exit(1);
    }
    static void Write(string result)
    {
        File.WriteAllLines(Receipt,new[]{result,"Unity "+Application.unityVersion,"UTC "+DateTime.UtcNow.ToString("O"),"Elapsed seconds: "+(elapsed?.Elapsed.TotalSeconds??0).ToString("0.0",System.Globalization.CultureInfo.InvariantCulture)}.Concat(evidence).Concat(errors));
    }
    static void Unsubscribe(){Lightmapping.bakeCompleted-=BakeCompleted;EditorApplication.update-=Update;Application.logMessageReceived-=Log;}
    static void Backup()
    {
        const string destination="Evidence/VisualRepairBefore";Directory.CreateDirectory(destination);
        foreach(string path in new[]{PhysicsPath,BootPath})if(File.Exists(path))
        {string copy=Path.Combine(destination,Path.GetFileName(path));if(!File.Exists(copy))File.Copy(path,copy);}
        const string source="Assets/Labs/Scenes/Physics";
        if(Directory.Exists(source))foreach(string path in Directory.GetFiles(source,"*",SearchOption.AllDirectories))
        {string copy=Path.Combine(destination,"Physics",path.Substring(source.Length).TrimStart('/','\\'));Directory.CreateDirectory(Path.GetDirectoryName(copy));if(!File.Exists(copy))File.Copy(path,copy);}
        evidence.Add("Pre-repair scene/lightmap backup retained at "+destination+"; failure does not erase that backup.");
    }
}
#endif
