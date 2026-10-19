using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class BuildLabs {
 [Serializable] public class Manifest { public Room[] rooms; }
 [Serializable] public class Room { public string name; public float[] size; public Node[] nodes; public Fixture[] lights; }
 [Serializable] public class Node { public float[] p; }
 [Serializable] public class Fixture { public float[] p,size; }
 static Vector3 V(float[] v)=>new Vector3(v[0],v[1],v[2]);
 const string Root="Assets/Labs/";
 public static void Run() {
  Directory.CreateDirectory(Root+"Scenes");Directory.CreateDirectory(Root+"Materials");Directory.CreateDirectory(Root+"Prefabs");Directory.CreateDirectory("../Evidence");
  AssetDatabase.Refresh();
  var data=JsonUtility.FromJson<Manifest>(File.ReadAllText(Root+"rooms.json"));
  var summaries=new List<string>();var builds=new List<EditorBuildSettingsScene>();
  foreach(var room in data.rooms) {
   string path=Root+"Models/"+room.name+".fbx";
   var importer=(ModelImporter)AssetImporter.GetAtPath(path);
   importer.globalScale=1;importer.useFileScale=true;importer.generateSecondaryUV=false;importer.isReadable=true;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;importer.SaveAndReimport();
   var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
   var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));model.name=room.name+" Room";
   // FBX handedness converts the authored X axis to negative X; restore the plan's east-positive axis.
   model.transform.localScale=new Vector3(-1,1,1);
   var floor=model.GetComponentsInChildren<MeshRenderer>().First(r=>r.name.StartsWith("Floor"));
   if(Vector3.Distance(floor.bounds.center,new Vector3(room.size[0]/2,-.075f,room.size[2]/2))>.01f)throw new Exception("FBX room coordinates do not match the source plan");
   int triangles=0;var materialCache=new Dictionary<string,Material>();
   foreach(var r in model.GetComponentsInChildren<MeshRenderer>()) {
    var materials=r.sharedMaterials;
    for(int i=0;i<materials.Length;i++) {
     var old=materials[i];string key=old?old.name:"White";
     if(!materialCache.TryGetValue(key,out var m)) {
      string mp=Root+"Materials/"+key+".mat";m=AssetDatabase.LoadAssetAtPath<Material>(mp);
      if(!m) {m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.name=key; m.SetColor("_BaseColor",old?old.color:Color.white);m.SetFloat("_Smoothness",key.Contains("Steel")?.65f:.28f);m.SetFloat("_Metallic",key.Contains("Steel")?.7f:0);if(key.Contains("Light")){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",new Color(1,.98f,.85f)*1.5f);}AssetDatabase.CreateAsset(m,mp);}materialCache[key]=m;
     }materials[i]=m;
    }r.sharedMaterials=materials;
    if(r.name.StartsWith("Room-title")){r.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/White.mat");r.transform.localScale*=2;}
    if(r.name.StartsWith("Coved-base")){r.transform.position+=r.name.Contains("west")?Vector3.right*.012f:r.name.Contains("east")?Vector3.left*.012f:Vector3.back*.012f;}
    foreach(var m in r.sharedMaterials)if(m&&m.name=="Glass"){m.SetColor("_BaseColor",new Color(.58f,.74f,.77f,.26f));m.SetFloat("_Surface",1);m.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);m.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);m.SetFloat("_ZWrite",0);m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");m.renderQueue=(int)RenderQueue.Transparent;EditorUtility.SetDirty(m);}
    var mf=r.GetComponent<MeshFilter>();triangles+=mf.sharedMesh.triangles.Length/3;
    if(!r.name.Contains("title")&&!r.name.Contains("label")&&!r.name.Contains("Exit")){var col=r.gameObject.AddComponent<MeshCollider>();col.sharedMesh=mf.sharedMesh;}
    GameObjectUtility.SetStaticEditorFlags(r.gameObject,StaticEditorFlags.BatchingStatic|StaticEditorFlags.ContributeGI|StaticEditorFlags.ReflectionProbeStatic);
   }
   var prefab=PrefabUtility.SaveAsPrefabAsset(model,Root+"Prefabs/"+room.name+"Room.prefab");
   RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.67f,.72f,.77f);RenderSettings.ambientEquatorColor=new Color(.44f,.49f,.51f);RenderSettings.ambientGroundColor=new Color(.28f,.30f,.29f);RenderSettings.ambientIntensity=1;
   RenderSettings.skybox=null;RenderSettings.fog=false;
   foreach(var f in room.lights){var go=new GameObject("Ceiling illumination");go.transform.position=V(f.p)-Vector3.up*.10f;go.transform.rotation=Quaternion.Euler(90,0,0);var l=go.AddComponent<Light>();l.type=LightType.Spot;l.spotAngle=135;l.innerSpotAngle=90;l.range=6;l.intensity=3.4f;l.color=new Color(1,.97f,.90f);l.shadows=LightShadows.Soft;l.shadowStrength=.45f;l.shadowBias=.03f;}
   var fill=new GameObject("Window soft fill");fill.transform.position=room.name=="Biology"||room.name=="Engineering"?new Vector3(room.size[0]/2,2,room.size[2]-.6f):new Vector3(room.size[0]-.6f,2,room.size[2]*.6f);var fl=fill.AddComponent<Light>();fl.type=LightType.Point;fl.range=6;fl.intensity=.3f;fl.color=new Color(.82f,.89f,1);fl.shadows=LightShadows.None;
   var camObj=new GameObject("Learner Camera");camObj.tag="MainCamera";var cam=camObj.AddComponent<Camera>();cam.nearClipPlane=.04f;cam.farClipPlane=30;cam.fieldOfView=72;cam.backgroundColor=new Color(.64f,.73f,.78f);cam.clearFlags=CameraClearFlags.SolidColor;camObj.AddComponent<AudioListener>();camObj.AddComponent<UniversalAdditionalCameraData>();
   camObj.transform.position=V(room.nodes[0].p);camObj.transform.LookAt(new Vector3(room.size[0]*.52f,1.25f,room.size[2]-.2f));
   var nav=camObj.AddComponent<LabReview>();nav.roomNames=data.rooms.Select(x=>x.name).ToArray();nav.positions=room.nodes.Select(x=>V(x.p)).ToArray();nav.roomIndex=Array.FindIndex(data.rooms,x=>x.name==room.name);
   for(int n=0;n<room.nodes.Length;n++){var anchor=new GameObject("Teleport_"+n);anchor.transform.position=V(room.nodes[n].p);}
   Physics.SyncTransforms();int clear=0;
   foreach(var n in room.nodes){var p=V(n.p);var overlaps=Physics.OverlapCapsule(new Vector3(p.x,.31f,p.z),new Vector3(p.x,1.45f,p.z),.28f);if(overlaps.Length==0)clear++;else summaries.Add(room.name+" NODE BLOCKED "+p+": "+string.Join(",",overlaps.Select(x=>x.name)));}
   string sp=Root+"Scenes/"+room.name+".unity";EditorSceneManager.SaveScene(scene,sp);builds.Add(new EditorBuildSettingsScene(sp,true));
   summaries.Add(room.name+": triangles="+triangles+" renderers="+model.GetComponentsInChildren<MeshRenderer>().Length+" clear_nodes="+clear+"/"+room.nodes.Length+" missing_scripts="+model.GetComponentsInChildren<Transform>().Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)));
   Capture(cam,"../Evidence/"+room.name+"-entry.png",1280,900);
   for(int c=0;c<4;c++){cam.transform.position=new Vector3(c%2==0?.95f:room.size[0]-.95f,1.65f,c<2?.95f:room.size[2]-.95f);cam.transform.LookAt(new Vector3(room.size[0]/2,1.25f,room.size[2]/2));cam.fieldOfView=100;Capture(cam,"../Evidence/"+room.name+"-corner"+c+".png",960,720);}
   cam.transform.position=new Vector3(room.size[0]/2,1.5f,room.size[2]/2);cam.transform.rotation=Quaternion.Euler(-90,0,0);Capture(cam,"../Evidence/"+room.name+"-ceiling.png",960,720);
   cam.fieldOfView=72;cam.transform.position=V(room.nodes[0].p);cam.transform.LookAt(new Vector3(room.size[0]*.52f,1.25f,room.size[2]-.2f));EditorSceneManager.SaveScene(scene,sp);
  }
  EditorBuildSettings.scenes=builds.ToArray();PlayerSettings.productName="VLAB Four Labs";PlayerSettings.companyName="VLAB";PlayerSettings.defaultInterfaceOrientation=UIOrientation.LandscapeLeft;
  AssetDatabase.SaveAssets();File.WriteAllLines("../Evidence/unity-checks.txt",summaries);EditorSceneManager.OpenScene(Root+"Scenes/Physics.unity");Debug.Log("Four rooms saved; validation evidence written.");
 }
 public static void Capture(Camera cam,string path,int w,int h){var rt=new RenderTexture(w,h,24);cam.targetTexture=rt;cam.Render();var prev=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(w,h,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,w,h),0,0);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());RenderTexture.active=prev;cam.targetTexture=null;UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(rt);}
}

