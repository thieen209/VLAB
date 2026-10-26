#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Vlab.Product;

public static class C04ManualDropperChecks
{
    static void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
    static T Item<T>(ApparatusRig rig,string key) where T:Component=>rig.items.First(i=>i.assetId==key).GetComponentInChildren<T>();
    static ApparatusRig Build(GameObject root,ExperimentSession session,Vector3 at)
    {var rig=root.AddComponent<ApparatusRig>();rig.Build(session,at);foreach(var item in rig.items){rig.Select(item);Check(rig.InstallSelected(),"Install "+item.assetId);}Check(rig.PrepareNext(out var why),why);return rig;}
    static void Aim(ApparatusRig rig,Vector3 point)
    {var drop=Item<VlabDropper>(rig,"dropper");drop.transform.rotation=Quaternion.FromToRotation(drop.EmissionDirection,Vector3.down)*drop.transform.rotation;drop.transform.position+=point-drop.Tip.position;}
    static void Source(ApparatusRig rig)
    {var bottle=Item<VlabReagentBottle>(rig,"reagent");Aim(rig,bottle.Liquid.CavityCentreAt(bottle.Liquid.SurfaceHeightMeters*.55));}
    static void Cup(ApparatusRig rig,string key)
    {var cup=Item<VlabBeaker>(rig,key);float rim=cup.transform.position.y+(float)(cup.InteriorBottom+cup.CapacityMl*1e-6/cup.CrossSectionArea);Aim(rig,new Vector3(cup.transform.position.x,rim+.05f,cup.transform.position.z));}
    static void Pulse(ChemistryDriver driver)
    {Check(driver.TransferManualDropper(out var why)==1,why);}
    static void Conserved(ChemistryDriver driver)
    {var r=driver.Observe();Check(r.Get("sourceDrops")+r.Get("loadedDrops")+r.Get("naclDrops")+r.Get("nitrateDrops")+r.Get("waterDrops")==12,"Source + loaded + three delivered qualitative unit counts stay exactly12.");}
    static void Dose(ApparatusRig rig,string cup)
    {var driver=(ChemistryDriver)rig.Driver;Check(driver.Operate("selectAg",out var why),why);Check(driver.BeginManualDropper(out why),why);Source(rig);Pulse(driver);Pulse(driver);Cup(rig,cup);Pulse(driver);Pulse(driver);Conserved(driver);}
    [MenuItem("VLAB/Checks/C04 manual qualitative dropper")]
    public static void Run()
    {
        if(!Application.isPlaying)throw new InvalidOperationException("Run in Play mode with delivered SDK geometry initialized.");
        var root=new GameObject("C04 manual dropper checks");GameObject restoredRoot=null,block=null,appearanceRoot=null;
        try
        {
            var session=new ExperimentSession("C04");var rig=Build(root,session,new Vector3(100,10,100));var driver=(ChemistryDriver)rig.Driver;var drop=Item<VlabDropper>(rig,"dropper");var bottle=Item<VlabReagentBottle>(rig,"reagent");
            Check(!driver.Operate("dose:water",out _),"Clicking a cup cannot position or dose the dropper.");
            Check(driver.Operate("selectAg",out var why),why);Check(driver.BeginManualDropper(out why),why);Check(driver.TransferManualDropper(out _)==0,"Parked/off-target tool transfers no simulated drops.");
            Source(rig);bottle.TrySetCapOpen(false);Check(driver.TransferManualDropper(out _)==0,"Closed source rejects loading.");bottle.TrySetCapOpen(true);var pose=drop.transform.position;Pulse(driver);
            Check(driver.LoadedDropperUnits==1&&drop.transform.position==pose,"Actual immersed source loads one unit without moving the tool.");Conserved(driver);
            driver.StopManualDropper();Check(driver.TransferManualDropper(out _)==0&&drop.Squeeze==0,"Release closes the bulb without losing loaded units.");
            var checkpoint=session.Checkpoint();restoredRoot=new GameObject("C04 restored partial load");var restoredSession=ExperimentSession.Restore(checkpoint);var restored=Build(restoredRoot,restoredSession,new Vector3(103,10,100));var resumed=(ChemistryDriver)restored.Driver;
            Check(resumed.LoadedDropperUnits==1&&Item<VlabDropper>(restored,"dropper").Squeeze==0&&resumed.TransferManualDropper(out _)==0,"Reload retains loaded reagent with no running pulse.");Conserved(resumed);
            var retained=new ChemistryDriver(new ExperimentSession("C04"));retained.RestoreState(checkpoint.detailJson);string valid=retained.SaveState();var invalid=JsonUtility.FromJson<ChemistryDriver.Draft>(valid);invalid.qualitative.source++;retained.RestoreState(JsonUtility.ToJson(invalid));Check(retained.SaveState()==valid,"Nonconserved checkpoint cannot replace a valid preceding draft.");
            Check(resumed.BeginManualDropper(out why),why);Source(restored);Pulse(resumed);Check(resumed.TransferManualDropper(out _)==0&&resumed.LoadedDropperUnits==2,"Two-unit dropper cannot load unlimited reagent.");
            Cup(restored,"water");var resumedDrop=Item<VlabDropper>(restored,"dropper");var water=Item<VlabBeaker>(restored,"water");double fill=water.Add(water.CapacityMl-water.VolumeMl);
            Check(resumed.TransferManualDropper(out _)==0,"Full receiver rejects a simulated dose.");water.Remove(fill);
            block=GameObject.CreatePrimitive(PrimitiveType.Cube);block.transform.localScale=Vector3.one*.01f;block.transform.position=resumedDrop.Tip.position+Vector3.down*.015f;
            Check(resumed.TransferManualDropper(out _)==0,"Intervening geometry blocks the actual drop path.");block.transform.position=resumedDrop.Tip.position;Check(resumed.TransferManualDropper(out _)==0,"Contact at the outlet blocks delivery.");block.SetActive(false);
            var at=resumedDrop.Tip.position;resumedDrop.transform.rotation=Quaternion.AngleAxis(180,Vector3.right)*resumedDrop.transform.rotation;resumedDrop.transform.position+=at-resumedDrop.Tip.position;
            Check(resumed.TransferManualDropper(out _)==0,"Upward emission cannot deliver into a cup.");Cup(restored,"water");Pulse(resumed);Check(resumed.QualitativeDoseUnits("water")==1&&resumed.LoadedDropperUnits==1&&!resumed.CanRecord(out _),"One accepted unit is an incomplete sample dose.");Conserved(resumed);
            Cup(restored,"nitrate");Check(resumed.TransferManualDropper(out _)==0,"An interrupted dose remains assigned to its original sample.");
            resumed.SetPaused(true);Cup(restored,"water");Check(resumed.TransferManualDropper(out _)==0,"Pause stops further unit transfer.");Check(resumed.BeginManualDropper(out why),why);resumed.InputLost();Check(resumed.TransferManualDropper(out _)==0,"Input loss closes the dose path.");Check(resumed.BeginManualDropper(out why),why);Pulse(resumed);
            Check(resumed.QualitativeDoseUnits("water")==2&&resumed.TransferManualDropper(out _)==0,"The second accepted unit completes the cup and clears reagent selection.");
            Dose(restored,"nitrate");Check(restored.PrepareNext(out why),why);Dose(restored,"nacl");Check(restored.PrepareNext(out why),why);
            Check(!resumed.CanRecord(out _)&&restoredSession.Start(out why),"All actual doses precede timed observation: "+why);restoredSession.Advance(1);
            var solid=Item<VlabPrecipitate>(restored,"solidNaCl");Check(solid.VisualLevel>0&&solid.VisualLevel<.8f,"NaCl appearance develops over active time.");float level=solid.VisualLevel;restoredSession.Pause();restoredSession.Advance(4);Check(solid.VisualLevel==level,"Pause freezes the illustrative appearance transition.");
            var appearance=restoredSession.Checkpoint();appearanceRoot=new GameObject("C04 restored appearance");var appearanceSession=ExperimentSession.Restore(appearance);var appearanceRig=Build(appearanceRoot,appearanceSession,new Vector3(106,10,100));var appearanceDriver=(ChemistryDriver)appearanceRig.Driver;
            Check(Mathf.Abs(Item<VlabPrecipitate>(appearanceRig,"solidNaCl").VisualLevel-level)<1e-6,"Reload preserves the observed appearance without resuming time.");Check(appearanceRig.PrepareNext(out why),why);Check(appearanceRig.PrepareNext(out why),why);Check(appearanceSession.Start(out why),why);appearanceSession.Advance(2);
            Check(appearanceDriver.CanRecord(out why),why);Conserved(appearanceDriver);Check(appearanceDriver.Observe().Get("NaClSolid")==1&&appearanceDriver.Observe().Get("NaNO3Solid")==0&&appearanceDriver.Observe().Get("waterSolid")==0,"Only NaCl is positive, with both negative controls retained.");
            Check(appearanceDriver.Observe().readings.All(r=>r.unit!="mL"&&r.unit!="mol"),"Qualitative unit accounting invents no calibrated mL or mol measurement.");
            Debug.Log("C04_MANUAL_DROPPER_PASS: actual source/tip/path geometry, finite qualitative units, partial load/dose, pause/loss/restore, invalid-draft protection and timed controlled observations; physical drop volume remains uncalibrated.");
        }
        finally{UnityEngine.Object.DestroyImmediate(root);if(restoredRoot)UnityEngine.Object.DestroyImmediate(restoredRoot);if(appearanceRoot)UnityEngine.Object.DestroyImmediate(appearanceRoot);if(block)UnityEngine.Object.DestroyImmediate(block);}
    }
}
#endif
