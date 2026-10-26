#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Vlab.Product;

public static class C05ManualPipetteChecks
{
    static void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
    static ApparatusRig Build(GameObject root,ExperimentSession session,Vector3 at)
    {
        var rig=root.AddComponent<ApparatusRig>();rig.Build(session,at);
        foreach(var item in rig.items){rig.Select(item);Check(rig.InstallSelected(),"Install "+item.assetId);}
        return rig;
    }
    static T Item<T>(ApparatusRig rig,string key) where T:Component=>rig.items.First(i=>i.assetId==key).GetComponentInChildren<T>();
    static void Aim(ApparatusRig rig,Vector3 target)
    {
        var pipette=Item<VlabFlask>(rig,"pipette");var sockets=pipette.GetComponentsInChildren<Transform>();
        var tip=sockets.Single(t=>t.name.EndsWith("_TipOutlet_SOCKET",StringComparison.Ordinal));var top=sockets.Single(t=>t.name.EndsWith("_BulbConnection_SOCKET",StringComparison.Ordinal));
        pipette.transform.rotation=Quaternion.FromToRotation((top.position-tip.position).normalized,Vector3.up)*pipette.transform.rotation;
        pipette.transform.position+=target-tip.position;
    }
    static void Stock(ApparatusRig rig)
    {var source=Item<VlabReagentBottle>(rig,"stock").Liquid;Aim(rig,source.CavityCentreAt(source.SurfaceHeightMeters*.55));}
    static void Waste(ApparatusRig rig)=>Aim(rig,Item<VlabBeaker>(rig,"waste").transform.position+Vector3.up*.02f);
    static void Flask(ApparatusRig rig)=>Aim(rig,Item<VlabVolumetricFlask>(rig,"vol50").Liquid.CavityCentreAt(.02));
    static void Transfer(ChemistryDriver driver,double ml)
    {Check(Math.Abs(driver.TransferManualPipette(ml,out var why)-ml)<1e-8,"Transfer "+ml+" actual mL: "+why);}
    static void Conserved(ApparatusRig rig)
    {
        var r=rig.Driver.Observe();double stock=Item<VlabReagentBottle>(rig,"stock").Concentration;
        double volume=r.Get("stockVolume")+r.Get("wasteVolume")+r.Get("pipetteVolume")+r.Get("finalVolume")+Item<VlabWashBottle>(rig,"wash").RemainingMl;
        Check(Math.Abs(volume-370)<1e-7,"Actual stock, waste, pipette, receiver and water volumes are conserved.");
        Check(Math.Abs(r.Get("stockVolume")*stock/1000+r.Get("wasteSolute")+r.Get("pipetteSolute")+r.Get("solute")-stock*.120)<1e-10,"Stock, waste, pipette and receiver solute is conserved.");
    }
    [MenuItem("VLAB/Checks/C05 manual pipette flow")]
    public static void Run()
    {
        if(!Application.isPlaying)throw new InvalidOperationException("Run in Play mode with delivered SDK Awake initialized.");
        var root=new GameObject("C05 manual pipette checks");GameObject rinseRoot=null,deliveryRoot=null;
        try
        {
            var session=new ExperimentSession("C05");var rig=Build(root,session,new Vector3(100,10,100));var driver=(ChemistryDriver)rig.Driver;
            var source=Item<VlabReagentBottle>(rig,"stock");var pipette=Item<VlabFlask>(rig,"pipette");var bulb=Item<VlabPipetteBulb>(rig,"bulb");
            Check(driver.ValidatePipetteParking(out var support),"Native upright parking: "+support);
            Check(!rig.PrepareNext(out _)&&source.Liquid.VolumeMl==120,"Prepare cannot perform a hidden automatic rinse.");
            Check(driver.BeginManualPipette(out var why),why);Check(driver.TransferManualPipette(1,out _)==0,"Off-target parked pipette transfers nothing.");
            Stock(rig);source.TrySetCapOpen(false);Check(driver.TransferManualPipette(4,out _)==0,"Closed stock blocks aspiration.");source.TrySetCapOpen(true);
            var pose=pipette.transform.position;Transfer(driver,4);Check(pipette.transform.position==pose&&pipette.VolumeMl==4,"SDK transfer retains real pose and finite 4 mL load.");Conserved(rig);
            driver.StopManualPipette();Check(driver.TransferManualPipette(1,out _)==0&&bulb.Control==0&&pipette.VolumeMl==4,"Cancel stops the valve and retains accepted liquid.");
            double solute=driver.Observe().Get("pipetteSolute");Check(driver.ParkManualPipette(out why),why);
            Check(pipette.VolumeMl==4&&driver.Observe().Get("pipetteSolute")==solute&&driver.ValidatePipetteParking(out why),"Loaded put-down uses real clamp pads and preserves volume/solute: "+why);
            var parked=pipette.transform.position;pipette.transform.position+=Vector3.right*.01f;Check(!driver.ValidatePipetteParking(out _),"Detached pipette cannot claim support.");pipette.transform.position=parked;
            var clamp=Item<VlabClamp>(rig,"clamp");clamp.SetOpening(.012);Check(!driver.ValidatePipetteParking(out _),"Open jaws cannot claim contact with a 6 mm stem.");Check(driver.ParkManualPipette(out why),why);
            var rinseCheckpoint=session.Checkpoint();rinseRoot=new GameObject("C05 restored rinse");var rinseRig=Build(rinseRoot,ExperimentSession.Restore(rinseCheckpoint),new Vector3(103,10,100));var rinseDriver=(ChemistryDriver)rinseRig.Driver;
            Check(Item<VlabFlask>(rinseRig,"pipette").VolumeMl==4&&Item<VlabReagentBottle>(rinseRig,"stock").Liquid.VolumeMl==116&&rinseDriver.CompletedPipetteOperations==0,"Reload preserves a partially loaded rinse without completing a stage.");Conserved(rinseRig);
            Check(rinseDriver.ValidatePipetteParking(out why),"Reloaded loaded pipette has real support: "+why);
            var retained=new ChemistryDriver(new ExperimentSession("C05"));retained.RestoreState(rinseCheckpoint.detailJson);string valid=retained.SaveState();
            var corrupt=JsonUtility.FromJson<ChemistryDriver.Draft>(valid);corrupt.dilution.pipetteMoles+=1;retained.RestoreState(JsonUtility.ToJson(corrupt));
            Check(retained.SaveState()==valid,"Invalid checkpoint cannot replace the preceding valid draft.");
            Check(rinseDriver.Operate("autoRinsePipette",out why),why);Check(rinseRig.PrepareNext(out why),why);Check(rinseDriver.Operate("transferAliquot",out why),why);Check(rinseRig.PrepareNext(out why),why);
            Check(Item<VlabReagentBottle>(rinseRig,"stock").Liquid.VolumeMl==100&&Item<VlabBeaker>(rinseRig,"waste").VolumeMl==10,"Explicit assistance consumes only the remaining amounts of the saved partial.");Conserved(rinseRig);
            Check(driver.BeginManualPipette(out why),why);Waste(rig);Transfer(driver,4);Stock(rig);Transfer(driver,6);Waste(rig);Transfer(driver,6);
            Check(driver.CompletedPipetteOperations==1&&driver.Observe().Get("rinse")==10&&pipette.VolumeMl==0,"Partial 4 + 6 mL rinse completes only after actual waste delivery.");Check(rig.PrepareNext(out why),why);Conserved(rig);
            Check(!rig.PrepareNext(out _)&&driver.Observe().Get("finalVolume")==0,"Prepare cannot perform a hidden automatic aliquot transfer.");
            Stock(rig);Transfer(driver,10);Check(driver.TransferManualPipette(10,out _)==0&&pipette.VolumeMl==10,"Pipette capacity prevents infinite aspiration.");
            driver.SetPaused(true);Check(driver.TransferManualPipette(1,out _)==0&&pipette.VolumeMl==10&&bulb.Control==0,"Pause closes the pipette without discarding liquid.");
            Check(driver.BeginManualPipette(out why),why);driver.InputLost();Check(driver.TransferManualPipette(1,out _)==0&&bulb.Control==0,"Input loss closes the pipette.");
            Check(driver.BeginManualPipette(out why),why);var receiver=Item<VlabVolumetricFlask>(rig,"vol50");Flask(rig);receiver.TrySetStopperOpen(false);
            Check(driver.TransferManualPipette(4,out _)==0&&pipette.VolumeMl==10&&receiver.Liquid.VolumeMl==0,"Closed receiving flask rejects liquid without losing it.");receiver.TrySetStopperOpen(true);
            Transfer(driver,4);Check(pipette.VolumeMl==6&&receiver.Liquid.VolumeMl==4&&!driver.CanRecord(out _),"Partial delivery retains six mL in the real pipette and cannot be recorded as complete.");Conserved(rig);
            var checkpoint=session.Checkpoint();deliveryRoot=new GameObject("C05 restored delivery");var deliverySession=ExperimentSession.Restore(checkpoint);var deliveryRig=Build(deliveryRoot,deliverySession,new Vector3(106,10,100));var deliveryDriver=(ChemistryDriver)deliveryRig.Driver;
            Check(Item<VlabFlask>(deliveryRig,"pipette").VolumeMl==6&&Item<VlabVolumetricFlask>(deliveryRig,"vol50").Liquid.VolumeMl==4&&Item<VlabPipetteBulb>(deliveryRig,"bulb").Control==0,"Reload restores partial delivery and loaded pipette with valve closed.");Conserved(deliveryRig);
            Check(deliveryRig.PrepareNext(out why),why);Check(deliveryDriver.BeginManualPipette(out why),why);Flask(deliveryRig);Transfer(deliveryDriver,6);Check(deliveryRig.PrepareNext(out why),why);Conserved(deliveryRig);
            Check(deliveryDriver.CompletedPipetteOperations==2&&deliveryDriver.Observe().Get("aliquot")==10&&string.IsNullOrEmpty(deliverySession.Assistance),"Manual 4 + 6 mL delivery completes the aliquot without an assisted transfer.");
            Check(JsonUtility.FromJson<ChemistryDriver.Draft>(deliveryDriver.SaveState()).samples.Count==0,"Partial checkpoint invents no labelled or measured sample.");
            var wash=Item<VlabWashBottle>(deliveryRig,"wash");var flask=Item<VlabVolumetricFlask>(deliveryRig,"vol50");var neck=flask.GetComponentsInChildren<Transform>().Single(t=>t.name.EndsWith("_NeckSocket_SOCKET",StringComparison.Ordinal));
            wash.transform.rotation=Quaternion.FromToRotation(wash.OutletDirection,Vector3.down)*wash.transform.rotation;wash.transform.position+=neck.position+Vector3.up*.08f-wash.Outlet;
            Check(deliveryDriver.BeginManualWash(out why),why);Check(Math.Abs(deliveryDriver.DispenseManualWater(40,out why)-40)<1e-8,why);Conserved(deliveryRig);
            Check(deliveryDriver.Operate("invert",out why),why);Check(deliveryRig.PrepareNext(out why),why);Check(deliverySession.Start(out why),why);deliverySession.Advance(2);Check(deliverySession.Measure(out why),why);
            Check(Math.Abs(deliverySession.Recorded.Get("concentration")-.2)<1e-10,"Completed concentration comes from 0.010 mol in the actual 50 mL flask.");
            Debug.Log("C05_MANUAL_PIPETTE_PASS: actual tip geometry, finite 4+6 rinse/delivery, stock/waste/pipette/flask volume and solute conservation, stop/pause/loss, partial reload and invalid-draft rejection, explicit assistance, manual wash completion. UI pointer path requires its separate runtime check.");
        }
        finally{UnityEngine.Object.DestroyImmediate(root);if(rinseRoot)UnityEngine.Object.DestroyImmediate(rinseRoot);if(deliveryRoot)UnityEngine.Object.DestroyImmediate(deliveryRoot);}
    }
}
#endif
