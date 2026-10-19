#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using Vlab.Product;

public sealed class VlabSmokeProbe:MonoBehaviour
{
    readonly List<string> checks=new List<string>(),errors=new List<string>();string folder;
    void Log(string condition,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception)errors.Add(condition+"\n"+stack);}
    void Check(string name,bool condition){if(!condition)throw new Exception("FAIL "+name);checks.Add(name);}
    IEnumerator Start()
    {
        DontDestroyOnLoad(gameObject);folder=Path.GetFullPath("Evidence/Runtime");Directory.CreateDirectory(folder);Application.logMessageReceived+=Log;
        var routines=new Stack<IEnumerator>();routines.Push(SessionState.GetBool("VLAB.PolishOnly",false)?PolishScenario():SessionState.GetBool("VLAB.AppearanceOnly",false)?AppearanceScenario():Scenario());
        while(routines.Count>0)
        {
            object value=null;bool next;try{next=routines.Peek().MoveNext();if(next)value=routines.Peek().Current;}catch(Exception ex){errors.Add(ex.ToString());break;}
            if(!next){(routines.Pop() as IDisposable)?.Dispose();continue;}
            if(value is IEnumerator nested&&!(value is CustomYieldInstruction)){routines.Push(nested);continue;}
            yield return value;
        }
        Application.logMessageReceived-=Log;
        File.WriteAllLines(Path.Combine(folder,SessionState.GetBool("VLAB.RenderingOnly",false)?"rendering-review-smoke.txt":SessionState.GetBool("VLAB.LayoutOnly",false)?"apparatus-layout-smoke.txt":SessionState.GetBool("VLAB.BrandingOnly",false)?"branding-review-smoke.txt":SessionState.GetBool("VLAB.LearningOnly",false)?"learning-review-smoke.txt":SessionState.GetBool("VLAB.ControlsOnly",false)?"controls-review-smoke.txt":SessionState.GetBool("VLAB.HomeOnly",false)?"home-review-smoke.txt":SessionState.GetBool("VLAB.PolishOnly",false)?"visual-review-smoke.txt":SessionState.GetBool("VLAB.MicroscopeOnly",false)?"microscope-smoke.txt":SessionState.GetBool("VLAB.AppearanceOnly",false)?"appearance-smoke.txt":SessionState.GetBool("VLAB.SetupAndGuidance",false)?"guidance-review-smoke.txt":"runtime-smoke.txt"),new[]{errors.Count==0?"PASS":"FAILED","UI callback and synthetic Input System checks; no physical phone/headset evidence."}.Concat(checks).Concat(errors));
        SessionState.SetBool("VLAB.RenderingOnly",false);SessionState.SetBool("VLAB.LayoutOnly",false);SessionState.SetBool("VLAB.BrandingOnly",false);SessionState.SetBool("VLAB.LearningOnly",false);
        SessionState.SetBool("VLAB.ControlsOnly",false);SessionState.SetBool("VLAB.HomeOnly",false);SessionState.SetBool("NguaManVLAB.ProductSmoke",false);SessionState.SetBool("VLAB.AppearanceOnly",false);SessionState.SetBool("VLAB.SetupAndGuidance",false);SessionState.SetBool("VLAB.MicroscopeOnly",false);SessionState.SetBool("VLAB.PolishOnly",false);SessionState.SetString("VLAB.TestProfile","");
        if(SessionState.GetBool("NguaManVLAB.LiveReview",false)){Debug.Log("VLAB live review checks: "+(errors.Count==0?"PASS":"FAILED")+"; isolated profile remains in memory.");yield break;}
        EditorApplication.Exit(errors.Count==0?0:1);
    }
    IEnumerator Scenario()
    {
        InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        // Batchmode has no focused Game view; the UI module explicitly skips navigation unless this test-only policy is set.
        InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        yield return new WaitForSecondsRealtime(1);
        var app=VlabApp.Instance;Check("Fresh UI starts at language",app&&app.Route=="language");Capture("01-language");
        var english=Find("English");var keyboard=Keyboard.current??InputSystem.AddDevice<Keyboard>();EventSystem.current.SetSelectedGameObject(english.gameObject);
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Enter));yield return null;yield return null;InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return new WaitForSecondsRealtime(.5f);
        Check("Keyboard Submit selects English without skipping the explicit Continue",app.Route=="language"&&app.Profile.locale=="");Capture("01b-language-selected");
        Click("Continue");yield return new WaitForSecondsRealtime(.3f);Check("Language Continue opens Privacy",app.Route=="privacy");Capture("02-privacy");
        Click("Continue");yield return new WaitForSecondsRealtime(.3f);Check("Legal Continue without consent stays at Privacy",app.Route=="privacy");Click("Back");yield return new WaitForSecondsRealtime(.3f);
        FindContains("I have read and accept").onClick.Invoke();yield return new WaitForSecondsRealtime(.3f);Click("Continue");yield return new WaitForSecondsRealtime(.4f);Check("Separate Terms route",app.Route=="terms");
        FindContains("I have read and accept").onClick.Invoke();yield return new WaitForSecondsRealtime(.3f);Click("Continue");yield return new WaitForSecondsRealtime(.4f);Check("Safety route follows Terms",app.Route=="safety");
        Click("I understand · Enter VLAB");yield return new WaitForSecondsRealtime(.4f);Check("Completed setup opens Home",app.Route=="home");Capture("03-home-en");
        Check("Exactly one active rendering camera",FindObjectsByType<Camera>(FindObjectsSortMode.None).Count(x=>x.enabled)==1);
        Check("Exactly one active audio listener",FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(x=>x.enabled)==1);
        Check("Room review controls disabled",FindObjectsByType<LabReview>(FindObjectsSortMode.None).All(x=>!x.enabled));
        var originalRoot=app.gameObject;Destroy(originalRoot);yield return null;yield return SceneManager.LoadSceneAsync("VLAB_Boot");yield return new WaitForSecondsRealtime(.5f);app=VlabApp.Instance;
        Check("Relaunch skips all completed onboarding",app.Route=="home");
        if(SessionState.GetBool("VLAB.RenderingOnly",false)){yield return VlabAdditionalUiChecks.RenderingOnly(app,Capture,Check);Check("No runtime errors during focused B04 rendering",errors.Count==0);yield break;}
        if(SessionState.GetBool("VLAB.MicroscopeOnly",false)){yield return PlanFlowChecks.Run(app,Capture,Check);yield return MicroscopePresentationChecks.Run(app,Capture,Check);Check("No runtime errors after actual microscope presentation checks",errors.Count==0);yield break;}
        if(SessionState.GetBool("VLAB.LayoutOnly",false)){yield return VlabAdditionalUiChecks.LayoutOnly(app,Capture,Check);Check("No runtime errors after all32 real-room worktop layout checks",errors.Count==0);yield break;}
        if(SessionState.GetBool("VLAB.BrandingOnly",false)){yield return VlabAdditionalUiChecks.BrandingOnly(app,Capture,Check);Check("No runtime errors after white room branding and live Settings language changes",errors.Count==0);yield break;}
        if(SessionState.GetBool("VLAB.SetupAndGuidance",false)){yield return VlabAdditionalUiChecks.GuidanceOnly(app,Capture,Check);Check("No runtime errors after explicit setup and instruction cues",errors.Count==0);yield break;}
        var portal=FindObjectsByType<Button>(FindObjectsSortMode.None).First(b=>b.name=="Subject P");portal.onClick.Invoke();portal.onClick.Invoke();yield return new WaitForSecondsRealtime(.7f);Check("Repeated portal activation opens one browser",app.Route=="browser");Capture("04-browser");
        Click("Start experiment");yield return new WaitForSecondsRealtime(.3f);Click("Enter station");yield return new WaitUntil(()=>app.Route=="activity");yield return new WaitForSecondsRealtime(.5f);Capture("05-apparatus-unassembled");
        Check("P01 apparatus uses delivered prefabs",app.Apparatus.items.Count==6&&app.Apparatus.items.All(x=>x.GetComponentsInChildren<MeshRenderer>().Length>0));
        Check("Run is gated by equipment",!app.Session.Start(out _));
        foreach(var item in app.Apparatus.items){app.Apparatus.Select(item);Check("Install "+item.assetId,app.Apparatus.InstallSelected());}
        for(int i=0;i<app.Session.spec.operationsEn.Length;i++){yield return new WaitForSecondsRealtime(.3f);Click("Perform current action");}
        Check("Preparation drives actual session readiness",app.Session.Phase==ExperimentPhase.Ready);
        Check("Apparatus passes socket, continuous shaft, cord and full range clearance checks: "+(app.Apparatus.ValidateAssembly(out var assemblyFailure)?"valid":assemblyFailure),string.IsNullOrEmpty(assemblyFailure));Capture("06-pendulum-ready");
        app.Input.SetSimulator(true);yield return new WaitForSecondsRealtime(.5f);
        var editorSimulatorPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VLABProduct/Editor/Simulator/XR Device Simulator.prefab");
        Check("Owned Editor simulator omits the unused Editor-only sample overlay",editorSimulatorPrefab.GetComponentInChildren<UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation.XRDeviceSimulator>(true).deviceSimulatorUI==null);
        var editorSimulatorDriver=GameObject.Find("Editor-only XR Device Simulator").GetComponentInChildren<UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation.XRDeviceSimulator>();
        Check("Official simulator driver, mouse sensitivity and tracked HMD remain active without sample overlay",editorSimulatorDriver&&editorSimulatorDriver.isActiveAndEnabled&&editorSimulatorDriver.mouseXRotateSensitivity>0&&editorSimulatorDriver.mouseYRotateSensitivity>0&&app.View.GetComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>().enabled&&InputSystem.devices.Any(device=>device is UnityEngine.InputSystem.XR.XRHMD));
        var activityCanvas=app.GetComponentInChildren<Canvas>();Check("Activity UI is a world-space VR surface",activityCanvas.renderMode==RenderMode.WorldSpace);Capture("06a-vr-pendulum-ready");
        Check("VR controls remain opaque while panel surfaces honour saved opacity",activityCanvas.GetComponentsInChildren<VlabPanelOpacity>().All(p=>Mathf.Abs(p.GetComponent<UnityEngine.UI.Image>().color.a-(p.Solid?1:p.FlatAlpha*app.Profile.settings.uiOpacity))<.001f));
        var release=Find("Release / Start");var releaseRect=release.GetComponent<RectTransform>();Vector3 releaseCentre=releaseRect.TransformPoint(releaseRect.rect.center);var corners=new Vector3[4];releaseRect.GetWorldCorners(corners);float buttonWidth=Vector3.Distance(corners[0],corners[3]),buttonHeight=Vector3.Distance(corners[0],corners[1]);float distance=Vector3.Distance(app.View.transform.position,releaseCentre);
        Check("VR primary action has at least two degrees angular width and height",2*Math.Atan(Math.Min(buttonWidth,buttonHeight)/(2*distance))*Mathf.Rad2Deg>=2);
        Canvas.ForceUpdateCanvases();var clipped=app.GetComponentsInChildren<TextMeshProUGUI>().Where(t=>t.isActiveAndEnabled&&t.overflowMode!=TextOverflowModes.Overflow&&t.isTextOverflowing).ToArray();
        Check("Ready VR activity text is not truncated: "+string.Join("; ",clipped.Select(t=>t.text)),clipped.Length==0);
        var shaft=app.Apparatus.items.First(x=>x.assetId=="stand").GetComponentsInChildren<Transform>().Single(t=>t.name.EndsWith("_Rod_VIS",StringComparison.Ordinal)).GetComponent<Renderer>().bounds;
        for(int corner=0;corner<8;corner++){var position=new Vector3((corner&1)==0?shaft.min.x:shaft.max.x,(corner&2)==0?shaft.min.y:shaft.max.y,(corner&4)==0?shaft.min.z:shaft.max.z);var projected=app.View.WorldToViewportPoint(position);Check("Continuous support corner "+corner+" is inside VR camera view",projected.z>0&&projected.x>.02f&&projected.x<.98f&&projected.y>.02f&&projected.y<.98f);}
        // Exercise the actual product ray path with the same normalized signal used by the controller adapter.
        app.Input.UseMockController=true;var direction=(releaseCentre-app.View.transform.position).normalized;var orientation=Quaternion.LookRotation(direction);
        app.Input.Controller.Submit(new ControllerSignal{sequence=100,receivedAt=Time.realtimeSinceStartupAsDouble,orientation=orientation});yield return null;yield return null;
        app.Input.Controller.Submit(new ControllerSignal{sequence=101,receivedAt=Time.realtimeSinceStartupAsDouble,orientation=orientation,select=true});yield return null;yield return null;
        Check("VR pointer selection starts dynamics through the actual UI ray path",app.Session.Phase==ExperimentPhase.Running);app.Input.UseMockController=false;
        yield return new WaitForSecondsRealtime(2);Capture("07-vr-pendulum-running");app.Input.SetSimulator(false);yield return new WaitForSecondsRealtime(.3f);app.Session.Resume();Capture("07-pendulum-running");app.Session.Pause();double time=app.Session.Elapsed;yield return new WaitForSecondsRealtime(.3f);Check("Paused runtime freezes scientific clock",Math.Abs(app.Session.Elapsed-time)<1e-8);app.Session.Resume();
        yield return new WaitForSecondsRealtime(17);yield return new WaitForSecondsRealtime(.3f);Click("Record measurement");yield return new WaitForSecondsRealtime(.4f);
        Check("Measured journey reaches recorded Results",app.Route=="results"&&app.Profile.attempts.Count==1);Capture("08-results");
        Check("Period derives from same-direction dynamic crossings",Math.Abs(app.Profile.attempts[0].readings.First(x=>x.key=="period").value-ScienceModels.PendulumPeriod(.6,8))<.001);
        Check("Recorded cycle count is exactly the selected ten",app.Profile.attempts[0].readings.First(x=>x.key=="timedCycles").value==10);
        Click("Try again");yield return new WaitUntil(()=>app.Route=="activity");Check("Retry returns to clean preparation",app.Session.Phase==ExperimentPhase.Preparing&&app.Session.Elapsed==0&&!app.Apparatus.Ready);Check("Retry retains notebook measurements",app.Profile.attempts.Count==1);
        yield return new WaitForSecondsRealtime(.3f);Click("‹ Back");yield return new WaitForSecondsRealtime(.3f);Click("Settings");yield return new WaitForSecondsRealtime(.3f);Capture("09-settings");
        Click("Language: English");yield return new WaitForSecondsRealtime(.3f);Click("Áp dụng");yield return new WaitForSecondsRealtime(.3f);Check("Language Apply commits Vietnamese",app.Profile.locale=="vi");
        app.ShowHome();yield return new WaitForSecondsRealtime(.3f);Capture("10-home-vi");
        app.Input.SetSimulator(true);yield return new WaitForSecondsRealtime(.5f);
        var canvas=app.GetComponentInChildren<Canvas>();Check("PC simulation switches to world-anchored UI",app.Input.SimulatorMode&&canvas.renderMode==RenderMode.WorldSpace);
        Check("Official XR Device Simulator supplies a virtual HMD",InputSystem.GetDevice<UnityEngine.InputSystem.XR.XRHMD>()!=null);
        Vector3 menuPosition=canvas.transform.position;Quaternion rotation=app.View.transform.rotation;var mouse=Mouse.current??InputSystem.AddDevice<Mouse>();
        InputSystem.QueueStateEvent(mouse,new MouseState{buttons=2,delta=new Vector2(150,0)});yield return null;yield return null;InputSystem.QueueStateEvent(mouse,new MouseState());yield return new WaitForSecondsRealtime(.2f);
        Check("Official simulator head rotation reaches the product camera",Quaternion.Angle(rotation,app.View.transform.rotation)>.1f);
        Check("VR menu stays anchored during head rotation",Vector3.Distance(menuPosition,canvas.transform.position)<.001f);Capture("11-world-ui-pc-simulator");
        InputSystem.QueueStateEvent(mouse,new MouseState{buttons=2,delta=new Vector2(-150,0)});yield return null;yield return null;InputSystem.QueueStateEvent(mouse,new MouseState());yield return new WaitForSecondsRealtime(.2f);
        app.Input.SetSimulator(false);yield return new WaitForSecondsRealtime(.3f);Check("Exit simulation restores flat UI",canvas.renderMode==RenderMode.ScreenSpaceOverlay);
        GamepadIntegrationChecks.Run();Check("Queued HID gamepad state, disconnect and mouse fallback checks passed",true);
        File.WriteAllText(Path.Combine(folder,"notebook-data-checks.txt"),NotebookDataChecks.Run());Check("Notebook measured-data grouping and fit fixture checks passed",true);
        yield return VlabAdditionalUiChecks.Run(app,Capture,Check);
        VlabAppearanceChecks.Core(Check);
        Check("All 32 comparison-series guides cover both languages",LessonSeries.ValidateMetadata().Length==0);
        yield return VlabAppearanceChecks.CaptureThemes(app,Capture,Check,false);
        Check("No runtime errors observed",errors.Count==0);
        if(SessionState.GetBool("NguaManVLAB.LiveReview",false))
        {
            app.ChooseLanguage("en");yield return new WaitForSecondsRealtime(.3f);
            FindObjectsByType<Button>(FindObjectsSortMode.None).First(b=>b.name=="Subject P").onClick.Invoke();yield return new WaitForSecondsRealtime(.9f);
            Click("Start experiment");yield return new WaitForSecondsRealtime(.3f);Click("Enter station");yield return new WaitUntil(()=>app.Route=="activity");yield return new WaitForSecondsRealtime(.4f);
            foreach(var item in app.Apparatus.items){app.Apparatus.Select(item);app.Apparatus.InstallSelected();}
            for(int i=0;i<app.Session.spec.operationsEn.Length;i++){yield return new WaitForSecondsRealtime(.3f);Click("Perform current action");}
            app.Input.SetSimulator(true);yield return new WaitForSecondsRealtime(.5f);Capture("12-live-review-vr-ready");
        }
    }
    IEnumerator PolishScenario()
    {
        InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        yield return new WaitForSecondsRealtime(1);var app=VlabApp.Instance;Check("Visual review reopens actual47-record isolated profile",app&&app.Route=="home"&&app.Profile.attempts.Count>=47);
        if(app.Profile.locale=="en"){Click("Settings");yield return new WaitForSecondsRealtime(.3f);Click("Language: English");yield return new WaitForSecondsRealtime(.3f);Click("Áp dụng");yield return new WaitForSecondsRealtime(.3f);}
        Check("User-facing review uses Vietnamese",app.Profile.locale=="vi");
        app.Input.UseMockController=true;app.Input.Controller.Submit(new ControllerSignal{sequence=10000,receivedAt=Time.realtimeSinceStartupAsDouble,orientation=Quaternion.LookRotation(Vector3.up,Vector3.forward)});app.Input.SetSimulator(true);yield return new WaitForSecondsRealtime(.4f);
        if(SessionState.GetBool("VLAB.HomeOnly",false))
        {
            Capture("49-trang-chu-theo-plan-VI");yield return HomePresentationChecks.Run(app,Capture,Check);
            Check("No runtime errors in the focused Home review",errors.Count==0);yield break;
        }
        if(SessionState.GetBool("VLAB.LearningOnly",false)){yield return GuidanceQualityChecks.Run(Check);yield return LearningGuideChecks.Run(app,Capture,Check);Check("No runtime errors in the focused learning review",errors.Count==0);yield break;}
        ObservationCards.CheckFormatting(Check);ObservationCardLayoutChecks.Run(app.Profile.attempts,Check);NotebookPersistenceChecks.Run(Check);NotebookKeyboardChecks.Pure(Check);VlabAppearanceChecks.Core(Check);
        yield return CardboardEntryChecks.Run(Check);
        GamepadIntegrationChecks.Run();Check("Native HID and phone touch gesture checks passed",true);
        ControllerBindingChecks.Core(app.Profile,Check);ControllerBindingChecks.Run(Check);
        yield return UiMotionChecks.Run(Check);
        yield return GuidanceQualityChecks.Run(Check);
        SettingsInteractionChecks.Core(app.Profile,Check);yield return SettingsInteractionChecks.Run(app,Check,Capture);
        yield return SettingsPresentationChecks.Run(app,Capture,Check);
        yield return UiMotionChecks.RunSettings(app,Check);
        yield return ControllerBindingUiChecks.Run(app,Capture,Check);
        SafetyPresentationChecks.Run(Check);yield return SafetyPresentationChecks.Capture(app,Capture,Check);
        if(SessionState.GetBool("VLAB.ControlsOnly",false)){Check("No runtime errors in focused settings and entry review",errors.Count==0);yield break;}
        yield return PlanFlowChecks.Run(app,Capture,Check);RoomBranding.SetVisible(SceneManager.GetActiveScene(),true);RoomBrandingChecks.Run(Check,SceneManager.GetActiveScene());RoomBranding.SetVisible(SceneManager.GetActiveScene(),false);
        foreach(var portal in app.GetComponentsInChildren<PortalFocus>())
        {
            bool reduced=portal.ReducedMotion;portal.ReducedMotion=true;Vector3 scale=portal.transform.localScale;
            ExecuteEvents.Execute(portal.gameObject,new PointerEventData(EventSystem.current),ExecuteEvents.pointerEnterHandler);yield return new WaitForSecondsRealtime(.3f);
            var edge=portal.GetComponent<UnityEngine.UI.Outline>();Check("Focused lab has a visible outline with reduced motion: "+portal.name,edge&&edge.effectDistance==new Vector2(3,-3)&&edge.effectColor==portal.FocusColor);
            Check("Reduced-motion lab focus does not enlarge the card: "+portal.name,Vector3.Distance(scale,portal.transform.localScale)<.001f);
            if(portal.name=="Subject P")Capture("46-focus-phong-VI");
            ExecuteEvents.Execute(portal.gameObject,new PointerEventData(EventSystem.current),ExecuteEvents.pointerExitHandler);Check("Lab focus outline resets on exit: "+portal.name,edge.effectDistance==new Vector2(2,-2));portal.ReducedMotion=reduced;
        }
        yield return new WaitForSecondsRealtime(.3f);Capture("44-trang-chu-VI");
        yield return HomePresentationChecks.Run(app,Capture,Check);
        yield return JournalNoteChecks.Run(app,Capture,Check);yield return NotebookVrChecks.Run(app,Capture,Check);yield return JournalReadabilityChecks.Run(app,Capture,Check);
        yield return LearningGuideChecks.Run(app,Capture,Check);
        yield return MicroscopePresentationChecks.Run(app,Capture,Check);
        var reopened=new ProfileStore(SessionState.GetString("VLAB.TestProfile","")).Load();
        Check("All47 scientific records survive notebook writing and schema2 file reopen",!reopened.writeProtected&&reopened.profile.attempts.Select(record=>JsonUtility.ToJson(record)).SequenceEqual(app.Profile.attempts.Select(record=>JsonUtility.ToJson(record))));
        Check("No runtime errors in Vietnamese visual review",errors.Count==0);
    }
    IEnumerator AppearanceScenario()
    {
        yield return new WaitForSecondsRealtime(1);var app=VlabApp.Instance;Check("Existing isolated profile reopens Home",app&&app.Route=="home");
        VlabAppearanceChecks.Core(Check);Check("All 32 comparison-series guides cover both languages",LessonSeries.ValidateMetadata().Length==0);
        yield return VlabAppearanceChecks.CaptureThemes(app,Capture,Check,true);Check("No runtime errors observed",errors.Count==0);
        var portals=app.GetComponentsInChildren<PortalFocus>();foreach(var portal in portals)portal.enabled=false;
        yield return LabPanoramaCapture.Verify(app.Preview,app.View,Check);foreach(var portal in portals)portal.enabled=true;
        app.Input.UseMockController=true;app.Input.Controller.Submit(new ControllerSignal{sequence=900000,receivedAt=Time.realtimeSinceStartupAsDouble,orientation=Quaternion.LookRotation(Vector3.up,Vector3.forward)});
        app.Input.SetSimulator(true);yield return new WaitForSecondsRealtime(.3f);
        string[] rooms={"Physics","Chemistry","Biology","Engineering"};
        for(int i=0;i<portals.Length;i++){ExecuteEvents.Execute(portals[i].gameObject,new PointerEventData(EventSystem.current),ExecuteEvents.pointerEnterHandler);yield return new WaitForSecondsRealtime(.25f);Check("Actual portal focus selects captured "+rooms[i],app.Preview.CurrentRoom==rooms[i]);Capture("25-hover-"+rooms[i]+"-VR");ExecuteEvents.Execute(portals[i].gameObject,new PointerEventData(EventSystem.current),ExecuteEvents.pointerExitHandler);yield return new WaitForSecondsRealtime(.25f);}
        app.Input.UseMockController=false;app.Input.SetSimulator(false);yield return null;
        yield return VlabAdditionalUiChecks.GuidanceOnly(app,Capture,Check);Check("No runtime errors after panoramas and animated guidance",errors.Count==0);
    }
    Button Find(string label)=>FindObjectsByType<Button>(FindObjectsSortMode.None).LastOrDefault(b=>b.isActiveAndEnabled&&b.GetComponentInChildren<TextMeshProUGUI>()?.text==label)??throw new Exception("Button unavailable: "+label);
    Button FindContains(string label)=>FindObjectsByType<Button>(FindObjectsSortMode.None).Last(b=>b.isActiveAndEnabled&&(b.GetComponentInChildren<TextMeshProUGUI>()?.text.Contains(label)??false));
    void Click(string label){Find(label).onClick.Invoke();}
    void Capture(string name)
    {
        CaptureSize(name,1600,1000);
        if(SessionState.GetBool("VLAB.MicroscopeOnly",false)||SessionState.GetBool("VLAB.PolishOnly",false))CaptureSize(name+"-landscape-phone",1920,1080);
    }
    void CaptureSize(string name,int width,int height)
    {
        var camera=VlabApp.Instance.View;var canvas=VlabApp.Instance.GetComponentInChildren<Canvas>();var oldMode=canvas.renderMode;var oldCamera=canvas.worldCamera;float oldDistance=canvas.planeDistance;
        var rt=new RenderTexture(width,height,24);camera.targetTexture=rt;if(oldMode!=RenderMode.WorldSpace){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=.2f;}
        Canvas.ForceUpdateCanvases();LabPanoramaCapture.RenderMono(camera,rt);var old=RenderTexture.active;RenderTexture.active=rt;var texture=new Texture2D(width,height,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,width,height),0,0);texture.Apply();File.WriteAllBytes(Path.Combine(folder,name+".png"),texture.EncodeToPNG());RenderTexture.active=old;camera.targetTexture=null;canvas.renderMode=oldMode;canvas.worldCamera=oldCamera;canvas.planeDistance=oldDistance;Destroy(texture);Destroy(rt);
    }
}

