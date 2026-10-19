using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.IO;
using System.Linq;
public static class FinishRoomAppearance {
 const string U="Assets/Labs/Upgrade/";
 static Material Mat(string n)=>AssetDatabase.LoadAssetAtPath<Material>(U+"Materials/"+n+".mat");
 public static void Run(){
 if(EditorApplication.isPlaying||Lightmapping.isRunning||UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new System.Exception("Saved idle editor required");
 EditorSceneManager.OpenScene("Assets/Labs/Scenes/Chemistry.unity");var sign=GameObject.Find("Sketchfab exit sign - JeffK");PrefabUtility.SaveAsPrefabAsset(sign,U+"WorkingExitSign.prefab");var signPosition=sign.transform.position;var signRotation=sign.transform.rotation;var signScale=sign.transform.localScale;
 foreach(var room in new[]{"Physics","Biology","Engineering"}){
 string sp="Assets/Labs/Scenes/"+room+".unity";Directory.CreateDirectory("../Backups/BeforeAppearance");if(!File.Exists("../Backups/BeforeAppearance/"+room+".unity"))File.Copy(sp,"../Backups/BeforeAppearance/"+room+".unity");var scene=EditorSceneManager.OpenScene(sp);if(GameObject.Find("Donor daylight treatment"))continue;
 float width=room=="Physics"?4.8f:room=="Biology"?4.6f:4.2f,depth=room=="Biology"?3.8f:3.6f,doorX=room=="Biology"?1.7f:1.6f;
 foreach(var root in scene.GetRootGameObjects())if(PrefabUtility.IsPartOfPrefabInstance(root)&&!root.GetComponent<LabSink>())PrefabUtility.UnpackPrefabInstance(root,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
 foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name.StartsWith("Door-")||t.name.StartsWith("Exit")||t.name.StartsWith("Window-")||t.name.StartsWith("Roller-shade")).ToArray())if(t)Object.DestroyImmediate(t.gameObject);
 var door=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(U+"WorkingEntrance.prefab"));door.name="Working entrance";door.transform.SetPositionAndRotation(new Vector3(doorX,0,-.025f),Quaternion.identity);
 var exit=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(U+"WorkingExitSign.prefab"));exit.transform.SetPositionAndRotation(signPosition+new Vector3(doorX-1.75f,0,0),signRotation);exit.transform.localScale=signScale;
 bool east=room=="Physics";Vector3 wp=east?new Vector3(width-.02f,1.725f,2.05f):new Vector3(room=="Biology"?2.35f:1.75f,1.75f,depth-.02f);float ww=east?1.6f:1.4f,wh=east?1.25f:1;
 var win=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(U+"FittedWindow.prefab"));win.name="Fitted Sketchfab window";win.transform.SetPositionAndRotation(wp,Quaternion.Euler(0,east?-90:180,0));win.transform.localScale=new Vector3((ww+.05f)/.70f,(wh+.076f)/1.076f,1);
 var treatment=new GameObject("Donor daylight treatment");var palette=new Material(Mat("ChemistrySage"));palette.name=room+" cabinet finish";palette.SetColor("_BaseColor",room=="Biology"?new Color(.52f,.61f,.53f):room=="Physics"?new Color(.42f,.55f,.64f):new Color(.43f,.52f,.55f));AssetDatabase.CreateAsset(palette,U+"Materials/"+room+"Finish.mat");
 foreach(var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)){
 if(r.name.StartsWith("Wall-"))r.sharedMaterial=Mat("WarmPlaster");else if(r.name.StartsWith("Floor"))r.sharedMaterial=Mat("DonorFloor");else if(r.name.StartsWith("Ceiling-grid")){r.sharedMaterial=Mat("White");var s=r.transform.localScale;s.y*=.35f;r.transform.localScale=s;}else if(r.name.StartsWith("Ceiling"))r.sharedMaterial=Mat("AcousticCeiling");
 else if(r.sharedMaterial&&(r.sharedMaterial.name==room||r.sharedMaterial.name=="Frame"))r.sharedMaterial=palette;
 if(r.name.EndsWith("-top"))r.sharedMaterial=Mat("Top");
 }
 RenderSettings.skybox=Mat("DonorDaylightSky");RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.72f,.77f,.83f);RenderSettings.ambientEquatorColor=new Color(.60f,.60f,.58f);RenderSettings.ambientGroundColor=new Color(.34f,.34f,.32f);
 foreach(var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None)){if(light.type==LightType.Spot){light.intensity=1.2f;light.color=new Color(1,.98f,.95f);}else light.enabled=false;}
 var sun=new GameObject("Soft daylight").AddComponent<Light>();sun.transform.SetParent(treatment.transform);sun.type=LightType.Directional;sun.intensity=1.8f;sun.color=new Color(1,.97f,.91f);sun.transform.rotation=Quaternion.Euler(36,east?270:180,0);sun.shadows=LightShadows.Soft;sun.shadowBias=.02f;sun.shadowNormalBias=.12f;sun.lightmapBakeType=LightmapBakeType.Mixed;
 foreach(float z in new[]{depth*.3f,depth*.73f}){var a=new GameObject("Baked ceiling softbox").AddComponent<Light>();a.transform.SetParent(treatment.transform);a.type=LightType.Rectangle;a.areaSize=new Vector2(width*.55f,.8f);a.transform.SetPositionAndRotation(new Vector3(width/2,2.72f,z),Quaternion.Euler(90,0,0));a.intensity=10;a.color=new Color(1,.98f,.95f);a.lightmapBakeType=LightmapBakeType.Baked;}
 var fill=new GameObject("Baked window fill").AddComponent<Light>();fill.transform.SetParent(treatment.transform);fill.type=LightType.Rectangle;fill.areaSize=new Vector2(ww,wh);fill.transform.SetPositionAndRotation(wp+(east?Vector3.left:Vector3.back)*.08f,Quaternion.Euler(0,east?-90:180,0));fill.intensity=17;fill.color=new Color(1,.99f,.97f);fill.lightmapBakeType=LightmapBakeType.Baked;
 var volume=treatment.AddComponent<Volume>();volume.isGlobal=true;volume.sharedProfile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(U+"Materials/ChemistryGrading.asset");var cam=Camera.main;cam.clearFlags=CameraClearFlags.Skybox;cam.allowHDR=true;cam.GetUniversalAdditionalCameraData().renderPostProcessing=true;
 var probe=new GameObject("Room reflection").AddComponent<ReflectionProbe>();probe.transform.SetParent(treatment.transform);probe.transform.position=new Vector3(width/2,1.5f,depth/2);probe.size=new Vector3(width,2.8f,depth);probe.boxProjection=true;probe.resolution=128;probe.mode=ReflectionProbeMode.Realtime;probe.refreshMode=ReflectionProbeRefreshMode.OnAwake;
 var probes=new GameObject("Room probes").AddComponent<LightProbeGroup>();probes.transform.SetParent(treatment.transform);var positions=new System.Collections.Generic.List<Vector3>();foreach(float x in new[]{.6f,width/2,width-.6f})foreach(float z in new[]{.6f,depth/2,depth-.6f})foreach(float y in new[]{.4f,1.2f,2.3f})positions.Add(new Vector3(x,y,z));probes.probePositions=positions.ToArray();
 var lighting=Object.Instantiate(AssetDatabase.LoadAssetAtPath<LightingSettings>(U+"ChemistryLighting.asset"));lighting.name=room+"Lighting";lighting.lightmapResolution=24;lighting.directSampleCount=64;lighting.indirectSampleCount=128;lighting.environmentSampleCount=128;AssetDatabase.CreateAsset(lighting,U+room+"Lighting.asset");Lightmapping.lightingSettings=lighting;
 int count=0;string uvDir=U+"LightmapMeshes/"+room;Directory.CreateDirectory(uvDir);AssetDatabase.Refresh();
 foreach(var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)){
 var filter=r.GetComponent<MeshFilter>();if(!filter||!filter.sharedMesh)continue;
 bool eligible=!r.GetComponentInParent<LabFixture>()&&r.bounds.size.magnitude>=.15f&&!r.sharedMaterials.Any(m=>m&&m.renderQueue>=3000);
 if(eligible&&filter.sharedMesh.uv2.Length!=filter.sharedMesh.vertexCount){var mesh=Object.Instantiate(filter.sharedMesh);UnwrapParam p;UnwrapParam.SetDefaults(out p);p.packMargin=.001f;eligible=Unwrapping.GenerateSecondaryUVSet(mesh,p)&&mesh.uv2.Length==mesh.vertexCount;if(eligible){AssetDatabase.CreateAsset(mesh,uvDir+"/"+(count++)+".asset");filter.sharedMesh=mesh;var col=r.GetComponent<MeshCollider>();if(col)col.sharedMesh=mesh;}else Object.DestroyImmediate(mesh);}
 GameObjectUtility.SetStaticEditorFlags(r.gameObject,eligible?StaticEditorFlags.ContributeGI|StaticEditorFlags.ReflectionProbeStatic:0);r.receiveGI=eligible?ReceiveGI.Lightmaps:ReceiveGI.LightProbes;
 }
 EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Debug.Log(room+" appearance saved; UV meshes "+count);
 }
 AssetDatabase.SaveAssets();EditorSceneManager.OpenScene("Assets/Labs/Scenes/Chemistry.unity");
 }
}
