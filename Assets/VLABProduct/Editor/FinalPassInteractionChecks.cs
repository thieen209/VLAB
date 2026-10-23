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
    public static void RunRecorded(){SessionState.SetBool("VLAB.RecordGameplay",true);Run();}
    public static void BiologyFocus(){SessionState.SetBool("VLAB.BiologyFocusOnly",true);Run();}
    public static void PendulumJourney(){SessionState.SetBool("VLAB.PendulumOnly",true);Run();}
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
    bool recordGameplay;UnityEditor.Media.MediaEncoder video;Coroutine videoRoutine;double videoStart;int videoFrames;string videoName,videoError;
    void BeginVideo(string id)
    {
        if(!recordGameplay)return;
        string folder=Path.Combine("Evidence/FinalPass/Gameplay",DateTime.Now.ToString("yyyyMMdd-HHmmss"));Directory.CreateDirectory(folder);
        videoName=Path.Combine(folder,id+"-editor-simulated-input-excerpt.mp4");videoFrames=0;videoError=null;videoStart=Time.realtimeSinceStartupAsDouble;
        video=new UnityEditor.Media.MediaEncoder(videoName,new UnityEditor.Media.VideoTrackAttributes{width=(uint)Screen.width,height=(uint)Screen.height,frameRate=new UnityEditor.Media.MediaRational(20),includeAlpha=false});
        videoRoutine=StartCoroutine(RecordVideo());
    }
    IEnumerator RecordVideo()
    {
        var endFrame=new WaitForEndOfFrame();var interval=new WaitForSecondsRealtime(.05f);
        while(video!=null)
        {
            yield return endFrame;Texture2D frame=null;
            try
            {
                frame=ScreenCapture.CaptureScreenshotAsTexture();
                var time=new UnityEditor.Media.MediaTime((long)((Time.realtimeSinceStartupAsDouble-videoStart)*1000000),1000000);
                if(!video.AddFrame(frame,time))throw new Exception("Encoder rejected gameplay frame");videoFrames++;if(videoFrames%60==1)File.WriteAllBytes(videoName+"-frame-"+videoFrames+".png",frame.EncodeToPNG());
            }
            catch(Exception e){videoError=e.Message;}
            finally{if(frame)Destroy(frame);}
            if(videoError!=null)yield break;
            yield return interval;
        }
    }
    void EndVideo()
    {
        if(video==null)return;if(videoRoutine!=null)StopCoroutine(videoRoutine);
        try{video.Dispose();}catch(Exception e){videoError=e.Message;}finally{video=null;videoRoutine=null;}
        File.WriteAllText(videoName+".txt","Editor Game View, synthetic input through existing interaction checks. Excerpt, not complete experiment acceptance. Real timestamps; recording overhead is not device performance. Frames="+videoFrames+"; error="+videoError);
        Check("Gameplay excerpt encoded "+Path.GetFileName(videoName),videoError==null&&videoFrames>1&&new FileInfo(videoName).Length>1024);
    }
    IEnumerator Capture(string name){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot("Evidence/FinalPass/"+name+".png");}
    void Touch(Touchscreen screen,UnityEngine.InputSystem.TouchPhase phase,Vector2 position)=>InputSystem.QueueStateEvent(screen,new TouchState{touchId=731,phase=phase,position=position});
    IEnumerator TapModel(Touchscreen screen,EquipmentHandle item,Func<Collider,bool> accept=null)
    {
        Vector2 point=App.View.WorldToScreenPoint(VisiblePoint(item,accept));Touch(screen,UnityEngine.InputSystem.TouchPhase.Began,point);yield return new WaitForSecondsRealtime(.12f);Touch(screen,UnityEngine.InputSystem.TouchPhase.Ended,point);yield return new WaitForSecondsRealtime(.3f);
    }
    IEnumerator Start()
    {
        var oldBackground=InputSystem.settings.backgroundBehavior;var oldEditorInput=InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        recordGameplay=SessionState.GetBool("VLAB.RecordGameplay",false);SessionState.SetBool("VLAB.RecordGameplay",false);Application.runInBackground=true;DontDestroyOnLoad(gameObject);var stack=new Stack<IEnumerator>();stack.Push(Run());
        while(stack.Count>0){object value=null;bool next=false;Exception error=null;try{next=stack.Peek().MoveNext();if(next)value=stack.Peek().Current;}catch(Exception e){error=e;}if(error!=null){rows.Add("BLOCKER "+error);failures++;break;}if(!next){stack.Pop();continue;}if(value is IEnumerator nested)stack.Push(nested);else yield return value;}
        yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot("Evidence/FinalPass/interaction-last-frame.png");yield return null;
        EndVideo();File.WriteAllLines("Evidence/FinalPass/"+(pendulumOnly?"pendulum-journey-checks":biologyFocusOnly?"biology-focus-checks":"interaction-checks")+".txt",new[]{"Unity Editor: application UI callbacks, synthetic XR-HMD poses through InputSystem/TrackedPoseDriver for world gaze, and mock-controller button regression. No sensor, Android or BLE hardware claim."}.Concat(rows));
        InputSystem.settings.backgroundBehavior=oldBackground;InputSystem.settings.editorInputBehaviorInPlayMode=oldEditorInput;SecondTestGameView.Restore();EditorApplication.Exit(failures==0?0:1);
    }
    IEnumerator Run()
    {
        biologyFocusOnly=SessionState.GetBool("VLAB.BiologyFocusOnly",false);SessionState.SetBool("VLAB.BiologyFocusOnly",false);
        pendulumOnly=SessionState.GetBool("VLAB.PendulumOnly",false);SessionState.SetBool("VLAB.PendulumOnly",false);
        if(pendulumOnly){yield return PendulumJourney();yield break;}
        if(biologyFocusOnly){yield return BiologyFocusJourney();yield break;}
        var hidden=GameObject.CreatePrimitive(PrimitiveType.Cube);var handle=hidden.AddComponent<EquipmentHandle>();handle.assetId="qa-hidden";
        Check("Visible-bounds query includes rendered apparatus",ApparatusRig.TryVisibleBounds(handle,out _));hidden.GetComponent<Renderer>().forceRenderingOff=true;
        Check("Visible-bounds query excludes force-hidden apparatus",!ApparatusRig.TryVisibleBounds(handle,out _));Destroy(hidden);
        Check("Original public one-argument apparatus selection API is preserved",typeof(ApparatusRig).GetMethod("Select",new[]{typeof(EquipmentHandle)})!=null);
        float until=Time.realtimeSinceStartup+50;while((!App||App.Route!="home")&&Time.realtimeSinceStartup<until)yield return null;if(!App||App.Route!="home")throw new Exception("Home timeout");
        App.Profile.settings.gazeAutoSelect=false;App.Profile.settings.gazeDwellSeconds=.6f;App.Input.SetSimulator(true);App.Input.UseMockController=true;yield return Away();
        var simulator=UnityEngine.Object.FindFirstObjectByType<XRDeviceSimulator>();if(!simulator)throw new Exception("Official simulator not initialized");simulator.enabled=false;simulatedHead=InputSystem.devices.OfType<XRSimulatedHMD>().Single();yield return null;
        yield return Enter("P01");BeginVideo("P01");App.Profile.settings.gazeAutoSelect=true;Check("P01 starts with real ready kit and no completed preparation",App.Apparatus.Ready&&App.Session.Operation==0);
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
                aim=App.Apparatus.Pivot+new Vector3(Mathf.Sin(22*Mathf.Deg2Rad)*length,-Mathf.Cos(22*Mathf.Deg2Rad)*length,0);
        float pullDeadline=Time.realtimeSinceStartup+2;
        do{yield return null;}while(Math.Abs(App.Session.parameters["angle"]-22)>=.25&&Time.realtimeSinceStartup<pullDeadline);
        rows.Add("PULL observed angle="+App.Session.parameters["angle"]+" head error="+Vector3.Angle(App.View.transform.forward,aim-App.View.transform.position)+" held="+App.Apparatus.IsPullingBob);
        var pulled=Item("pendulum").transform.position;Check("Pointer physically moves original bob along fixed length",Vector3.Distance(before,pulled)>.05f&&Math.Abs(Vector3.Distance(pulled,App.Apparatus.Pivot)-length)<.002f);
        Check("Pulling updates release angle from pointer",Math.Abs(App.Session.parameters["angle"]-22)<.25);
        cue=(GuideCue)typeof(VlabApp).GetField("worldGuideCue",Flags).GetValue(App);caption=cue?cue.GetComponentInChildren<TextMeshProUGUI>(true):null;if(caption)caption.ForceMeshUpdate(true,true);Check("Manual-pull instruction remains fully visible",caption&&!caption.isTextTruncated&&caption.text.Contains("Chọn Thả"));
        yield return Capture("P01-manual-pull");
        var release=App.GetComponentsInChildren<Button>().Single(b=>b.name=="Thả dụng cụ");var rect=(RectTransform)release.transform;
        aim=rect.TransformPoint(rect.rect.center);yield return new WaitForSecondsRealtime(.3f);
        Check("Moving gaze onto release UI preserves bob position",Vector3.Distance(pulled,Item("pendulum").transform.position)<.002f&&App.Session.Phase==ExperimentPhase.Ready);
        yield return new WaitForSecondsRealtime(.8f);Check("Gaze release starts pendulum from learner position",!App.Apparatus.IsPullingBob&&App.Session.Phase==ExperimentPhase.Running&&App.Session.Pendulum!=null);
                float oscillationDeadline=Time.realtimeSinceStartup+1.6f,maxDisplacement=0;
        do{yield return null;maxDisplacement=Mathf.Max(maxDisplacement,Vector3.Distance(pulled,Item("pendulum").transform.position));}while(Time.realtimeSinceStartup<oscillationDeadline);
        Check("Original model oscillates after explicit release",maxDisplacement>.025f);
        Check("Experiment progress never moves or turns the VR tracking origin",Vector3.Distance(originPosition,trackedOrigin.position)<.0001f&&Quaternion.Angle(originRotation,trackedOrigin.rotation)<.001f);
        yield return Capture("P01-explicit-release");EndVideo();
        yield return Enter("P02");var loose=App.Apparatus.NextInstallableItem();
        if(!loose||!loose.gameObject.activeInHierarchy)throw new Exception("P02 first instrument is unavailable");
        yield return Dwell(VisiblePoint(loose));Check("Gaze picks actual loose instrument and retains carry",App.Apparatus.IsCarrying&&App.Apparatus.Selected==loose);
        Call("ShowStationTools");yield return new WaitForSecondsRealtime(.3f);Check("Opening tools cancels carry and restores supported tray pose",!App.Apparatus.IsCarrying&&Vector3.Distance(loose.transform.position,loose.trayPosition)<.002f);Call("CloseModal");
        yield return Enter("C04");BeginVideo("C04");yield return Dwell(VisiblePoint(Item("reagent")));yield return Dwell(VisiblePoint(Item("dropper")));
        Check("Chemistry gaze starts actual dropper manipulation",typeof(VlabApp).GetField("flagshipDrag",Flags).GetValue(App) as EquipmentHandle==Item("dropper"));
        Call("ShowStationTools");yield return new WaitForSecondsRealtime(.2f);Check("Opening chemistry UI cancels pending transfer",typeof(VlabApp).GetField("flagshipDrag",Flags).GetValue(App)==null);Call("CloseModal");
        App.Input.SetSimulator(false);yield return new WaitForSecondsRealtime(.5f);Call("FocusStation");yield return new WaitForSecondsRealtime(.2f);
        var cup=Item("nacl");var cupScale=cup.transform.lossyScale;float cupOverview=ScreenWidth(cup);Call("FocusInstrument",cup,false);yield return new WaitForSecondsRealtime(.2f);
        Check("Inspecting one beaker enlarges it without rescaling",ScreenWidth(cup)>cupOverview*1.5f&&Vector3.Distance(cupScale,cup.transform.lossyScale)<.0001f);var identity=App.Apparatus.GetComponent<FlagshipStageLabels>().GetComponentsInChildren<TextMeshPro>().Single(x=>x.text=="NaCl");
        var glyph=identity.textBounds;var labelTop=App.View.WorldToViewportPoint(identity.transform.TransformPoint(glyph.center+Vector3.up*glyph.extents.y));var labelBottom=App.View.WorldToViewportPoint(identity.transform.TransformPoint(glyph.center-Vector3.up*glyph.extents.y));
        Check("Close-up sample identity stays readable and bounded",labelTop.y-labelBottom.y>=.015f&&labelTop.y-labelBottom.y<=.035f&&labelBottom.y>=.01f&&labelTop.y<=.99f);
        yield return Capture("C04-cup-inspection");
        App.Input.SetSimulator(true);yield return new WaitForSecondsRealtime(.5f);var vrPosition=App.View.transform.parent.position;var vrRotation=App.View.transform.parent.rotation;Call("FocusInstrument",cup,false);
        Check("Instrument focus does not move VR tracking origin",Vector3.Distance(vrPosition,App.View.transform.parent.position)<.0001f&&Quaternion.Angle(vrRotation,App.View.transform.parent.rotation)<.001f);
        EndVideo();yield return Enter("B01");BeginVideo("B01");yield return Dwell(VisiblePoint(Item("onion")));yield return Dwell(VisiblePoint(Item("coverslip")));yield return Dwell(VisiblePoint(Item("slide")));
        Check("Biology preparation follows three distinct world selections",App.Session.Operation==3);yield return Capture("B01-gaze-preparation");
        EndVideo();yield return Enter("M01");BeginVideo("M01");yield return Dwell(VisiblePoint(Item("motor")));Check("Engineering motor responds to world gaze",App.Session.Phase==ExperimentPhase.Running);yield return Capture("M01-gaze-motor");yield return new WaitForSecondsRealtime(3);EndVideo();
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
                yield return Capture("P01-phone-drag-release");
        yield return Enter("C04");headGaze=false;App.Input.UseMockController=false;App.Input.SetSimulator(false);yield return new WaitForSecondsRealtime(.5f);
        for(int trial=0;trial<2;trial++)
        {
            BeginVideo("C04-phone-trial-"+(trial+1));
            int savedBefore=App.Profile.attempts.Count;float comparisonBefore=ScreenWidth(Item("nacl"));
            Check("C04 cannot record before transfers "+trial,!App.Session.Authority.CanRecord(out _));
            foreach(string sample in new[]{"nitrate","water","nacl"})
            {
                yield return TapModel(touch,Item("reagent"));
                Vector2 source=App.View.WorldToScreenPoint(VisiblePoint(Item("dropper")));
                Touch(touch,UnityEngine.InputSystem.TouchPhase.Began,source);yield return new WaitForSecondsRealtime(.15f);
                var resting=Item("dropper").transform.position;
                Vector2 target=App.View.WorldToScreenPoint(VisiblePoint(Item(sample)));
                for(int step=1;step<=6;step++){Touch(touch,UnityEngine.InputSystem.TouchPhase.Moved,Vector2.Lerp(source,target,step/6f));yield return new WaitForSecondsRealtime(.08f);}
                Check("C04 phone drag moves dropper "+sample+" trial "+trial,Vector3.Distance(resting,Item("dropper").transform.position)>.01f);
                Touch(touch,UnityEngine.InputSystem.TouchPhase.Ended,target);yield return new WaitForSecondsRealtime(.6f);
                Check("C04 release clears held tool "+sample+" trial "+trial,typeof(VlabApp).GetField("flagshipDrag",Flags).GetValue(App)==null);
            }
            Check("C04 all samples allow recording "+trial,App.Session.Phase==ExperimentPhase.Running&&App.Session.Authority.CanRecord(out _));
            Check("C04 comparison focus enlarges sample after transfers "+trial,ScreenWidth(Item("nacl"))>comparisonBefore*1.2f);yield return Capture("C04-comparison-"+trial);
            yield return Press("Ghi số đo");
            Check("C04 UI saves one new trial "+trial,App.Route=="results"&&App.Profile.attempts.Count==savedBefore+1);
            var recorded=App.Profile.attempts.Last();
            Check("C04 saved chloride positive and controls negative "+trial,recorded.experimentId=="C04"&&recorded.readings.Single(r=>r.key=="NaClSolid").value==1&&recorded.readings.Single(r=>r.key=="NaNO3Solid").value==0&&recorded.readings.Single(r=>r.key=="waterSolid").value==0);
            yield return Capture("C04-phone-results-"+trial);yield return Press("Làm lại");
            float resetDeadline=Time.realtimeSinceStartup+45;while(App.Route!="activity"&&Time.realtimeSinceStartup<resetDeadline)yield return null;
            Check("C04 retry resets apparatus without deleting saved trial "+trial,App.Route=="activity"&&App.Session.Operation==0&&App.Profile.attempts.Count==savedBefore+1&&!App.Session.Authority.CanRecord(out _));
            EndVideo();
        }
        yield return Enter("M01");headGaze=false;App.Input.UseMockController=false;App.Input.SetSimulator(false);yield return new WaitForSecondsRealtime(.5f);
        for(int trial=0;trial<2;trial++)
        {
            BeginVideo("M01-phone-trial-"+(trial+1));int savedBefore=App.Profile.attempts.Count;int inputTeeth=trial==0?12:24;
            yield return TapModel(touch,Item("gears"),c=>c.name.Contains("Gear"+inputTeeth));
            yield return TapModel(touch,Item("gears"),c=>c.name.Contains("Gear36"));
            Check("M01 touching gears selects real tooth counts "+trial,App.Session.parameters["driver"]==inputTeeth&&App.Session.parameters["driven"]==36);
            var gear=Item("gears").GetComponentsInChildren<Transform>().First(t=>t.name.Contains("Gear12")&&t.name.EndsWith("_VIS"));var angle=gear.localRotation;
            yield return TapModel(touch,Item("motor"));yield return new WaitForSecondsRealtime(.35f);
            Check("M01 motor starts rotating original gear "+trial,App.Session.Phase==ExperimentPhase.Running&&Quaternion.Angle(angle,gear.localRotation)>.1f);
            yield return TapModel(touch,Item("motor"));angle=gear.localRotation;yield return new WaitForSecondsRealtime(.4f);
            Check("M01 pause stops gear geometry "+trial,App.Session.Phase==ExperimentPhase.Paused&&Quaternion.Angle(angle,gear.localRotation)<.01f);
            yield return TapModel(touch,Item("motor"));float ready=Time.realtimeSinceStartup+30;
            while(!App.Session.Authority.CanRecord(out _)&&Time.realtimeSinceStartup<ready)yield return null;
            Check("M01 completes an observed revolution "+trial,App.Session.Phase==ExperimentPhase.Running&&App.Session.Authority.CanRecord(out _));
            yield return Press("Ghi số đo");var result=App.Profile.attempts.Last();double expected=trial==0?1.0/3:-2.0/3;
            Check("M01 saved speed ratio includes correct direction "+trial,App.Route=="results"&&App.Profile.attempts.Count==savedBefore+1&&result.experimentId=="M01"&&Math.Abs(result.readings.Single(x=>x.key=="signedRatio").value-expected)<.0001);
            yield return Capture("M01-phone-results-"+trial);yield return Press("Làm lại");float reset=Time.realtimeSinceStartup+45;while(App.Route!="activity"&&Time.realtimeSinceStartup<reset)yield return null;
            Check("M01 retry keeps notebook and resets preparation "+trial,App.Route=="activity"&&App.Session.Operation==0&&App.Profile.attempts.Count==savedBefore+1);EndVideo();
        }
        InputSystem.RemoveDevice(touch);
        yield return PhoneInputChecks.RunUiTap(App,name=>ScreenCapture.CaptureScreenshot("Evidence/FinalPass/"+name+".png"),Check);
    }
    bool biologyFocusOnly,pendulumOnly;
    IEnumerator CheckCueVisible(string key)
    {
        Call("ShowEquipmentCue",Item(key),true);yield return new WaitForSecondsRealtime(.3f);
        var cue=App.Apparatus.GetComponentsInChildren<GuideCue>().First(c=>!c.IsStopped);
        var note=cue.GetComponentsInChildren<RectTransform>().First(r=>r.name=="Object instruction");
        var corners=new Vector3[4];note.GetWorldCorners(corners);bool clear=true;
        foreach(var point in corners.Concat(new[]{note.position}))
            clear&=!Physics.Linecast(App.View.transform.position,point,out _,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
        Check("P01 "+key+" instruction is not behind apparatus",clear);
        Check("P01 "+key+" cue changes no apparatus or lesson state",cue.AssertPresentationOnly(App.Session,out _));
        yield return Capture("P01-"+key+"-clear-cue");
    }
    IEnumerator PendulumJourney()
    {
        float until=Time.realtimeSinceStartup+50;while((!App||App.Route!="home")&&Time.realtimeSinceStartup<until)yield return null;
        if(!App||App.Route!="home")throw new Exception("Home timeout");
        App.Profile.settings.gazeAutoSelect=false;yield return Enter("P01");headGaze=false;App.Input.UseMockController=false;
        var touch=InputSystem.AddDevice<Touchscreen>();yield return null;
        for(int trial=0;trial<2;trial++)
        {
            yield return TapModel(touch,Item("stand"));if(trial==0)yield return CheckCueVisible("stand");
            yield return TapModel(touch,Item("pendulum"));yield return TapModel(touch,Item("pendulum"));
            if(trial==0)yield return CheckCueVisible("pendulum");
            Check("P01 model touches prepare each new trial "+trial,App.Session.Operation==3);
            Check("P01 compact timer placement preserves assembly and surface clearances "+trial,App.Apparatus.ValidateAssembly(out _));
            Vector2 start=App.View.WorldToScreenPoint(VisiblePoint(Item("pendulum")));
            Touch(touch,UnityEngine.InputSystem.TouchPhase.Began,start);yield return new WaitForSecondsRealtime(.15f);
            Touch(touch,UnityEngine.InputSystem.TouchPhase.Canceled,start);yield return new WaitForSecondsRealtime(.3f);
            Check("P01 canceled phone touch does not release or start measurement "+trial,!App.Apparatus.IsPullingBob&&App.Session.Phase==ExperimentPhase.Ready);
            start=App.View.WorldToScreenPoint(VisiblePoint(Item("pendulum")));
            Touch(touch,UnityEngine.InputSystem.TouchPhase.Began,start);yield return new WaitForSecondsRealtime(.15f);
            float length=(float)App.Session.parameters["length"];float angle=(trial==0?12:-18)*Mathf.Deg2Rad;
            Vector2 end=App.View.WorldToScreenPoint(App.Apparatus.Pivot+new Vector3(Mathf.Sin(angle)*length,-Mathf.Cos(angle)*length,0));
            for(int step=1;step<=5;step++){Touch(touch,UnityEngine.InputSystem.TouchPhase.Moved,Vector2.Lerp(start,end,step/5f));yield return new WaitForSecondsRealtime(.06f);}
            Touch(touch,UnityEngine.InputSystem.TouchPhase.Ended,end);yield return new WaitForSecondsRealtime(.3f);
            Check("P01 deliberate release starts motion "+trial,App.Session.Phase==ExperimentPhase.Running&&App.Session.Pendulum!=null);
            var clearArea=(Rect)Call("ActivityObservationArea");ApparatusRig.TryVisibleBounds(Item("timer"),out var timerBounds);
            bool clockVisible=true;
            for(int corner=0;corner<8;corner++)
            {
                var point=App.View.WorldToViewportPoint(timerBounds.center+Vector3.Scale(timerBounds.extents,new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1)));
                clockVisible&=point.z>0&&clearArea.Contains(new Vector2(point.x,point.y));
            }
            Check("P01 running timer remains outside HUD "+trial,clockVisible);
            Check("P01 running bob remains readable "+trial,ScreenWidth(Item("pendulum"))>=Screen.height*.035f);
            if(trial==0)yield return Capture("P01-running-timer-clear");
            var ready=Time.realtimeSinceStartup+60;int requested=(int)App.Session.parameters["cycles"];
            while(App.Session.Pendulum!=null&&App.Session.Pendulum.Cycles<requested&&Time.realtimeSinceStartup<ready)yield return null;
            Check("P01 completes the requested observed cycles "+trial,App.Session.Pendulum!=null&&App.Session.Pendulum.Cycles>=requested);
            int saved=App.Profile.attempts.Count;yield return Press("Ghi số đo");
            Check("P01 real cycle measurement saves once "+trial,App.Route=="results"&&App.Profile.attempts.Count==saved+1);
            yield return Press("Làm lại");ready=Time.realtimeSinceStartup+45;while(App.Route!="activity"&&Time.realtimeSinceStartup<ready)yield return null;
            Check("P01 retry preserves notebook and clears trial "+trial,App.Route=="activity"&&App.Session.Operation==0&&App.Profile.attempts.Count==saved+1);
        }
        InputSystem.RemoveDevice(touch);
    }
    IEnumerator BiologyFocusJourney()
    {
        float until=Time.realtimeSinceStartup+50;while((!App||App.Route!="home")&&Time.realtimeSinceStartup<until)yield return null;
        if(!App||App.Route!="home")throw new Exception("Home timeout");
        App.Profile.settings.gazeAutoSelect=false;yield return Enter("B01");headGaze=false;App.Input.UseMockController=false;
        var touch=InputSystem.AddDevice<Touchscreen>();yield return null;
        yield return TapModel(touch,Item("onion"));yield return TapModel(touch,Item("coverslip"));yield return TapModel(touch,Item("slide"));
        Check("B01 actual touches prepare and mount the slide",App.Session.Operation==3);
        Call("ShowInstrument",Item("microscope"));yield return new WaitForSecondsRealtime(.4f);
        yield return Press("Instrument tab tools");var biology=(BiologyDriver)App.Apparatus.Driver;
        for(int i=0;i<26&&Mathf.Abs(biology.MicroscopeFocusMm-6.5f)>.1f;i++)yield return Press("Instrument control "+(biology.MicroscopeFocusMm>6.5f?"focus-":"focus+"));
        for(int i=0;i<4;i++)yield return Press("Instrument control focus-");
        yield return Press("Instrument tab view");Check("B01 unfocused observation does not bypass preparation",App.Session.Operation==3&&App.Session.Phase==ExperimentPhase.Preparing);
        App.GetComponentInChildren<InstrumentInspection>().Selected(new Vector2(.5f,.5f));yield return null;
        Check("B01 unfocused image selection retains focus controls",App.GetComponentsInChildren<Button>().Any(b=>b.name=="Instrument control focus+"&&b.IsInteractable()));
        for(int i=0;i<4;i++)yield return Press("Instrument control focus+");
        yield return Press("Instrument tab view");Check("B01 focused view starts observation without closing microscope",App.Session.Phase==ExperimentPhase.Running);
        Canvas.ForceUpdateCanvases();Check("B01 observation tabs have no overlapping controls heading",!App.GetComponentsInChildren<TextMeshProUGUI>().Any(t=>t.text=="THAO TÁC"));
        Check("B01 cannot record an unclassified observation",!App.Session.Authority.CanRecord(out _));
        for(int structure=1;structure<=3;structure++)
        {
            Vector2 uv=Vector2.zero;bool found=false;
            for(int y=0;y<128&&!found;y++)for(int x=0;x<128&&!found;x++)
            {
                var candidate=new Vector2((x+.5f)/128,(y+.5f)/128);
                if(biology.TryB01SourceIdentity(candidate,out _,out int region)&&region==structure){uv=candidate;found=true;}
            }
            if(!found)throw new Exception("Missing source structure fixture "+structure);
            var inspection=App.GetComponentInChildren<InstrumentInspection>();var rect=(RectTransform)inspection.transform;
            var point=rect.TransformPoint(new Vector3(rect.rect.xMin+uv.x*rect.rect.width,rect.rect.yMin+uv.y*rect.rect.height));
            inspection.OnPointerClick(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,point)});
            yield return null;
            if(structure==1)yield return Press("Instrument control structure:1");
            yield return Press("Instrument control structure:"+(structure-1));
            Check("B01 classification edits do not duplicate structure "+structure,App.Session.Observe().readings.Single(r=>r.key=="identifiedStructures").value==structure);
        }
        yield return Capture("B01-classified-observation");
        yield return Press("Trở lại trạm");Check("B01 observation survives returning to station",App.Route=="activity"&&App.Session.Phase==ExperimentPhase.Running);
        int saved=App.Profile.attempts.Count;
        yield return Press("Ghi số đo");Check("B01 observed classifications save a result",App.Route=="results"&&App.Profile.attempts.Count==saved+1);
        yield return Press("Làm lại");float ready=Time.realtimeSinceStartup+45;while(App.Route!="activity"&&Time.realtimeSinceStartup<ready)yield return null;
        Check("B01 reset clears classification and preserves notebook",App.Route=="activity"&&App.Session.Operation==0&&App.Profile.attempts.Count==saved+1&&!App.Session.Authority.CanRecord(out _));
        InputSystem.RemoveDevice(touch);
    }
}
#endif
