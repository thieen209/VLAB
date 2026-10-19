using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.Management;
#if UNITY_EDITOR
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
#endif

namespace Vlab.Product
{
    [Flags] public enum ControllerCapabilities { None=0,Orientation=1,Select=2,Grip=4,Adjustment=8,Menu=16,Pointer=32,Look=64,Back=128 }
    [Serializable] public struct ControllerSignal
    {
        public long sequence;public double receivedAt;
        public Quaternion orientation;public bool select,grip,menu;
        public float adjustment;
    }
    public struct ActionFrame
    {public Ray pointer;public bool pressed,released,held,grabPressed,menu,recenter,back,lost;public float adjustment;}
    public interface IVlabInputProvider {ActionFrame Poll(Camera camera,double now);ControllerCapabilities Capabilities{get;}}
    public sealed class MockControllerProvider:IVlabInputProvider
    {
        public ControllerCapabilities Capabilities{get;private set;}=ControllerCapabilities.Orientation|ControllerCapabilities.Select|ControllerCapabilities.Grip|ControllerCapabilities.Adjustment|ControllerCapabilities.Menu;
        ControllerSignal signal;bool received,lastSelect,lastGrip,lastMenu,wasConnected;Quaternion calibration=Quaternion.identity;
        public bool Submit(ControllerSignal value)
        {
            var q=value.orientation;float norm=q.x*q.x+q.y*q.y+q.z*q.z+q.w*q.w;
            if(value.sequence<0||(received&&value.sequence<=signal.sequence)||!double.IsFinite(value.receivedAt)||!float.IsFinite(norm)||norm<.25f||norm>4||!float.IsFinite(value.adjustment)||Math.Abs(value.adjustment)>1)return false;
            value.orientation=Quaternion.Normalize(q);signal=value;received=true;return true;
        }
        public void Calibrate(){if(received)calibration=Quaternion.Inverse(signal.orientation);}
        public void Calibrate(Quaternion forward){if(received)calibration=forward*Quaternion.Inverse(signal.orientation);}
        public ActionFrame Poll(Camera camera,double now)
        {
            bool connected=received&&now>=signal.receivedAt&&now-signal.receivedAt<=.35;
            bool select=connected&&signal.select,grip=connected&&signal.grip,menu=connected&&signal.menu;
            var orientation=received?signal.orientation:Quaternion.identity;
            var f=new ActionFrame{pointer=new Ray(camera.transform.position,calibration*orientation*Vector3.forward),held=select||grip,pressed=select&&!lastSelect,grabPressed=grip&&!lastGrip,released=!select&&!grip&&(lastSelect||lastGrip),menu=menu&&!lastMenu,lost=wasConnected&&!connected,adjustment=connected?signal.adjustment:0};
            lastSelect=select;lastGrip=grip;lastMenu=menu;wasConnected=connected;return f;
        }
    }
    // Standard OS-paired Bluetooth/USB HID gamepads are read by Input System.
    // This deliberately defines no ESP32/BLE packet format or transport claim.
    public sealed class GamepadControllerProvider:IVlabInputProvider
    {
        public Gamepad Device{get;}
        public ControllerCapabilities Capabilities=>ControllerCapabilities.Select|ControllerCapabilities.Grip|ControllerCapabilities.Adjustment|ControllerCapabilities.Menu|ControllerCapabilities.Pointer|ControllerCapabilities.Look|ControllerCapabilities.Back;
        public Vector2 CursorNormalized{get;private set;}=new Vector2(.5f,.5f);
        bool lastSelect,lastGrip,lastMenu,lastRecenter,lastBack,connected,suppressHeld,pendingRelease;
        GameSettings bindings=new GameSettings();
        double lastTime=-1,nextAdjustment;int lastDirection;
        public GamepadControllerProvider(Gamepad device,GameSettings configuration=null){Device=device;if(configuration!=null){ControllerBindings.Validate(configuration);bindings=configuration.Copy();}}
        public void ConfigureBindings(GameSettings configuration)
        {ControllerBindings.Validate(configuration);if(ControllerBindings.Equal(bindings,configuration))return;bindings=configuration.Copy();CancelUntilReleased();}
        static int HeldButtons(Gamepad pad)
        {
            int mask=0;if(pad.buttonSouth.isPressed)mask|=1;if(pad.buttonEast.isPressed)mask|=2;if(pad.buttonWest.isPressed)mask|=4;if(pad.buttonNorth.isPressed)mask|=8;
            if(pad.leftShoulder.isPressed)mask|=16;if(pad.rightShoulder.isPressed)mask|=32;if(pad.leftTrigger.ReadValue()>.5f)mask|=64;if(pad.rightTrigger.ReadValue()>.5f)mask|=128;
            if(pad.startButton.isPressed)mask|=256;if(pad.selectButton.isPressed)mask|=512;if(pad.leftStickButton.isPressed)mask|=1024;if(pad.rightStickButton.isPressed)mask|=2048;return mask;
        }
        public static bool HasButtonActivity(Gamepad pad)=>pad.buttonSouth.wasPressedThisFrame||pad.buttonEast.wasPressedThisFrame||pad.buttonWest.wasPressedThisFrame||pad.buttonNorth.wasPressedThisFrame||pad.leftShoulder.wasPressedThisFrame||pad.rightShoulder.wasPressedThisFrame||pad.leftTrigger.wasPressedThisFrame||pad.rightTrigger.wasPressedThisFrame||pad.startButton.wasPressedThisFrame||pad.selectButton.wasPressedThisFrame||pad.leftStickButton.wasPressedThisFrame||pad.rightStickButton.wasPressedThisFrame;
        public static bool IsEligible(Gamepad device)
        {
            if(device==null||!device.added||!device.enabled)return false;
            var d=device.description;
            string identity=(device.name+" "+device.layout+" "+d.product+" "+d.manufacturer+" "+d.interfaceName).ToLowerInvariant();
            if(identity.Contains("simulat")||identity.Contains("xr device")||identity.Contains("xrcontroller"))return false;
            // Native Input System devices or a recognised OS controller backend.
            // Unlabelled virtual Gamepad devices do not take over the pointer.
            string backend=(d.interfaceName??"").ToLowerInvariant();
            return device.native||backend.Contains("hid")||backend.Contains("xinput")||backend.Contains("android")||backend.Contains("windows.gaming.input")||backend.Contains("gamecontroller");
        }
        public void CancelUntilReleased(){pendingRelease|=lastSelect||lastGrip;lastSelect=lastGrip=lastMenu=lastRecenter=lastBack=false;suppressHeld=true;lastDirection=0;}
        public ActionFrame Poll(Camera camera,double now)=>Poll(camera,now,false,true);
        public ActionFrame Poll(Camera camera,double now,bool gaze,bool allowLook)
        {
            bool available=Device!=null&&Device.added&&Device.enabled;
            double dt=lastTime<0?0:Math.Max(0,Math.Min(.05,now-lastTime));lastTime=now;
            var f=new ActionFrame{pointer=new Ray(camera.transform.position,camera.transform.forward)};
            if(!available)
            {f.lost=connected;f.released=pendingRelease||lastSelect||lastGrip;pendingRelease=false;lastSelect=lastGrip=lastMenu=lastRecenter=lastBack=false;connected=false;return f;}
            connected=true;
            int held=HeldButtons(Device);bool select=(held&ControllerBindings.Mask(bindings.controllerSelect))!=0,grip=(held&ControllerBindings.Mask(bindings.controllerGrip))!=0;
            bool menu=(held&ControllerBindings.Mask(bindings.controllerMenu))!=0,recenter=(held&ControllerBindings.Mask(bindings.controllerRecenter))!=0,back=(held&ControllerBindings.Mask(bindings.controllerBack))!=0;
            if(suppressHeld){if(held==0)suppressHeld=false;select=grip=menu=recenter=back=false;}
            f.pressed=select&&!lastSelect;f.grabPressed=grip&&!lastGrip;f.held=select||grip;f.released=pendingRelease||!f.held&&(lastSelect||lastGrip);pendingRelease=false;
            f.menu=menu&&!lastMenu;f.recenter=recenter&&!lastRecenter;f.back=back&&!lastBack;
            var dpad=Device.dpad.ReadValue();int direction=dpad.y>.5f||dpad.x>.5f?1:dpad.y<-.5f||dpad.x<-.5f?-1:0;
            if(direction!=0&&(direction!=lastDirection||now>=nextAdjustment))
            {f.adjustment=direction;nextAdjustment=now+(direction!=lastDirection?.35:.15);}lastDirection=direction;
            if(!gaze)
            {
                var stick=Device.leftStick.ReadValue();if(stick.sqrMagnitude<.04f)stick=Vector2.zero;
                var p=CursorNormalized+stick*(float)(dt*.65);CursorNormalized=new Vector2(Mathf.Clamp(p.x,.02f,.98f),Mathf.Clamp(p.y,.02f,.98f));
                var area=Screen.safeArea;if(area.width<1||area.height<1)area=new Rect(0,0,Math.Max(1,Screen.width),Math.Max(1,Screen.height));
                if(allowLook)
                {var look=Device.rightStick.ReadValue();if(look.sqrMagnitude>=.04f){var a=camera.transform.eulerAngles;a.y+=look.x*(float)(dt*110);a.x=Mathf.Clamp(Mathf.DeltaAngle(0,a.x)-look.y*(float)(dt*90),-70,70);camera.transform.rotation=Quaternion.Euler(a.x,a.y,0);}}
                f.pointer=camera.ScreenPointToRay(new Vector2(area.x+area.width*CursorNormalized.x,area.y+area.height*CursorNormalized.y));
            }
            lastSelect=select;lastGrip=grip;lastMenu=menu;lastRecenter=recenter;lastBack=back;return f;
        }
    }
    public sealed class VlabInput:MonoBehaviour
    {
        public bool VrMode{get;private set;}public bool SimulatorMode{get;private set;}
        public Camera View{get;private set;}
        public MockControllerProvider Controller{get;}=new MockControllerProvider();
        public bool UseMockController;public bool Gameplay;
        public Func<Ray,bool> BlocksTouchLook;
        bool touchLooking;Vector2 touchOrigin,touchPrevious;
        float mouseLookSensitivity=1;
        public float MouseLookSensitivity
        {
            get=>mouseLookSensitivity;
            set
            {
                mouseLookSensitivity=value;
#if UNITY_EDITOR
                ApplySimulatorMouseLook();
#endif
            }
        }
        public bool CanScanViewer=>CardboardRuntime.Supported;
        public bool CanPairController=>Application.platform==RuntimePlatform.Android;
        public string AvailableControllerName=>System.Linq.Enumerable.FirstOrDefault(Gamepad.all,GamepadControllerProvider.IsEligible)?.displayName;
        public bool OpenControllerPairing()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using(var player=new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using(var activity=player.GetStatic<AndroidJavaObject>("currentActivity"))
                using(var intent=new AndroidJavaObject("android.content.Intent","android.settings.BLUETOOTH_SETTINGS"))activity.Call("startActivity",intent);
                return true;
            }
            catch(AndroidJavaException){return false;}
#else
            return false;
#endif
        }
        public bool UsingGamepad=>activeGamepad!=null&&activeGamepad.Device!=null&&activeGamepad.Device.added&&activeGamepad.Device.enabled&&!UseMockController;
        public bool ControllerPointerActive=>VrMode||SimulatorMode||UseMockController||UsingGamepad;
        public string CurrentInputLabel=>UseMockController?"Orientation adapter (mock)":UsingGamepad?(VrMode||SimulatorMode?"VR gaze + HID gamepad":"HID gamepad cursor"):(VrMode||SimulatorMode?"VR gaze / viewer trigger":"Mouse / touchscreen");
        public ControllerCapabilities Capabilities=>UseMockController?Controller.Capabilities:UsingGamepad?(VrMode||SimulatorMode?activeGamepad.Capabilities&~ControllerCapabilities.Look:activeGamepad.Capabilities):ControllerCapabilities.Select|ControllerCapabilities.Grip|ControllerCapabilities.Adjustment|ControllerCapabilities.Menu|ControllerCapabilities.Pointer;
        public event Action<bool,string> ModeChanged;
        TrackedPoseDriver trackedPose;bool switching,lastPointerHeld;
        GamepadControllerProvider activeGamepad;
        GameSettings controllerBindings=new GameSettings();
        public void ConfigureControllerBindings(GameSettings configuration)
        {ControllerBindings.Validate(configuration);if(ControllerBindings.Equal(controllerBindings,configuration))return;controllerBindings=configuration.Copy();activeGamepad?.ConfigureBindings(controllerBindings);}
        readonly System.Collections.Generic.Dictionary<int,Vector4> gamepadActivity=new System.Collections.Generic.Dictionary<int,Vector4>();
        bool pendingLoss;
#if UNITY_EDITOR
        GameObject simulator;
        XRDeviceSimulator simulatorDriver;float simulatorMouseX,simulatorMouseY;
        void ApplySimulatorMouseLook()
        {
            if(!simulatorDriver)return;
            simulatorDriver.mouseXRotateSensitivity=simulatorMouseX*mouseLookSensitivity;
            simulatorDriver.mouseYRotateSensitivity=simulatorMouseY*mouseLookSensitivity;
        }
#endif
        public void Adopt(Camera view)
        {
            View=view;trackedPose=view.GetComponent<TrackedPoseDriver>();
            if(!trackedPose)trackedPose=view.gameObject.AddComponent<TrackedPoseDriver>();
            trackedPose.positionInput=new InputActionProperty(new InputAction("Head position",InputActionType.Value,"<XRHMD>/centerEyePosition"));
            trackedPose.rotationInput=new InputActionProperty(new InputAction("Head rotation",InputActionType.Value,"<XRHMD>/centerEyeRotation"));
            trackedPose.trackingStateInput=new InputActionProperty(new InputAction("Head tracking",InputActionType.Value,"<XRHMD>/trackingState"));
            trackedPose.trackingType=TrackedPoseDriver.TrackingType.RotationOnly;
            trackedPose.enabled=VrMode||SimulatorMode;
        }
        public ActionFrame Read()
        {
            if(!View)return default;
            double now=Time.realtimeSinceStartupAsDouble;
            if(UseMockController)return FinishFrame(Controller.Poll(View,now));
            var keyboard=Keyboard.current;var mouse=Mouse.current;
            bool mouseActivity=mouse!=null&&(mouse.delta.ReadValue().sqrMagnitude>.25f||mouse.scroll.ReadValue().sqrMagnitude>.01f||mouse.leftButton.wasPressedThisFrame||mouse.leftButton.wasReleasedThisFrame||mouse.rightButton.wasPressedThisFrame||mouse.middleButton.wasPressedThisFrame);
            bool touchActivity=Touchscreen.current!=null&&(Touchscreen.current.primaryTouch.press.wasPressedThisFrame||Touchscreen.current.primaryTouch.press.isPressed);
            bool changed=false;
            if(activeGamepad!=null&&(activeGamepad.Device==null||!activeGamepad.Device.added||!activeGamepad.Device.enabled))
            {activeGamepad.CancelUntilReleased();activeGamepad=null;pendingLoss=true;changed=true;}
            var candidate=ActiveGamepadFromRealActivity();
            if(mouseActivity||touchActivity)
            {if(activeGamepad!=null){activeGamepad.CancelUntilReleased();activeGamepad=null;changed=true;if(lastPointerHeld)pendingLoss=true;}}
            else if(candidate!=null&&(activeGamepad==null||activeGamepad.Device!=candidate))
            {if(activeGamepad!=null){activeGamepad.CancelUntilReleased();if(lastPointerHeld)pendingLoss=true;}activeGamepad=new GamepadControllerProvider(candidate,controllerBindings);changed=true;}
            if(changed)ModeChanged?.Invoke(VrMode,"");
            if(activeGamepad!=null)
            {
                var gamepadFrame=activeGamepad.Poll(View,now,VrMode||SimulatorMode,Gameplay);
                gamepadFrame.menu|=keyboard!=null&&keyboard.escapeKey.wasPressedThisFrame;
                gamepadFrame.recenter|=keyboard!=null&&(SimulatorMode?keyboard.homeKey.wasPressedThisFrame:keyboard.rKey.wasPressedThisFrame);
                ApplyViewerButtons(ref gamepadFrame);return FinishFrame(gamepadFrame);
            }
            var f=new ActionFrame{pointer=VrMode||SimulatorMode?new Ray(View.transform.position,View.transform.forward):View.ScreenPointToRay(mouse!=null?mouse.position.ReadValue():(Vector2)Screen.safeArea.center),menu=keyboard!=null&&keyboard.escapeKey.wasPressedThisFrame,recenter=keyboard!=null&&(SimulatorMode?keyboard.homeKey.wasPressedThisFrame:keyboard.rKey.wasPressedThisFrame)};
            if(mouse!=null){f.pressed=mouse.leftButton.wasPressedThisFrame;f.released=mouse.leftButton.wasReleasedThisFrame;f.held=mouse.leftButton.isPressed||mouse.middleButton.isPressed;f.grabPressed=mouse.middleButton.wasPressedThisFrame;f.adjustment=mouse.scroll.ReadValue().y/120;}
            if(Touchscreen.current!=null&&(Touchscreen.current.primaryTouch.press.isPressed||Touchscreen.current.primaryTouch.press.wasReleasedThisFrame))
            {
                var touch=Touchscreen.current.primaryTouch;bool pressed=touch.press.isPressed;var position=pressed?touch.position.ReadValue():touchPrevious;
                if(!VrMode&&!SimulatorMode)
                {
                    f.pointer=View.ScreenPointToRay(position);
                    if(touch.press.wasPressedThisFrame){touchOrigin=touchPrevious=position;touchLooking=Gameplay&&!(BlocksTouchLook?.Invoke(f.pointer)??false);}
                    if(pressed&&touchLooking&&Gameplay&&!(BlocksTouchLook?.Invoke(f.pointer)??false)&&(position-touchOrigin).sqrMagnitude>=144)
                    {
                        var delta=position-touchPrevious;var angles=View.transform.eulerAngles;
                        angles.y-=delta.x/Mathf.Max(1,Screen.width)*180*MouseLookSensitivity;
                        angles.x=Mathf.Clamp(Mathf.DeltaAngle(0,angles.x)+delta.y/Mathf.Max(1,Screen.height)*120*MouseLookSensitivity,-70,70);
                        View.transform.rotation=Quaternion.Euler(angles.x,angles.y,0);
                    }
                    touchPrevious=position;
                }
                f.pressed=touch.press.wasPressedThisFrame;f.held=pressed;f.released=touch.press.wasReleasedThisFrame;
            }
            else touchLooking=false;
            if(SimulatorMode&&keyboard!=null&&keyboard.enterKey.wasPressedThisFrame)f.pressed=true;
            ApplyViewerButtons(ref f);
            if(Gameplay&&!VrMode&&!SimulatorMode&&mouse!=null&&mouse.rightButton.isPressed)
            {var delta=mouse.delta.ReadValue();var a=View.transform.eulerAngles;float speed=.12f*MouseLookSensitivity;a.y+=delta.x*speed;a.x=Mathf.Clamp(Mathf.DeltaAngle(0,a.x)-delta.y*speed,-70,70);View.transform.rotation=Quaternion.Euler(a.x,a.y,0);}
            return FinishFrame(f);
        }
        Gamepad ActiveGamepadFromRealActivity()
        {
            Gamepad candidate=null;
            foreach(var pad in Gamepad.all)
            {
                if(!GamepadControllerProvider.IsEligible(pad))continue;
                var left=pad.leftStick.ReadValue();var right=pad.rightStick.ReadValue();var current=new Vector4(left.x,left.y,right.x,right.y);
                bool seen=gamepadActivity.TryGetValue(pad.deviceId,out var previous);gamepadActivity[pad.deviceId]=current;
                bool moved=seen&&(current-previous).sqrMagnitude>.0025f&&(left.sqrMagnitude>.04f||right.sqrMagnitude>.04f);
                bool edge=GamepadControllerProvider.HasButtonActivity(pad)||pad.dpad.up.wasPressedThisFrame||pad.dpad.down.wasPressedThisFrame||pad.dpad.left.wasPressedThisFrame||pad.dpad.right.wasPressedThisFrame;
                if(seen&&(moved||edge))candidate=pad;
            }
            return candidate;
        }
        ActionFrame FinishFrame(ActionFrame f)
        {
            if(pendingLoss){f.lost=true;f.released|=lastPointerHeld;f.held=f.pressed=f.grabPressed=f.menu=f.recenter=f.back=false;pendingLoss=false;}
            f.released|=!f.held&&lastPointerHeld;lastPointerHeld=f.held;return f;
        }
        void ApplyViewerButtons(ref ActionFrame f)
        {
            if(!VrMode)return;
            bool close=Google.XR.Cardboard.Api.IsCloseButtonPressed,gear=Google.XR.Cardboard.Api.IsGearButtonPressed;
            if(close||gear)
            {
                f.pressed=f.held=f.grabPressed=false;f.released=lastPointerHeld;f.lost=true;
                if(close)SetVr(false);else ScanViewer();
                return;
            }
            if(Google.XR.Cardboard.Api.IsTriggerPressed)f.pressed=true;
        }
        ICardboardEntryRuntime nativeCardboard;
#if UNITY_EDITOR
        public ICardboardEntryRuntime EntryRuntimeForChecks;
#endif
        ICardboardEntryRuntime CardboardRuntime
        {
            get
            {
#if UNITY_EDITOR
                if(EntryRuntimeForChecks!=null)return EntryRuntimeForChecks;
#endif
                return nativeCardboard??(nativeCardboard=new NativeCardboardEntryRuntime());
            }
        }
        void Update(){if(VrMode)CardboardRuntime.TickParameters();}
        public void SetVr(bool enabled){if(!switching)StartCoroutine(SwitchVr(enabled));}
        IEnumerator SwitchVr(bool enabled)
        {
            var runtime=CardboardRuntime;
            if(enabled&&!runtime.Supported){ModeChanged?.Invoke(false,"Cardboard stereo is available on Android. Use PC simulation here.");yield break;}
            if(enabled&&VrMode)yield break;
            switching=true;
            try
            {
                if(enabled)
                {
                    if(!runtime.Initialized)yield return runtime.Initialize();
                    if(!runtime.Initialized){ModeChanged?.Invoke(false,"Cardboard could not initialize. Flat mode remains available.");yield break;}
                    if(!runtime.Start()){runtime.StopAndDeinitialize();ModeChanged?.Invoke(false,"Cardboard could not start stereo. Flat mode remains available.");yield break;}
                    // The SDK uses saved viewer optics, or its built-in Cardboard V1 profile.
                    // Entering VR never launches a scanner or overwrites a saved profile.
                    VrMode=true;SimulatorMode=false;if(trackedPose)trackedPose.enabled=true;runtime.TickParameters();
                }
                else{runtime.StopAndDeinitialize();VrMode=false;if(trackedPose)trackedPose.enabled=SimulatorMode;}
                ModeChanged?.Invoke(VrMode,"");
            }
            finally{switching=false;}
        }
        public void SetSimulator(bool enabled)
        {
#if UNITY_EDITOR
            if(enabled&&!simulator)
            {
                var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VLABProduct/Editor/Simulator/XR Device Simulator.prefab");
                if(!prefab){ModeChanged?.Invoke(VrMode,"The official Editor simulator has not been imported.");return;}
                simulator=Instantiate(prefab);simulator.name="Editor-only XR Device Simulator";DontDestroyOnLoad(simulator);
                simulatorDriver=simulator.GetComponentInChildren<XRDeviceSimulator>(true);
                if(simulatorDriver){simulatorMouseX=simulatorDriver.mouseXRotateSensitivity;simulatorMouseY=simulatorDriver.mouseYRotateSensitivity;}
                foreach(var canvas in simulator.GetComponentsInChildren<Canvas>(true))canvas.enabled=false;
            }
            if(simulator){ApplySimulatorMouseLook();simulator.SetActive(enabled);}
            SimulatorMode=enabled;if(trackedPose)trackedPose.enabled=enabled||VrMode;ModeChanged?.Invoke(VrMode,"");
#else
            ModeChanged?.Invoke(VrMode,"PC simulation runs inside the Unity Editor.");
#endif
        }
        public void Recenter(){if(VrMode)Google.XR.Cardboard.Api.Recenter();Controller.Calibrate(View.transform.rotation);}
        public void ScanViewer(){if(CanScanViewer&&!switching)StartCoroutine(ScanViewerOptional());}
        IEnumerator ScanViewerOptional()
        {
            var runtime=CardboardRuntime;switching=true;
            try
            {
                // The public SDK scan API requires initialization even when requested from
                // flat Settings. Initialize without starting stereo; cancel leaves mode intact.
                if(!runtime.Initialized)yield return runtime.Initialize();
                if(!runtime.Initialized){ModeChanged?.Invoke(VrMode,"Viewer scanning is unavailable because Cardboard could not initialize.");yield break;}
                runtime.Scan();
            }
            finally{switching=false;}
        }
        void OnApplicationPause(bool paused){if(paused){Gameplay=false;pendingLoss=true;activeGamepad?.CancelUntilReleased();}}
        void OnApplicationFocus(bool focused){if(!focused){pendingLoss=true;activeGamepad?.CancelUntilReleased();}}
    }
    // Narrow boundary for Editor lifecycle checks. The production path always calls the
    // installed Cardboard SDK; synthetic tests cannot certify Android optics or permissions.
    public interface ICardboardEntryRuntime
    {
        bool Supported{get;}bool Initialized{get;}
        IEnumerator Initialize();bool Start();void StopAndDeinitialize();void Scan();void TickParameters();
    }
    sealed class NativeCardboardEntryRuntime:ICardboardEntryRuntime
    {
        UnityEngine.XR.Management.XRManagerSettings Manager=>XRGeneralSettings.Instance?XRGeneralSettings.Instance.Manager:null;
        public bool Supported=>Application.platform==RuntimePlatform.Android;
        public bool Initialized=>Manager&&Manager.activeLoader is Google.XR.Cardboard.XRLoader;
        public IEnumerator Initialize(){var manager=Manager;if(manager&&manager.activeLoader==null)yield return manager.InitializeLoader();}
        public bool Start()
        {
            if(!Initialized)return false;Manager.StartSubsystems();var display=Manager.activeLoader.GetLoadedSubsystem<UnityEngine.XR.XRDisplaySubsystem>();
            if(display==null||!display.running)return false;
            Google.XR.Cardboard.Api.ReloadDeviceParams();return true;
        }
        public void StopAndDeinitialize(){var manager=Manager;if(manager&&manager.activeLoader!=null){manager.StopSubsystems();manager.DeinitializeLoader();}}
        public void Scan()=>Google.XR.Cardboard.Api.ScanDeviceParams();
        public void TickParameters(){if(Google.XR.Cardboard.Api.HasNewDeviceParams())Google.XR.Cardboard.Api.ReloadDeviceParams();Google.XR.Cardboard.Api.UpdateScreenParams();}
    }
}
