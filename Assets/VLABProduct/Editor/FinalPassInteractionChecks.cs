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
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using Vlab.Product;
using TMPro;

[InitializeOnLoad]
public static class FinalPassInteractionChecks
{
    static FinalPassInteractionChecks(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("VLAB.FinalPassInteraction",false)){SessionState.SetBool("VLAB.FinalPassInteraction",false);new GameObject("Direct interaction checks",typeof(FinalPassInteractionProbe));}};}
    public static void Run()
    {
        Directory.CreateDirectory("Evidence/FinalPass");SecondTestGameView.Configure(out _);
        SessionState.SetString("VLAB.TestProfile",Path.Combine(Application.persistentDataPath,"QA","FinalPassInteraction-"+Guid.NewGuid().ToString("N")));
        SessionState.SetBool("VLAB.FinalPassInteraction",true);EditorSceneManager.OpenScene("Assets/VLABProduct/Scenes/VLAB_Boot.unity");EditorApplication.EnterPlaymode();
    }
}
public sealed class FinalPassInteractionProbe:MonoBehaviour
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    readonly List<string> rows=new List<string>();int failures;long sequence;Vector3 aim;bool select,headGaze;XRSimulatedHMD simulatedHead;
    VlabApp App=>VlabApp.Instance;
    object Call(string name,params object[] args)=>typeof(VlabApp).GetMethod(name,Flags).Invoke(App,args);
    void Check(string name,bool ok){rows.Add((ok?"PASS ":"FAIL ")+name);if(!ok)failures++;Debug.Log(rows[rows.Count-1]);}
    void Update()
    {
        if(!App)return;
        var direction=aim-App.View.transform.position;if(direction.sqrMagnitude<.001f)direction=Vector3.up;
        if(simulatedHead!=null&&simulatedHead.added)
        {
            var pose=headGaze?Quaternion.Inverse(App.View.transform.parent.rotation)*Quaternion.LookRotation(direction):Quaternion.identity;
            InputSystem.QueueStateEvent(simulatedHead,new XRSimulatedHMDState{centerEyeRotation=pose,deviceRotation=pose,leftEyeRotation=pose,rightEyeRotation=pose,isTracked=true,trackingState=3});
        }
        if(!App.Input.UseMockController)return;
        App.Input.Controller.Submit(new ControllerSignal{sequence=sequence++,receivedAt=Time.realtimeSinceStartupAsDouble,orientation=Quaternion.LookRotation(direction),select=select});
    }
    IEnumerator Press(string name)
    {yield return new WaitForSecondsRealtime(.4f);var b=App.GetComponentsInChildren<Button>().FirstOrDefault(x=>x.name==name&&x.IsInteractable());if(!b)throw new Exception("Missing UI action "+name+" route "+App.Route);b.onClick.Invoke();yield return new WaitForSecondsRealtime(.4f);}
    IEnumerator Away(){aim=App.View.transform.position+Vector3.up*5;yield return new WaitForSecondsRealtime(.2f);}
    IEnumerator Dwell(Vector3 point,float duration=1.05f){yield return Away();aim=point;yield return new WaitForSecondsRealtime(duration);}
    Vector3 VisiblePoint(EquipmentHandle item,Func<Collider,bool> accept=null)
    {
        Physics.SyncTransforms();
        foreach(var c in item.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&c.gameObject.activeInHierarchy&&(accept==null||accept(c))))
        {
            var b=c.bounds;
            foreach(var point in new[]{b.center,b.ClosestPoint(App.View.transform.position),b.center+Vector3.up*b.extents.y*.8f,b.center-Vector3.up*b.extents.y*.8f})
            {
                var ray=new Ray(App.View.transform.position,point-App.View.transform.position);
                if(Physics.Raycast(ray,out var hit,3.5f)&&hit.collider.GetComponentInParent<EquipmentHandle>()==item&&(accept==null||accept(hit.collider))&&!(bool)Call("PointerHitsUi",ray))return point;
            }
        }
        throw new Exception("No unobstructed pointer target for "+App.Session.spec.id+"/"+item.assetId);
    }
    EquipmentHandle Item(string key)=>App.Apparatus.items.Single(x=>x.assetId==key);
    float ScreenWidth(EquipmentHandle item)
    {
        if(!ApparatusRig.TryVisibleBounds(item,out var bounds))return 0;
        float min=float.MaxValue,max=float.MinValue;
        for(int i=0;i<8;i++){var p=App.View.WorldToScreenPoint(bounds.center+Vector3.Scale(bounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));min=Mathf.Min(min,p.x);max=Mathf.Max(max,p.x);}
        return max-min;
    }
    IEnumerator Enter(string id)
    {
        bool dwell=App.Profile.settings.gazeAutoSelect;App.Profile.settings.gazeAutoSelect=false;headGaze=false;App.Input.UseMockController=true;
        App.ShowHome();yield return Away();yield return Press("Subject "+id[0]);yield return Press("Browser lesson "+id);yield return Press("Browser start");yield return Press("Readiness enter");
        float until=Time.realtimeSinceStartup+50;while(App.Route!="activity"&&Time.realtimeSinceStartup<until)yield return null;
        if(App.Session?.spec.id!=id||App.Route!="activity")throw new Exception("Entry failed "+id+" route="+App.Route);
        yield return Away();yield return new WaitForSecondsRealtime(.5f);headGaze=true;App.Input.UseMockController=false;App.Profile.settings.gazeAutoSelect=dwell;
    }
    IEnumerator Capture(string name){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot("Evidence/FinalPass/"+name+".png");}
    void Touch(Touchscreen screen,UnityEngine.InputSystem.TouchPhase phase,Vector2 position)=>InputSystem.QueueStateEvent(screen,new TouchState{touchId=731,phase=phase,position=position});
    IEnumerator TapModel(Touchscreen screen,EquipmentHandle item)
    {
        Vector2 point=App.View.WorldToScreenPoint(VisiblePoint(item));Touch(screen,UnityEngine.InputSystem.TouchPhase.Began,point);yield return new WaitForSecondsRealtime(.12f);Touch(screen,UnityEngine.InputSystem.TouchPhase.Ended,point);yield return new WaitForSecondsRealtime(.3f);
    }
    IEnumerator Start()
    {
        DontDestroyOnLoad(gameObject);var stack=new Stack<IEnumerator>();stack.Push(Run());
        while(stack.Count>0){object value=null;bool next=false;Exception error=null;try{next=stack.Peek().MoveNext();if(next)value=stack.Peek().Current;}catch(Exception e){error=e;}if(error!=null){rows.Add("BLOCKER "+error);failures++;break;}if(!next){stack.Pop();continue;}if(value is IEnumerator nested)stack.Push(nested);else yield return value;}
        yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot("Evidence/FinalPass/interaction-last-frame.png");yield return null;
        File.WriteAllLines("Evidence/FinalPass/interaction-checks.txt",new[]{"Unity Editor: application UI callbacks, synthetic XR-HMD poses through InputSystem/TrackedPoseDriver for world gaze, and mock-controller button regression. No sensor, Android or BLE hardware claim."}.Concat(rows));
        SecondTestGameView.Restore();EditorApplication.Exit(failures==0?0:1);
    }
    IEnumerator Run()
    {
        Check("Original public one-argument apparatus selection API is preserved",typeof(ApparatusRig).GetMethod("Select",new[]{typeof(EquipmentHandle)})!=null);
        float until=Time.realtimeSinceStartup+50;while((!App||App.Route!="home")&&Time.realtimeSinceStartup<until)yield return null;if(!App||App.Route!="home")throw new Exception("Home timeout");
        App.Profile.settings.gazeAutoSelect=false;App.Profile.settings.gazeDwellSeconds=.6f;App.Input.SetSimulator(true);App.Input.UseMockController=true;yield return Away();
        var simulator=UnityEngine.Object.FindFirstObjectByType<XRDeviceSimulator>();if(!simulator)throw new Exception("Official simulator not initialized");simulator.enabled=false;simulatedHead=InputSystem.devices.OfType<XRSimulatedHMD>().Single();yield return null;
        yield return Enter("P01");App.Profile.settings.gazeAutoSelect=true;Check("P01 starts with real ready kit and no completed preparation",App.Apparatus.Ready&&App.Session.Operation==0);
        var shade=UnityEngine.Object.FindAnyObjectByType<LabShade>();var shadeControl=shade.GetComponentsInChildren<LabFixture>().Single(f=>f.action==LabFixture.Action.Shade);
        yield return Press("HUD visibility");yield return Dwell(shadeControl.transform.position);
        Check("Actual gaze ray operates the room shade fixture",shade.lowered);
        yield return new WaitForSecondsRealtime(1);Check("Continued gaze does not repeatedly toggle shade",shade.lowered);
        yield return Dwell(shadeControl.transform.position);Check("Intentional second gaze reopens shade",!shade.lowered);
        yield return Press("HUD visibility");
        var trackedOrigin=App.View.transform.parent;var originPosition=trackedOrigin.position;var originRotation=trackedOrigin.rotation;
        aim=App.Apparatus.Pivot+Vector3.right*.5f;yield return new WaitForSecondsRealtime(12.8f);
        var cue=(GuideCue)typeof(VlabApp).GetField("worldGuideCue",Flags).GetValue(App);var caption=cue?cue.GetComponentInChildren<TextMeshProUGUI>(true):null;
        Check("Initial idle hint is a minimal local instrument label",caption&&caption.text==App.Apparatus.EquipmentLabel(Item("stand"),true)&&App.Session.Operation==0);
        App.Profile.settings.gazeAutoSelect=false;App.Input.UseMockController=true;aim=VisiblePoint(Item("timer"));
        for(int attempt=0;attempt<3;attempt++){select=true;yield return new WaitForSecondsRealtime(.15f);select=false;yield return new WaitForSecondsRealtime(.2f);}
        cue=(GuideCue)typeof(VlabApp).GetField("worldGuideCue",Flags).GetValue(App);caption=cue?cue.GetComponentInChildren<TextMeshProUGUI>(true):null;
        Check("Three incorrect attempts explain correct support without completing it",caption&&caption.text.StartsWith(App.Apparatus.EquipmentLabel(Item("stand"),true)+"\n",StringComparison.Ordinal)&&App.Session.Operation==0);
        if(caption){caption.ForceMeshUpdate(true,true);Check("Expanded local instruction is not truncated",!caption.isTextTruncated);}
        App.Input.UseMockController=false;App.Profile.settings.gazeAutoSelect=true;
        yield return Dwell(VisiblePoint(Item("stand")));Check("Gaze on actual support advances its check",App.Session.Operation==1);
        var poseDriver=App.View.GetComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>();var rotationAction=poseDriver.rotationInput.action;
        rows.Add("POSE device="+simulatedHead.centerEyeRotation.ReadValue()+" camera="+App.View.transform.localRotation+" driver="+poseDriver.enabled+" action="+rotationAction?.enabled+" controls="+rotationAction?.controls.Count+" value="+rotationAction?.ReadValue<Quaternion>()+" devices="+string.Join(",",InputSystem.devices.Select(x=>x.name)));
        Check("Gaze follows simulated XR pose through tracked pose driver",Vector3.Angle(App.View.transform.forward,aim-App.View.transform.position)<.5f);
        if(Vector3.Angle(App.View.transform.forward,aim-App.View.transform.position)>=.5f)throw new Exception("Synthetic XR pose did not reach camera; do not interpret gaze journey as a device failure.");
        yield return new WaitForSecondsRealtime(.9f);Check("Stationary gaze does not repeatedly execute support",App.Session.Operation==1);
        yield return Dwell(VisiblePoint(Item("pendulum")));Check("Gaze selects actual suspended bob",App.Session.Operation==2);
        yield return Dwell(VisiblePoint(Item("pendulum")));Check("Equilibrium check remains a separate intentional gaze",App.Session.Operation==3);
        yield return Dwell(VisiblePoint(Item("pendulum")));Check("Impulse selection latches bob without starting animation",App.Apparatus.IsPullingBob&&App.Session.Phase==ExperimentPhase.Ready&&App.Session.Pendulum==null);
        var before=Item("pendulum").transform.position;float length=(float)App.Session.parameters["length"];
        aim=App.Apparatus.Pivot+new Vector3(Mathf.Sin(22*Mathf.Deg2Rad)*length,-Mathf.Cos(22*Mathf.Deg2Rad)*length,0);yield return new WaitForSecondsRealtime(.4f);
        var pulled=Item("pendulum").transform.position;Check("Pointer physically moves original bob along fixed length",Vector3.Distance(before,pulled)>.05f&&Math.Abs(Vector3.Distance(pulled,App.Apparatus.Pivot)-length)<.002f);
        Check("Pulling updates release angle from pointer",Math.Abs(App.Session.parameters["angle"]-22)<.25);
        cue=(GuideCue)typeof(VlabApp).GetField("worldGuideCue",Flags).GetValue(App);caption=cue?cue.GetComponentInChildren<TextMeshProUGUI>(true):null;if(caption)caption.ForceMeshUpdate(true,true);Check("Manual-pull instruction remains fully visible",caption&&!caption.isTextTruncated&&caption.text.Contains("Chọn Thả"));
        yield return Capture("P01-manual-pull");
        var release=App.GetComponentsInChildren<Button>().Single(b=>b.name=="Thả dụng cụ");var rect=(RectTransform)release.transform;
        aim=rect.TransformPoint(rect.rect.center);yield return new WaitForSecondsRealtime(.3f);
        Check("Moving gaze onto release UI preserves bob position",Vector3.Distance(pulled,Item("pendulum").transform.position)<.002f&&App.Session.Phase==ExperimentPhase.Ready);
        yield return new WaitForSecondsRealtime(.8f);Check("Gaze release starts pendulum from learner position",!App.Apparatus.IsPullingBob&&App.Session.Phase==ExperimentPhase.Running&&App.Session.Pendulum!=null);
        yield return new WaitForSecondsRealtime(.8f);Check("Original model oscillates after explicit release",Vector3.Distance(pulled,Item("pendulum").transform.position)>.025f);
        Check("Experiment progress never moves or turns the VR tracking origin",Vector3.Distance(originPosition,trackedOrigin.position)<.0001f&&Quaternion.Angle(originRotation,trackedOrigin.rotation)<.001f);
        yield return Capture("P01-explicit-release");
        yield return Enter("P02");var loose=App.Apparatus.NextInstallableItem();
        if(!loose||!loose.gameObject.activeInHierarchy)throw new Exception("P02 first instrument is unavailable");
        yield return Dwell(VisiblePoint(loose));Check("Gaze picks actual loose instrument and retains carry",App.Apparatus.IsCarrying&&App.Apparatus.Selected==loose);
        Call("ShowStationTools");yield return new WaitForSecondsRealtime(.3f);Check("Opening tools cancels carry and restores supported tray pose",!App.Apparatus.IsCarrying&&Vector3.Distance(loose.transform.position,loose.trayPosition)<.002f);Call("CloseModal");
        yield return Enter("C04");yield return Dwell(VisiblePoint(Item("reagent")));yield return Dwell(VisiblePoint(Item("dropper")));
        Check("Chemistry gaze starts actual dropper manipulation",typeof(VlabApp).GetField("flagshipDrag",Flags).GetValue(App) as EquipmentHandle==Item("dropper"));
        Call("ShowStationTools");yield return new WaitForSecondsRealtime(.2f);Check("Opening chemistry UI cancels pending transfer",typeof(VlabApp).GetField("flagshipDrag",Flags).GetValue(App)==null);Call("CloseModal");
        yield return Enter("B01");yield return Dwell(VisiblePoint(Item("onion")));yield return Dwell(VisiblePoint(Item("coverslip")));yield return Dwell(VisiblePoint(Item("slide")));
        Check("Biology preparation follows three distinct world selections",App.Session.Operation==3);yield return Capture("B01-gaze-preparation");
        yield return Enter("M01");yield return Dwell(VisiblePoint(Item("motor")));Check("Engineering motor responds to world gaze",App.Session.Phase==ExperimentPhase.Running);yield return Capture("M01-gaze-motor");
        yield return Away();App.Profile.settings.gazeAutoSelect=false;
        yield return Enter("P01");aim=VisiblePoint(Item("stand"));yield return new WaitForSecondsRealtime(1.2f);Check("Disabling auto-select prevents world dwell",App.Session.Operation==0);
        headGaze=false;App.Input.UseMockController=true;
        select=true;yield return new WaitForSecondsRealtime(.12f);select=false;yield return new WaitForSecondsRealtime(.3f);Check("Physical select adapter still works with dwell disabled",App.Session.Operation==1);
        App.Input.UseMockController=false;App.Input.SetSimulator(false);yield return new WaitForSecondsRealtime(.5f);Call("FocusStation");yield return new WaitForSecondsRealtime(.3f);
        float overviewBob=ScreenWidth(Item("pendulum"));
        var touch=InputSystem.AddDevice<Touchscreen>();yield return null;
        yield return TapModel(touch,Item("pendulum"));yield return TapModel(touch,Item("pendulum"));Check("Phone touches advance distinct model preparation actions",App.Session.Operation==3);
        float focusedBob=ScreenWidth(Item("pendulum"));rows.Add("VISIBILITY bob projected width overview="+overviewBob+" focused="+focusedBob+" at "+Screen.width+"x"+Screen.height);
        Check("Precision-step phone framing enlarges visible bob without rescaling model",focusedBob>=overviewBob*1.5f&&focusedBob>=Screen.height*.04f);
        Vector2 start=App.View.WorldToScreenPoint(VisiblePoint(Item("pendulum")));Touch(touch,UnityEngine.InputSystem.TouchPhase.Began,start);yield return new WaitForSecondsRealtime(.15f);
        Check("Phone touch captures actual bob instead of look swipe",App.Apparatus.IsPullingBob&&App.Session.Phase==ExperimentPhase.Ready);
        var phoneEye=App.View.transform.position;var phoneRotation=App.View.transform.rotation;float phoneLens=App.View.fieldOfView;
        var phoneBefore=Item("pendulum").transform.position;float phoneLength=(float)App.Session.parameters["length"];
        Vector2 end=App.View.WorldToScreenPoint(App.Apparatus.Pivot+new Vector3(Mathf.Sin(18*Mathf.Deg2Rad)*phoneLength,-Mathf.Cos(18*Mathf.Deg2Rad)*phoneLength,0));
        for(int step=1;step<=5;step++){Touch(touch,UnityEngine.InputSystem.TouchPhase.Moved,Vector2.Lerp(start,end,step/5f));yield return new WaitForSecondsRealtime(.06f);}
        Check("Phone drag visibly moves the same bob",App.Apparatus.IsPullingBob&&Vector3.Distance(phoneBefore,Item("pendulum").transform.position)>.05f);
        Check("Phone camera stays fixed throughout the captured drag",Vector3.Distance(phoneEye,App.View.transform.position)<.0001f&&Quaternion.Angle(phoneRotation,App.View.transform.rotation)<.001f&&Mathf.Abs(phoneLens-App.View.fieldOfView)<.001f);
        Touch(touch,UnityEngine.InputSystem.TouchPhase.Ended,end);yield return new WaitForSecondsRealtime(.3f);Check("Phone finger release starts real pendulum once",!App.Apparatus.IsPullingBob&&App.Session.Phase==ExperimentPhase.Running&&App.Session.Pendulum!=null);
        yield return Capture("P01-phone-drag-release");InputSystem.RemoveDevice(touch);
        yield return PhoneInputChecks.RunUiTap(App,name=>ScreenCapture.CaptureScreenshot("Evidence/FinalPass/"+name+".png"),Check);
    }
}
#endif
