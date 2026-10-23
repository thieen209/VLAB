#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Vlab.Product;

public static class FinalPassBleChecks
{
    sealed class Transport:IBleTransport
    {
        public int Connections; public string State=>"Idle"; public int Generation=>0;
        public string Poll()=>null; public void Scan(){} public void StopScan(){}
        public void Connect(string a,string s,string c,bool r){Connections++;}
        public void Stop(){} public void Dispose(){}
    }
    public static void Run()
    {
        var rows=new System.Collections.Generic.List<string>(); int failures=0;
        Action<string,bool> check=(name,ok)=>{rows.Add((ok?"PASS ":"FAIL ")+name);if(!ok)failures++;};
        string folder=Path.Combine(Path.GetTempPath(),"VlabBleChecks-"+Guid.NewGuid().ToString("N"));
        var go=new GameObject("BLE lifecycle checks");
        try
        {
            var runtime=go.AddComponent<BleControllerRuntime>();runtime.Initialize(folder);
            var transport=new Transport();runtime.SetBackendForChecks(transport);
            var pause=typeof(BleControllerRuntime).GetMethod("OnApplicationPause",BindingFlags.Instance|BindingFlags.NonPublic);
            runtime.Connect("AA:BB:CC:DD:EE:01");check("explicit inspect connection",transport.Connections==1);
            pause.Invoke(runtime,new object[]{true});pause.Invoke(runtime,new object[]{false});
            check("active inspection resumes once",transport.Connections==2);
            runtime.Stop();pause.Invoke(runtime,new object[]{true});pause.Invoke(runtime,new object[]{false});
            check("explicit stop survives pause/resume",transport.Connections==2);
            pause.Invoke(runtime,new object[]{false});check("duplicate resume does not reconnect",transport.Connections==2);
            runtime.Connect("invalid address");check("invalid address never reaches native transport",transport.Connections==2);
            var profile=new BleControllerProfile{packetLength=4,sequenceOffset=0,sequenceBytes=2,buttonsOffset=2};profile.Validate();
            var provider=new BleControllerProvider();provider.Configure(profile);
            check("first packet accepted",provider.Submit(new byte[]{255,255,0,0},1,0));
            check("sequence wraps",provider.Submit(new byte[]{0,0,0,0},1.01,0));
            check("duplicate rejected",!provider.Submit(new byte[]{0,0,0,0},1.02,0));
            check("old packet rejected",!provider.Submit(new byte[]{255,255,0,0},1.03,0));
            check("stale native queue rejected",!provider.Submit(new byte[]{1,0,0,0},2,1000));
            check("truncated packet rejected",!provider.Submit(new byte[]{1},2,0));
            check("nonfinite time rejected",!provider.Submit(new byte[]{1,0,0,0},double.NaN,0));
            profile.axisX=int.MinValue;bool invalid=false;try{profile.Validate();}catch(InvalidDataException){invalid=true;}check("extreme axis rejected safely",invalid);
            string storage=Path.Combine(folder,"persistence");var store=new BleProfileStore(storage);var saved=new BleProfileCollection();
            saved.Selected.name="Tay phải";check("independent profile saves",store.Save(saved,out _));
            check("profile survives reload",new BleProfileStore(storage).Load().Selected.name=="Tay phải");
            string primary=Path.Combine(storage,"ble-profiles.json");File.WriteAllText(primary,"broken");
            check("valid backup recovers damaged primary",new BleProfileStore(storage).Load().Selected.name=="Tay phải");
            File.WriteAllText(primary+".backup","also broken");var damaged=new BleProfileStore(storage);damaged.Load();
            check("damaged copies prevent overwrite",damaged.Protected&&!damaged.Save(new BleProfileCollection(),out _)&&File.ReadAllText(primary)=="broken");
            check("explicit reset archives both copies",damaged.Reset(out _)&&Directory.GetFiles(storage,"*.preserved-*").Length==2);
        }
        catch(Exception e){rows.Add("FAIL "+e);failures++;}
        finally{UnityEngine.Object.DestroyImmediate(go);}
        Directory.CreateDirectory("Evidence/FinalPass");File.WriteAllLines("Evidence/FinalPass/ble-core-checks.txt",rows);
        Debug.Log("BLE checks: "+rows.Count+" checks, "+failures+" failures; simulated transport only.");EditorApplication.Exit(failures==0?0:1);
    }
}
#endif
