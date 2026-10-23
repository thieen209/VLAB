#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class FinalPassShadeUpgrade
{
    const string Folder="Assets/Labs/Upgrade/RollerBlind/";
    public static void Run()
    {
        var rows=new List<string>();string output="Evidence/FinalPass/Rooms";Directory.CreateDirectory(output+"/BeforeShade");
        try
        {
            if(EditorApplication.isPlaying||Lightmapping.isRunning)throw new Exception("Idle editor required");
            var model=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"LabRollerBlind.fbx");if(!model)throw new Exception("Missing blind model");
            var material=AssetDatabase.LoadAssetAtPath<Material>(Folder+"BlindIvory.mat");
            if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.SetColor("_BaseColor",new Color(.72f,.70f,.65f));material.SetFloat("_Smoothness",.12f);AssetDatabase.CreateAsset(material,Folder+"BlindIvory.mat");}
            foreach(string room in new[]{"Physics","Chemistry","Biology","Engineering"})
            {
                string scenePath="Assets/Labs/Scenes/"+room+".unity",backup=output+"/BeforeShade/"+room+".unity";
                if(!File.Exists(backup))File.Copy(scenePath,backup);
                var scene=EditorSceneManager.OpenScene(scenePath);var shade=UnityEngine.Object.FindAnyObjectByType<LabShade>();if(!shade)throw new Exception(room+" missing shade");
                if(shade.bottomBar){rows.Add("SKIP already upgraded "+room);continue;}
                var control=shade.GetComponentsInChildren<LabFixture>().Single(f=>f.action==LabFixture.Action.Shade);
                foreach(var r in shade.GetComponentsInChildren<Renderer>())r.enabled=false;
                var instance=(GameObject)PrefabUtility.InstantiatePrefab(model,shade.transform);instance.name="Authored roller blind";PrefabUtility.UnpackPrefabInstance(instance,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
                float scale=shade.width/1.75f;instance.transform.localPosition=new Vector3(0,0,.075f);instance.transform.localRotation=Quaternion.identity;instance.transform.localScale=Vector3.one*scale;
                var fabric=instance.GetComponentsInChildren<Transform>().Single(t=>t.name=="ShadeFabric");
                var wrapper=new GameObject("Fabric travel").transform;wrapper.SetParent(instance.transform,false);fabric.SetParent(wrapper,false);shade.fabric=wrapper;shade.fabricScaleY=shade.fullDrop/(1.95f*scale);
                shade.bottomBar=instance.GetComponentsInChildren<Transform>().Single(t=>t.name=="ShadeBottomBar");shade.bottomY=-shade.fullDrop/scale;
                foreach(var r in instance.GetComponentsInChildren<Renderer>()){r.sharedMaterial=material;GameObjectUtility.SetStaticEditorFlags(r.gameObject,0);}
                var loop=instance.GetComponentsInChildren<Renderer>().Single(r=>r.name=="Blind pull loop");
                control.transform.localScale=Vector3.one;control.transform.position=loop.bounds.center;var hit=control.GetComponent<BoxCollider>();hit.center=Vector3.zero;hit.size=new Vector3(.12f,Mathf.Max(.18f,loop.bounds.size.y),.12f);
                shade.lowered=false;shade.Step(10);var camera=UnityEngine.Object.FindObjectsByType<Camera>().First(c=>c.name.Contains("Learner"));
                var cameraPosition=camera.transform.position;var cameraRotation=camera.transform.rotation;float cameraFov=camera.fieldOfView;
                camera.transform.position=shade.transform.position+shade.transform.forward*1.6f-Vector3.up*.4f;camera.transform.LookAt(shade.transform.position-Vector3.up*.45f);camera.fieldOfView=55;
                BuildLabs.Capture(camera,output+"/blind-open-"+room+".png",1280,800);
                shade.Toggle();for(int i=0;i<120;i++)shade.Step(1f/60);
                if(Mathf.Abs(shade.bottomBar.localPosition.y-shade.bottomY)>.001f)throw new Exception(room+" lower endpoint");
                BuildLabs.Capture(camera,output+"/blind-closed-"+room+".png",1280,800);
                shade.Toggle();shade.Step(.2f);float y=shade.bottomBar.localPosition.y;shade.Toggle();shade.Step(1f/60);if(Mathf.Abs(shade.bottomBar.localPosition.y-y)>.05f)throw new Exception(room+" direction reversal jumped");
                for(int j=0;j<10;j++){shade.Toggle();shade.Step(10);}
                shade.lowered=false;shade.Step(10);if(Mathf.Abs(shade.bottomBar.localPosition.y-shade.bottomY*.025f/shade.fullDrop)>.001f)throw new Exception(room+" raise endpoint");
                // Restore the learner camera before saving; QA capture must not alter user spawn.
                camera.transform.SetPositionAndRotation(cameraPosition,cameraRotation);camera.fieldOfView=cameraFov;
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                rows.Add("PASS "+room+" authored blind fits width; down/up endpoints; reversal continuity; ten toggles; original learner pose restored. Lighting not yet upgraded.");
            }
            File.WriteAllLines(output+"/shade-upgrade.txt",rows);AssetDatabase.SaveAssets();EditorApplication.Exit(0);
        }
        catch(Exception e){rows.Add("FAIL "+e);File.WriteAllLines(output+"/shade-upgrade.txt",rows);Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
#endif
