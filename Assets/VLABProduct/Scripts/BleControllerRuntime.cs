using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

namespace Vlab.Product
{
    public interface IBleTransport:IDisposable
    {string State{get;}int Generation{get;}string Poll();void Scan();void StopScan();void Connect(string address,string service,string characteristic,bool reconnect);void Stop();}
    [Serializable] public sealed class BleFoundDevice{public string name,address;public int rssi;}
    [Serializable] public sealed class BleService{public string service,characteristic;public int properties;}
    [DefaultExecutionOrder(-200)]
    public sealed class BleControllerRuntime:MonoBehaviour
    {
        [Serializable] sealed class NativeEvent{public string type,state,code,name,address,data;public int generation,rssi;public double queuedMs;public BleService[] services;}
        public BleControllerProvider Provider{get;}=new BleControllerProvider();
        public BleProfileCollection Profiles{get;private set;}=new BleProfileCollection();
        public IReadOnlyList<BleFoundDevice> Devices=>devices;readonly List<BleFoundDevice> devices=new List<BleFoundDevice>();
        public BleService[] Services{get;private set;}=Array.Empty<BleService>();
        public string State{get;private set;}="Idle";public string Code{get;private set;}="";public string StorageError=>store?.Error??"";
        public bool StorageProtected=>store!=null&&store.Protected;public bool Simulated{get;private set;}
        public bool Available=>Application.platform==RuntimePlatform.Android||Simulated;
        public bool Fresh=>wanted&&State=="Ready"&&Provider.Fresh(Time.realtimeSinceStartupAsDouble);
        public bool IsCalibrating=>calibratingCentre||calibratingRange;
        public event Action Changed;
        BleProfileStore store;IBleTransport backend;int generation=-1;bool paused,permissionPending,disposed,wanted,resumeConnection;long lastCalibrationPacket;Action afterPermissionResume;
        int calibrationSamples;double calibrationStarted;Vector2 calibrationSum,calibrationMin,calibrationMax;bool calibratingRange,calibratingCentre;
        public string Calibration{get;private set;}="";public int CalibrationSamples=>calibrationSamples;
        public void Initialize(string folder)
        {store=new BleProfileStore(folder);Profiles=store.Load();Provider.Configure(Profiles.Selected);if(Profiles.Selected.autoReconnect&&Profiles.Selected.Configured&&Profiles.Selected.address!=""&&HasPermissions(false))Connect(Profiles.Selected.address,false);}
        public bool Apply(BleProfileCollection value,out string error)
        {
            if(!store.Save(value,out error))return false;Stop();Profiles=JsonUtility.FromJson<BleProfileCollection>(JsonUtility.ToJson(value));Provider.Configure(Profiles.Selected);Changed?.Invoke();return true;
        }
        public bool ResetProfiles(out string error)
        {Stop();if(!store.Reset(out error))return false;Profiles=store.Load();Provider.Configure(Profiles.Selected);Changed?.Invoke();return true;}
        bool EnsureBackend()
        {
            if(backend!=null)return true;
#if UNITY_ANDROID && !UNITY_EDITOR
            try{backend=new AndroidBleTransport();return true;}catch(AndroidJavaException){State="Error";Code="backend";Changed?.Invoke();return false;}
#else
            Code="androidOnly";Changed?.Invoke();return false;
#endif
        }
        public static bool HasPermissions(bool scan)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            using(var build=new AndroidJavaClass("android.os.Build$VERSION"))
            {return build.GetStatic<int>("SDK_INT")>=31?Permission.HasUserAuthorizedPermission("android.permission.BLUETOOTH_CONNECT")&&(!scan||Permission.HasUserAuthorizedPermission("android.permission.BLUETOOTH_SCAN")):!scan||Permission.HasUserAuthorizedPermission(Permission.FineLocation);}
#else
            return false;
#endif
        }
        void WithPermissions(bool scan,Action action)
        {
            if(Simulated||HasPermissions(scan)){if(EnsureBackend())action();return;}
#if UNITY_ANDROID && !UNITY_EDITOR
            if(permissionPending)return;permissionPending=true;Code="permission";Changed?.Invoke();
            string[] permissions;using(var build=new AndroidJavaClass("android.os.Build$VERSION"))permissions=build.GetStatic<int>("SDK_INT")>=31?(scan?new[]{"android.permission.BLUETOOTH_SCAN","android.permission.BLUETOOTH_CONNECT"}:new[]{"android.permission.BLUETOOTH_CONNECT"}):new[]{Permission.FineLocation};
            var needed=new List<string>();foreach(string item in permissions)if(!Permission.HasUserAuthorizedPermission(item))needed.Add(item);
            if(needed.Count==0){permissionPending=false;if(paused)afterPermissionResume=()=>{if(EnsureBackend())action();};else if(EnsureBackend())action();return;}
            int left=needed.Count;var answered=new HashSet<string>();var callbacks=new PermissionCallbacks();Action<string> finish=name=>{if(!answered.Add(name)||--left>0)return;permissionPending=false;if(disposed)return;if(HasPermissions(scan)){if(paused)afterPermissionResume=()=>{if(EnsureBackend())action();};else if(EnsureBackend())action();}else{State="Error";Code="permission";Changed?.Invoke();}};
            callbacks.PermissionGranted+=finish;callbacks.PermissionDenied+=finish;callbacks.PermissionDeniedAndDontAskAgain+=finish;Permission.RequestUserPermissions(needed.ToArray(),callbacks);
#else
            Code="androidOnly";Changed?.Invoke();
#endif
        }
        public void Scan(){if(paused||disposed)return;WithPermissions(true,()=>{devices.Clear();Code="";backend.Scan();Changed?.Invoke();});}
        public void StopScan(){backend?.StopScan();}
        public void Connect(string address,bool remember=true)
        {
            if(paused||disposed)return;var candidate=JsonUtility.FromJson<BleProfileCollection>(JsonUtility.ToJson(Profiles));candidate.Selected.address=address;
            try{candidate.Validate();}catch(Exception e)when(e is InvalidDataException||e is IOException||e is ArgumentException||e is FormatException){Code="configuration";Changed?.Invoke();return;}
            if(remember&&!Apply(candidate,out _)){Code="storage";Changed?.Invoke();return;}
            WithPermissions(false,()=>{wanted=true;Provider.Disconnect();Services=Array.Empty<BleService>();backend.Connect(address,Profiles.Selected.service,Profiles.Selected.characteristic,Profiles.Selected.autoReconnect);});
        }
        public void Stop(){resumeConnection=false;wanted=false;afterPermissionResume=null;backend?.Stop();Provider.Disconnect();State="Stopped";calibratingCentre=calibratingRange=false;Changed?.Invoke();}
        public void ConfigureBindings(GameSettings bindings)=>Provider.ConfigureBindings(bindings);
        void Update()
        {
            if(backend==null||paused||disposed)return;
            try
            {
                int currentGeneration=backend.Generation;string current=backend.State;
                if(currentGeneration!=generation){Provider.Disconnect();generation=currentGeneration;}
                if(!wanted&&current=="Ready")current="Stopped";
                if(current!="Ready"&&State=="Ready")Provider.Disconnect();
                if(current!=State){State=current;Changed?.Invoke();}
                for(int i=0;i<32;i++){string value=backend.Poll();if(string.IsNullOrEmpty(value))break;Receive(value,Time.realtimeSinceStartupAsDouble);}
                TickCalibration();
            }
            catch(Exception e)when(e is ArgumentException||e is FormatException||e is InvalidOperationException){Provider.Disconnect();State="Error";Code="backend";Changed?.Invoke();}
#if UNITY_ANDROID && !UNITY_EDITOR
            catch(AndroidJavaException){Provider.Disconnect();State="Error";Code="backend";Changed?.Invoke();}
#endif
        }
        void Receive(string json,double now)
        {
            if(json==null||json.Length>16384)return;NativeEvent value;
            try{value=JsonUtility.FromJson<NativeEvent>(json);}catch(ArgumentException){return;}
            if(value==null||value.generation!=generation)return;
            switch(value.type)
            {
                case "state":Code=value.code??"";Changed?.Invoke();break;
                case "device":
                    if(!System.Text.RegularExpressions.Regex.IsMatch(value.address??"","^[0-9A-Fa-f]{2}(:[0-9A-Fa-f]{2}){5}$"))return;
                    var found=devices.Find(d=>d.address==value.address);if(found!=null){found.rssi=value.rssi;return;}if(devices.Count>=24)return;
                    devices.Add(new BleFoundDevice{name=Plain(value.name),address=value.address,rssi=value.rssi});Changed?.Invoke();break;
                case "services":
                    var services=new List<BleService>();if(value.services!=null)foreach(var item in value.services){if(services.Count>=64)break;if(item!=null&&Guid.TryParseExact(item.service,"D",out _)&&Guid.TryParseExact(item.characteristic,"D",out _))services.Add(item);}Services=services.ToArray();Changed?.Invoke();break;
                case "packet":
                    if(!wanted||State!="Ready"||string.IsNullOrEmpty(value.data)||value.data.Length>1024||!System.Text.RegularExpressions.Regex.IsMatch(json,"\"queuedMs\"\\s*:"))return;
                    try{Provider.Submit(Convert.FromBase64String(value.data),now,value.queuedMs);}catch(FormatException){}break;
            }
        }
        public static string Plain(string name)
        {if(string.IsNullOrEmpty(name))return "BLE";var text=new System.Text.StringBuilder();foreach(char c in name){if(!char.IsControl(c)&&c!='<'&&c!='>')text.Append(c);if(text.Length>=48)break;}return text.ToString();}
        public bool BeginCalibration(bool range)
        {
            if(!Fresh||Profiles.Selected.stickXOffset<0){Calibration="noSamples";return false;}
            Provider.CancelUntilReleased();calibrationSamples=0;calibrationSum=Vector2.zero;calibrationMin=Vector2.one*32767;calibrationMax=Vector2.one*-32768;calibratingRange=range;calibratingCentre=!range;calibrationStarted=Time.realtimeSinceStartupAsDouble;lastCalibrationPacket=Provider.Accepted;Calibration=range?"range":"centre";return true;
        }
        void TickCalibration()
        {
            if(!calibratingCentre&&!calibratingRange)return;
            if(!Fresh){calibratingCentre=calibratingRange=false;Calibration="disconnected";Changed?.Invoke();return;}
            if(lastCalibrationPacket==Provider.Accepted)return;lastCalibrationPacket=Provider.Accepted;var raw=new Vector2(Provider.LastPacket.rawX,Provider.LastPacket.rawY);calibrationSamples++;calibrationSum+=raw;calibrationMin=Vector2.Min(calibrationMin,raw);calibrationMax=Vector2.Max(calibrationMax,raw);
            if(calibratingCentre&&calibrationSamples>=30)FinishCalibration();
        }
        public bool FinishCalibration()
        {
            if(!calibratingCentre&&!calibratingRange)return false;
            if(calibrationSamples<30||calibratingRange&&Time.realtimeSinceStartupAsDouble-calibrationStarted<2){Calibration="moreSamples";return false;}
            var value=JsonUtility.FromJson<BleProfileCollection>(JsonUtility.ToJson(Profiles));var p=value.Selected;
            if(calibratingCentre){if((calibrationMax-calibrationMin).sqrMagnitude>128*128){Calibration="keepStill";calibratingCentre=false;return false;}var centre=calibrationSum/calibrationSamples;p.x.centre=centre.x;p.y.centre=centre.y;}
            else{if(calibrationMax.x-calibrationMin.x<128||calibrationMax.y-calibrationMin.y<128){Calibration="fullRange";return false;}p.x.min=calibrationMin.x;p.x.max=calibrationMax.x;p.y.min=calibrationMin.y;p.y.max=calibrationMax.y;}
            calibratingCentre=calibratingRange=false;
            if(!Apply(value,out _)){Calibration="invalidRange";return false;}Calibration="saved";Changed?.Invoke();return true;
        }
        public void CancelCalibration(){calibratingCentre=calibratingRange=false;Calibration="";Provider.CancelUntilReleased();}
        void OnApplicationFocus(bool focused){if(!focused)Provider.CancelUntilReleased();}
        void OnApplicationPause(bool value)
        {paused=value;if(value){bool reconnect=wanted&&Profiles.Selected.autoReconnect;Stop();resumeConnection=reconnect;}else if(afterPermissionResume!=null){var action=afterPermissionResume;afterPermissionResume=null;resumeConnection=false;action();}else if(resumeConnection){resumeConnection=false;if(Profiles.Selected.address!=""&&(Simulated||HasPermissions(false)))Connect(Profiles.Selected.address,false);}}
        void OnApplicationQuit()=>Dispose();
        void OnDestroy()=>Dispose();
        void Dispose(){if(disposed)return;disposed=true;Provider.Disconnect();backend?.Dispose();backend=null;}
#if UNITY_EDITOR
        public void SetBackendForChecks(IBleTransport value){backend?.Dispose();backend=value;Simulated=true;generation=-1;Provider.Disconnect();}
#endif
    }
#if UNITY_ANDROID && !UNITY_EDITOR
    sealed class AndroidBleTransport:IBleTransport
    {
        readonly AndroidJavaObject native;
        public AndroidBleTransport(){using(var player=new AndroidJavaClass("com.unity3d.player.UnityPlayer"))using(var activity=player.GetStatic<AndroidJavaObject>("currentActivity"))native=new AndroidJavaObject("com.nguaman.vlab.ble.BleTransport",activity);}
        public string State=>native.Call<string>("state");public int Generation=>native.Call<int>("generation");
        public string Poll()=>native.Call<string>("poll");public void Scan()=>native.Call("scan");public void StopScan()=>native.Call("stopScan");
        public void Connect(string address,string service,string characteristic,bool reconnect)=>native.Call("connect",address,service,characteristic,reconnect);
        public void Stop()=>native.Call("stop");public void Dispose(){native.Call("dispose");native.Dispose();}
    }
#endif
}
