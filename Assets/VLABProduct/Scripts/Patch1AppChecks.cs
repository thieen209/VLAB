#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.EventSystems;
using UnityEditor;
using Vlab.Product;

// Run only with VLAB.TestProfile installed before app Awake. Invokes real UI
// callbacks; does not accept legal documents, reset a profile or fabricate data.
public static class Patch1AppChecks
{
    static void FollowupCheck(Action<string,bool> check,string label,bool ok){check(label,ok);}
    static bool Isolated(VlabApp app)
    {
        string path=SessionState.GetString("VLAB.TestProfile","");
        return !string.IsNullOrEmpty(path)&&Path.GetFullPath(((ProfileStore)typeof(VlabApp).GetField("store",Private).GetValue(app)).Folder)==Path.GetFullPath(path);
    }
    static IEnumerator FreshP01(VlabApp app)
    {
        app.ShowHome();yield return Press(app,"Subject P");yield return new WaitForSecondsRealtime(.4f);
        yield return app.CheckRoomLoad("Physics",true,false);yield return new WaitForSecondsRealtime(.35f);
        if(app.Route!="activity"||app.Session?.spec.id!="P01"||!app.Apparatus)throw new Exception("Followup requires actual P01 room activity.");
    }
    static void Touch(Touchscreen device,int id,UnityEngine.InputSystem.TouchPhase phase,Vector2 point)
    {InputSystem.QueueStateEvent(device,new TouchState{touchId=id,phase=phase,position=point});}
    static IEnumerator Stick(VlabApp app,Touchscreen device,int id)
    {
        var safe=app.GetComponentsInChildren<PhoneUiViewport>().Single(v=>v.isActiveAndEnabled).SafePixels;var c=app.Input.PhoneJoystickCenter;
        var point=new Vector2(safe.x+safe.width*c.x,safe.y+safe.height*c.y);var moved=point+Vector2.right*(safe.height*app.Input.PhoneJoystickRadius*.5f);
        Touch(device,id,UnityEngine.InputSystem.TouchPhase.Began,point);yield return null;
        Touch(device,id,UnityEngine.InputSystem.TouchPhase.Moved,moved);yield return new WaitForSecondsRealtime(.15f);
        Touch(device,id,UnityEngine.InputSystem.TouchPhase.Ended,moved);yield return null;yield return null;
    }
    // Independent lifecycle case. SendMessage delivers the synthetic OS callback to
    // BOTH actual App and Input components, without calling NewPage on return.
    public static IEnumerator RunLifecycle(VlabApp app,Action<string> capture,Action<string,bool> check)
    {
        if(!Isolated(app))throw new Exception("Followup requires pre-Awake isolated VLAB.TestProfile.");
        Touchscreen owned=null;var screen=Touchscreen.current;if(screen==null)screen=owned=InputSystem.AddDevice<Touchscreen>();
        try
        {
            yield return FreshP01(app);
            var rig=app.Apparatus;
            while(!rig.Ready){var item=rig.NextInstallableItem();if(!item)throw new Exception("No source installation order.");rig.Select(item);if(!rig.InstallSelected())throw new Exception("Source setup failed for "+item.assetId);}
            while(app.Session.Phase==ExperimentPhase.Preparing)if(!rig.PrepareNext(out var why))throw new Exception("Source preparation failed: "+why);
            if(!app.Session.Start(out var startWhy))throw new Exception("Source Start failed: "+startWhy);
            app.ShowActivity();yield return new WaitForSecondsRealtime(.25f);
            var page=typeof(VlabApp).GetField("page",Private).GetValue(app);
            app.gameObject.SendMessage("OnApplicationPause",true,SendMessageOptions.RequireReceiver);yield return null;
            double stopped=app.Session.Elapsed;app.gameObject.SendMessage("OnApplicationPause",false,SendMessageOptions.RequireReceiver);yield return null;yield return null;
            FollowupCheck(check,"Pause false restores actual activity Gameplay without NewPage",app.Input.Gameplay&&ReferenceEquals(page,typeof(VlabApp).GetField("page",Private).GetValue(app))&&app.Route=="activity");
            FollowupCheck(check,"Application return keeps the real experiment explicitly paused",app.Session.Phase==ExperimentPhase.Paused&&app.Session.Elapsed==stopped&&app.Session.Recorded==null);
            FollowupCheck(check,"Application return restores visible phone controls",app.Input.PhoneControlsVisible&&app.GetComponentsInChildren<RectTransform>().Any(r=>r.name=="Phone movement joystick"&&r.gameObject.activeInHierarchy));
            var origin=app.View.transform.parent;var before=origin.position;yield return Stick(app,screen,99101);
            var delta=origin.position-before;FollowupCheck(check,"Fresh touch after application return moves actual body without resuming science",new Vector2(delta.x,delta.z).magnitude>.005f&&Mathf.Abs(delta.y)<.0005f&&app.Session.Phase==ExperimentPhase.Paused&&app.Session.Elapsed==stopped);
            app.gameObject.SendMessage("OnApplicationFocus",false,SendMessageOptions.RequireReceiver);yield return null;app.gameObject.SendMessage("OnApplicationFocus",true,SendMessageOptions.RequireReceiver);yield return null;yield return null;
            FollowupCheck(check,"Focus false/true retains gameplay but does not auto-resume trial",app.Input.Gameplay&&app.Session.Phase==ExperimentPhase.Paused&&app.Session.Elapsed==stopped);
            yield return Press(app,"Dụng cụ & hướng dẫn","Station tools");before=origin.position;yield return Stick(app,screen,99102);
            FollowupCheck(check,"Open activity modal blocks fresh touch movement after lifecycle recovery",!app.Input.PhoneControlsVisible&&Vector3.Distance(origin.position,before)<.0005f&&app.Session.Phase==ExperimentPhase.Paused);
            yield return Press(app,"Close dialog");app.ShowHome();yield return new WaitForSecondsRealtime(.25f);
            app.gameObject.SendMessage("OnApplicationPause",true,SendMessageOptions.RequireReceiver);yield return null;app.gameObject.SendMessage("OnApplicationPause",false,SendMessageOptions.RequireReceiver);yield return null;yield return null;
            FollowupCheck(check,"Returning to non-play Home keeps gameplay and joystick blocked",app.Route=="home"&&!app.Input.Gameplay&&!app.Input.PhoneControlsVisible&&app.Session.Phase==ExperimentPhase.Paused&&app.Session.Elapsed==stopped);
            capture?.Invoke("followup-lifecycle-home-blocked");
        }
        finally{if(screen!=null&&screen.added){Touch(screen,99101,UnityEngine.InputSystem.TouchPhase.Ended,Vector2.zero);Touch(screen,99102,UnityEngine.InputSystem.TouchPhase.Ended,Vector2.zero);}if(owned!=null&&owned.added)InputSystem.RemoveDevice(owned);}
    }
    static EquipmentHandle AccessibleLoose(VlabApp app,out Vector2 point)
    {
        point=default;var safe=app.GetComponentsInChildren<PhoneUiViewport>().Single(v=>v.isActiveAndEnabled).SafePixels;
        foreach(var item in app.Apparatus.items.Where(i=>!i.installed&&i.gameObject.activeInHierarchy&&Vector3.Distance(i.trayPosition,i.slot)>.22f))
        foreach(var renderer in item.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled))
        {
            var pixel=app.View.WorldToScreenPoint(renderer.bounds.center);if(pixel.z<=0||!safe.Contains(pixel))continue;
            var n=new Vector2((pixel.x-safe.x)/safe.width,(pixel.y-safe.y)/safe.height);if(n.x<=.34f&&n.y<=.46f)continue;
            var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=pixel},hits);if(hits.Count!=0)continue;
            if(!Physics.Raycast(app.View.ScreenPointToRay(pixel),out var hit,3.5f)||hit.collider.GetComponentInParent<EquipmentHandle>()!=item)continue;
            point=pixel;return item;
        }
        return null;
    }
    // No ActionFrame.phoneWorldPress dependency: this exact test compiles on0.1.2.
    public static IEnumerator RunWorldTap(VlabApp app,Action<string> capture,Action<string,bool> check)
    {
        if(!Isolated(app))throw new Exception("Followup requires pre-Awake isolated VLAB.TestProfile.");
        Touchscreen ownedTouch=null;Mouse ownedMouse=null;var screen=Touchscreen.current;if(screen==null)screen=ownedTouch=InputSystem.AddDevice<Touchscreen>();
        var mouse=Mouse.current;Vector2 oldMouse=mouse?.position.ReadValue()??Vector2.zero;
        try
        {
            yield return FreshP01(app);yield return null;yield return null;
            var rig=app.Apparatus;int operation=app.Session.Operation;
            var item=AccessibleLoose(app,out var pixel);if(!item)throw new Exception("No actual visible UI-clear loose collider outside joystick claim with tray-slot distance>.22m.");
            capture?.Invoke("followup-phone-world-before");
            Touch(screen,99201,UnityEngine.InputSystem.TouchPhase.Began,pixel);yield return null;yield return null;
            Touch(screen,99201,UnityEngine.InputSystem.TouchPhase.Ended,pixel);yield return null;yield return null;
            FollowupCheck(check,"Actual phone world tap stays selected and held for explicit Place or Return",rig.Selected==item&&rig.IsCarrying&&!item.installed&&app.Session.Operation==operation&&app.Session.Recorded==null);
            capture?.Invoke("followup-phone-world-released");rig.CancelGrab();
            if(mouse==null)mouse=ownedMouse=InputSystem.AddDevice<Mouse>();
            item=AccessibleLoose(app,out pixel);if(!item)throw new Exception("No source loose collider for PC drag regression.");
            InputSystem.QueueStateEvent(mouse,new MouseState{position=pixel,buttons=1});yield return null;yield return null;
            FollowupCheck(check,"Actual PC pointer press still begins legacy drag",rig.IsCarrying&&rig.Selected==item&&!item.installed);
            var safe=app.GetComponentsInChildren<PhoneUiViewport>().Single(v=>v.isActiveAndEnabled).SafePixels;var released=new Vector2(safe.xMax-20,safe.yMax-20);
            InputSystem.QueueStateEvent(mouse,new MouseState{position=released,buttons=1});yield return null;
            InputSystem.QueueStateEvent(mouse,new MouseState{position=released,buttons=0});yield return null;yield return null;
            FollowupCheck(check,"PC missed drag release still returns tray rather than phone latch",!rig.IsCarrying&&rig.Selected==null&&!item.installed&&Vector3.Distance(item.transform.position,item.trayPosition)<.001f);
            FollowupCheck(check,"World input does not fabricate experimental steps, time or record",app.Session.Operation==operation&&app.Session.Elapsed==0&&app.Session.Recorded==null);
        }
        finally{if(screen!=null&&screen.added)Touch(screen,99201,UnityEngine.InputSystem.TouchPhase.Ended,Vector2.zero);if(mouse!=null&&mouse.added)InputSystem.QueueStateEvent(mouse,new MouseState{position=oldMouse});if(ownedMouse!=null&&ownedMouse.added)InputSystem.RemoveDevice(ownedMouse);if(ownedTouch!=null&&ownedTouch.added)InputSystem.RemoveDevice(ownedTouch);}
    }
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static Button[] Buttons(VlabApp app)=>app.GetComponentsInChildren<Button>().Where(b=>b.isActiveAndEnabled&&b.interactable).ToArray();
    static string Caption(Button b)=>b.GetComponentInChildren<TextMeshProUGUI>()?.text??"";
    static Button Find(VlabApp app,params string[] names)=>Buttons(app).FirstOrDefault(b=>names.Contains(b.name)||names.Contains(Caption(b)))??throw new Exception("Patch1 button missing: "+string.Join(" / ",names));
    static IEnumerator Press(VlabApp app,params string[] names)
    {yield return new WaitForSecondsRealtime(.28f);Find(app,names).onClick.Invoke();yield return new WaitForSecondsRealtime(.28f);}
    static IEnumerator PressPrefix(VlabApp app,string prefix)
    {yield return new WaitForSecondsRealtime(.28f);var button=Buttons(app).FirstOrDefault(b=>Caption(b).StartsWith(prefix,StringComparison.Ordinal))??throw new Exception("Patch1 prefix missing: "+prefix);button.onClick.Invoke();yield return new WaitForSecondsRealtime(.28f);}
    static IEnumerator Bar(VlabApp app,float top,bool increase)
    {
        yield return new WaitForSecondsRealtime(.28f);
        var before=((GameSettings)typeof(VlabApp).GetField("draft",Private).GetValue(app)).Copy();
        string name=(top==652?"Phone FOV ":"Phone movement speed ")+(increase?"plus":"minus");
        Find(app,name).onClick.Invoke();yield return new WaitForSecondsRealtime(.28f);
        var after=(GameSettings)typeof(VlabApp).GetField("draft",Private).GetValue(app);
        float actual=top==652?after.cameraFov:after.moveSpeed;
        float expected=top==652?Mathf.Clamp(before.cameraFov+(increase?5:-5),45,85):Mathf.Clamp(before.moveSpeed+(increase?.1f:-.1f),.3f,2);
        string diagnostic=name+" before="+(top==652?before.cameraFov:before.moveSpeed)+" expected="+expected+" actual="+actual+" joystick before="+before.joystickScale+" after="+after.joystickScale;
        Debug.Log("PATCH1 SETTINGS "+diagnostic);
        if(Mathf.Abs(actual-expected)>.001f||after.joystickScale!=before.joystickScale)throw new Exception("Settings callback changed wrong value: "+diagnostic);
    }
    static bool Dialog(VlabApp app)=>app.GetComponentsInChildren<RectTransform>().Any(r=>r.gameObject.activeInHierarchy&&r.name=="Dialog");
    static ProfileLoad Reload(VlabApp app)
    {var store=(ProfileStore)typeof(VlabApp).GetField("store",Private).GetValue(app);return new ProfileStore(store.Folder).Load();}
    static string Attempts(LocalProfile p)=>string.Join("|",p.attempts.Select(x=>JsonUtility.ToJson(x)));
    static string Notes(LocalProfile p)=>string.Join("|",p.notebookPages.Select(x=>JsonUtility.ToJson(x)));
    static string Legal(LocalProfile p)=>string.Join("|",p.legal.Select(x=>JsonUtility.ToJson(x)));
    static IEnumerator Equipment(VlabApp app)
    {yield return Press(app,"Dụng cụ & hướng dẫn","Station tools");yield return Press(app,"Dụng cụ","Equipment");}
    static Rect ScreenBounds(RectTransform rect)
    {
        var canvas=rect.GetComponentInParent<Canvas>();var camera=canvas&&canvas.renderMode!=RenderMode.ScreenSpaceOverlay?canvas.worldCamera:null;
        var corners=new Vector3[4];rect.GetWorldCorners(corners);var pixels=corners.Select(point=>RectTransformUtility.WorldToScreenPoint(camera,point)).ToArray();
        return Rect.MinMaxRect(pixels.Min(p=>p.x),pixels.Min(p=>p.y),pixels.Max(p=>p.x),pixels.Max(p=>p.y));
    }
    static bool Overlaps(Rect a,Rect b)=>Mathf.Min(a.xMax,b.xMax)-Mathf.Max(a.xMin,b.xMin)>.5f&&Mathf.Min(a.yMax,b.yMax)-Mathf.Max(a.yMin,b.yMin)>.5f;

    public static IEnumerator Run(VlabApp app,Action<string> capture,Action<string,bool> check)
    {
        yield return new WaitForSecondsRealtime(.4f);
        var store=(ProfileStore)typeof(VlabApp).GetField("store",Private).GetValue(app);
        string fixture=SessionState.GetString("VLAB.TestProfile","");
        bool isolated=!string.IsNullOrEmpty(fixture)&&Path.GetFullPath(store.Folder)==Path.GetFullPath(fixture);
        check("Patch1 UI requires explicit isolated profile before app Awake",isolated);if(!isolated)yield break;
        check("Fresh launch reaches Vietnamese Home without accepting legal documents",app.Route=="home"&&app.Profile.locale=="vi"&&app.Profile.legal.Count==0&&!app.Profile.safetyAcknowledged);
        string attempts=Attempts(app.Profile),notes=Notes(app.Profile),legal=Legal(app.Profile),clipboard=GUIUtility.systemCopyBuffer;
        Touchscreen ownedTouchscreen=null,screen=null;Vector2 touchEnd=default;
        var original=app.Profile.settings.Copy();
        try
        {
            yield return Press(app,"Home utility 0","Cài đặt","Settings");
            yield return Press(app,"Điều khiển","Controls");yield return Press(app,"Controls phone");yield return Press(app,"Controls phone settings");
            check("Phone settings opens as a closable UI dialog",Dialog(app)&&Find(app,"Close dialog"));
            yield return Press(app,"Đảo chiều ngang","Invert horizontal look");
            yield return Press(app,"Đảo chiều dọc","Invert vertical look");
            yield return Press(app,"Xoay máy để nhìn (cần cảm biến)","Look by rotating phone (sensor required)");
            bool fovUp=original.cameraFov<85;yield return Bar(app,652,fovUp);
            yield return Press(app,"Di chuyển","Movement");bool speedUp=original.moveSpeed<1.99f;yield return Bar(app,344,speedUp);
            capture?.Invoke("patch1-phone-movement");
            yield return Press(app,"Xong · Chưa áp dụng","Done · Apply in Settings");yield return Press(app,"Settings apply","Áp dụng","Apply");
            var loaded=Reload(app);var s=loaded.profile.settings;
            check("Phone UI Apply persists inversion, gyro preference, speed and FOV",!loaded.writeProtected&&!loaded.recovered&&s.invertLookX==!original.invertLookX&&s.invertLookY==!original.invertLookY&&s.phoneGyro==!original.phoneGyro&&Mathf.Abs(s.cameraFov-Mathf.Clamp(original.cameraFov+(fovUp?5:-5),45,85))<.01f&&Mathf.Abs(s.moveSpeed-Mathf.Clamp(original.moveSpeed+(speedUp?.1f:-.1f),.3f,2))<.001f);
            check("Flat camera applies saved FOV",!app.Input.VrMode&&!app.Input.SimulatorMode&&Mathf.Abs(app.View.fieldOfView-s.cameraFov)<.01f);

            yield return Press(app,"Home utility 0","Cài đặt","Settings");yield return Press(app,"Điều khiển","Controls");yield return Press(app,"Controls button mapping");
            yield return Press(app,"Controller physical 5");yield return Press(app,"Controller assign Select");
            check("Controller diagram remap returns to physical view",app.GetComponentsInChildren<ControllerDiagramState>().Any(x=>x.isActiveAndEnabled));capture?.Invoke("patch1-controller-remapped");
            yield return Press(app,"Quay lại cài đặt","Back to Settings");yield return Press(app,"Settings apply","Áp dụng","Apply");loaded=Reload(app);
            bool valid=true;try{ControllerBindings.Validate(loaded.profile.settings);}catch(InvalidDataException){valid=false;}
            check("Controller UI assignment survives Apply/reopen with valid complete mapping",!loaded.writeProtected&&valid&&loaded.profile.settings.controllerSelect==5);

            yield return Press(app,"Home utility 2","Giới thiệu","About");
            foreach(string document in new[]{"Chính sách bảo mật","Điều khoản"})
            {
                yield return Press(app,document);var scroll=app.GetComponentsInChildren<ScrollRect>().FirstOrDefault(x=>x.isActiveAndEnabled);
                var text=scroll?scroll.content.GetComponentInChildren<TextMeshProUGUI>():null;
                check("Legal document has full readable offline scroll: "+document,app.Route=="legal"&&scroll&&text&&text.text.Length>250&&text.fontSize>=25);
                if(scroll)scroll.verticalNormalizedPosition=0;capture?.Invoke(document=="Điều khoản"?"patch1-terms-readable":"patch1-privacy-readable");
                yield return Press(app,"Quay lại","Back");check("Legal Back returns to About without consent mutation",app.Route=="about"&&Legal(app.Profile)==legal);
            }
            yield return Press(app,"Phản hồi","Feedback");yield return Press(app,"Copy feedback email");
            check("Feedback Copy uses actual clipboard callback",GUIUtility.systemCopyBuffer=="nguamanvlab@gmail.com");capture?.Invoke("patch1-feedback");
            yield return Press(app,"‹ Quay lại","‹ Back");
            yield return Press(app,"Subject P");yield return new WaitForSecondsRealtime(.45f);
            yield return app.CheckRoomLoad("Physics",true,false);yield return new WaitForSecondsRealtime(.35f);
            check("P01 real-room load reaches manual setup",app.Route=="activity"&&app.Session?.spec.id=="P01"&&app.Apparatus&&!app.Apparatus.Ready);
            if(app.Route!="activity"||app.Session?.spec.id!="P01")yield break;
            var rig=app.Apparatus;int operation=app.Session.Operation;double elapsed=app.Session.Elapsed;
            yield return Equipment(app);yield return Press(app,"Equipment item clamp");yield return Press(app,"Equipment take selected");
            check("Taking equipment through UI closes dialog and retains selected item",!Dialog(app)&&rig.IsCarrying&&rig.Selected?.assetId=="clamp");
            rig.UpdateGrab(new ActionFrame{pointer=app.View.ViewportPointToRay(new Vector3(.58f,.48f,0)),released=true});yield return null;
            check("Take UI plus release input retains item until explicit Place/Return",rig.IsCarrying&&rig.Selected?.assetId=="clamp");
            yield return Press(app,"Đặt dụng cụ","Place equipment");
            check("HUD refuses unsupported clamp without silently fitting or losing held item",!rig.items.Single(x=>x.assetId=="clamp").installed&&rig.IsCarrying&&Dialog(app));
            yield return Press(app,"Close dialog");check("Universal close exits refusal and returns held item safely",!Dialog(app)&&!rig.IsCarrying&&rig.Selected==null);
            yield return Equipment(app);yield return Press(app,"Equipment item stand");yield return Press(app,"Equipment take selected");yield return Press(app,"Đặt dụng cụ","Place equipment");
            check("HUD explicit placement fits selected stand",rig.items.Single(x=>x.assetId=="stand").installed&&!rig.IsCarrying&&!Dialog(app));
            yield return Equipment(app);yield return Press(app,"Equipment item ruler");yield return Press(app,"Equipment take selected");
            yield return Equipment(app);yield return Press(app,"Equipment return selected");
            var ruler=rig.items.Single(x=>x.assetId=="ruler");check("Equipment Return callback restores supported loose tray pose",!rig.IsCarrying&&rig.Selected==null&&!ruler.installed&&Vector3.Distance(ruler.transform.position,ruler.trayPosition)<.001f);
            yield return Press(app,"Close dialog");
            yield return Press(app,"Dụng cụ & hướng dẫn","Station tools");yield return Press(app,"Hướng dẫn","Guidance");
            if(app.Profile.settings.hints)yield return PressPrefix(app,"Gợi ý:");yield return Press(app,"Close dialog");
            check("Hints Off clears automatic snap cues and existing help visuals",!app.Profile.settings.hints&&!rig.CoachingEnabled&&!rig.GetComponentsInChildren<LineRenderer>(true).Any(x=>x.name.StartsWith("Snap target",StringComparison.Ordinal)&&x.enabled)&&!app.GetComponentsInChildren<GuideCue>().Any(x=>x.isActiveAndEnabled));
            yield return Press(app,"Focus workstation");
            var target=rig.Worktop+Vector3.up*.3f;check("Focus button aims flat-screen learner down toward working table",Vector3.Dot(app.View.transform.forward,(target-app.View.transform.position).normalized)>.9f);capture?.Invoke("patch1-manual-setup-focused");
            screen=Touchscreen.current;
            if(screen==null){ownedTouchscreen=InputSystem.AddDevice<Touchscreen>();screen=ownedTouchscreen;}
            yield return null;yield return null;Canvas.ForceUpdateCanvases();
            var viewport=app.GetComponentsInChildren<PhoneUiViewport>().Single(x=>x.isActiveAndEnabled);
            var safe=viewport.SafePixels;
            check("Phone joystick is enabled in actual flat-screen gameplay with no modal",app.Input.Gameplay&&app.Input.PhoneControlsVisible&&!Dialog(app));
            var ring=app.GetComponentsInChildren<RectTransform>().SingleOrDefault(r=>r.gameObject.activeInHierarchy&&r.name=="Phone movement joystick");
            check("Actual phone joystick renderer is active",ring);
            if(ring)
            {
                var bounds=ScreenBounds(ring);
                check("Rendered joystick radius matches input claim radius",Mathf.Abs(bounds.width*.5f-safe.height*app.Input.PhoneJoystickRadius)<1.1f&&Mathf.Abs(bounds.height*.5f-safe.height*app.Input.PhoneJoystickRadius)<1.1f);
                check("Entire rendered joystick fits actual safe screen",bounds.xMin>=safe.xMin-.5f&&bounds.yMin>=safe.yMin-.5f&&bounds.xMax<=safe.xMax+.5f&&bounds.yMax<=safe.yMax+.5f);
                var hud=app.GetComponentsInChildren<RectTransform>().Single(r=>r.gameObject.activeInHierarchy&&r.name=="Current laboratory action");
                check("Phone joystick does not cover current action or clock panel",!Overlaps(bounds,ScreenBounds(hud)));
                string overlap=string.Join(", ",app.GetComponentsInChildren<Button>().Where(b=>b.isActiveAndEnabled&&Overlaps(bounds,ScreenBounds((RectTransform)b.transform))).Select(b=>b.name));
                check("Phone joystick does not cover any active action button: "+overlap,string.IsNullOrEmpty(overlap));
            }
            var configured=app.Profile.settings.Copy();var joystickView=app.GetComponentInChildren<PhoneJoystickView>();
            try
            {
                foreach(float scale in new[]{.75f,1f,1.25f})
                {
                    var sized=configured.Copy();sized.joystickScale=scale;app.Input.ConfigurePhoneInput(sized);
                    joystickView.Refresh();Canvas.ForceUpdateCanvases();
                    var sizedBounds=ScreenBounds(ring);float expected=safe.height*.14f*scale;
                    check("Rendered joystick radius matches size setting "+scale,Mathf.Abs(sizedBounds.width*.5f-expected)<1.1f&&Mathf.Abs(sizedBounds.height*.5f-expected)<1.1f);
                    check("Sized joystick remains within safe pixels "+scale,sizedBounds.xMin>=safe.xMin-.5f&&sizedBounds.yMin>=safe.yMin-.5f&&sizedBounds.xMax<=safe.xMax+.5f&&sizedBounds.yMax<=safe.yMax+.5f);
                    var action=app.GetComponentsInChildren<RectTransform>().Single(r=>r.gameObject.activeInHierarchy&&r.name=="Current laboratory action");
                    check("Sized joystick clears HUD and all active buttons "+scale,!Overlaps(sizedBounds,ScreenBounds(action))&&!app.GetComponentsInChildren<Button>().Any(b=>b.isActiveAndEnabled&&Overlaps(sizedBounds,ScreenBounds((RectTransform)b.transform))));
                    var sizedCentre=new Vector2(safe.x+safe.width*app.Input.PhoneJoystickCenter.x,safe.y+safe.height*app.Input.PhoneJoystickCenter.y);
                    InputSystem.QueueStateEvent(screen,new TouchState{touchId=99001,phase=UnityEngine.InputSystem.TouchPhase.Began,position=sizedCentre});yield return null;
                    touchEnd=sizedCentre-Vector2.up*expected;
                    InputSystem.QueueStateEvent(screen,new TouchState{touchId=99001,phase=UnityEngine.InputSystem.TouchPhase.Moved,position=touchEnd});yield return null;
                    joystickView.Refresh();Canvas.ForceUpdateCanvases();
                    var thumbBounds=ScreenBounds((RectTransform)ring.Find("Phone joystick thumb"));
                    check("Full-down joystick input and visible knob stay usable "+scale,app.Input.PhoneJoystickOffset.y<-.99f&&thumbBounds.yMin>=sizedBounds.yMin-.5f&&thumbBounds.xMin>=sizedBounds.xMin-.5f&&thumbBounds.xMax<=sizedBounds.xMax+.5f&&thumbBounds.yMax<=sizedBounds.yMax+.5f);
                    InputSystem.QueueStateEvent(screen,new TouchState{touchId=99001,phase=UnityEngine.InputSystem.TouchPhase.Ended,position=touchEnd});yield return null;
                }
            }
            finally{app.Input.ConfigurePhoneInput(configured);joystickView.Refresh();}
            var centre=app.Input.PhoneJoystickCenter;float radius=safe.height*app.Input.PhoneJoystickRadius;
            var start=new Vector2(safe.x+safe.width*centre.x,safe.y+safe.height*centre.y);touchEnd=start+Vector2.right*(radius*.5f);
            var origin=app.View.transform.parent;Vector3 beforeMove=origin.position;
            check("Learner origin remains level for tracked VR axes",Vector3.Dot(origin.up,Vector3.up)>.9999f);
            InputSystem.QueueStateEvent(screen,new TouchState{touchId=99001,phase=UnityEngine.InputSystem.TouchPhase.Began,position=start});yield return null;
            InputSystem.QueueStateEvent(screen,new TouchState{touchId=99001,phase=UnityEngine.InputSystem.TouchPhase.Moved,position=touchEnd});yield return new WaitForSecondsRealtime(.15f);
            InputSystem.QueueStateEvent(screen,new TouchState{touchId=99001,phase=UnityEngine.InputSystem.TouchPhase.Ended,position=touchEnd});yield return null;yield return null;
            Vector3 moved=origin.position-beforeMove;
            check("Actual phone joystick moves learner through App locomotion without vertical drift",new Vector2(moved.x,moved.z).magnitude>.005f&&Mathf.Abs(moved.y)<.0005f);
            yield return Press(app,"Focus workstation");yield return null;yield return null;Canvas.ForceUpdateCanvases();capture?.Invoke("patch1-phone-joystick-composite");
            check("UI handling and optional guidance do not fabricate preparation/time/results",app.Session.Operation==operation&&app.Session.Elapsed==elapsed&&app.Session.Recorded==null);
            loaded=Reload(app);check("Settings/legal/setup callbacks retain old attempts, notes and legal bytes",!loaded.writeProtected&&Attempts(loaded.profile)==attempts&&Notes(loaded.profile)==notes&&Legal(loaded.profile)==legal);
        }
        finally
        {
            if(screen!=null&&screen.added)InputSystem.QueueStateEvent(screen,new TouchState{touchId=99001,phase=UnityEngine.InputSystem.TouchPhase.Ended,position=touchEnd});
            if(ownedTouchscreen!=null&&ownedTouchscreen.added)InputSystem.RemoveDevice(ownedTouchscreen);
            GUIUtility.systemCopyBuffer=clipboard;
        }
    }
}
#endif
