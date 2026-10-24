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
using Vlab.Product;

public static class ControllerV6RegressionChecks
{
    public static readonly List<string> Rows=new List<string>();public static int Failures;
    public static readonly GameSettings Bindings=new GameSettings();
    public static void Check(string name,bool ok){Rows.Add((ok?"PASS ":"FAIL ")+name);if(!ok)Failures++;}
    public static void Case(string name,Action test){try{test();}catch(Exception e){Check(name+" exception: "+e,false);}}
    public static byte[] Packet(int hand,int seq,int buttons=0,float trigger=0,uint? stamp=null,Vector3? acceleration=null,bool valid=true,Quaternion? rotation=null)
    {
        var p=ControllerV6Checks.Packet(hand,seq,buttons,0,0,trigger,rotation,acceleration);
        if(stamp.HasValue)for(int i=0;i<4;i++)p[8+i]=(byte)(stamp.Value>>(8*i));
        if(!valid)p[6]=6;ControllerV6Checks.Word(p,54,BleControllerProfile.Crc(p,54));return p;
    }
    public static ControllerV6State Armed(Camera view,int hand=1,double at=1)
    {var s=new ControllerV6State(hand){Connected=true};s.Submit(Packet(hand,1,stamp:1000),at,0);s.Tick(view,at,.02f,Bindings,true);return s;}
    static string Edges(ControllerV6State s,Camera camera,int count,double now)
    {
        var result=new List<string>();for(int i=0;i<count;i++){s.Tick(camera,now+i*.02,.02f,Bindings,true);var f=s.Frame;if(f.pressed)result.Add("select");if(f.grabPressed)result.Add("grip");if(f.released)result.Add("release");if(f.menu)result.Add("menu");if(f.recenter)result.Add("recenter");if(f.back)result.Add("back");}
        return string.Join(",",result);
    }
    public static void Core(Camera camera)
    {
        Case("F01",()=>{
            var s=Armed(camera);s.Submit(Packet(1,2,32),1.01,0);s.Submit(Packet(1,3),1.02,0);
            Check("F01 Grip press/release before Tick retained once in order",Edges(s,camera,4,1.03)=="grip,release");
            s=Armed(camera);for(int i=0;i<4;i++)s.Submit(Packet(1,i+2,i%2==0?32:0),1.01+i*.01,0);
            Check("F01 two clicks before one Tick retain both counts",Edges(s,camera,6,1.05)=="grip,release,grip,release");
            s=Armed(camera);s.Submit(Packet(1,2,32),1.01,0);s.Tick(camera,1.01,.01f,Bindings,true);s.Submit(Packet(1,3),1.02,0);s.Submit(Packet(1,4,32),1.03,0);
            Check("F01 release/repress is not continuous hold",Edges(s,camera,3,1.04)=="release,grip");
            s=Armed(camera);s.Submit(Packet(1,2,trigger:.6f),1.01,0);s.Submit(Packet(1,3,trigger:.4f),1.02,0);
            Check("F01 Hall threshold edges survive batching",Edges(s,camera,3,1.03)=="select,release");
            foreach(int button in new[]{256,512,2}){s=Armed(camera);s.Submit(Packet(1,2,button),1.01,0);s.Submit(Packet(1,3),1.02,0);Check("F01 edge survives batch for mask "+button,Edges(s,camera,3,1.03)==(button==256?"menu":button==512?"recenter":"back"));}
            s=Armed(camera);s.Submit(Packet(1,2,1024),1.01,0);s.Submit(Packet(1,3),1.02,0);s.Tick(camera,1.03,.02f,Bindings,true);Check("F01 joystick click survives batching",s.MovementMode);
            foreach(int hz in new[]{50,100}){
                s=Armed(camera);int next=1,seq=2,presses=0,releases=0;
                for(int frame=1;frame<=30;frame++){
                    double now=1+frame/30d;
                    while(next<=hz&&1+next/(double)hz<=now+1e-9){double t=1+next/(double)hz;bool down=(t>=1.1&&t<1.16)||(t>=1.3&&t<1.36);s.Submit(Packet(1,seq++,down?32:0),now,(now-t)*1000);next++;}
                    s.Tick(camera,now,1f/30,Bindings,true);if(s.Frame.grabPressed)presses++;if(s.Frame.released)releases++;
                }
                Check("F01 30 FPS / "+hz+" Hz preserves two physical clicks",presses==2&&releases==2);
            }
            s=Armed(camera);s.Submit(Packet(1,2,32),1.01,0);s.Submit(Packet(1,3),1.02,0);s.CancelUntilReleased();
            Check("F01 context cancellation discards queued clicks",Edges(s,camera,3,1.03)=="");
        });
        Case("F02",()=>{
            var s=Armed(camera);s.Submit(Packet(1,2,32),1.02,0);s.Tick(camera,1.02,.02f,Bindings,true);
            s.Submit(Packet(1,3,32),1.5,0);s.Tick(camera,1.5,.05f,Bindings,true);
            Check("F02 480ms gap detected when Submit comes before Tick",s.Frame.lost&&s.Frame.released&&!s.Armed&&!s.Frame.held);
            s.Tick(camera,1.51,.01f,Bindings,true);Check("F02 loss/release emitted only once",!s.Frame.lost&&!s.Frame.released);
            s.Submit(Packet(1,4),1.52,0);s.Tick(camera,1.52,.01f,Bindings,true);s.Submit(Packet(1,5,32),1.54,0);s.Tick(camera,1.54,.02f,Bindings,true);
            Check("F02 neutral then a new press rearms",s.Armed&&s.Frame.grabPressed);
            s=Armed(camera);s.Submit(Packet(1,2,32),1.02,0);s.Tick(camera,1.02,.02f,Bindings,true);s.Tick(camera,1.5,.05f,Bindings,true);
            Check("F02 Tick before Submit also detects loss",s.Frame.lost&&!s.Armed);
            s=Armed(camera);s.Submit(Packet(1,2,32),1.02,0);s.Tick(camera,1.02,.02f,Bindings,true);
            for(int i=0;i<12;i++)s.Submit(Packet(1,3+i,32),1.5,(1.5-(1.22+i*.02))*1000);
            s.Tick(camera,1.5,.05f,Bindings,true);Check("F02 gameplay stall cancels despite fresh native backlog",s.Frame.lost&&!s.Armed&&!s.Frame.held);
            var left=Armed(camera);var right=Armed(camera,2);right.Submit(Packet(2,2,32),1.02,0);right.Tick(camera,1.02,.02f,Bindings,true);
            for(int i=0;i<25;i++){double now=1.04+i*.02;right.Submit(Packet(2,i+3,32),now,0);right.Tick(camera,now,.02f,Bindings,true);}
            left.Submit(Packet(1,2,32),1.52,0);left.Tick(camera,1.52,.02f,Bindings,true);
            Check("F02 one hand loss leaves the other hand held",left.Frame.lost&&!left.Armed&&right.Armed&&right.Frame.held);
            s=Armed(camera);var bad=Packet(1,2);bad[20]^=1;s.Submit(bad,1.4,0);Check("F02 rejected packet cannot advance valid arrival time",Math.Abs(s.ReceivedAt-1)<1e-8);
        });
        Case("F05",()=>{
            var reference=MotionTrace(camera,60,50,false);float worst=0;
            Check("F05 gestures remain enabled and produce nonzero bounded movement",reference.magnitude>.001f&&reference.magnitude<=.09f);
            foreach(int fps in new[]{25,30,60,90})foreach(bool burst in new[]{false,true})worst=Mathf.Max(worst,Vector3.Distance(reference,MotionTrace(camera,fps,50,burst)));
            Rows.Add("MEASURE F05 same trace max deviation meters="+worst.ToString("F8"));
            Check("F05 same IMU trace across FPS/batching within 0.5mm",worst<=.0005f);
            foreach(int hz in new[]{20,50,100})Check("F05 "+hz+"Hz resampling within 5mm",Vector3.Distance(reference,MotionTrace(camera,60,hz,false))<=.005f);
            Check("F05 duplicate IMU timestamps are not integrated twice",Vector3.Distance(MotionTrace(camera,60,50,false),MotionTrace(camera,60,50,false,true))<=.0005f);
            var s=Armed(camera);s.Submit(Packet(1,2,stamp:1500,acceleration:Vector3.right*4),1.02,0);s.Tick(camera,1.02,.02f,Bindings,true);
            Check("F05 missing sensor interval cannot invent an impulse",Vector3.Distance(s.Pose.position,s.Settings.anchor)<.0001f);
            s=Armed(camera);s.Submit(Packet(1,2,stamp:1020,valid:false),1.02,0);s.Tick(camera,1.02,.02f,Bindings,true);s.Submit(Packet(1,3,stamp:1040,acceleration:Vector3.right*4),1.04,0);s.Tick(camera,1.04,.02f,Bindings,true);
            Check("F05 IMU recovery starts a new motion baseline",Vector3.Distance(s.Pose.position,s.Settings.anchor)<.0001f);
        });
    }
    static Vector3 MotionTrace(Camera camera,int fps,int hz,bool burst,bool duplicate=false)
    {
        var s=Armed(camera);int next=1,seq=2;
        for(int frame=1;frame<=fps;frame++){
            double now=1+frame/(double)fps;
            if(!burst||frame%3==0||frame==fps)while(next<=hz&&1+next/(double)hz<=now+1e-8){
                double at=1+next/(double)hz;uint stamp=(uint)(1000+Math.Round(next*1000d/hz));Vector3 accel=at<=1.45?Vector3.right*4:Vector3.zero;
                s.Submit(Packet(1,seq++,stamp:stamp,acceleration:accel),now,Math.Max(0,(now-at)*1000));
                if(duplicate)s.Submit(Packet(1,seq++,stamp:stamp,acceleration:accel),now,Math.Max(0,(now-at)*1000));next++;
            }
            s.Tick(camera,now,1f/fps,Bindings,true);
        }
        return s.Pose.position-s.Settings.anchor;
    }
    public static object Get(object o,string name)=>o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(o);
    public static void Set(object o,string name,object value){var property=o.GetType().GetProperty(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);if(property!=null)property.SetValue(o,value);else o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,value);}
    public static object Invoke(object o,string name,params object[] args)=>o.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).Invoke(o,args);
    public static ActionFrame Hands(VlabApp app,ActionFrame frame){var args=new object[]{frame};Invoke(app,"UpdateDualHands",args);return (ActionFrame)args[0];}
    public static void Run()
    {
        string label=Environment.GetEnvironmentVariable("VLAB_V6_REGRESSION_LABEL")??"after";SessionState.SetString("VLAB.V6.Regression",label);
        SessionState.SetString("VLAB.TestProfile",Path.Combine(Path.GetTempPath(),"vlab-v6-regression-"+Guid.NewGuid().ToString("N")));
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorApplication.isPlaying=true;
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void StartProbe(){if(SessionState.GetString("VLAB.V6.Regression","")!=""){var root=new GameObject("V6 regression probe",typeof(ControllerV6RegressionProbe));UnityEngine.Object.DontDestroyOnLoad(root);}}
    public static void Finish()
    {
        string label=SessionState.GetString("VLAB.V6.Regression","after"),folder="Evidence/ControllerV6Fixes";Directory.CreateDirectory(folder);File.WriteAllLines(folder+"/"+label+"-regressions.txt",Rows);
        SessionState.EraseString("VLAB.V6.Regression");SessionState.EraseString("VLAB.TestProfile");Debug.Log("V6 REGRESSIONS "+label+": "+Rows.Count+" results, "+Failures+" failures; Unity Play mode, synthetic BLE data.");EditorApplication.Exit(Failures==0?0:1);
    }
}
public sealed class ControllerV6RegressionProbe:MonoBehaviour
{
    IEnumerator Start()
    {
        ControllerV6RegressionChecks.Rows.Clear();ControllerV6RegressionChecks.Failures=0;
        var cameraGo=new GameObject("V6 core camera",typeof(Camera));ControllerV6RegressionChecks.Core(cameraGo.GetComponent<Camera>());Destroy(cameraGo);
        yield return SceneManager.LoadSceneAsync("VLAB_Boot");yield return null;yield return null;
        var app=VlabApp.Instance;if(!app){ControllerV6RegressionChecks.Check("real VLAB boot",false);ControllerV6RegressionChecks.Finish();yield break;}
        app.enabled=false;
        foreach(var canvas in app.GetComponentsInChildren<UnityEngine.UI.GraphicRaycaster>(true))canvas.enabled=false;
        ControllerV6RegressionChecks.Set(app,"Route","activity");ControllerV6RegressionChecks.Set(app,"inputAfter",-1f);ControllerV6RegressionChecks.Set(app,"changing",false);
        void TestRig(string name,Action<ApparatusRig,ExperimentSession> body){
            var root=new GameObject("V6 regression kit "+name);try{var rig=root.AddComponent<ApparatusRig>();var session=new ExperimentSession("C04");rig.Build(session,new Vector3(5,.8f,0));ControllerV6RegressionChecks.Set(app,"Apparatus",rig);ControllerV6RegressionChecks.Set(app,"Session",session);body(rig,session);}catch(Exception e){ControllerV6RegressionChecks.Check(name+" exception: "+e,false);}finally{ControllerV6RegressionChecks.Invoke(app,"CancelFlagshipDrag");ControllerV6RegressionChecks.Set(app,"Apparatus",null);DestroyImmediate(root);}}
        TestRig("F03",(rig,session)=>{
            var dual=app.Input.Dual;dual.LeftControllerState.Disconnect();dual.RightControllerState.Disconnect();var left=dual.LeftControllerState;var right=dual.RightControllerState;
            left.Connected=right.Connected=true;double now=Math.Max(0,Time.realtimeSinceStartupAsDouble-.01);left.Submit(ControllerV6RegressionChecks.Packet(1,1),now,0);right.Submit(ControllerV6RegressionChecks.Packet(2,1),now,0);app.Input.Read();
            var item=rig.items.First(i=>!i.stowed);var pose=new Pose(item.transform.position,Quaternion.identity);rig.BeginHandGrab(1,item,pose,item.transform.position);left.Submit(ControllerV6RegressionChecks.Packet(1,2,32),now+.0001,0);ControllerV6RegressionChecks.Hands(app,app.Input.Read());
            right.Disconnect();var f=app.Input.Read();ControllerV6RegressionChecks.Hands(app,f);
            ControllerV6RegressionChecks.Check("F03 right loss/pointer switch preserves left hold",rig.HandItem(1)==item&&left.Armed);
            ControllerV6RegressionChecks.Check("F03 pointer switch is not whole-session input loss",!f.lost&&!f.pressed);
        });
        var promotionRoot=new GameObject("F03 right pointer promotion kit");ApparatusRig promotionRig=null;
        try{promotionRig=promotionRoot.AddComponent<ApparatusRig>();var session=new ExperimentSession("C04");promotionRig.Build(session,new Vector3(5,.8f,0));ControllerV6RegressionChecks.Set(app,"Apparatus",promotionRig);ControllerV6RegressionChecks.Set(app,"Session",session);app.Input.Dual.LeftControllerState.Disconnect();app.Input.Dual.RightControllerState.Disconnect();}
        catch(Exception e){ControllerV6RegressionChecks.Check("F03 promotion setup "+e,false);}
        yield return null;
        try{double arrival=Time.realtimeSinceStartupAsDouble;foreach(int hand in new[]{1,2}){var state=app.Input.Dual.Hand(hand);state.Connected=true;state.Submit(ControllerV6RegressionChecks.Packet(hand,1),arrival,0);}app.Input.Read();}
        catch(Exception e){ControllerV6RegressionChecks.Check("F03 reconnect neutral "+e,false);}
        yield return null;
        try{
            var right=app.Input.Dual.RightControllerState;right.Submit(ControllerV6RegressionChecks.Packet(2,2,32),Time.realtimeSinceStartupAsDouble,0);var frame=app.Input.Read();
            var item=promotionRig.items.First(i=>!i.stowed);if(!right.Armed||!promotionRig.BeginHandGrab(2,item,right.Pose,item.transform.position))throw new Exception("Fresh armed right hold required before pointer promotion");
            ControllerV6RegressionChecks.Hands(app,frame);ControllerV6RegressionChecks.Set(app.Input,"dualPrimary",1);
            ControllerV6RegressionChecks.Hands(app,app.Input.Read());ControllerV6RegressionChecks.Check("F03 restored right pointer preserves independently acquired right hold",promotionRig.HandItem(2)==item&&right.Armed);
        }catch(Exception e){ControllerV6RegressionChecks.Check("F03 right pointer promotion "+e,false);}
        ControllerV6RegressionChecks.Set(app,"Apparatus",null);DestroyImmediate(promotionRoot);
        TestRig("F04 hand legacy",(rig,session)=>{
            var item=rig.items.First(i=>!i.stowed);var pose=new Pose(item.transform.position+Vector3.up*.2f,Quaternion.identity);rig.BeginHandGrab(1,item,pose,item.transform.position);rig.UpdateHandGrab(1,pose,0);Vector3 held=item.transform.position;
            rig.BeginGrab(item,new Ray(held+Vector3.left,Vector3.right));rig.UpdateGrab(new ActionFrame{pointer=new Ray(held+Vector3.right*2,Vector3.right),held=true});
            ControllerV6RegressionChecks.Check("F04 hand owner blocks legacy transform writes",Vector3.Distance(item.transform.position,held)<.0001f);
        });
        TestRig("F04 take install cancel",(rig,session)=>{
            var item=rig.items.First(i=>!i.stowed);var pose=new Pose(item.transform.position+Vector3.up*.2f,Quaternion.identity);rig.BeginHandGrab(1,item,pose,item.transform.position);rig.UpdateHandGrab(1,pose,0);Vector3 held=item.transform.position;
            rig.Select(item);ControllerV6RegressionChecks.Check("F04 legacy TakeSelected cannot acquire hand-owned item",!rig.TakeSelected(new Ray(held,Vector3.forward),out _));
            ControllerV6RegressionChecks.Check("F04 installation rejects hand-owned item",!rig.InstallSelected());rig.CancelGrab();
            ControllerV6RegressionChecks.Check("F04 legacy cancel cannot return another owner's item",Vector3.Distance(item.transform.position,held)<.0001f&&rig.HandItem(1)==item);
        });
        TestRig("F04 legacy hand",(rig,session)=>{
            var item=rig.items.First(i=>!i.stowed);rig.BeginGrab(item,new Ray(item.transform.position-Vector3.forward,Vector3.forward));
            ControllerV6RegressionChecks.Check("F04 legacy owner blocks hand acquisition",!rig.BeginHandGrab(1,item,new Pose(item.transform.position,Quaternion.identity),item.transform.position));
        });
        foreach(bool handFirst in new[]{true,false})TestRig("F04 flagship "+handFirst,(rig,session)=>{
            foreach(var item in rig.items.ToArray()){rig.Select(item);if(!rig.InstallSelected())throw new Exception("Install "+item.assetId);}
            ControllerV6RegressionChecks.Invoke(app,"ConfigureFlagshipTargets");
            var tool=rig.items.First(i=>i.assetId=="dropper");var pose=new Pose(tool.transform.position,Quaternion.identity);var collider=tool.GetComponentInChildren<Collider>();Physics.SyncTransforms();
            var ray=new Ray(collider.bounds.center-Vector3.forward,Vector3.forward);if(!collider.Raycast(ray,out var hit,3))throw new Exception("Dropper collider hit required");
            if(handFirst){rig.BeginHandGrab(1,tool,pose,tool.transform.position);ControllerV6RegressionChecks.Invoke(app,"BeginFlagship",tool,hit,new ActionFrame{pointer=ray,held=true});ControllerV6RegressionChecks.Check("F04 hand owner blocks flagship acquisition",ControllerV6RegressionChecks.Get(app,"flagshipDrag")==null&&rig.HandItem(1)==tool);}
            else{ControllerV6RegressionChecks.Invoke(app,"BeginFlagship",tool,hit,new ActionFrame{pointer=ray,held=true});ControllerV6RegressionChecks.Check("F04 flagship owner blocks hand acquisition",!rig.BeginHandGrab(1,tool,pose,tool.transform.position));}
        });
        ControllerV6RegressionChecks.Case("F08",()=>{
            var dual=app.Input.Dual;dual.LeftControllerState.Disconnect();dual.RightControllerState.Disconnect();var s=dual.LeftControllerState;s.Connected=true;app.View.transform.rotation=Quaternion.identity;
            double now=Math.Max(0,Time.realtimeSinceStartupAsDouble-.01);s.Submit(ControllerV6RegressionChecks.Packet(1,1),now,0);app.Input.Read();
            s.Submit(ControllerV6RegressionChecks.Packet(1,2,rotation:Quaternion.Euler(0,80,0)),now+.0001,0);app.Input.Read();app.View.transform.rotation=Quaternion.Euler(0,-65,0);
            s.Submit(ControllerV6RegressionChecks.Packet(1,3,valid:false),now+.0002,0);var f=app.Input.Read();ControllerV6RegressionChecks.Hands(app,f);
            var lines=(LineRenderer[])ControllerV6RegressionChecks.Get(app,"handRays");var line=lines[0];Vector3 direction=(line.GetPosition(1)-line.GetPosition(0)).normalized;
            ControllerV6RegressionChecks.Check("F08 actual displayed laser agrees with effective input ray",Vector3.Angle(direction,f.pointer.direction)<.01f&&Vector3.Distance(line.GetPosition(0),f.pointer.GetPoint(.06f))<.0001f);
            ControllerV6RegressionChecks.Check("F08 state/Grip and primary pointer use same gaze fallback",Vector3.Angle(s.Frame.pointer.direction,f.pointer.direction)<.01f&&Vector3.Distance(s.Frame.pointer.origin,f.pointer.origin)<.0001f);
        });
        ControllerV6RegressionChecks.Case("R01 observation",()=>{
            var runtime=app.Input.Dual;runtime.LeftControllerState.Disconnect();runtime.LeftControllerState.Connected=true;
            ControllerV6RegressionChecks.Invoke(runtime,"OnApplicationFocus",false);runtime.LeftControllerState.Submit(ControllerV6RegressionChecks.Packet(1,1),Time.realtimeSinceStartupAsDouble,0);
            ControllerV6RegressionChecks.Rows.Add("OBSERVE R01 neutral Submit after focus=false Armed="+runtime.LeftControllerState.Armed+"; background Android behavior not exercised");ControllerV6RegressionChecks.Invoke(runtime,"OnApplicationFocus",true);
        });
        ControllerV6RegressionChecks.Case("R07 observation",()=>{
            var settings=new GameSettings();bool accepted=ControllerBindings.TryAssign(settings,ControllerAction.Select,4);
            ControllerV6RegressionChecks.Rows.Add("OBSERVE R07 generic settings permit Select=A (mask0x1, absent in V6): "+accepted);
            var s=ControllerV6RegressionChecks.Armed(app.View);s.Submit(ControllerV6RegressionChecks.Packet(1,2,trigger:.8f),1.02,0);s.Tick(app.View,1.02,.02f,settings,true);
            ControllerV6RegressionChecks.Rows.Add("OBSERVE R07 Select remapped to A: Hall0.8 produces Frame.pressed="+s.Frame.pressed+" while ActionTrigger="+s.ActionTrigger.ToString("F2"));
        });
        ControllerV6RegressionChecks.Finish();
    }
}
#endif