[InitializeOnLoad] public static class VlabRuntimeSmoke
{
    static VlabRuntimeSmoke(){EditorApplication.playModeStateChanged+=State;}
    static void State(PlayModeStateChange state){if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("NguaManVLAB.ProductSmoke",false))new GameObject("Isolated VLAB runtime verification",typeof(VlabSmokeProbe));}
    public static void Run()
    {
        SessionState.SetBool("VLAB.RenderingOnly",false);SessionState.SetBool("VLAB.LayoutOnly",false);SessionState.SetBool("VLAB.BrandingOnly",false);
        SessionState.SetBool("VLAB.HomeOnly",false);
        SessionState.SetBool("VLAB.AppearanceOnly",false);
        SessionState.SetBool("VLAB.SetupAndGuidance",false);SessionState.SetBool("VLAB.MicroscopeOnly",false);SessionState.SetBool("VLAB.PolishOnly",false);
        SessionState.SetBool("NguaManVLAB.LiveReview",false);
        SessionState.SetString("VLAB.TestProfile",Path.GetFullPath("Evidence/TestProfile-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")));SessionState.SetBool("NguaManVLAB.ProductSmoke",true);EditorSceneManager.OpenScene("Assets/VLABProduct/Scenes/VLAB_Boot.unity");EditorApplication.EnterPlaymode();
    }
    public static void Appearance()
    {
        SessionState.SetBool("VLAB.HomeOnly",false);
        SessionState.SetBool("NguaManVLAB.LiveReview",false);SessionState.SetBool("VLAB.AppearanceOnly",true);SessionState.SetBool("VLAB.PolishOnly",false);SessionState.SetBool("VLAB.MicroscopeOnly",false);SessionState.SetBool("VLAB.SetupAndGuidance",false);
        SessionState.SetString("VLAB.TestProfile",ReviewProfile());
        SessionState.SetBool("NguaManVLAB.ProductSmoke",true);EditorSceneManager.OpenScene("Assets/VLABProduct/Scenes/VLAB_Boot.unity");EditorApplication.EnterPlaymode();
    }
    public static void Polish()
    {
        SessionState.SetBool("VLAB.HomeOnly",false);
        LabPanoramaCapture.Run();
        SessionState.SetBool("VLAB.AppearanceOnly",false);SessionState.SetBool("VLAB.MicroscopeOnly",false);SessionState.SetBool("VLAB.PolishOnly",true);SessionState.SetBool("NguaManVLAB.LiveReview",false);
        SessionState.SetString("VLAB.TestProfile",ReviewProfile());
        SessionState.SetBool("NguaManVLAB.ProductSmoke",true);EditorSceneManager.OpenScene("Assets/VLABProduct/Scenes/VLAB_Boot.unity");EditorApplication.EnterPlaymode();
    }
    public static void HomeReview()
    {
        SessionState.SetBool("VLAB.HomeOnly",true);SessionState.SetBool("VLAB.PolishOnly",true);SessionState.SetBool("VLAB.AppearanceOnly",false);SessionState.SetBool("VLAB.MicroscopeOnly",false);SessionState.SetBool("VLAB.SetupAndGuidance",false);SessionState.SetBool("NguaManVLAB.LiveReview",false);
        SessionState.SetString("VLAB.TestProfile",ReviewProfile());SessionState.SetBool("NguaManVLAB.ProductSmoke",true);EditorSceneManager.OpenScene("Assets/VLABProduct/Scenes/VLAB_Boot.unity");EditorApplication.EnterPlaymode();
    }
    public static void ControlsReview()
    {
        SessionState.SetBool("VLAB.ControlsOnly",true);SessionState.SetBool("VLAB.HomeOnly",false);SessionState.SetBool("VLAB.PolishOnly",true);SessionState.SetBool("VLAB.AppearanceOnly",false);SessionState.SetBool("VLAB.MicroscopeOnly",false);SessionState.SetBool("VLAB.SetupAndGuidance",false);SessionState.SetBool("NguaManVLAB.LiveReview",false);
        SessionState.SetString("VLAB.TestProfile",ReviewProfile());SessionState.SetBool("NguaManVLAB.ProductSmoke",true);EditorSceneManager.OpenScene("Assets/VLABProduct/Scenes/VLAB_Boot.unity");EditorApplication.EnterPlaymode();
    }
    public static void LearningReview()
    {
        SessionState.SetBool("VLAB.LearningOnly",true);SessionState.SetBool("VLAB.ControlsOnly",false);SessionState.SetBool("VLAB.HomeOnly",false);SessionState.SetBool("VLAB.PolishOnly",true);SessionState.SetBool("VLAB.AppearanceOnly",false);SessionState.SetBool("VLAB.MicroscopeOnly",false);SessionState.SetBool("VLAB.SetupAndGuidance",false);SessionState.SetBool("NguaManVLAB.LiveReview",false);
        SessionState.SetString("VLAB.TestProfile",ReviewProfile());SessionState.SetBool("NguaManVLAB.ProductSmoke",true);EditorSceneManager.OpenScene("Assets/VLABProduct/Scenes/VLAB_Boot.unity");EditorApplication.EnterPlaymode();
    }
    static string ReviewProfile()
    {
        string source=Directory.GetDirectories(Path.GetFullPath("Evidence/Subjects"),"OfflineProfile-*").OrderByDescending(path=>path,StringComparer.Ordinal).First();
        var loaded=new ProfileStore(source).Load();if(loaded.writeProtected||loaded.profile.attempts.Count<47)throw new InvalidOperationException("The isolated native47-record fixture is unavailable.");var profile=loaded.profile;profile.locale="vi";profile.settings.uiOpacity=new GameSettings().uiOpacity;
        foreach(var attempt in profile.attempts)if(attempt.observation=="Isolated full-journey persistence test.")attempt.observation="";
        string review=Path.GetFullPath("Evidence/VisualProfile-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));if(!new ProfileStore(review).Save(profile,out var error))throw new InvalidOperationException(error);
        File.WriteAllText(Path.Combine(review,"SCOPE.txt"),"Derived visual-review copy of actual47 native attempts, using Vietnamese and the current default panel opacity. English QA annotation removed; scientific values, timestamps, configurations and model notes unchanged. Not a learner profile.");
        return review;
    }
    public static void Review(){Run();SessionState.SetBool("NguaManVLAB.LiveReview",true);}
    public static void RenderingReview(){Run();SessionState.SetBool("VLAB.RenderingOnly",true);}
    public static void LayoutReview(){Run();SessionState.SetBool("VLAB.LayoutOnly",true);}
    public static void BrandingReview(){Run();SessionState.SetBool("VLAB.BrandingOnly",true);}
    public static void SetupAndGuidance(){Run();SessionState.SetBool("VLAB.SetupAndGuidance",true);}
    public static void Microscope(){Run();SessionState.SetBool("VLAB.MicroscopeOnly",true);}
}
#endif
