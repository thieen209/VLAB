using System;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class UpgradeRemainingStorage {
 static void Panel(string name,Transform parent,Vector3 p,Vector3 size,Material mat){var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=name;o.transform.SetParent(parent,false);o.transform.localPosition=p;o.transform.localScale=size;o.GetComponent<Renderer>().sharedMaterial=mat;}
 public static void Run(){
 if(EditorApplication.isPlaying||Lightmapping.isRunning||UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Saved idle editor required");
 var original=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
 Directory.CreateDirectory("../Backups/BeforeRemainingStorage");
 try{foreach(var room in new[]{"Physics","Biology","Engineering"}){
 var scenePath="Assets/Labs/Scenes/"+room+".unity";var backup="../Backups/BeforeRemainingStorage/"+room+".unity";if(!File.Exists(backup))File.Copy(scenePath,backup);
 var scene=EditorSceneManager.OpenScene(scenePath);if(GameObject.Find(room+" working storage"))continue;
 foreach(var sceneRoot in scene.GetRootGameObjects())if(PrefabUtility.IsPartOfPrefabInstance(sceneRoot))PrefabUtility.UnpackPrefabInstance(sceneRoot,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction); var root=new GameObject(room+" working storage").transform;var renderers=UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);var mat=AssetDatabase.LoadAssetAtPath<Material>("Assets/Labs/Materials/White.mat");
 foreach(var front in renderers.Where(r=>r.name.Contains("-door")||r.name.Contains("-drawer"))){
 bool drawer=front.name.Contains("-drawer");string prefix=front.name.Substring(0,front.name.IndexOf(drawer?"-drawer":"-door"));var b=front.bounds;
 var pull=renderers.Where(r=>r.name.StartsWith(prefix+"-pull")&&!r.GetComponentInParent<LabFixture>()).OrderBy(r=>Vector3.Distance(r.bounds.center,b.center)).First();
 Vector3 outward=pull.bounds.center-b.center;outward.y=0;outward=b.size.x<b.size.z?new Vector3(Mathf.Sign(outward.x),0,0):new Vector3(0,0,Mathf.Sign(outward.z));
 var pivot=new GameObject(front.name+(drawer?" sliding box":" hinge")).transform;pivot.SetParent(root,false);pivot.position=b.center;
 var fixture=pivot.gameObject.AddComponent<LabFixture>();fixture.movingPart=pivot;
 if(drawer){pivot.rotation=Quaternion.LookRotation(outward);fixture.action=LabFixture.Action.Drawer;// Movement uses the world-aligned parent axes.
 fixture.openOffset=outward*.26f;fixture.label="Open / close drawer";float w=(Mathf.Abs(outward.x)>.5f?b.size.z:b.size.x)-.035f,h=b.size.y-.025f,d=.35f;
 Panel("Drawer bottom",pivot,new Vector3(0,-h/2,-d/2),new Vector3(w,.012f,d),mat);foreach(int side in new[]{-1,1})Panel("Drawer side",pivot,new Vector3(side*(w-.012f)/2,0,-d/2),new Vector3(.012f,h,d),mat);Panel("Drawer back",pivot,new Vector3(0,0,-d),new Vector3(w,h,.012f),mat);
 }else{int axis=Mathf.Abs(outward.x)>.5f?2:0;var siblings=renderers.Where(r=>r.name.StartsWith(prefix+"-door")).ToArray();float mid=siblings.Average(r=>r.bounds.center[axis]);bool low=b.center[axis]<mid;
 var width=Vector3.zero;width[axis]=1;var local=front.transform.InverseTransformDirection(width);int localAxis=0;for(int a=1;a<3;a++)if(Mathf.Abs(local[a])>Mathf.Abs(local[localAxis]))localAxis=a;var scale=front.transform.localScale;scale[localAxis]*=(b.size[axis]+.022f)/b.size[axis];front.transform.localScale=scale;
 var p=b.center;p[axis]+=(low?-1:1)*(b.size[axis]+.022f)/2;pivot.position=p;float sign=Mathf.Sign(Vector3.Dot(Vector3.Cross(Vector3.up,b.center-p),outward));fixture.openAngles=new Vector3(0,sign*95,0);fixture.label="Open / close cabinet";
 }
 front.transform.SetParent(pivot,true);pull.transform.SetParent(pivot,true);foreach(var t in pivot.GetComponentsInChildren<Transform>())GameObjectUtility.SetStaticEditorFlags(t.gameObject,0);
 }
 // Remove only the fixed shelves behind drawer fronts; each drawer now has its own bottom.
 foreach(var r in renderers.Where(r=>r&&r.name.Contains("-shelf")).ToArray()){string prefix=r.name.Substring(0,r.name.IndexOf("-shelf"));if(renderers.Any(f=>f&&f.name.StartsWith(prefix+"-drawer")))UnityEngine.Object.DestroyImmediate(r.gameObject);}
 if(!Camera.main.GetComponent<LabInteractor>())Camera.main.gameObject.AddComponent<LabInteractor>();
 PrefabUtility.SaveAsPrefabAsset(root.gameObject,"Assets/Labs/Upgrade/"+room+"WorkingStorage.prefab");EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
 Debug.Log(room+": saved "+root.GetComponentsInChildren<LabFixture>().Length+" working storage controls");
 }}finally{EditorSceneManager.OpenScene(original);}AssetDatabase.SaveAssets();
 }
}


