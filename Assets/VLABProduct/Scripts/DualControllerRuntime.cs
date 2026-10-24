using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif
namespace Vlab.Product
{
    [DefaultExecutionOrder(-180)]
    public sealed class DualControllerRuntime:MonoBehaviour
    {
        [Serializable] sealed class NativeEvent {public string type,state,code,name,address,data;public int generation,rssi;public double queuedMs;}
        sealed class Link {public IBleTransport transport;public int generation=-1;public string status="Stopped",code="";public bool wanted;}
        public ControllerV6State LeftControllerState{get;}=new ControllerV6State(1);
        public ControllerV6State RightControllerState{get;}=new ControllerV6State(2);
        public IReadOnlyList<BleFoundDevice> Devices=>devices;
        public string ScanStatus{get;private set;}="Idle";
        public bool Paused{get;private set;}public bool Available=>Application.platform==RuntimePlatform.Android;
        public event Action<ControllerV6State> StateReceived;
        readonly List<BleFoundDevice> devices=new List<BleFoundDevice>();
        readonly Link[] links={new Link(),new Link()};IBleTransport scanner;
        GameSettings bindings=new GameSettings();bool permissionsPending,scanPending,disposed;
        public bool AnyFresh=>LeftControllerState.Fresh(Time.realtimeSinceStartupAsDouble)||RightControllerState.Fresh(Time.realtimeSinceStartupAsDouble);
        public ControllerV6State Hand(int hand)=>hand==1?LeftControllerState:hand==2?RightControllerState:throw new ArgumentOutOfRangeException(nameof(hand));
        public string Status(int hand)=>links[hand-1].status+" "+links[hand-1].code;
        public void ConfigureBindings(GameSettings value){ControllerBindings.Validate(value);bindings=value.Copy();LeftControllerState.CancelUntilReleased();RightControllerState.CancelUntilReleased();}
        public void Initialize()
        {
            for(int hand=1;hand<=2;hand++) {
                try{string json=PlayerPrefs.GetString("VLAB.V6.Motion."+hand,"");if(json!=""){var settings=JsonUtility.FromJson<ControllerV6MotionSettings>(json);if(settings!=null&&settings.Valid)Hand(hand).Settings=settings;}}
                catch(ArgumentException){Debug.LogWarning("V6 motion settings ignored: invalid saved JSON.");}
            }
            if(Available&&BleControllerRuntime.HasPermissions(false))ReconnectSaved();
        }
        public bool SaveMotion(int hand,ControllerV6MotionSettings value)
        {
            if(value==null||!value.Valid)return false;Hand(hand).Settings=value;Hand(hand).Recenter(Camera.main?Camera.main.transform.rotation:Quaternion.identity);
            PlayerPrefs.SetString("VLAB.V6.Motion."+hand,JsonUtility.ToJson(value));PlayerPrefs.Save();return true;
        }
        IBleTransport NewTransport()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try{return new AndroidBleTransport();}catch(AndroidJavaException e){ScanStatus="BLE backend: "+e.Message;return null;}
#else
            ScanStatus="Android required";return null;
#endif
        }
        public void Scan()
        {
            if(disposed)return;
            if(!BleControllerRuntime.HasPermissions(true)) {
                scanPending=true;
#if UNITY_ANDROID && !UNITY_EDITOR
                if(permissionsPending)return;permissionsPending=true;
                string[] required;using(var build=new AndroidJavaClass("android.os.Build$VERSION"))required=build.GetStatic<int>("SDK_INT")>=31?new[]{"android.permission.BLUETOOTH_SCAN","android.permission.BLUETOOTH_CONNECT"}:new[]{Permission.FineLocation};
                var missing=new List<string>();foreach(string p in required)if(!Permission.HasUserAuthorizedPermission(p))missing.Add(p);
                var answered=new HashSet<string>();var callbacks=new PermissionCallbacks();Action<string> done=p=>{if(!answered.Add(p)||answered.Count<missing.Count)return;permissionsPending=false;if(!BleControllerRuntime.HasPermissions(true)){scanPending=false;ScanStatus="Permission denied; grant Nearby devices in Android Settings";}else if(!Paused)Scan();};
                callbacks.PermissionGranted+=done;callbacks.PermissionDenied+=done;callbacks.PermissionDeniedAndDontAskAgain+=done;
                if(missing.Count>0)Permission.RequestUserPermissions(missing.ToArray(),callbacks);else{permissionsPending=false;if(!Paused)StartScan();}
#else
                scanPending=false;ScanStatus="Android required";
#endif
                return;
            }
            if(!Paused)StartScan();
        }
        void StartScan(){scanPending=false;scanner??=NewTransport();if(scanner==null)return;devices.Clear();scanner.Scan();ScanStatus="Scanning";}
        public bool Connect(int hand,string address,bool remember=true)
        {
            if(disposed||Paused||!BleControllerRuntime.HasPermissions(false))return false;
            if(hand<1||hand>2||!System.Text.RegularExpressions.Regex.IsMatch(address??"","^[0-9A-Fa-f]{2}(:[0-9A-Fa-f]{2}){5}$")||string.Equals(Hand(3-hand).Address,address,StringComparison.OrdinalIgnoreCase))return false;
            var link=links[hand-1];link.transport??=NewTransport();if(link.transport==null)return false;
            var state=Hand(hand);state.Disconnect();state.Address=address;link.wanted=true;link.status="Connecting";link.code="";
            link.transport.Connect(address,ControllerV6Protocol.Service,ControllerV6Protocol.Notify,true);
            if(remember){PlayerPrefs.SetString("VLAB.V6.Address."+hand,address);PlayerPrefs.Save();}return true;
        }
        public void ReconnectSaved()
        {for(int hand=1;hand<=2;hand++){string address=PlayerPrefs.GetString("VLAB.V6.Address."+hand,"");if(address!="")Connect(hand,address,false);}}
        public void Stop(int hand,bool forget=false)
        {
            var link=links[hand-1];link.wanted=false;link.transport?.Stop();link.status="Stopped";link.code="";Hand(hand).Disconnect();
            if(forget){Hand(hand).Address="";PlayerPrefs.DeleteKey("VLAB.V6.Address."+hand);PlayerPrefs.Save();}
        }
        public bool SendCommand(int hand,byte opcode,ushort argument=0)
        {
            byte[] command=ControllerV6Protocol.MakeCommand(opcode,argument);
#if UNITY_ANDROID && !UNITY_EDITOR
            if(!Paused&&links[hand-1].status=="Ready"&&links[hand-1].transport is AndroidBleTransport native){native.Write(ControllerV6Protocol.Service,ControllerV6Protocol.Command,command);return true;}
#endif
            return false;
        }
        public void Recenter(Quaternion view){LeftControllerState.Recenter(view);RightControllerState.Recenter(view);}
        public void CancelInputContext(){double now=Time.realtimeSinceStartupAsDouble;LeftControllerState.CancelUntilReleased(now);RightControllerState.CancelUntilReleased(now);}
        public void Tick(Camera view,bool gameplay)
        {double now=Time.realtimeSinceStartupAsDouble;LeftControllerState.Tick(view,now,Time.unscaledDeltaTime,bindings,gameplay);RightControllerState.Tick(view,now,Time.unscaledDeltaTime,bindings,gameplay);}
        void Update()
        {
            if(Paused||disposed)return;
            if(scanner!=null)for(int i=0;i<48;i++) {
                string json=scanner.Poll();if(string.IsNullOrEmpty(json))break;
                try{var ev=JsonUtility.FromJson<NativeEvent>(json);if(ev==null)continue;if(ev.type=="state")ScanStatus=ev.state+" "+ev.code;
                    if(ev.type=="device"&&(ev.name=="VLAB-Left"||ev.name=="VLAB-Right")) {
                        var device=devices.Find(d=>d.address==ev.address);if(device==null){if(devices.Count>=16)continue;device=new BleFoundDevice{address=ev.address};devices.Add(device);}device.name=ev.name;device.rssi=ev.rssi;
                    }}catch(ArgumentException){ScanStatus="Invalid scan event";}
            }
            for(int i=0;i<2;i++)Drain(i);
        }
        void Drain(int index)
        {
            var link=links[index];if(link.transport==null)return;var state=Hand(index+1);
            try {
                int generation=link.transport.Generation;
                if(generation!=link.generation){state.Disconnect();link.generation=generation;}
                string status=link.transport.State;link.status=status;if(status!="Ready")state.Disconnect();
                for(int n=0;n<64;n++) {
                    string json=link.transport.Poll();if(string.IsNullOrEmpty(json))break;var ev=JsonUtility.FromJson<NativeEvent>(json);
                    if(ev==null||ev.generation!=link.generation)continue;
                    if(ev.type=="state"){link.status=ev.state;link.code=ev.code;if(ev.state!="Ready")state.Disconnect();}
                    else if(ev.type=="packet"&&link.wanted&&status=="Ready"&&ev.data!=null&&ev.data.Length<=128) {
                        state.Connected=true;
                        if(state.Submit(Convert.FromBase64String(ev.data),Time.realtimeSinceStartupAsDouble,ev.queuedMs))StateReceived?.Invoke(state);
                    }
                    else if(ev.type=="command")link.code=ev.code;
                }
            }catch(Exception e)when(e is ArgumentException||e is FormatException){link.code="Invalid native event";state.Disconnect();}
#if UNITY_ANDROID && !UNITY_EDITOR
            catch(AndroidJavaException){link.code="BLE backend unavailable";state.Disconnect();}
#endif
        }
        void OnApplicationPause(bool paused)
        {
            Paused=paused;
            if(paused){scanner?.StopScan();foreach(var link in links)link.transport?.Stop();LeftControllerState.Disconnect();RightControllerState.Disconnect();}
            else if(scanPending)Scan();else for(int i=0;i<2;i++)if(links[i].wanted)Connect(i+1,Hand(i+1).Address,false);
        }
        void OnApplicationFocus(bool focus){if(!focus){LeftControllerState.CancelUntilReleased();RightControllerState.CancelUntilReleased();}}
        void OnDestroy(){disposed=true;scanner?.Dispose();foreach(var link in links)link.transport?.Dispose();}
#if UNITY_EDITOR
        public void SetBackendForChecks(int hand,IBleTransport transport){links[hand-1].transport=transport;links[hand-1].wanted=true;}
        public void PumpForChecks()=>Update();
#endif
    }
}
