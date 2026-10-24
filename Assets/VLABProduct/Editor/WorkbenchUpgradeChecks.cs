#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using Vlab.Product;

[InitializeOnLoad]
public static class WorkbenchUpgradeChecks
{
    static WorkbenchUpgradeChecks(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("VLAB.WorkbenchChecks",false)){SessionState.SetBool("VLAB.WorkbenchChecks",false);new GameObject("Workbench checks",typeof(WorkbenchUpgradeProbe));}};}
    public static void Run(){Directory.CreateDirectory("Evidence/WorkbenchUpgrade");SecondTestGameView.Configure(out _);SessionState.SetString("VLAB.TestProfile",Path.Combine(Application.persistentDataPath,"QA","Workbench-"+Guid.NewGuid().ToString("N")));SessionState.SetBool("VLAB.WorkbenchChecks",true);EditorSceneManager.OpenScene("Assets/VLABProduct/Scenes/VLAB_Boot.unity");EditorApplication.EnterPlaymode();}
}
public sealed class WorkbenchUpgradeProbe:MonoBehaviour
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    readonly List<string> rows=new List<string>();Touchscreen touch;Mouse mouse;int serial,failures;VlabApp App=>VlabApp.Instance;
    T Read<T>(string field)=>(T)typeof(VlabApp).GetField(field,Private).GetValue(App);
    object Call(string method,params object[] args)=>typeof(VlabApp).GetMethod(method,Private).Invoke(App,args);
    Button Find(string name)=>App.GetComponentsInChildren<Button>().FirstOrDefault(b=>b.name==name&&b.IsInteractable());
    void Check(string name,bool pass){rows.Add((pass?"PASS ":"FAIL ")+name);File.WriteAllLines("Evidence/WorkbenchUpgrade/progress.txt",rows);if(!pass)throw new Exception(name);}
    IEnumerator Tap(string name)
    {
        yield return new WaitForSecondsRealtime(.3f);Canvas.ForceUpdateCanvases();var button=Find(name);if(!button)throw new Exception("Missing button "+name+" on "+App.Route);
        var rect=(RectTransform)button.transform;var canvas=button.GetComponentInParent<Canvas>();Vector2 point=RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,rect.TransformPoint(rect.rect.center));
        var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},hits);
        Check("Reachable "+name,hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==button);
        bool clicked=false;button.onClick.AddListener(()=>clicked=true);var trace=button.gameObject.AddComponent<WorkbenchPointerTrace>();
        int id=++serial;InputSystem.QueueStateEvent(touch,new TouchState{touchId=id,phase=UnityEngine.InputSystem.TouchPhase.Began,position=point});yield return null;yield return new WaitForSecondsRealtime(.12f);
        InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(8+serial%4,8)});yield return null;
        InputSystem.QueueStateEvent(touch,new TouchState{touchId=id,phase=UnityEngine.InputSystem.TouchPhase.Ended,position=point});yield return null;yield return new WaitForSecondsRealtime(.35f);
        if(!clicked)rows.Add("Pointer trace "+name+": "+string.Join("; ",trace.events)+"; active module="+EventSystem.current.currentInputModule+" touch="+touch.primaryTouch.phase.ReadValue());
        if(trace)Destroy(trace);Check("Touch click delivered "+name,clicked);
    }
    IEnumerator Wait(string route){float until=Time.realtimeSinceStartup+45;while((!App||App.Route!=route)&&Time.realtimeSinceStartup<until)yield return null;Check("Reached "+route,App&&App.Route==route);yield return new WaitForSecondsRealtime(.4f);}
    IEnumerator Capture(string name){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot("Evidence/WorkbenchUpgrade/"+name+".png");yield return null;}
    Vector2 ModelPoint(EquipmentHandle item)
    {
        foreach(var collider in item.GetComponentsInChildren<Collider>().Where(c=>c.enabled))foreach(var point in new[]{collider.bounds.center,collider.bounds.ClosestPoint(App.View.transform.position),collider.bounds.center+Vector3.up*collider.bounds.extents.y*.7f})
        {
            Vector3 screen=App.View.WorldToScreenPoint(point);if(screen.z<=0||screen.x<0||screen.x>Screen.width||screen.y<0||screen.y>Screen.height)continue;
            if(Physics.Raycast(App.View.ScreenPointToRay(screen),out var hit,3.5f)&&hit.collider.GetComponentInParent<EquipmentHandle>()==item)return screen;
        }
        foreach(var renderer in item.GetComponentsInChildren<Renderer>())
        {
            var screen=App.View.WorldToScreenPoint(renderer.bounds.center);var ray=App.View.ScreenPointToRay(screen);
            rows.Add(item.assetId+" target "+renderer.name+" screen="+screen+" hits="+string.Join(", ",Physics.RaycastAll(ray,20).OrderBy(h=>h.distance).Select(h=>h.collider.name+":"+h.distance.ToString("F2"))));
        }
        throw new Exception("No visible collider for "+item.assetId);
    }
    IEnumerator Drag(Vector2 from,Vector2 to)
    {
        var cameraBefore=App.View.transform.rotation;int id=++serial;InputSystem.QueueStateEvent(touch,new TouchState{touchId=id,phase=UnityEngine.InputSystem.TouchPhase.Began,position=from});yield return null;yield return new WaitForSecondsRealtime(.15f);
        for(int n=1;n<=12;n++)
        {
            Vector2 point=Vector2.Lerp(from,to,n/12f);InputSystem.QueueStateEvent(touch,new TouchState{touchId=id,phase=UnityEngine.InputSystem.TouchPhase.Moved,position=point});yield return new WaitForSecondsRealtime(.05f);
            if(App.Session.spec.id=="P03"&&App.Apparatus.Driver.Controls("meter").Any(c=>c.id=="steady"&&c.enabled))
            {to=point-Vector2.right*45;InputSystem.QueueStateEvent(touch,new TouchState{touchId=id,phase=UnityEngine.InputSystem.TouchPhase.Moved,position=to});yield return new WaitForSecondsRealtime(.1f);break;}
        }
        Check(App.Session.spec.id+" model drag does not rotate camera",Quaternion.Angle(cameraBefore,App.View.transform.rotation)<.1f);
        InputSystem.QueueStateEvent(touch,new TouchState{touchId=id,phase=UnityEngine.InputSystem.TouchPhase.Ended,position=to});yield return null;yield return new WaitForSecondsRealtime(.25f);
    }
    IEnumerator Gesture(string id)
    {
        var rig=App.Apparatus;for(int n=0;n<50&&!rig.Ready;n++){rig.Select(rig.NextInstallableItem());Check(id+" fixture installs support before instrument",rig.InstallSelected());}
        yield return null;rig.RefreshConfiguration();
        if(id=="C06")Check(id+" fixture labels samples",rig.PrepareNext(out _));
        else{while(App.Session.Phase==ExperimentPhase.Preparing)Check(id+" fixture preparation",rig.PrepareNext(out _));Check(id+" fixture starts",App.Session.Start(out _));}
        rows.Add(id+" prepared fixture above uses direct API; measured gesture below uses actual queued touch.");
        App.ShowActivity();Call("FocusStation");yield return new WaitForSecondsRealtime(.5f);
        var item=rig.items.Single(i=>i.assetId==(id=="C06"?"probe":"meter"));var from=ModelPoint(item);
        string reading=id=="P03"?"currentPull":"lift";double before=id=="C06"?0:rig.Driver.Observe().Get(reading);
        Vector2 to=id=="C06"?ModelPoint(rig.items.Single(i=>i.assetId=="buffer4")):from+(id=="P03"?Vector2.right:Vector2.down)*90;
        yield return Drag(from,to);
        if(id=="C06")Check(id+" real probe read pH4 after drag to buffer",Mathf.Abs(item.GetComponentInChildren<VlabPhProbe>().Reading-4)<.01f);
        else Check(id+" drag changes actual scientific reading",rig.Driver.Observe().Get(reading)>before);
        Check(id+" release clears exclusive ownership",item.Owner==EquipmentGrabOwner.None);yield return Capture(id+"-direct-gesture");
        if(id=="C06")
        {
            foreach(string cup in new[]{"rinse","buffer7","rinse","water","rinse"})yield return Drag(ModelPoint(item),ModelPoint(rig.items.Single(i=>i.assetId==cup)));
            Check("C06 water control after actual probe sequence",System.Math.Abs(rig.Driver.Observe().Get("waterControlPh")-7)<.001);
            Check("C06 calibrated preparation",rig.PrepareNext(out _));yield return new WaitForSecondsRealtime(.4f);yield return Drag(ModelPoint(item),ModelPoint(rig.items.Single(i=>i.assetId=="test")));
            Call("SelectWorkbenchTool",rig.items.Single(i=>i.assetId=="paper"),"");yield return Tap("Instrument control paper");yield return Tap("Close equipment controls");
            Check("C06 prepared from actual probe and paper evidence",rig.PrepareNext(out _));Check("C06 start",App.Session.Start(out _));
        }
        else
        {
            for(int n=0;n<60;n++)
            {
                bool done=id=="P03"?rig.Driver.Controls("meter").Any(c=>c.id=="steady"&&c.enabled):rig.Driver.CanRecord(out _);if(done)break;
                from=ModelPoint(item);yield return Drag(from,from+(id=="P03"?Vector2.right:Vector2.down)*90);
            }
            if(id=="P03"){Call("SelectWorkbenchTool",item,"");yield return Tap("Instrument control steady");yield return Tap("Close equipment controls");yield return new WaitForSecondsRealtime(.6f);}
        }
        yield return new WaitForSecondsRealtime(.25f);Check(id+" actual observation permits record",rig.Driver.CanRecord(out _));yield return Tap("Compact current action");Check(id+" native record saves measurement",App.Profile.attempts.Any(a=>a.experimentId==id));
        Call("CloseModal");Call("Restart");yield return Wait("activity");Check(id+" restart preserves notebook",App.Profile.attempts.Any(a=>a.experimentId==id));
    }
    IEnumerator Start()
    {
        DontDestroyOnLoad(gameObject);Application.runInBackground=true;
        InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        touch=InputSystem.AddDevice<Touchscreen>();mouse=InputSystem.AddDevice<Mouse>();var stack=new Stack<IEnumerator>();stack.Push(Run());
        while(stack.Count>0){object value=null;bool next=false;try{next=stack.Peek().MoveNext();if(next)value=stack.Peek().Current;}catch(Exception e){rows.Add("FAIL "+e);failures++;break;}if(!next){stack.Pop();continue;}if(value is IEnumerator nested&&!(value is CustomYieldInstruction))stack.Push(nested);else yield return value;}
        yield return Capture("last-state");InputSystem.RemoveDevice(touch);InputSystem.RemoveDevice(mouse);File.WriteAllLines("Evidence/WorkbenchUpgrade/checks.txt",new[]{"Native Editor touchscreen events with simultaneous mouse movement and runtime checks. Not Android/headset hardware evidence."}.Concat(rows));SessionState.SetString("VLAB.TestProfile","");SecondTestGameView.Restore();EditorApplication.Exit(failures==0?0:1);
    }
    IEnumerator Run()
    {
        yield return Wait("home");yield return Capture("home-choice");yield return Tap("Skip quick tour");Check("Choice saved",App.Profile.quickStartSeen);
        var store=Read<ProfileStore>("store");Check("Choice persisted",new ProfileStore(store.Folder).Load().profile.quickStartSeen);
        Call("StartQuickStart");yield return null;yield return Tap("Quick tour exit");Check("Replay exits without changing route",App.Route=="home"&&!Read<bool>("quickStartActive"));yield return Capture("home");
        Call("StartQuickStart");yield return new WaitForSecondsRealtime(.3f);yield return Tap("Subject P");yield return Wait("browser");Check("Tour follows selected room",Read<int>("quickStartStep")==1);
        yield return Tap("Browser lesson P02");yield return Capture("tour-browser");Check("Tour spotlight cannot intercept touch",App.GetComponentInChildren<TourSpotlight>()&&!App.GetComponentInChildren<TourSpotlight>().raycastTarget);yield return Tap("Browser start");yield return Wait("readiness");yield return Tap("Readiness enter");yield return Wait("activity");Check("Tour follows lesson entry",Read<int>("quickStartStep")==2);
        yield return Tap("Station tools");yield return Tap("Dụng cụ");Check("Tour follows picker",Read<int>("quickStartStep")==3);
        yield return Tap("Equipment item "+App.Apparatus.items.First().assetId);Check("Tour follows selected instrument",Read<int>("quickStartStep")==4);yield return Capture("tour-action");
        yield return Tap("Equipment place selected");Check("Tour advances only after real operation",Read<int>("quickStartStep")==5);yield return Tap("Close equipment controls");yield return Tap("Quick tour next");yield return Tap("Quick tour next");Check("Tour completes without blocking lesson",!Read<bool>("quickStartActive")&&App.Route=="activity");
        App.ShowHome();yield return new WaitForSecondsRealtime(.4f);
        Check("All 32 guidance definitions valid",!LessonGuide.ValidateMetadata().Any());
        Check("Authored chemical ions and indices use supported rich text",UiFormula.RichText("Ag⁺ + Cu²⁺ + H₂O")=="Ag<sup>+</sup> + Cu<sup>2+</sup> + H<sub>2</sub>O");
        foreach(var id in LessonSpec.All.Keys)Check(id+" formula and approved notes",FormulaSheet.Image(id,0)&&LearningGuide.Load(id)!=null);
        string requested=Environment.GetEnvironmentVariable("VLAB_WORKBENCH_IDS");var ids=string.IsNullOrEmpty(requested)?new[]{"P01","C04","B01","M01"}:requested.Split(',');
        foreach(string id in ids)
        {
            App.ShowHome();yield return new WaitForSecondsRealtime(.4f);yield return Tap("Subject "+id[0]);yield return Wait("browser");yield return Tap("Browser lesson "+id);yield return Tap("Browser start");yield return Wait("readiness");yield return Tap("Readiness enter");yield return Wait("activity");yield return Capture(id+"-bench");
            var task=(GuideTask)Call("CurrentGuide");Check(id+" hint targets current kit",task!=null&&(string.IsNullOrEmpty(task.key)||App.Apparatus.items.Any(i=>i.assetId==task.key)));
            if(id=="C06"||id=="P03"||id=="M03")yield return Gesture(id);
            yield return Tap("Station tools");var equipment=App.GetComponentsInChildren<Button>().First(b=>b.GetComponentInChildren<TextMeshProUGUI>().text=="Dụng cụ");yield return Tap(equipment.name);
            Check(id+" picker compact",Read<RectTransform>("modal").GetComponentInChildren<WorkbenchPanelLayout>()!=null);
            yield return Capture(id+"-picker");
            var previews=App.GetComponentsInChildren<RawImage>().Where(i=>i.texture&&i.texture.name.StartsWith("Preview ")).ToArray();Check(id+" real-model previews",previews.Length>0);
            var first=App.Apparatus.items.First();yield return Tap("Equipment item "+first.assetId);Check(id+" selection closes blocking modal",!Read<RectTransform>("modal")&&Read<EquipmentHandle>("workbenchItem")==first);
            if(!first.installed)
            {
                Check(id+" picker release keeps loose instrument in hand",App.Apparatus.HasLatchedCarry&&first.Owner==EquipmentGrabOwner.Legacy);
                bool installable=App.Apparatus.CanInstallSelected(out _);yield return Tap("Equipment place selected");Check(id+" placement respects support and ownership",installable?first.installed&&!App.Apparatus.IsCarrying:!first.installed&&App.Apparatus.HasLatchedCarry);
            }
            if(Find("Lift equipment"))
            {
                Vector3 rest=first.transform.position;Quaternion rotation=first.transform.rotation;yield return Tap("Lift equipment");Check(id+" actual model lifted",App.Apparatus.InspectingInstalled&&Vector3.Distance(rest,first.transform.position)>.05f);
                yield return Tap("Rotate equipment right");Check(id+" actual model rotated",Quaternion.Angle(rotation,first.transform.rotation)>1);
                yield return Tap("Return inspected equipment");Check(id+" original supported pose restored",!App.Apparatus.IsCarrying&&Vector3.Distance(rest,first.transform.position)<.001f&&Quaternion.Angle(rotation,first.transform.rotation)<.1f);
            }
            yield return Capture(id+"-controls");yield return Tap("Close equipment controls");yield return Tap("Station tools");yield return Tap("Sketch board");Check(id+" lesson formula visible",App.GetComponentsInChildren<RawImage>().Any(i=>i.name=="Lesson board formula"&&i.texture));yield return Capture(id+"-board");yield return Tap("Open sketch page");Check(id+" existing drawing preserved",App.GetComponentInChildren<VlabWhiteboard>());yield return Tap("Close dialog");
            App.Profile.settings.hints=false;Call("ApplySettings");Call("ShowEquipmentCue",first,true);Check(id+" hint off suppresses cue",!Read<GuideCue>("worldGuideCue"));App.Profile.settings.hints=true;Call("ApplySettings");
            if(id=="C04")
            {
                foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(2340,1080),new Vector2Int(2048,1536)})
                {
                    Check("Set real viewport "+size,SecondTestGameView.Configure(out _,size.x,size.y));yield return new WaitForSecondsRealtime(.5f);Call("ShowEquipment");yield return new WaitForSecondsRealtime(.4f);
                    var layout=Read<RectTransform>("modal").GetComponentInChildren<WorkbenchPanelLayout>();var corners=new Vector3[4];((RectTransform)layout.transform).GetWorldCorners(corners);Check("Picker inside real viewport "+size,corners.All(v=>v.x>=0&&v.y>=0&&v.x<=Screen.width&&v.y<=Screen.height));yield return Capture("picker-"+size.x+"x"+size.y);Call("CloseModal");
                }
                SecondTestGameView.Configure(out _);yield return new WaitForSecondsRealtime(.5f);App.Input.SetSimulator(true);yield return new WaitForSecondsRealtime(.6f);Call("ShowEquipment");yield return new WaitForSecondsRealtime(.5f);
                Check("World picker uses world canvas",Read<RectTransform>("modal").GetComponentInParent<Canvas>().renderMode==RenderMode.WorldSpace);yield return Capture("picker-world");Call("CloseModal");App.Input.SetSimulator(false);yield return new WaitForSecondsRealtime(.6f);Check("Phone restored after VR layout check",!App.Input.SimulatorMode&&!Read<RectTransform>("modal"));
            }
        }
        if(Environment.GetEnvironmentVariable("VLAB_WORKBENCH_SCIENCE")=="1")
        {
            App.ShowHome();yield return null;var oldScene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();var fixture=UnityEngine.SceneManagement.SceneManager.CreateScene("Isolated SDK fixture");UnityEngine.SceneManagement.SceneManager.SetActiveScene(fixture);yield return UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(oldScene);rows.Add("SDK tests run in an isolated fixture scene; not room or hardware validation.");
            yield return PhysicsMechanicsSmoke.Run(line=>Check("SDK "+line,line.Contains(": PASS")));
            ChemistryIntegrationChecks.Run();Check("Chemistry SDK integration",true);
            rows.Add(BiologyIntegrationChecks.Run());Check("Biology SDK integration",true);
        }
        {
            var coreView=new GameObject("Isolated identity controller fixture",typeof(Camera));coreView.GetComponent<Camera>().enabled=false;ControllerV6RegressionChecks.Rows.Clear();ControllerV6RegressionChecks.Failures=0;ControllerV6RegressionChecks.Core(coreView.GetComponent<Camera>());Destroy(coreView);rows.AddRange(ControllerV6RegressionChecks.Rows);Check("Controller v6 core regression",ControllerV6RegressionChecks.Failures==0);
            PhoneInputChecks.Run();GamepadIntegrationChecks.Run();Check("Phone and gamepad input regression",true);VlabAudioChecks.Run();Check("Contact audio finite and unclipped",true);var audioSource=App.transform.Find("Apparatus contact and liquid").GetComponent<AudioSource>();audioSource.Stop();float volume=AudioListener.volume;try{AudioListener.volume=0;VlabAudio.At(VlabSound.Liquid,Vector3.zero);Check("Master mute prevents apparatus sound",!audioSource.isPlaying);}finally{AudioListener.volume=volume;}Check("Audio matches actual actions",VlabAudio.SoundForAction("dose:nacl")==VlabSound.Liquid&&VlabAudio.SoundForAction("focus+")==VlabSound.Mechanism&&VlabAudio.SoundForAction("label")==null);
        }
    }
}
public sealed class WorkbenchPointerTrace:MonoBehaviour,IPointerDownHandler,IPointerUpHandler,IPointerExitHandler,IBeginDragHandler
{
    public readonly List<string> events=new List<string>();
    public void OnPointerDown(PointerEventData e)=>events.Add("down "+e.pointerId+" "+e.position+" target "+e.pointerCurrentRaycast.gameObject);
    public void OnPointerUp(PointerEventData e)=>events.Add("up "+e.pointerId+" "+e.position+" target "+e.pointerCurrentRaycast.gameObject+" eligible "+e.eligibleForClick);
    public void OnPointerExit(PointerEventData e)=>events.Add("exit "+e.pointerId+" "+e.position);
    public void OnBeginDrag(PointerEventData e)=>events.Add("drag "+e.pointerId+" "+e.position);
}
#endif
