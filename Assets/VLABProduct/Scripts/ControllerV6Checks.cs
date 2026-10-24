#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using Vlab.Product;

public static class ControllerV6Checks
{
    public const string Evidence="Evidence/ControllerV6";
    public static readonly List<string> Rows=new List<string>();
    public static int Failures;
    public static void Check(string name,bool ok){Rows.Add((ok?"PASS ":"FAIL ")+name);if(!ok){Failures++;Debug.LogError("V6 CHECK FAIL: "+name);}}
    public static void Word(byte[] p,int at,int value){p[at]=(byte)value;p[at+1]=(byte)(value>>8);}
    public static byte[] Packet(int hand,int sequence,int buttons=0,int x=0,int y=0,float trigger=0,Quaternion? rotation=null,Vector3? acceleration=null)
    {
        var p=new byte[56];p[0]=86;p[1]=54;p[2]=1;p[3]=(byte)hand;Word(p,4,sequence);p[6]=23;p[7]=255;
        uint time=unchecked((uint)sequence*20);for(int i=0;i<4;i++)p[8+i]=(byte)(time>>(8*i));
        Word(p,12,buttons);Word(p,14,x);Word(p,16,y);Word(p,18,Mathf.RoundToInt(trigger*65535));
        var q=rotation??Quaternion.identity;Word(p,20,Mathf.RoundToInt(q.x*16384));Word(p,22,Mathf.RoundToInt(q.y*16384));Word(p,24,Mathf.RoundToInt(q.z*16384));Word(p,26,Mathf.RoundToInt(q.w*16384));
        var a=acceleration??Vector3.zero;for(int i=0;i<3;i++)Word(p,28+i*2,Mathf.RoundToInt(a[i]*100));Word(p,44,981);
        Word(p,46,2048);Word(p,48,2048);Word(p,50,2048);Seal(p);return p;
    }
    static void Seal(byte[] p)=>Word(p,54,BleControllerProfile.Crc(p,54));
    [MenuItem("VLAB/Checks/V6 dual controller acceptance")]
    public static void Run()
    {
        Directory.CreateDirectory(Evidence);Rows.Clear();Failures=0;
        var root=new GameObject("V6 deterministic checks");var camera=root.AddComponent<Camera>();
        try {
            foreach(int hand in new[]{1,2}) {
                var fixture=File.ReadAllBytes("Assets/VLABProduct/Tests/Fixtures/golden-"+(hand==1?"left":"right")+".bin");
                Check("C++ golden packet decodes for hand "+hand,ControllerV6Protocol.TryDecode(fixture,hand,out var value)&&value.hand==hand&&value.sequence==65535&&value.sampledMilliseconds==0x12345678&&value.rawHall==3000&&Mathf.Abs(value.trigger-.5f)<.001f&&Mathf.Abs(value.linearAcceleration.y+2.5f)<.001f&&Mathf.Abs(value.angularVelocity.x-10)<.001f&&Mathf.Abs(value.gravity.z-9.81f)<.001f);
            }
            var p=Packet(1,1);Check("wrong hand rejected",!ControllerV6Protocol.TryDecode(p,2,out _));
            Check("truncation rejected",!ControllerV6Protocol.TryDecode(new byte[55],1,out _));
            p[30]^=4;Check("corruption rejected",!ControllerV6Protocol.TryDecode(p,1,out _));
            p=Packet(1,1);p[6]|=128;Seal(p);Check("unknown flags rejected",!ControllerV6Protocol.TryDecode(p,1,out _));
            p=Packet(1,1);Word(p,26,0);Seal(p);Check("invalid quaternion rejected",!ControllerV6Protocol.TryDecode(p,1,out _));
            p=Packet(1,1);Word(p,46,4096);Seal(p);Check("invalid ADC rejected",!ControllerV6Protocol.TryDecode(p,1,out _));
            p=Packet(1,1);Word(p,12,1);Seal(p);Check("unknown button rejected",!ControllerV6Protocol.TryDecode(p,1,out _));
            p=Packet(1,1);Word(p,14,32767);Word(p,16,32767);Seal(p);Check("invalid radial axis magnitude rejected",!ControllerV6Protocol.TryDecode(p,1,out _));
            var left=new ControllerV6State(1){Connected=true};var right=new ControllerV6State(2){Connected=true};var settings=new GameSettings();
            Check("neutral packet arms left",left.Submit(Packet(1,65535),1,0)&&left.Armed);
            Check("sequence wrap accepted",left.Submit(Packet(1,0),1.02,0));Check("duplicate rejected",!left.Submit(Packet(1,0),1.03,0));Check("backward rejected",!left.Submit(Packet(1,65535),1.04,0));
            Check("stale queue rejected",!left.Submit(Packet(1,1),2,301));Check("nonfinite host time rejected",!left.Submit(Packet(1,1),double.NaN,0));
            Check("neutral packet arms right",right.Submit(Packet(2,1),1,0)&&right.Armed);
            left.Tick(camera,1.02,.02f,settings,true);right.Tick(camera,1,.02f,settings,true);
            right.Submit(Packet(2,2,32),1.02,0);right.Tick(camera,1.02,.02f,settings,true);
            left.Disconnect();left.Tick(camera,1.04,.02f,settings,true);right.Tick(camera,1.04,.02f,settings,true);
            Check("left loss leaves right grip held",left.Frame.lost&&right.Frame.held&&!right.Frame.lost);
            right.Tick(camera,1.4,.02f,settings,true);Check("stale data releases and disarms",right.Frame.lost&&right.Frame.released&&!right.Armed);
            right.Disconnect();right.Connected=true;right.Submit(Packet(2,1,32),2,0);right.Tick(camera,2,.02f,settings,true);Check("reconnect held button cannot grab",!right.Armed&&!right.Frame.grabPressed);
            right.Submit(Packet(2,2),2.02,0);right.Tick(camera,2.02,.02f,settings,true);right.Submit(Packet(2,3,0,0,0,.8f),2.04,0);right.Tick(camera,2.04,.02f,settings,true);
            Check("analog trigger produces select edge",right.Frame.pressed&&right.Trigger>.79f);
            right.Submit(Packet(2,4),2.06,0);right.Tick(camera,2.06,.02f,settings,true);Check("trigger release edge",right.Frame.released&&!right.Frame.held);
            var motion=new ControllerV6State(1){Connected=true};double now=3;int seq=1;
            motion.Submit(Packet(1,seq++),now,0);motion.Tick(camera,now,.02f,settings,true);var initial=motion.Pose;
            motion.Submit(Packet(1,seq++,1024),now+=.02,0);motion.Tick(camera,now,.02f,settings,true);
            Check("stick click enters hand movement mode",motion.MovementMode);
            for(int n=0;n<60;n++){motion.Submit(Packet(1,seq++,0,16000,16000),now+=.02,0);motion.Tick(camera,now,.02f,settings,true);}
            Check("assisted XYZ moves right/forward",motion.Pose.position.x>initial.position.x+.1f&&motion.Pose.position.z>initial.position.z+.1f);
            float before=motion.Pose.position.y;
            for(int n=0;n<40;n++){motion.Submit(Packet(1,seq++,2,0,32767),now+=.02,0);motion.Tick(camera,now,.02f,settings,true);}
            Check("MODE plus stick moves vertically without Back",motion.Pose.position.y>before+.1f&&!motion.Frame.back);
            motion.Recenter(camera.transform.rotation);motion.Submit(Packet(1,seq++),now+=.02,0);motion.Tick(camera,now,.02f,settings,true);var neutral=motion.Pose;
            for(int n=0;n<50;n++){motion.Submit(Packet(1,seq++,0,0,0,0,Quaternion.Euler(0,90,0)),now+=.02,0);motion.Tick(camera,now,.02f,settings,true);}
            Check("quaternion rotation reaches corresponding hand",Quaternion.Angle(neutral.rotation,motion.Pose.rotation)>80);
            var q=Quaternion.Euler(25,-40,60);Check("quaternion sign equivalence",Quaternion.Angle(ControllerV6Protocol.ToUnity(q),ControllerV6Protocol.ToUnity(new Quaternion(-q.x,-q.y,-q.z,-q.w)))<.01f);
            motion.Recenter(camera.transform.rotation);
            for(int n=0;n<500;n++){motion.Submit(Packet(1,seq++,0,0,0,0,null,new Vector3(4,3,2)),now+=.02,0);motion.Tick(camera,now,.02f,settings,true);}
            Check("persistent acceleration remains bounded",(motion.Pose.position-motion.Settings.anchor).magnitude<=.151f);
            for(int n=0;n<180;n++){motion.Submit(Packet(1,seq++),now+=.02,0);motion.Tick(camera,now,.02f,settings,true);}
            Check("stationary hand returns to stable anchor",Vector3.Distance(motion.Pose.position,motion.Settings.anchor)<.001f);
            motion.Settings.anchor=new Vector3(float.NaN,0,0);motion.Tick(camera,now,.02f,settings,true);Check("invalid motion configuration recovers",float.IsFinite(motion.Pose.position.x));
            var cmd=ControllerV6Protocol.MakeCommand(4,50);Check("command CRC and layout",cmd.Length==8&&cmd[3]==4&&cmd[4]==50&&BleControllerProfile.Crc(cmd,6)==(cmd[6]|cmd[7]<<8));
            bool rejected=false;try{ControllerV6Protocol.MakeCommand(4,0);}catch(ArgumentOutOfRangeException){rejected=true;}Check("invalid configuration command rejected",rejected);
        }catch(Exception e){Check(e.ToString(),false);}
        finally{UnityEngine.Object.DestroyImmediate(root);}
        File.WriteAllLines(Evidence+"/core-checks.txt",Rows);
        if(Failures>0){EditorApplication.Exit(1);return;}
        SessionState.SetBool("VLAB.V6.Checks",true);SessionState.SetString("VLAB.TestProfile",Path.Combine(Path.GetTempPath(),"vlab-v6-"+Guid.NewGuid().ToString("N")));
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorApplication.isPlaying=true;
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void StartPlayChecks(){if(SessionState.GetBool("VLAB.V6.Checks",false)){var root=new GameObject("V6 play acceptance",typeof(ControllerV6PlayProbe));UnityEngine.Object.DontDestroyOnLoad(root);}}
    public static void Finish()
    {
        File.WriteAllLines(Evidence+"/play-checks.txt",Rows);SessionState.EraseBool("VLAB.V6.Checks");SessionState.EraseString("VLAB.TestProfile");Debug.Log("V6 ACCEPTANCE: "+Rows.Count+" checks, "+Failures+" failures. Synthetic packets; no physical BLE/device validation.");EditorApplication.Exit(Failures==0?0:1);
    }
}
public sealed class ControllerV6PlayProbe:MonoBehaviour
{
    IEnumerator Start()
    {
        ControllerV6Checks.Rows.Clear();ControllerV6Checks.Failures=0;yield return null;
        var inputSettings=InputSystem.settings;var oldFocus=inputSettings.backgroundBehavior;var oldEditor=inputSettings.editorInputBehaviorInPlayMode;var oldUpdates=inputSettings.updateMode;bool oldBackground=Application.runInBackground;
        // Hidden batch tests must receive their synthetic device events without Game-view focus.
        inputSettings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;inputSettings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;inputSettings.updateMode=InputSettings.UpdateMode.ProcessEventsManually;Application.runInBackground=true;
        try{GamepadIntegrationChecks.Run();ControllerV6Checks.Check("existing HID input regression",true);}catch(Exception e){ControllerV6Checks.Check("existing HID input: "+e,false);}
        try{PhoneInputChecks.Run();ControllerV6Checks.Check("existing phone input regression",true);}catch(Exception e){ControllerV6Checks.Check("existing phone input: "+e,false);}
        inputSettings.backgroundBehavior=oldFocus;inputSettings.editorInputBehaviorInPlayMode=oldEditor;inputSettings.updateMode=oldUpdates;Application.runInBackground=oldBackground;
        foreach(var room in new[]{"Physics","Chemistry","Biology","Engineering"}) {
            yield return SceneManager.LoadSceneAsync(room);yield return null;yield return null;
            string id=room=="Physics"?"P01":room=="Chemistry"?"C04":room=="Biology"?"B01":"M01";
            GameObject root=null;
            try {
                var top=UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.enabled&&r.bounds.max.y>.72f&&r.bounds.max.y<1.12f&&r.bounds.size.x>.65f&&r.bounds.size.z>.3f).OrderByDescending(r=>r.bounds.size.x*r.bounds.size.z).First();
                root=new GameObject("V6 actual kit "+id);var rig=root.AddComponent<ApparatusRig>();var session=new ExperimentSession(id);rig.Build(session,new Vector3(top.bounds.center.x,top.bounds.max.y+.005f,top.bounds.center.z));
                var pair=rig.items.Where(i=>!i.stowed).Take(2).ToArray();if(pair.Length<2)throw new Exception("Two accessible source instruments required");
                var rest=pair.Select(i=>new Pose(i.transform.position,i.transform.rotation)).ToArray();var pose=new Pose(rest[0].position+Vector3.up*.2f,Quaternion.identity);
                ControllerV6Checks.Check(room+" left grabs source instrument",rig.BeginHandGrab(1,pair[0],pose,pair[0].transform.position));
                ControllerV6Checks.Check(room+" right grabs a different source instrument",rig.BeginHandGrab(2,pair[1],pose,pair[1].transform.position));
                ControllerV6Checks.Check(room+" duplicate ownership rejected",!rig.BeginHandGrab(2,pair[0],pose,pair[0].transform.position));
                pose.rotation=Quaternion.Euler(20,40,60);rig.UpdateHandGrab(1,pose,.7f);rig.UpdateHandGrab(2,new Pose(pose.position+Vector3.right*.2f,pose.rotation),.25f);
                ControllerV6Checks.Check(room+" held instrument follows XYZ and rotation",Vector3.Distance(pair[0].transform.position,rest[0].position)>.1f&&Quaternion.Angle(pair[0].transform.rotation,rest[0].rotation)>10);
                rig.EndHandGrab(1,false);ControllerV6Checks.Check(room+" left loss leaves right holding",!rig.HandItem(1)&&rig.HandItem(2)==pair[1]);
                rig.EndHandGrab(2,false);ControllerV6Checks.Check(room+" release restores supported poses",pair.Select((item,i)=>Vector3.Distance(item.transform.position,rest[i].position)<.0001f&&Quaternion.Angle(item.transform.rotation,rest[i].rotation)<.01f).All(x=>x));
                ControllerV6Checks.Check(room+" manipulation preserves lesson setup phase",session.Phase==ExperimentPhase.Preparing&&!rig.Ready);
                if(room=="Chemistry") {
                    foreach(var item in rig.items.ToArray()){rig.Select(item);if(!rig.InstallSelected())throw new Exception("Cannot install "+item.assetId);}
                    var dropper=rig.items.First(i=>i.assetId=="dropper");pose=new Pose(dropper.transform.position,Quaternion.identity);
                    ControllerV6Checks.Check("C04 installed dropper can be held",rig.BeginHandGrab(1,dropper,pose,dropper.transform.position));rig.UpdateHandGrab(1,pose,.73f);
                    ControllerV6Checks.Check("C04 analog Hall drives actual delivered dropper",Mathf.Abs(dropper.GetComponentInChildren<VlabDropper>().Squeeze-.73f)<.001f);rig.EndHandGrab(1,false);
                }
            }catch(Exception e){ControllerV6Checks.Check(room+" integration: "+e,false);}
            if(root)Destroy(root);yield return null;
        }
        yield return SceneManager.LoadSceneAsync("VLAB_Boot");yield return null;yield return null;
        try {
            var app=VlabApp.Instance;ControllerV6Checks.Check("real boot creates dual BLE runtime",app&&app.Input.Dual);
            typeof(VlabApp).GetMethod("ShowDualControllers",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(app,new object[]{true,0});
            ControllerV6Checks.Check("live dual diagnostics connected to real app UI",app.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true).Any(t=>t.text.Contains("V6")));
        }catch(Exception e){ControllerV6Checks.Check("real boot UI: "+e,false);}
        yield return null;yield return null;ControllerV6Checks.Finish();
    }
}
#endif
