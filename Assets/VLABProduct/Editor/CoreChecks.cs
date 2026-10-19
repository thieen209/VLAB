#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Vlab.Product;

public static class VlabCoreChecks
{
    static readonly List<string> passed=new List<string>();
    static void Check(string name,bool condition){if(!condition)throw new Exception("FAIL: "+name);passed.Add(name);}
    static void Near(string name,double actual,double expected,double tolerance=1e-6)=>Check(name,Math.Abs(actual-expected)<=tolerance);
    static ModelResult E(string id,double time=120,double dose=0)=>ScienceModels.Evaluate(id,ScienceModels.Defaults(id),time,dose);
    public static void Run()
    {
        string evidence=Path.GetFullPath("Evidence");Directory.CreateDirectory(evidence);
        try
        {
            passed.Clear();Check("32 unique source-defined model configurations",LessonSpec.All.Count==32);
            foreach(var spec in LessonSpec.All.Values){var result=E(spec.id,120,spec.id=="C01"?10:0);Check(spec.id+" nominal outputs finite with explicit units",result.readings.Count>0&&result.readings.All(r=>!double.IsNaN(r.value)&&!double.IsInfinity(r.value)&&r.unit!=null));}
            Near("P01 fourfold length doubles period",ScienceModels.PendulumPeriod(.8,5)/ScienceModels.PendulumPeriod(.2,5),2);
            Check("P01 large angle increases period",ScienceModels.PendulumPeriod(.6,60)>ScienceModels.PendulumPeriod(.6,5));
            var motion=new PendulumDynamics(.6,8);for(int i=0;i<10000;i++)motion.Advance(.002);Near("P01 RK4 crossing clock agrees with independent elliptic period",motion.MeasuredPeriod,ScienceModels.PendulumPeriod(.6,8),.00002);
            var counted=new PendulumDynamics(.6,8,10);counted.Advance(40);Check("P01 delayed recording preserves selected ten cycles",counted.Cycles>10&&counted.MeasuredCycles==10);Near("P01 selected cycle clock excludes late recording delay",counted.MeasuredStop-counted.FirstCrossing,10*ScienceModels.PendulumPeriod(.6,8),.00002);
            Near("P02 first impact range",E("P02").Get("range"),25/9.81);
            Near("P03 static threshold",E("P03").Get("staticPeak"),1.962);
            Near("P03 sliding force",E("P03").Get("slidingForce"),1.4715);
            Near("P04 photogate acceleration",E("P04").Get("acceleration"),.75);
            Near("P05 static extension",E("P05").Get("extension"),.00981);
            Near("P06 momentum conserved",E("P06").Get("momentumBefore"),E("P06").Get("momentumAfter"));
            var collision=ScienceModels.Defaults("P06");collision["elastic"]=0;var sticking=ScienceModels.Evaluate("P06",collision,3);Near("P06 sticking speed",sticking.Get("velocityA"),.5);Check("P06 sticking loses kinetic energy",sticking.Get("energyAfter")<sticking.Get("energyBefore"));
            Near("P07 acceleration with cart plus hanger mass",E("P07").Get("acceleration"),.05*9.81/.45);
            Near("P08 energy balance",E("P08").Get("potentialEnergy"),E("P08").Get("kineticEnergy")+E("P08").Get("frictionLoss"));
            Near("C01 stoichiometric endpoint",E("C01",0,10).Get("endpointTarget"),10);Check("C01 weak-acid equivalence above neutral",E("C01",0,10).Get("pH")>7);Check("C01 pre/post equivalence respond to dose",E("C01",0,9).Get("pH")<E("C01",0,11).Get("pH"));
            Near("C02 standard cell voltage",E("C02").Get("voltage"),1.1);
            var cell=ScienceModels.Defaults("C02");cell["polarity"]=-1;Near("C02 reversing leads reverses sign",ScienceModels.Evaluate("C02",cell,0).Get("voltage"),-1.1);
            Near("C03 Faraday copper mass",E("C03",120).Get("copperMass"),60/(2*96485.33212)*63.546);
            Near("C04 blank stays clear",E("C04").Get("blankPrecipitate"),0);
            Near("C05 fivefold dilution",E("C05").Get("finalConcentration"),.2);
            var ph=ScienceModels.Defaults("C06");ph["dilution"]=1000;double dilute=ScienceModels.Evaluate("C06",ph,0).Get("pH");Check("C06 acid dilution approaches neutral without crossing",dilute>E("C06").Get("pH")&&dilute<7);
            Check("C07 finite reagent gas limit",E("C07",10000).Get("gasVolume")<=E("C07",10000).Get("gasLimit")+1e-8);
            Near("C08 energy capacity rise",E("C08",100).Get("temperatureRise"),.0125*57000/229,.00001);
            Near("B01 objective magnification",E("B01").Get("magnification"),40);
            var salt=ScienceModels.Defaults("B02");salt["salt"]=0;Near("B02 water control has no plasmolysis",ScienceModels.Evaluate("B02",salt,60).Get("plasmolysisPercent"),0);
            Check("B03 low sucrose gains water",E("B03",600).Get("massChange")>0);var potato=ScienceModels.Defaults("B03");potato["concentration"]=.6;Check("B03 high sucrose loses water",ScienceModels.Evaluate("B03",potato,600).Get("massChange")<0);
            var enzyme=ScienceModels.Defaults("B04");enzyme["inactive"]=1;Near("B04 inactive enzyme control",ScienceModels.Evaluate("B04",enzyme,60).Get("oxygenVolume"),0);
            var yeast=ScienceModels.Defaults("B05");yeast["sugar"]=0;Near("B05 no-sugar control",ScienceModels.Evaluate("B05",yeast,120).Get("carbonDioxide"),0);
            Near("B06 band/front gives Rf",E("B06").Get("bandA")/E("B06").Get("solventFront"),.55);
            var dna=ScienceModels.Defaults("B07");dna["detergent"]=0;Near("B07 omission affects qualitative result",ScienceModels.Evaluate("B07",dna,60).Get("crudePrecipitate"),0);
            Near("B08 stage counts sum to field total",E("B08").readings.Where(x=>new[]{"interphase","prophase","metaphase","anaphase","telophase"}.Contains(x.key)).Sum(x=>x.value),30);
            Near("M01 12 to 24 signed ratio",E("M01").Get("signedRatio"),-.5);
            Near("M02 equal moments balance",E("M02").Get("netMoment"),0);
            Near("M03 ideal work conserved",E("M03").Get("loadWork"),E("M03").Get("inputWork"));
            Near("M04 open 40/80 ratio",E("M04").Get("signedRatio"),.5);
            Near("M05 stroke twice radius",E("M05").Get("stroke"),60);
            Near("M06 periodic profile",E("M06",0).Get("lift"),E("M06",3).Get("lift"));
            Near("M07 corrected size",E("M07").Get("correctedReading"),12.1);
            var beam=ScienceModels.Defaults("M08");beam["height"]=2;beam["load"]=.1;double thin=ScienceModels.Evaluate("M08",beam,2).Get("deflection");beam["height"]=4;Near("M08 doubling section height divides deflection by eight",ScienceModels.Evaluate("M08",beam,2).Get("deflection"),thin/8);
            foreach(var id in LessonSpec.All.Keys)
            {
                var s=new ExperimentSession(id);Check(id+" cannot run before apparatus/preparation",!s.Start(out _));s.SetApparatusReady(true);for(int i=0;i<s.spec.operationsEn.Length;i++)Check(id+" preparation action "+i,s.Prepare(i,out _));Check(id+" starts after valid preparation",s.Start(out _));s.Advance(.1);s.Pause();double t=s.Elapsed;s.Advance(1);Near(id+" pause freezes scientific clock",s.Elapsed,t);s.InputLost();Check(id+" lost input is safe",s.Phase==ExperimentPhase.Paused);
            }
            ProfileChecks();InputChecks();
            File.WriteAllLines(Path.Combine(evidence,"core-checks.txt"),new[]{"PASS "+passed.Count+" checks", "These checks cover model kernels, lifecycle rules and file persistence; they do not prove apparatus/UI/VR journeys."}.Concat(passed));
            Debug.Log("VLAB CORE CHECKS PASS "+passed.Count);EditorApplication.Exit(0);
        }
        catch(Exception ex){File.WriteAllLines(Path.Combine(evidence,"core-checks.txt"),new[]{"FAILED",ex.ToString()}.Concat(passed));Debug.LogException(ex);EditorApplication.Exit(1);}
    }
    static void ProfileChecks()
    {
        string dir=Path.Combine(Path.GetTempPath(),"vlab-profile-check-"+Guid.NewGuid().ToString("N"));var store=new ProfileStore(dir);var p=store.Load().profile;
        Check("New profile starts at language",p.NextSetup("privacy-hash","terms-hash")=="language");
        p.locale="vi";Check("Language persists independently",store.Save(p,out _));p=new ProfileStore(dir).Load().profile;Check("Interrupted setup resumes at privacy",p.NextSetup("privacy-hash","terms-hash")=="privacy");
        bool refused=false;try{p.Accept("privacy","privacy-hash",false);}catch(InvalidOperationException){refused=true;}Check("Implicit legal acceptance rejected",refused);
        p.Accept("privacy","privacy-hash",true);Check("Privacy acceptance saved",store.Save(p,out _));Check("Next setup is separate terms",new ProfileStore(dir).Load().profile.NextSetup("privacy-hash","terms-hash")=="terms");
        p.Accept("terms","terms-hash",true);p.safetyAcknowledged=true;p.settings.volume=.33f;Check("Completed setup saved",store.Save(p,out _));var returning=new ProfileStore(dir).Load();Check("Returning user goes directly home",returning.profile.NextSetup("privacy-hash","terms-hash")=="home");Near("Settings survive relaunch",returning.profile.settings.volume,.33,.000001);
        Check("Only changed privacy is renewed",returning.profile.NextSetup("privacy-new","terms-hash")=="privacy");
        File.WriteAllText(Path.Combine(dir,"profile.json"),"broken");var recovered=new ProfileStore(dir).Load();Check("Corrupted primary recovers valid backup",recovered.recovered&&recovered.profile.safetyAcknowledged);
        File.WriteAllText(Path.Combine(dir,"profile.json.tmp"),"interrupted write");Check("Interrupted temp write ignored",new ProfileStore(dir).Load().profile.safetyAcknowledged);
        var recoveredStore=new ProfileStore(dir);recoveredStore.Load();Check("Progress reset succeeds",recoveredStore.Reset(recovered.profile,false,out var reset,out _));Check("Progress reset retains setup",reset.safetyAcknowledged&&reset.attempts.Count==0);
        Check("Full reset succeeds",recoveredStore.Reset(reset,true,out var cleared,out _));Check("Full reset restarts language",new ProfileStore(dir).Load().profile.NextSetup("privacy-hash","terms-hash")=="language");
        File.WriteAllText(Path.Combine(dir,"profile.json"),"damaged after reset");Check("Backup cannot resurrect pre-reset setup",new ProfileStore(dir).Load().profile.locale=="");
        var folderAsFile=Path.Combine(dir,"not-a-directory");File.WriteAllText(folderAsFile,"x");Check("Storage failure is reported",!new ProfileStore(folderAsFile).Save(new LocalProfile(),out var error)&&!string.IsNullOrEmpty(error));
        File.WriteAllLines(Path.Combine(dir,"test-receipt.txt"),passed); // preserve isolated failure/recovery fixtures for inspection
    }
    static void InputChecks()
    {
        var go=new GameObject("Isolated input test camera");var camera=go.AddComponent<Camera>();var input=new MockControllerProvider();
        Check("Mock rejects invalid orientation",!input.Submit(new ControllerSignal{sequence=0,receivedAt=0,orientation=new Quaternion(0,0,0,0)}));
        Check("Mock accepts normalized in-process signal",input.Submit(new ControllerSignal{sequence=1,receivedAt=1,orientation=Quaternion.identity,select=true}));
        var first=input.Poll(camera,1);Check("Select produces one press edge",first.pressed&&first.held);Check("Held select does not repeat press",!input.Poll(camera,1.01).pressed);
        Check("Out-of-order signal rejected",!input.Submit(new ControllerSignal{sequence=0,receivedAt=1.1,orientation=Quaternion.identity}));
        var lost=input.Poll(camera,1.5);Check("Stale signal releases held action",lost.lost&&lost.released&&!lost.held);
        Check("Loss reported only once",!input.Poll(camera,1.6).lost);
        input.Submit(new ControllerSignal{sequence=2,receivedAt=2,orientation=Quaternion.Euler(0,40,0)});input.Calibrate();var calibrated=input.Poll(camera,2);Check("Orientation calibration restores forward",Vector3.Angle(calibrated.pointer.direction,Vector3.forward)<.01f);
        UnityEngine.Object.DestroyImmediate(go);
    }
}
#endif
