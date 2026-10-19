#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Vlab.Product;

// Run only after root integrates staged files and the active Android build ends.
// These checks instantiate the real Resources prefabs and exercise SDK methods.
// They do not certify headset ergonomics, rendered appearance or device input.
public static class ChemistryIntegrationChecks
{
    static readonly List<string> evidence=new List<string>();
    static void Assert(bool value,string message){if(!value)throw new Exception(message);evidence.Add("PASS "+message);}
    static void Action(ApparatusRig rig,string action)
    {Assert(rig.Driver.Operate(action,out var why),rig.Driver.GetType().Name+" "+action+" "+why);}
    static void Prepare(ApparatusRig rig)
    {Assert(rig.PrepareNext(out var why),"Prepare operation "+(rig.Driver==null?"?":rig.Driver.GetType().Name)+" "+why);}
    static void Tick(ExperimentSession session,double seconds)
    {int whole=(int)Math.Floor(seconds);for(int i=0;i<whole;i++)session.Advance(1);if(seconds-whole>1e-9)session.Advance(seconds-whole);}
    [MenuItem("VLAB/Checks/Chemistry apparatus integration")]
    public static void Run()
    {
        if(!Application.isPlaying)throw new InvalidOperationException("Run these apparatus integration checks in Play mode so the delivered SDK Awake geometry initialization has executed.");
        evidence.Clear();bool success=true;
        foreach(string id in new[]{"C01","C02","C03","C04","C05","C06","C07","C08"})
        {
            var root=new GameObject("Chemistry integration "+id);
            try
            {
                var session=new ExperimentSession(id);var rig=root.AddComponent<ApparatusRig>();rig.Build(session,new Vector3(0,.8f,0));
                foreach(var item in rig.items){rig.Select(item);Assert(rig.InstallSelected(),id+" install "+item.assetId);}
                Assert(rig.Ready,id+" apparatus installed");
                Assert(!rig.Driver.TryMeasure(out _,out _),id+" rejects measurement before preparation");
                switch(id)
                {
                    case "C01":
                        Prepare(rig);Prepare(rig);Action(rig,"initial");Prepare(rig);Prepare(rig);
                        Assert(session.Start(out var startWhy),id+" start "+startWhy);
                        for(int trial=0;trial<3;trial++)
                        {
                            if(trial>0){Action(rig,"fresh");Action(rig,"initial");Action(rig,"aliquot");Action(rig,"indicator");}
                            Assert(session.SetParameter("flow",100,out _),"C01 live valve control");Tick(session,20);Action(rig,"close");Action(rig,"swirl");
                            Assert(!rig.Driver.CanRecord(out _),"C01 rejects incomplete concordant series");Tick(session,30);Action(rig,"final");Action(rig,"titre");
                        }
                        Assert(rig.Driver.CanRecord(out var concordance),"C01 three concordant actual transfers "+concordance);break;
                    case "C02":
                        Prepare(rig);Prepare(rig);Prepare(rig);Assert(session.Start(out startWhy),id+" start "+startWhy);Tick(session,2);
                        double before=rig.Driver.Observe().Get("voltage");Action(rig,"reverse");double after=rig.Driver.Observe().Get("voltage");Assert(Math.Abs(before+after)<1e-8,"C02 actual reversed probes reverse sign");
                        Action(rig,"bridgeOff");Assert(!rig.Driver.CanRecord(out _),"C02 removed salt bridge blocks measurement");Action(rig,"bridgeOn");break;
                    case "C03":
                        Prepare(rig);Prepare(rig);Prepare(rig);Assert(session.Start(out startWhy),id+" start "+startWhy);Tick(session,session.parameters["time"]);
                        double q=rig.Driver.Observe().Get("charge");Assert(Math.Abs(q-session.parameters["time"]*session.parameters["current"])<1e-8,"C03 integrates actual source current on explicit clock");
                        Assert(!rig.Driver.CanRecord(out _),"C03 wet electrode cannot be recorded as dry mass");Action(rig,"off");Action(rig,"rinseCathode");Action(rig,"dryCathode");Action(rig,"weighFinal");break;
                    case "C04":
                        Prepare(rig);Assert(!rig.Driver.Operate("dose:water",out _),"C04 rejects cup click without selected reagent");
                        Action(rig,"selectAg");Action(rig,"dose:water");Assert(!rig.Driver.Operate("dose:nitrate",out _),"C04 selection clears after transfer");Action(rig,"selectAg");Action(rig,"dose:nitrate");Prepare(rig);
                        Action(rig,"selectAg");Action(rig,"dose:nacl");Prepare(rig);Assert(session.Start(out startWhy),id+" start "+startWhy);Tick(session,3);
                        Assert(rig.Driver.Observe().Get("NaClSolid")==1&&rig.Driver.Observe().Get("NaNO3Solid")==0,"C04 qualitative positive and controls");break;
                    case "C05":
                        Prepare(rig);Prepare(rig);Action(rig,"waterMark");Action(rig,"invert");Prepare(rig);Assert(session.Start(out startWhy),id+" start "+startWhy);Tick(session,2);
                        Assert(Math.Abs(rig.Driver.Observe().Get("concentration")-.2)<1e-8,"C05 10 mL 1 M aliquot diluted to actual50 mL");
                        Action(rig,"serial");Action(rig,"waterMark");Action(rig,"invert");Assert(Math.Abs(rig.Driver.Observe().Get("concentration")-.02)<1e-8,"C05 serial sample conserves measured aliquot solute");break;
                    case "C06":
                        Prepare(rig);Action(rig,"buffer4");Assert(!rig.Driver.Operate("buffer7",out _),"C06 requires rinse between buffers");Action(rig,"rinseProbe");Action(rig,"buffer7");Action(rig,"rinseProbe");Action(rig,"probeWater");Assert(Math.Abs(rig.Driver.Observe().Get("waterControlPh")-7)<1e-8,"C06 actual immersed water control stays visible");Assert(!rig.Driver.Operate("probeSample",out _),"C06 water control requires rinse before sample");Action(rig,"rinseProbe");Prepare(rig);Action(rig,"probeSample");Action(rig,"paper");Prepare(rig);
                        Assert(session.Start(out startWhy),id+" start "+startWhy);Tick(session,2);before=rig.Driver.Observe().Get("pH");Action(rig,"dilute10");Assert(!rig.Driver.CanRecord(out _),"C06 dilution requires fresh rinsed probe and BTB observation");
                        Action(rig,"rinseProbe");Action(rig,"probeSample");Action(rig,"paper");after=rig.Driver.Observe().Get("pH");Assert(Math.Abs(rig.Driver.Observe().Get("waterControlPh")-7)<1e-8,"C06 retains water control after sample dilution");Assert(after>before&&after<7,"C06 actual tenfold acid dilution approaches neutral");break;
                    case "C07":
                        Prepare(rig);Prepare(rig);Prepare(rig);Assert(session.Start(out startWhy),id+" start "+startWhy);Tick(session,300);
                        var gas=rig.Driver.Observe();Assert(gas.Get("gasVolume")<=gas.Get("gasLimit")+1e-5,"C07 syringe collection respects limiting moles");break;
                    case "C08":
                        Prepare(rig);Action(rig,"acidT");Action(rig,"baseT");Prepare(rig);Prepare(rig);Assert(session.Start(out startWhy),id+" start "+startWhy);Tick(session,20);
                        var thermal=rig.Driver.Observe();Assert(Math.Abs(thermal.Get("releasedHeat")-712.5*(1-Math.Exp(-10)))<1e-6,"C08 SDK actual25mL0.5M heat with57000J/mol");
                        Assert(thermal.Get("temperature")>thermal.Get("initialTemperature"),"C08 real calorimeter thermal state heats");break;
                }
                // Paused clocks must not integrate fluids, charge or reaction.
                session.Pause();string paused=rig.Driver.Observe().detailJson;session.Advance(10);Assert(paused==rig.Driver.Observe().detailJson,id+" paused clock preserves instrument state");session.Resume();
                Assert(session.Measure(out var why),id+" final source-backed measurement "+why);Assert(!string.IsNullOrEmpty(session.Recorded.detailJson),id+" detailed apparatus record");SubjectRecordVerifier.Verify(session);
                string draft=rig.Driver.SaveState();var replacement=new ChemistryDriver(new ExperimentSession(id));replacement.RestoreState(draft);Assert(!replacement.CanStart(out _),id+" safe draft restore never resumes apparatus/power");
            }
            catch(Exception ex){success=false;evidence.Add("FAIL "+id+" "+ex);}
            finally {UnityEngine.Object.DestroyImmediate(root);}
        }
        Directory.CreateDirectory("Evidence/Chemistry");File.WriteAllLines("Evidence/Chemistry/integration-checks.txt",evidence);Debug.Log("Chemistry integration: "+(success?"PASS":"FAIL")+" (see Evidence/Chemistry/integration-checks.txt)");
        if(!success)throw new Exception("Chemistry apparatus checks failed; inspect the evidence file.");
    }
}
#endif
