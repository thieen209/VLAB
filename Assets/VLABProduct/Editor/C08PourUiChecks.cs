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
    IEnumerator ManualPourJourney()
    {
        rows.Add(CalorimeterPourChecks.Run());LiquidPourTransferChecks.Run();Check("C08 conserved thermal SDK and physical pour geometry core",true);
        App.ShowHome();yield return Wait("home");yield return Tap("Subject C");yield return Wait("browser");yield return Tap("Browser lesson C08");yield return Tap("Browser start");yield return Wait("readiness");yield return Tap("Readiness enter");yield return Wait("activity");
        var rig=App.Apparatus;for(int i=0;i<30&&!rig.Ready;i++){rig.Select(rig.NextInstallableItem());Check("C08 apparatus fixture installation",rig.InstallSelected());}
        Check("C08 fixture confirms premeasured volumes",rig.PrepareNext(out _));Check("C08 fixture observes acid initial temperature",rig.Driver.Operate("acidT",out _));Check("C08 fixture observes base initial temperature",rig.Driver.Operate("baseT",out _));Check("C08 fixture records initial temperatures",rig.PrepareNext(out _));Check("C08 fixture prepares empty cup for actual pours",rig.PrepareNext(out _));Check("C08 fixture starts existing session clock",App.Session.Start(out _));
        rows.Add("C08 installation and initial temperature preparation above are explicit API fixtures. All solution transfers below use queued native touchscreen hold/drag/release.");
        var driver=(ChemistryDriver)rig.Driver;var calItem=rig.items.Single(i=>i.assetId=="cal");var cal=calItem.GetComponentInChildren<VlabCalorimeter>();
        var acidItem=rig.items.Single(i=>i.assetId=="acidCup");var baseItem=rig.items.Single(i=>i.assetId=="baseCup");var acid=acidItem.GetComponentInChildren<VlabBeaker>();var alkali=baseItem.GetComponentInChildren<VlabBeaker>();double total=acid.VolumeMl+alkali.VolumeMl+cal.Vessel.VolumeMl;
        Check("Open native lid has a supported position outside the pouring mouth",cal.HasOpenLidRest&&cal.LidOpen&&Vector2.Distance(new Vector2(cal.Lid.position.x,cal.Lid.position.z),new Vector2(cal.Vessel.transform.position.x,cal.Vessel.transform.position.z))>.05f);
        App.ShowActivity();Call("FocusStation");yield return new WaitForSecondsRealtime(.4f);
        var hide=App.GetComponentsInChildren<UnityEngine.UI.Button>().FirstOrDefault(b=>b.GetComponentInChildren<TMPro.TMP_Text>()?.text=="Ẩn UI"||b.GetComponentInChildren<TMPro.TMP_Text>()?.text=="Hide UI");if(hide)yield return Tap(hide.name);
        var camera=App.View.transform.rotation;int finger=0;Vector2 pointer=default;
        IEnumerator HoldAtReceiver(EquipmentHandle item,Func<bool> done,string label)
        {
            pointer=ModelPoint(item);finger=++serial;InputSystem.QueueStateEvent(touch,new TouchState{touchId=finger,phase=UnityEngine.InputSystem.TouchPhase.Began,position=pointer});yield return null;yield return new WaitForSecondsRealtime(.15f);
            Check("C08 real source cup acquires exclusive gesture ownership",item.Owner==EquipmentGrabOwner.Flagship);
            var world=cal.Vessel.transform.position+Vector3.up*.22f;var screen=App.View.WorldToScreenPoint(world);
            Check("C08 actual receiving-mouth pointer is visible",screen.z>0&&screen.x>=0&&screen.x<=Screen.width&&screen.y>=0&&screen.y<=Screen.height);
            var from=pointer;pointer=screen;for(int n=1;n<=12;n++){InputSystem.QueueStateEvent(touch,new TouchState{touchId=finger,phase=UnityEngine.InputSystem.TouchPhase.Moved,position=Vector2.Lerp(from,pointer,n/12f)});yield return new WaitForSecondsRealtime(.03f);}
            float until=Time.realtimeSinceStartup+12;while(!done()&&Time.realtimeSinceStartup<until)yield return null;
            if(!done())rows.Add("C08 pour diagnostic: "+driver.SaveState());Check(label,done());
            Check("C08 pouring preserves learner camera orientation",Quaternion.Angle(camera,App.View.transform.rotation)<.1f);
        }
        IEnumerator Release(EquipmentHandle item)
        {
            InputSystem.QueueStateEvent(touch,new TouchState{touchId=finger,phase=UnityEngine.InputSystem.TouchPhase.Ended,position=pointer});yield return null;yield return new WaitForSecondsRealtime(.2f);
            Check("C08 release stops ownership and visible stream",item.Owner==EquipmentGrabOwner.None&&(!Read<VlabLiquidStream>("pourStream")||!Read<VlabLiquidStream>("pourStream").Flowing));
            Check("C08 accepted flow conserves finite source plus receiver volume",Math.Abs(acid.VolumeMl+alkali.VolumeMl+cal.Vessel.VolumeMl-total)<1e-7);
        }
        yield return HoldAtReceiver(acidItem,()=>cal.AcidReceivedMl>=4,"Phone pour adds an actual partial acid volume");yield return Capture("C08-manual-acid-pour");yield return Release(acidItem);
        double partial=cal.AcidReceivedMl;yield return new WaitForSecondsRealtime(.3f);Check("Released cup retains its actual unpoured liquid",cal.AcidReceivedMl==partial&&acid.VolumeMl>0);
        yield return HoldAtReceiver(acidItem,()=>cal.AcidReceivedMl>=partial+3,"Regrab continues pouring without resetting accepted acid");yield return Release(acidItem);
        yield return HoldAtReceiver(baseItem,()=>cal.BaseReceivedMl>=5,"Phone base pour creates actual mixed reactant amounts");yield return Capture("C08-manual-base-pour");yield return Release(baseItem);
        double heat=cal.ReleasedHeatJ,age=cal.ElapsedSeconds;Check("Actual overlapping reactants release heat over elapsed time",heat>0&&age>0&&cal.AcidMoles>0&&cal.BaseMoles>0);
        Call("SelectWorkbenchTool",calItem,"");yield return Tap("Instrument control lidClose");yield return Tap("Close equipment controls");double before=cal.BaseReceivedMl;
        yield return HoldAtReceiver(baseItem,()=>true,"Closed-lid attempt reaches source hold");yield return new WaitForSecondsRealtime(.5f);Check("Closed native lid permits no solution transfer",cal.BaseReceivedMl==before);yield return Release(baseItem);
        Call("SelectWorkbenchTool",calItem,"");yield return Tap("Instrument control lidOpen");yield return Tap("Close equipment controls");
        yield return HoldAtReceiver(baseItem,()=>cal.BaseReceivedMl>=before+2,"Reopened native lid permits continued physical transfer");yield return Release(baseItem);
        Check("Later pours retain thermal time and already released heat",cal.ElapsedSeconds>=age&&cal.ReleasedHeatJ>=heat);
        Check("C08 actual input heat ledger conserves energy",Math.Abs(cal.StoredHeatJ-cal.ReleasedHeatJ+cal.LostHeatJ)<2e-6);
        yield return HoldAtReceiver(acidItem,()=>acid.VolumeMl<1e-8,"Phone finishes the measured acid portion");yield return Release(acidItem);
        yield return HoldAtReceiver(baseItem,()=>alkali.VolumeMl<1e-8,"Phone finishes the measured base portion");yield return Release(baseItem);
        Call("SelectWorkbenchTool",rig.items.Single(i=>i.assetId=="thermometer"),"");yield return Tap("Instrument control observeCal");yield return Tap("Close equipment controls");
        Check("C08 cannot record immediately after final pour",!driver.CanRecord(out _));
        float observationDeadline=Time.realtimeSinceStartup+60;int interruptions=0;
        while(!driver.CanRecord(out _)&&Time.realtimeSinceStartup<observationDeadline)
        {
            if(App.Session.Phase==ExperimentPhase.Paused)
            {
                Check("C08 observation interruption remains bounded",++interruptions<=2);
                rows.Add("Editor observation was paused; resume through the existing visible UI, preserving the production pause safeguard.");
                if(Read<bool>("hudHidden"))yield return Tap("HUD visibility");
                yield return Tap("Compact current action");Check("C08 native resume restores the running clock",App.Session.Phase==ExperimentPhase.Running);
            }
            yield return null;
        }
        if(!driver.CanRecord(out var recordWhy))rows.Add("C08 observation diagnostic: phase="+App.Session.Phase+" elapsed="+App.Session.Elapsed+" reason="+recordWhy+" state="+driver.Observe().detailJson+" readings="+driver.SaveState());
        Check("C08 actual full doses and timed immersed observation unlock recording",driver.CanRecord(out _));yield return Capture("C08-manual-pour-observation");
        rows.Add("Native Editor touch checked partial, repeat, closed/open-lid pours and conserved thermal state; not Android, physical controller or calibrated kinetics evidence.");
    }
}
#endif
