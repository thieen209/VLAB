#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Linq;
using Vlab.Product;

// Synthetic Input System states and deterministic coordinate/lifecycle checks.
// This is not a physical Android touch, gyroscope or Cardboard certification.
public static class PhoneInputChecks
{
    static readonly List<string> rows=new List<string>();
    static bool PhonePress(ActionFrame frame){var field=typeof(ActionFrame).GetField("phoneWorldPress");return field!=null&&(bool)field.GetValue(frame);}
    static void Check(bool ok,string name){if(!ok)throw new Exception(name);rows.Add("PASS "+name);}
    static Vector2 At(Rect r,float x,float y)=>new Vector2(r.x+r.width*x,r.y+r.height*y);
    static float Yaw(Quaternion q)=>Mathf.DeltaAngle(0,q.eulerAngles.y);
    static float Pitch(Quaternion q)=>Mathf.DeltaAngle(0,q.eulerAngles.x);
    static void Touch(Touchscreen screen,int id,UnityEngine.InputSystem.TouchPhase phase,Vector2 position)
    {InputSystem.QueueStateEvent(screen,new TouchState{touchId=id,phase=phase,position=position});}
    static void Send(Touchscreen screen,int id,UnityEngine.InputSystem.TouchPhase phase,Vector2 position)
    {Touch(screen,id,phase,position);InputSystem.Update();}
    static void Sensor(AttitudeSensor sensor,Quaternion raw)
    {InputSystem.QueueDeltaStateEvent(sensor.attitude,raw);InputSystem.Update();}
    static void Pure()
    {
        var normal=PhoneLookMath.Drag(Quaternion.identity,new Vector2(100,100),new Vector2(1000,1000),1,false,false);
        Check(Yaw(normal)>0&&Pitch(normal)<0,"swipe right looks right; swipe up looks up, matching desktop");
        var x=PhoneLookMath.Drag(Quaternion.identity,new Vector2(100,100),new Vector2(1000,1000),1,true,false);
        var y=PhoneLookMath.Drag(Quaternion.identity,new Vector2(100,100),new Vector2(1000,1000),1,false,true);
        Check(Mathf.Abs(Yaw(x)+Yaw(normal))<.001f&&Mathf.Abs(Pitch(x)-Pitch(normal))<.001f,"invert X affects yaw only");
        Check(Mathf.Abs(Pitch(y)+Pitch(normal))<.001f&&Mathf.Abs(Yaw(y)-Yaw(normal))<.001f,"invert Y affects pitch only");
        var fast=PhoneLookMath.Drag(Quaternion.identity,new Vector2(100,100),new Vector2(1000,1000),2,false,false);
        Check(Mathf.Abs(Yaw(fast)-2*Yaw(normal))<.001f&&Mathf.Abs(Pitch(fast)-2*Pitch(normal))<.001f,"sensitivity scales both swipe axes");
        Check(Mathf.Abs(Pitch(PhoneLookMath.Drag(Quaternion.identity,new Vector2(0,10000),Vector2.one,2,false,false))+70)<.001f,"pitch remains within comfort bounds");
        Check(!PhoneLookMath.TryDeviceRotation(default,ScreenOrientation.Portrait,out _)&&!PhoneLookMath.TryDeviceRotation(new Quaternion(float.NaN,0,0,1),ScreenOrientation.Portrait,out _),"invalid gyro quaternions rejected");
        var raw=Quaternion.Euler(21,37,13);
        PhoneLookMath.TryDeviceRotation(raw,ScreenOrientation.Portrait,out var portrait);
        PhoneLookMath.TryDeviceRotation(raw*Quaternion.Euler(0,0,90),ScreenOrientation.LandscapeLeft,out var left);
        PhoneLookMath.TryDeviceRotation(raw*Quaternion.Euler(0,0,-90),ScreenOrientation.LandscapeRight,out var right);
        Check(Quaternion.Angle(portrait,left)<.01f&&Quaternion.Angle(portrait,right)<.01f,"both landscape sensor bases compensate exactly once");
        var view=Quaternion.Euler(10,45,0);
        Check(Quaternion.Angle(view,PhoneLookMath.RelativeGyro(view,portrait,portrait,1,false,false))<.01f,"gyro calibration preserves current world heading/pitch without a jump");
        var turned=PhoneLookMath.RelativeGyro(view,portrait,portrait*Quaternion.Euler(0,15,0),1,false,false);
        Check(Mathf.Abs(Yaw(turned)-60)<.01f&&Mathf.Abs(Pitch(turned)-10)<.01f,"relative gyro yaw uses calibrated screen coordinates");
        var inverted=PhoneLookMath.RelativeGyro(view,portrait,portrait*Quaternion.Euler(0,15,0),2,true,false);
        Check(Mathf.Abs(Yaw(inverted)-15)<.01f,"gyro respects invert X and common sensitivity");
        var router=new PhoneTouchRouter();var safe=new Rect(25,10,1000,600);
        router.BeginFrame();router.Sample(1,UnityEngine.InputSystem.TouchPhase.Began,At(safe,.2f,.25f),safe,true,true,true,false);router.EndFrame();
        Check(!router.JoystickActive&&!router.WorldHeld&&!router.LookActive,"UI-started touch takes priority over joystick region");
        router.BeginFrame();router.Sample(1,UnityEngine.InputSystem.TouchPhase.Moved,At(safe,.85f,.6f),safe,true,true,false,false);router.EndFrame();
        Check(router.Move==Vector2.zero&&router.LookDelta==Vector2.zero&&!router.Pressed,"UI-started finger cannot become look/move after leaving button");
        router.Clear();router.BeginFrame();router.Sample(2,UnityEngine.InputSystem.TouchPhase.Began,At(safe,PhoneTouchRouter.JoystickCenter.x,PhoneTouchRouter.JoystickCenter.y),safe,true,true,false,false);router.Sample(3,UnityEngine.InputSystem.TouchPhase.Began,At(safe,.7f,.55f),safe,true,true,false,false);router.EndFrame();
        router.BeginFrame();router.Sample(2,UnityEngine.InputSystem.TouchPhase.Moved,At(safe,.16f,.4f),safe,true,true,false,false);router.Sample(3,UnityEngine.InputSystem.TouchPhase.Moved,At(safe,.8f,.65f),safe,true,true,false,false);router.EndFrame();
        Check(router.Move.y>.9f&&router.LookDelta.x>0&&router.LookDelta.y>0&&!router.Pressed&&!router.WorldHeld,"two fingers move and look together without selecting world objects");
        router.BeginFrame();router.Sample(2,UnityEngine.InputSystem.TouchPhase.Canceled,Vector2.zero,safe,true,true,false,false);router.Sample(3,UnityEngine.InputSystem.TouchPhase.Stationary,At(safe,.8f,.65f),safe,true,true,false,false);router.EndFrame();
        Check(router.Move==Vector2.zero&&router.LookActive,"joystick cancellation stops movement while other finger retains look");
        router.Clear();router.BeginFrame();router.Sample(4,UnityEngine.InputSystem.TouchPhase.Began,At(safe,.65f,.5f),safe,true,true,false,true);router.EndFrame();
        Check(router.Pressed&&router.WorldHeld,"equipment-started touch emits world press and hold");
        var final=At(safe,.8f,.6f);router.BeginFrame();router.Sample(4,UnityEngine.InputSystem.TouchPhase.Moved,final,safe,true,true,true,true);router.EndFrame();
        Check(router.WorldHeld&&!router.Pressed&&router.LookDelta==Vector2.zero,"equipment drag retains owner across UI and never rotates camera");
        router.BeginFrame();router.Sample(4,UnityEngine.InputSystem.TouchPhase.Canceled,Vector2.zero,safe,true,true,false,false);router.EndFrame();
        Check(router.Released&&!router.WorldHeld&&router.WorldPosition==final,"canceled equipment touch releases at final real pointer position");
        router.Clear();router.BeginFrame();router.Sample(5,UnityEngine.InputSystem.TouchPhase.Stationary,final,safe,true,true,false,false);router.EndFrame();
        Check(!router.JoystickActive&&!router.LookActive&&!router.WorldHeld,"already-held finger is not acquired after page/mode reset");
        router.Clear();Check(!router.HasTouches&&router.Move==Vector2.zero,"reset clears all touch and movement ownership");
        var centre=At(safe,PhoneTouchRouter.JoystickCenter.x,PhoneTouchRouter.JoystickCenter.y);
        float radius=safe.height*PhoneTouchRouter.JoystickRadius;
        Check(Mathf.Abs(radius-safe.height*.14f)<.01f,"larger joystick radius matches 14% safe height; radius="+radius+", safeHeight="+safe.height);
        Check(centre.x-radius>=safe.x-.01f&&centre.x+radius<=safe.xMax+.01f&&centre.y-radius>=safe.y-.01f&&centre.y+radius<=safe.yMax+.01f,
            "larger joystick stays inside safe area; centre="+centre+", radius="+radius+", safe="+safe);
        router.BeginFrame();router.Sample(6,UnityEngine.InputSystem.TouchPhase.Began,At(safe,.31f,.4f),safe,true,true,false,false);router.EndFrame();
        Check(!router.JoystickActive&&router.Move==Vector2.zero&&router.LookActive,"lower-left swipe outside visible joystick never becomes unintended movement");
        router.Clear();router.BeginFrame();router.Sample(7,UnityEngine.InputSystem.TouchPhase.Began,At(safe,.31f,.4f),safe,true,true,false,true);router.EndFrame();
        Check(router.Pressed&&router.WorldHeld&&!router.JoystickActive,"equipment outside joystick disc remains selectable in lower-left quadrant");
        foreach(var direction in new[]{Vector2.up,Vector2.down,Vector2.left,Vector2.right})
        {
            router.Clear();router.BeginFrame();router.Sample(8,UnityEngine.InputSystem.TouchPhase.Began,centre,safe,true,true,false,false);router.EndFrame();
            Check(router.JoystickActive&&router.Move==Vector2.zero,"joystick centre begins at rest "+direction);
            router.BeginFrame();router.Sample(8,UnityEngine.InputSystem.TouchPhase.Moved,centre+direction*radius*2,safe,true,true,false,false);router.EndFrame();
            Check(Vector2.Distance(router.Move,direction)<.001f&&!router.LookActive&&!router.Pressed,"joystick retains direction and ownership outside disc "+direction);
            router.BeginFrame();router.EndFrame();Check(router.Move==Vector2.zero&&!router.HasTouches,"missing joystick finger releases movement "+direction);
        }
        router.Clear();router.BeginFrame();router.Sample(9,UnityEngine.InputSystem.TouchPhase.Began,centre+Vector2.up*radius*.05f,safe,true,true,false,false);router.EndFrame();
        Check(router.JoystickActive&&router.Move==Vector2.zero,"joystick dead zone suppresses slight initial finger offset");
        router.Clear();router.BeginFrame();router.Sample(10,UnityEngine.InputSystem.TouchPhase.Began,centre,safe,true,false,false,false);router.EndFrame();
        Check(!router.JoystickActive&&router.Move==Vector2.zero&&router.LookActive,"hidden joystick releases its area for normal camera swipes");
    }
    public static IEnumerator RunUiTap(VlabApp app,Action<string> capture,Action<string,bool> check)
    {
        var log=new List<string>();bool passed=false;Touchscreen owned=null,previous=Touchscreen.current;Vector2 last=default;
        string profile=JsonUtility.ToJson(app.Profile);
        void Verify(bool ok,string name){log.Add((ok?"PASS ":"FAIL ")+name);check(name,ok);if(!ok)throw new Exception(name);}
        Vector2 Position(Button button)
        {
            Canvas.ForceUpdateCanvases();var rect=(RectTransform)button.transform;var canvas=button.GetComponentInParent<Canvas>();
            return RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,rect.TransformPoint(rect.rect.center));
        }
        IEnumerator Tap(Button button)
        {
            last=Position(button);var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=last},hits);
            Verify(hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==button,"Native queued tap ray reaches actual "+button.name);
            Touch(owned,99011,UnityEngine.InputSystem.TouchPhase.Began,last);yield return null;yield return null;
            Touch(owned,99011,UnityEngine.InputSystem.TouchPhase.Ended,last);yield return null;yield return null;yield return new WaitForSecondsRealtime(.32f);
        }
        try
        {
            app.ShowHome();yield return new WaitForSecondsRealtime(.4f);
            owned=InputSystem.AddDevice<Touchscreen>();yield return null;yield return null;
            var module=EventSystem.current?EventSystem.current.GetComponent<InputSystemUIInputModule>():null;
            Verify(module&&module.enabled&&module.point?.action?.enabled==true&&module.leftClick?.action?.enabled==true,"Actual flat UGUI pointer and click actions are enabled");
            var settings=app.GetComponentsInChildren<Button>().Single(b=>b.isActiveAndEnabled&&b.name=="Home utility 0");
            yield return Tap(settings);Verify(app.Route=="settings","Queued touchscreen DOWN/UP opens actual Settings through InputSystemUIInputModule");
            capture?.Invoke("patch1-native-touch-settings");
            var back=app.GetComponentsInChildren<Button>().Single(b=>b.isActiveAndEnabled&&b.name=="Settings back");
            yield return Tap(back);Verify(app.Route=="home","Queued touchscreen DOWN/UP on actual Back returns Home");
            Verify(JsonUtility.ToJson(app.Profile)==profile,"Native touch UI navigation preserves learner data and settings");passed=true;
        }
        finally
        {
            if(owned!=null&&owned.added){Touch(owned,99011,UnityEngine.InputSystem.TouchPhase.Ended,last);InputSystem.RemoveDevice(owned);}
            if(previous!=null&&previous.added)previous.MakeCurrent();
            Directory.CreateDirectory("Evidence/Input");File.WriteAllLines("Evidence/Input/phone-touch-ui.txt",new[]{passed?"PASS":"FAILED","Synthetic touchscreen events through actual UGUI module; no physical-device certification."}.Concat(log));
        }
    }
    sealed class Viewer:ICardboardEntryRuntime
    {
        public bool Supported=>true;public bool Initialized{get;private set;}=true;
        public IEnumerator Initialize(){Initialized=true;yield break;}public bool Start()=>true;
        public void StopAndDeinitialize(){Initialized=false;}public void Scan(){}public void TickParameters(){}
    }
    public static void Run()
    {
        if(!Application.isPlaying)throw new InvalidOperationException("Phone checks require Play mode.");
        rows.Clear();GameObject go=null;Touchscreen touch=null;AttitudeSensor sensor=null;bool passed=false;
        try
        {
            Pure();go=new GameObject("PATCH1 isolated phone input");var camera=go.AddComponent<Camera>();camera.enabled=false;
            var input=go.AddComponent<VlabInput>();input.Adopt(camera);input.Gameplay=true;
            var settings=new GameSettings();input.ConfigurePhoneInput(settings);input.BlocksPhoneGameplay=p=>false;input.BlocksTouchLook=r=>false;
            touch=InputSystem.AddDevice<Touchscreen>();InputSystem.Update();input.Read();
            var safe=Screen.safeArea;if(safe.width<2||safe.height<2)safe=new Rect(0,0,Screen.width,Screen.height);
            var stick=At(safe,PhoneTouchRouter.JoystickCenter.x,PhoneTouchRouter.JoystickCenter.y);var look=At(safe,.7f,.55f);
            Touch(touch,11,UnityEngine.InputSystem.TouchPhase.Began,stick);Touch(touch,12,UnityEngine.InputSystem.TouchPhase.Began,look);InputSystem.Update();input.Read();
            Touch(touch,11,UnityEngine.InputSystem.TouchPhase.Moved,stick+Vector2.up*(safe.height*PhoneTouchRouter.JoystickRadius*1.1f));Touch(touch,12,UnityEngine.InputSystem.TouchPhase.Moved,look+new Vector2(safe.width*.08f,safe.height*.08f));InputSystem.Update();var frame=input.Read();
            Check(frame.move.y>.9f&&Yaw(camera.transform.rotation)>0&&Pitch(camera.transform.rotation)<0&&!frame.pressed&&!frame.held&&!PhonePress(frame),"native Input System multitouch joystick and correctly directed look are simultaneous");
            input.SetUiBlocked(true);frame=input.Read();Check(frame.move==Vector2.zero&&!input.PhoneControlsVisible,"modal blocks movement and hides phone controls");
            var rotation=camera.transform.rotation;input.SetUiBlocked(false);frame=input.Read();Check(frame.move==Vector2.zero&&Quaternion.Angle(rotation,camera.transform.rotation)<.001f,"closing modal does not adopt fingers that remain held");
            Touch(touch,11,UnityEngine.InputSystem.TouchPhase.Ended,stick);Touch(touch,12,UnityEngine.InputSystem.TouchPhase.Ended,look);InputSystem.Update();input.Read();
            input.BlocksPhoneGameplay=p=>true;Send(touch,13,UnityEngine.InputSystem.TouchPhase.Began,look);frame=input.Read();
            Check(!frame.pressed&&!frame.held&&!PhonePress(frame)&&frame.move==Vector2.zero,"UI touch remains exclusively with native UI input module");
            input.BlocksPhoneGameplay=p=>false;Send(touch,13,UnityEngine.InputSystem.TouchPhase.Moved,look+Vector2.right*100);input.Read();
            Check(Quaternion.Angle(rotation,camera.transform.rotation)<.001f,"native UI-started drag never rotates camera after leaving control");
            Send(touch,13,UnityEngine.InputSystem.TouchPhase.Ended,look);input.Read();input.BlocksTouchLook=r=>true;
            Send(touch,14,UnityEngine.InputSystem.TouchPhase.Began,look);frame=input.Read();Check(frame.pressed&&frame.held&&PhonePress(frame),"native equipment touch marks only actual world press edge");
            Send(touch,14,UnityEngine.InputSystem.TouchPhase.Moved,look+Vector2.right*2);frame=input.Read();Check(frame.held&&!frame.pressed&&!PhonePress(frame),"held equipment finger does not repeat the world press marker");
            input.SetUiBlocked(true);frame=input.Read();Check(frame.lost&&frame.released&&!frame.held&&!PhonePress(frame),"modal opening releases held apparatus exactly once");
            input.SetUiBlocked(false);Send(touch,14,UnityEngine.InputSystem.TouchPhase.Ended,look);input.Read();input.BlocksTouchLook=r=>false;
            input.BlocksTouchLook=r=>true;go.SendMessage("OnApplicationFocus",false);Send(touch,16,UnityEngine.InputSystem.TouchPhase.Began,look);frame=input.Read();
            Check(frame.lost&&!frame.pressed&&!frame.held&&!PhonePress(frame),"pending input loss cancels even a fresh world-touch press marker");
            Send(touch,16,UnityEngine.InputSystem.TouchPhase.Ended,look);input.Read();input.BlocksTouchLook=r=>false;
            var mock=new MockControllerProvider();mock.Submit(new ControllerSignal{sequence=1,receivedAt=10,orientation=Quaternion.identity,select=true});var mockFrame=mock.Poll(camera,10);Check(mockFrame.pressed&&!PhonePress(mockFrame),"controller select never claims phone world-touch origin");
            sensor=InputSystem.AddDevice<AttitudeSensor>();InputSystem.EnableDevice(sensor);settings.phoneGyro=true;input.ConfigurePhoneInput(settings);
            Sensor(sensor,Quaternion.Euler(20,30,5));input.Read();rotation=camera.transform.rotation;
            Sensor(sensor,Quaternion.Euler(20,45,5));input.Read();Check(Quaternion.Angle(rotation,camera.transform.rotation)>.5f,"native attitude sensor drives opt-in flat camera");
            input.Recenter();rotation=camera.transform.rotation;input.Read();Check(Quaternion.Angle(rotation,camera.transform.rotation)<.001f,"recenter zeros gyro at current view without snapping");
            typeof(VlabInput).GetField("phoneWidth",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(input,-1);
            input.Read();Check(Quaternion.Angle(rotation,camera.transform.rotation)<.001f,"orientation/size transition rebases gyro without camera jump");
            input.FocusAtWorldTarget(camera.transform.position+new Vector3(1,.2f,2));rotation=camera.transform.rotation;input.Read();
            Check(Quaternion.Angle(rotation,camera.transform.rotation)<.001f,"station focus rebases gyro so cached pose cannot snap back");
            input.EntryRuntimeForChecks=new Viewer();input.SetVr(true);Check(input.VrMode,"synthetic Cardboard lifecycle entered stereo");
            Sensor(sensor,Quaternion.Euler(50,80,30));Send(touch,15,UnityEngine.InputSystem.TouchPhase.Began,look);rotation=camera.transform.rotation;frame=input.Read();
            Check(frame.move==Vector2.zero&&!frame.pressed&&!frame.held&&!input.PhoneControlsVisible&&!input.PhoneGyroActive&&Quaternion.Angle(rotation,camera.transform.rotation)<.001f,"Cardboard ignores raw phone touch, joystick and custom gyro pose");
            input.FocusAtWorldTarget(Vector3.right*20);Check(Quaternion.Angle(rotation,camera.transform.rotation)<.001f,"station focus does not overwrite Cardboard tracked head rotation");
            input.SetVr(false);input.Read();rotation=camera.transform.rotation;input.Read();Check(!input.VrMode&&Quaternion.Angle(rotation,camera.transform.rotation)<.001f,"return from Cardboard rebases flat gyro safely");
            Send(touch,15,UnityEngine.InputSystem.TouchPhase.Ended,look);input.Read();
            settings.phoneGyro=false;input.ConfigurePhoneInput(settings);input.Read();
            var outsideStick=At(safe,.31f,.4f);rotation=camera.transform.rotation;
            Send(touch,17,UnityEngine.InputSystem.TouchPhase.Began,outsideStick);frame=input.Read();
            Check(frame.move==Vector2.zero&&!input.PhoneJoystickActive&&!frame.pressed,"native lower-left touch outside joystick does not move or select");
            Send(touch,17,UnityEngine.InputSystem.TouchPhase.Moved,outsideStick+Vector2.right*(safe.width*.08f));frame=input.Read();
            Check(frame.move==Vector2.zero&&Quaternion.Angle(rotation,camera.transform.rotation)>.5f,"native lower-left swipe outside joystick rotates view without unintended walking");
            Send(touch,17,UnityEngine.InputSystem.TouchPhase.Ended,outsideStick);input.Read();
            go.SendMessage("OnApplicationPause",true);frame=input.Read();Check(frame.move==Vector2.zero&&frame.lost&&!frame.held,"pause cancels phone input without retained movement");
            passed=true;
        }
        catch(Exception ex){rows.Add("FAIL "+ex);throw;}
        finally
        {
            if(go)UnityEngine.Object.DestroyImmediate(go);if(sensor!=null&&sensor.added)InputSystem.RemoveDevice(sensor);if(touch!=null&&touch.added)InputSystem.RemoveDevice(touch);
            Directory.CreateDirectory("Evidence/Input");File.WriteAllLines("Evidence/Input/phone-input.txt",rows);Debug.Log("PATCH1 PHONE INPUT "+(passed?"PASS "+rows.Count:"FAIL"));
        }
    }
}
#endif
