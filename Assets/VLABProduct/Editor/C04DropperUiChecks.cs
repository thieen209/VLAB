#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Vlab.Product;

public sealed partial class WorkbenchUpgradeProbe
{
    IEnumerator ManualDropperJourney(bool currentStation=false)
    {
        if(!currentStation)
        {
        C04ManualDropperChecks.Run();Check("C04 real geometry and finite qualitative unit regression",true);
        ChemistryIntegrationChecks.Run();Check("C01-C08 regression after qualitative dropper integration",true);
        yield return Tap("Subject C");yield return Wait("browser");yield return Tap("Browser lesson C04");yield return Tap("Browser start");yield return Wait("readiness");yield return Tap("Readiness enter");yield return Wait("activity");
        }
        var rig=App.Apparatus;Check("C04 supplied flagship kit is installed",rig.Ready);App.ShowActivity();Call("FocusStation");yield return new WaitForSecondsRealtime(.4f);
        var hide=App.GetComponentsInChildren<UnityEngine.UI.Button>().FirstOrDefault(b=>b.GetComponentInChildren<TMPro.TMP_Text>()?.text=="Ẩn UI"||b.GetComponentInChildren<TMPro.TMP_Text>()?.text=="Hide UI");if(hide)yield return Tap(hide.name);
        var driver=(ChemistryDriver)rig.Driver;var item=rig.items.Single(i=>i.assetId=="dropper");var reagent=rig.items.Single(i=>i.assetId=="reagent");var drop=item.GetComponentInChildren<VlabDropper>();int finger=0;Vector2 pointer=default;
        IEnumerator SelectSource()
        {
            var point=ModelPoint(reagent);int tap=++serial;InputSystem.QueueStateEvent(touch,new TouchState{touchId=tap,phase=UnityEngine.InputSystem.TouchPhase.Began,position=point});yield return null;yield return new WaitForSecondsRealtime(.08f);InputSystem.QueueStateEvent(touch,new TouchState{touchId=tap,phase=UnityEngine.InputSystem.TouchPhase.Ended,position=point});yield return null;yield return new WaitForSecondsRealtime(.2f);
            Check("C04 source selection arms a real load without transferring units",driver.CanManualDropper&&driver.LoadedDropperUnits==0);
        }
        IEnumerator Hold()
        {
            pointer=ModelPoint(item);finger=++serial;InputSystem.QueueStateEvent(touch,new TouchState{touchId=finger,phase=UnityEngine.InputSystem.TouchPhase.Began,position=pointer});yield return null;yield return new WaitForSecondsRealtime(.15f);Check("C04 phone owns actual dropper",item.Owner==EquipmentGrabOwner.Flagship);
        }
        IEnumerator Move(EquipmentHandle target,Func<bool> done,string label)
        {
            var start=pointer;var projected=App.View.WorldToScreenPoint(new Vector3(target.transform.position.x,rig.Worktop.y+.16f,target.transform.position.z));Check("C04 actual target is on screen",projected.z>0&&projected.x>=0&&projected.x<=Screen.width&&projected.y>=0&&projected.y<=Screen.height);pointer=projected;
            for(int n=1;n<=12;n++){InputSystem.QueueStateEvent(touch,new TouchState{touchId=finger,phase=UnityEngine.InputSystem.TouchPhase.Moved,position=Vector2.Lerp(start,pointer,n/12f)});yield return new WaitForSecondsRealtime(.03f);}
            float until=Time.realtimeSinceStartup+10;while(!done()&&Time.realtimeSinceStartup<until)yield return null;
            if(!done())rows.Add("C04 dose state "+driver.SaveState()+" actual tip="+drop.Tip.position+" direction="+drop.EmissionDirection);
            Check(label,done());
        }
        IEnumerator Release()
        {InputSystem.QueueStateEvent(touch,new TouchState{touchId=finger,phase=UnityEngine.InputSystem.TouchPhase.Ended,position=pointer});yield return null;yield return new WaitForSecondsRealtime(.2f);Check("C04 release stops squeezing and clears ownership",drop.Squeeze==0&&item.Owner==EquipmentGrabOwner.None);}
        yield return SelectSource();yield return Hold();yield return Move(reagent,()=>driver.LoadedDropperUnits==1,"C04 actual source immersion loads one simulated drop");yield return Release();
        var partial=JsonUtility.FromJson<ChemistryDriver.Draft>(App.Session.Checkpoint().detailJson);Check("C04 release keeps partial qualitative load with conserved source",partial.qualitative.loaded==1&&partial.qualitative.source==11);yield return Capture("C04-manual-partial-load");
        yield return Hold();yield return Move(reagent,()=>driver.LoadedDropperUnits==2,"C04 regrab completes finite loading");
        yield return Move(rig.items.Single(i=>i.assetId=="water"),()=>driver.QualitativeDoseUnits("water")==2,"C04 actual tip delivers water-control simulated dose");yield return Release();
        var waterRecord=App.Profile.attempts.SingleOrDefault(a=>a.attemptId==App.Session.TrialId);
        Check("C04 accepted water-control dose and named zero reading persist",waterRecord!=null&&waterRecord.readings.Any(r=>r.key=="waterDrops"&&r.value==2)&&waterRecord.readings.Any(r=>r.key=="waterSolid"&&r.value==0));
        foreach(string cup in new[]{"nitrate","nacl"})
        {
            yield return SelectSource();yield return Hold();yield return Move(reagent,()=>driver.LoadedDropperUnits==2,"C04 reloads from source for "+cup);
            yield return Move(rig.items.Single(i=>i.assetId==cup),()=>driver.QualitativeDoseUnits(cup)==2,"C04 accepted actual-tip dose reaches "+cup);yield return Release();
        }
        Check("C04 preparation advances from actual completed doses",App.Session.Phase==ExperimentPhase.Running&&App.Session.Operation==3);
        yield return new WaitForSecondsRealtime(3.2f);Check("C04 appearance completes after active observation",driver.CanRecord(out _));var readings=driver.Observe();
        Check("C04 phone sequence conserves all12 qualitative units",readings.Get("sourceDrops")==6&&readings.Get("loadedDrops")==0&&readings.Get("naclDrops")==2&&readings.Get("nitrateDrops")==2&&readings.Get("waterDrops")==2);
        Check("C04 retains two negative controls and no calibrated-volume claim",readings.Get("NaClSolid")==1&&readings.Get("NaNO3Solid")==0&&readings.Get("waterSolid")==0&&readings.readings.All(r=>r.unit!="mL"&&r.unit!="mol"));
        Check("C04 phone sequence used no assisted reagent transfer",!App.Session.Assistance.Contains("qualitative"));yield return Capture("C04-manual-doses-and-controls");
        var show=App.GetComponentsInChildren<UnityEngine.UI.Button>().FirstOrDefault(b=>b.GetComponentInChildren<TMPro.TMP_Text>()?.text=="Hiện UI"||b.GetComponentInChildren<TMPro.TMP_Text>()?.text=="Show UI");if(show)yield return Tap(show.name);
    }
}
#endif
