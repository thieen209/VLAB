#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class FinalPassRoomAudit
{
    public static void Run()
    {
        var rows=new List<string>();string output="Evidence/FinalPass/Rooms";Directory.CreateDirectory(output);
        try
        {
            string path="Assets/Labs/Upgrade/RollerBlind/LabRollerBlind.fbx";
            var importer=(ModelImporter)AssetImporter.GetAtPath(path);importer.bakeAxisConversion=true;importer.importAnimation=false;importer.SaveAndReimport();
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!asset)throw new Exception("Blind model missing");
            foreach(var t in asset.GetComponentsInChildren<Transform>())rows.Add("MODEL "+t.name+" local="+t.localPosition+" scale="+t.localScale+" rotation="+t.localEulerAngles);
            foreach(var room in new[]{"Physics","Chemistry","Biology","Engineering"})
            {
                EditorSceneManager.OpenScene("Assets/Labs/Scenes/"+room+".unity");rows.Add("ROOM "+room);
                foreach(var shade in UnityEngine.Object.FindObjectsByType<LabShade>())
                {rows.Add("SHADE "+shade.name+" pos="+shade.transform.position+" rot="+shade.transform.eulerAngles+" scale="+shade.transform.lossyScale+" width="+shade.width+" drop="+shade.fullDrop+" fabric="+shade.fabric);foreach(var t in shade.GetComponentsInChildren<Transform>())rows.Add("  "+t.name+" local="+t.localPosition+" scale="+t.localScale);}
                foreach(var fixture in UnityEngine.Object.FindObjectsByType<LabFixture>().Where(f=>f.action==LabFixture.Action.Shade))rows.Add("CONTROL "+fixture.name+" pos="+fixture.transform.position+" shade="+fixture.shade+" colliders="+fixture.GetComponentsInChildren<Collider>().Length);
                foreach(var light in UnityEngine.Object.FindObjectsByType<Light>())rows.Add("LIGHT "+light.name+" "+light.type+" "+light.lightmapBakeType+" enabled="+light.enabled+" intensity="+light.intensity+" pos="+light.transform.position);
                rows.Add("MAPS "+LightmapSettings.lightmaps.Length+" ambient="+RenderSettings.ambientMode+" sky="+RenderSettings.ambientSkyColor+" probes="+(LightmapSettings.lightProbes?LightmapSettings.lightProbes.count:0));
                foreach(var camera in UnityEngine.Object.FindObjectsByType<Camera>().Where(c=>c.name.Contains("Learner")))BuildLabs.Capture(camera,output+"/before-"+room+".png",1280,800);
            }
            File.WriteAllLines(output+"/inventory.txt",rows);EditorApplication.Exit(0);
        }
        catch(Exception e){rows.Add("FAIL "+e);File.WriteAllLines(output+"/inventory.txt",rows);Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
#endif
