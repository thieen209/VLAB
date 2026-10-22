#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using Vlab.Product;

[InitializeOnLoad]
public static class VrWorkstationChecks
{
    public static readonly string Folder=Path.GetFullPath("../work/vr-workstation-code18");
    public static void AuditDecor()
    {
        var rows=new List<string>();
        foreach(string name in new[]{"Physics","Chemistry","Biology","Engineering"})
        {
            EditorSceneManager.OpenScene("Assets/Labs/Scenes/"+name+".unity");
            var parts=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            var balances=parts.Where(t=>t.name.IndexOf("balance",StringComparison.OrdinalIgnoreCase)>=0).ToArray();
            rows.Add(name+" balance-named scene transforms="+balances.Length);
            foreach(var part in balances)rows.Add(name+" balance name="+part.name+" activeSelf="+part.gameObject.activeSelf+" activeHierarchy="+part.gameObject.activeInHierarchy+" prefab="+PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(part.gameObject));
            foreach(var part in parts.Where(t=>BenchDisplayVisibility.IsDisplayProp(t.name)))
            {rows.Add(name+" known decor="+part.name+" activeHierarchy="+part.gameObject.activeInHierarchy);if(part.gameObject.activeInHierarchy)throw new Exception("Decor still active: "+name+"/"+part.name);}
        }
        File.WriteAllLines(Path.Combine(Folder,"decor-instance-audit.txt"),rows);
        EditorSceneManager.OpenScene("Assets/VLABProduct/Scenes/VLAB_Boot.unity");
    }
    static VrWorkstationChecks(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("VLAB.WorkstationCheck",false)){SessionState.SetBool("VLAB.WorkstationCheck",false);new GameObject("VR workstation checks",typeof(VrWorkstationProbe));}};}
    public static void Run()
    {
        Directory.CreateDirectory(Folder);var rows=new List<string>();
        const string path="Assets/VLABProduct/Resources/UI/WorktopIvory.mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}
        material.color=new Color(.84f,.85f,.82f);material.SetFloat("_Metallic",0);material.SetFloat("_Smoothness",.1f);EditorUtility.SetDirty(material);
        foreach(string name in new[]{"Physics","Chemistry","Biology","Engineering"})
        {
            var scene=EditorSceneManager.OpenScene("Assets/Labs/Scenes/"+name+".unity");int tops=0,props=0;
            foreach(var renderer in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                var mats=renderer.sharedMaterials;bool changed=false;
                for(int i=0;i<mats.Length;i++)if(mats[i]&&(mats[i].name=="Top"||mats[i]==material)){mats[i]=material;changed=true;}
                if(changed){renderer.sharedMaterials=mats;PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);tops++;}
                var fixture=renderer.GetComponentInParent<LabFixture>();
                if(BenchDisplayVisibility.IsDisplayProp(renderer.name)&&!renderer.GetComponentInParent<EquipmentHandle>()&&(!fixture||!fixture.CanInteract))
                {rows.Add(name+" decor instance: "+renderer.name+" wasActive="+renderer.gameObject.activeSelf+" fixture="+(fixture?fixture.label:"none"));renderer.gameObject.SetActive(false);PrefabUtility.RecordPrefabInstancePropertyModifications(renderer.gameObject);props++;}
            }
            if(tops==0)throw new Exception("No actual worktop material assigned: "+name);
            rows.Add(name+" ivory worktop renderers="+tops+" decorative balance/tray parts disabled="+props);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        AssetDatabase.SaveAssets();File.WriteAllLines(Path.Combine(Folder,"room-materials.txt"),rows);
        if(!SecondTestGameView.Configure(out var why))throw new Exception(why);
        SessionState.SetBool("VLAB.WorkstationCheck",true);SessionState.SetString("VLAB.TestProfile",Path.Combine(Application.persistentDataPath,"QA","Workstation-"+Guid.NewGuid().ToString("N")));
        EditorSceneManager.OpenScene("Assets/VLABProduct/Scenes/VLAB_Boot.unity");EditorApplication.EnterPlaymode();
    }
}
public sealed class VrWorkstationProbe:MonoBehaviour
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    readonly List<string> rows=new List<string>();int failed;
    VlabApp App=>VlabApp.Instance;
    object Call(string name,params object[] args)=>typeof(VlabApp).GetMethod(name,Flags).Invoke(App,args);
    T Field<T>(string name)=>(T)typeof(VlabApp).GetField(name,Flags).GetValue(App);
    void Check(string name,bool ok){rows.Add((ok?"PASS ":"FAIL ")+name);if(!ok)failed++;}
    IEnumerator Press(string name){yield return new WaitForSecondsRealtime(.8f);var button=App.GetComponentsInChildren<Button>(false).FirstOrDefault(b=>b.name==name);if(!button)throw new Exception("Missing "+name+" route="+App.Route+" buttons="+string.Join("|",App.GetComponentsInChildren<Button>(false).Select(b=>b.name)));button.onClick.Invoke();yield return new WaitForSecondsRealtime(.8f);}
    IEnumerator Start()
    {
        DontDestroyOnLoad(gameObject);var stack=new Stack<IEnumerator>();stack.Push(Checks());
        while(stack.Count>0){object v=null;bool next=false;Exception error=null;try{next=stack.Peek().MoveNext();if(next)v=stack.Peek().Current;}catch(Exception e){error=e;}if(error!=null){rows.Add("BLOCKER "+error);failed++;break;}if(!next){stack.Pop();continue;}if(v is IEnumerator child)stack.Push(child);else yield return v;}
        File.WriteAllLines(Path.Combine(VrWorkstationChecks.Folder,"checks.txt"),new[]{"Editor geometry/input-component assertions only, not physical head-tracking proof."}.Concat(rows));SecondTestGameView.Restore();EditorApplication.Exit(failed==0?0:1);
    }
    IEnumerator Checks()
    {
        float until=Time.realtimeSinceStartup+45;while((!App||App.Route!="home")&&Time.realtimeSinceStartup<until)yield return null;
        if(!App||App.Route!="home")throw new Exception("Home timeout");
        bool held=VlabInput.ContinueViewerHold(false,true,true);Check("SDK-authorized viewer trigger begins holding",held);
        held=VlabInput.ContinueViewerHold(held,false,true);Check("Viewer hold survives stationary touch",held);
        held=VlabInput.ContinueViewerHold(held,false,false);Check("Viewer release ends holding",!held);
        Check("Unapproved raw touch never starts a viewer grab",!VlabInput.ContinueViewerHold(false,false,true));
        Call("OpenSettings","home");typeof(VlabApp).GetField("settingsTab",Flags).SetValue(App,4);Call("ShowSettings");
        foreach(float opacity in new[]{.55f,.8f,1f})
        {
            var draft=Field<GameSettings>("draft");
            while(Mathf.Abs(draft.uiOpacity-opacity)>.01f){yield return Press(draft.uiOpacity<opacity?"Appearance opacity plus":"Appearance opacity minus");draft=Field<GameSettings>("draft");}
            var panel=Field<RectTransform>("page").GetComponentsInChildren<Image>().First(x=>x.name=="Settings");
            Check("Draft opacity immediately changes rendered panel alpha "+opacity,Mathf.Abs(panel.color.a-opacity)<.01f);
        }
        Call("ApplyDraft");Call("OpenSettings","home");Check("Applied opacity survives settings reopen",Mathf.Abs(Field<GameSettings>("draft").uiOpacity-1)<.01f);Call("ReturnTo","home");
        App.Profile.settings.gazeAutoSelect=false;App.Input.SetSimulator(true);yield return new WaitForSecondsRealtime(1);
        App.ShowHome();yield return new WaitForSecondsRealtime(.5f);App.Profile.settings.gazeAutoSelect=true;
        var subject=App.GetComponentsInChildren<Button>(false).First(x=>x.name=="Subject P");
        var subjectRect=(RectTransform)subject.transform;var gaze=new ActionFrame{pointer=new Ray(App.View.transform.position,subjectRect.TransformPoint(subjectRect.rect.center)-App.View.transform.position)};
        Call("ProcessGaze",gaze);typeof(VlabApp).GetField("gazeTime",Flags).SetValue(App,10f);Call("ProcessGaze",gaze);
        Check("Opening page cannot chain dwell into the initial target",App.Route=="home");
        gaze.pressed=true;Call("ProcessGaze",gaze);yield return new WaitForSecondsRealtime(.8f);Check("Explicit trigger still activates initial target",App.Route=="browser");App.Profile.settings.gazeAutoSelect=false;
        foreach(string id in new[]{"P01","C04","B01","M01"})
        {
            App.ShowHome();yield return Press("Subject "+id[0]);yield return Press("Browser lesson "+id);yield return Press("Browser start");yield return Press("Readiness enter");
            until=Time.realtimeSinceStartup+45;while(App.Route!="activity"&&Time.realtimeSinceStartup<until)yield return null;
            if(App.Session?.spec.id!=id)throw new Exception("Wrong lesson "+id);
            var surface=App.Apparatus.WorkSurface;var eye=App.View.transform.position;
            Check(id+" enters outside front edge within 0.85 m",eye.z<surface.min.z-.20f&&eye.z>surface.min.z-.85f);
            Check(id+" head clearance",!Physics.CheckSphere(eye,.15f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore));
            var origin=Field<Transform>("origin");var local=App.View.transform.localRotation;float lens=App.View.fieldOfView;
            Call("FocusStation");Check(id+" explicit workbench action preserves local head and lens",Quaternion.Angle(local,App.View.transform.localRotation)<.001f&&Mathf.Abs(lens-App.View.fieldOfView)<.001f);
            Check(id+" workbench return has no position drift",Vector3.Distance(eye,App.View.transform.position)<.001f);
            Call("ShowStationTools");var modal=Field<RectTransform>("modal");Check(id+" modal stays in front of close bench apparatus",modal&&Vector3.Distance(modal.position,App.View.transform.position)>.25f&&Vector3.Distance(modal.position,App.View.transform.position)<.65f);Call("CloseModal");
            var position=origin.position;App.Input.Recenter();Call("RecenterInterface");Check(id+" recenter preserves working position",Vector3.Distance(position,origin.position)<.001f);
            Physics.SyncTransforms();int reachable=0;
            foreach(var item in App.Apparatus.items.Where(x=>x.installed))foreach(var collider in item.GetComponentsInChildren<Collider>().Where(x=>x.enabled&&x.gameObject.activeInHierarchy))
            {
                var ray=new Ray(App.View.transform.position,collider.bounds.center-App.View.transform.position);
                if(!Physics.Raycast(ray,out var hit,3.5f)||hit.collider.GetComponentInParent<EquipmentHandle>()!=item)continue;
                if((bool)Call("PointerHitsUi",ray))continue;reachable++;break;
            }
            Check(id+" real colliders reachable without UI interception",reachable>0);rows.Add(id+" eye="+eye.ToString("F3")+" surface="+surface+" reachableItems="+reachable);
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(VrWorkstationChecks.Folder,id+"-editor.png"));yield return null;
            App.Input.SetSimulator(false);App.Input.SetSimulator(true);yield return null;Check(id+" phone to VR keeps reachable work position",App.View.transform.position.z<surface.min.z-.2f&&App.View.transform.position.z>surface.min.z-.85f);
        }
    }
}
#endif
