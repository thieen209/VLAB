#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Vlab.Product;

public static class C05ManualWashChecks
{
    static void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
    static ApparatusRig Build(GameObject root,ExperimentSession session,Vector3 at)
    {
        var rig=root.AddComponent<ApparatusRig>();rig.Build(session,at);
        foreach(var item in rig.items){rig.Select(item);Check(rig.InstallSelected(),"Install "+item.assetId);}
        Check(rig.PrepareNext(out var why),why);Check(rig.PrepareNext(out why),why);return rig;
    }
    static T Item<T>(ApparatusRig rig,string key) where T:Component=>rig.items.First(i=>i.assetId==key).GetComponentInChildren<T>();
    static void Aim(VlabWashBottle wash,VlabVolumetricFlask flask)
    {
        var neck=flask.GetComponentsInChildren<Transform>().First(t=>t.name.EndsWith("_NeckSocket_SOCKET",StringComparison.Ordinal));
        wash.transform.rotation=Quaternion.FromToRotation(wash.OutletDirection,Vector3.down)*wash.transform.rotation;
        wash.transform.position+=neck.position+Vector3.up*.08f-wash.Outlet;
    }
    [MenuItem("VLAB/Checks/C05 manual wash flow")]
    public static void Run()
    {
        if(!Application.isPlaying)throw new InvalidOperationException("Run in Play mode with delivered SDK Awake initialized.");
        var root=new GameObject("C05 manual wash checks");GameObject restoredRoot=null,block=null;
        try
        {
            var session=new ExperimentSession("C05");var rig=Build(root,session,new Vector3(100,10,100));var driver=(ChemistryDriver)rig.Driver;
            var wash=Item<VlabWashBottle>(rig,"wash");var flask=Item<VlabVolumetricFlask>(rig,"vol50");
            double moles=driver.Observe().Get("solute");Check(driver.BeginManualWash(out var why),why);
            Check(driver.DispenseManualWater(1,out _)==0,"Original parked nozzle cannot magically fill the flask.");
            Aim(wash,flask);flask.TrySetStopperOpen(false);Check(driver.DispenseManualWater(1,out _)==0&&!wash.Stream.Flowing,"Closed stopper blocks water and stream.");flask.TrySetStopperOpen(true);
            block=GameObject.CreatePrimitive(PrimitiveType.Cube);block.transform.localScale=Vector3.one*.02f;block.transform.position=wash.Outlet+Vector3.down*.04f;
            Check(driver.DispenseManualWater(1,out _)==0,"Intervening geometry blocks the water jet.");block.transform.position=wash.Outlet;
            Check(driver.DispenseManualWater(1,out _)==0,"Contact at the outlet blocks the water jet.");block.SetActive(false);
            var pose=wash.transform.position;double waterBefore=wash.RemainingMl;
            Check(Math.Abs(driver.DispenseManualWater(12,out why)-12)<1e-8,"Accept 12 actual mL: "+why);
            Check(wash.transform.position==pose&&Math.Abs(flask.Liquid.VolumeMl-22)<1e-8&&Math.Abs(waterBefore-wash.RemainingMl-12)<1e-8,"Manual transfer retains actual pose and conserves water.");
            Check(Math.Abs(driver.Observe().Get("solute")-moles)<1e-12&&!driver.CanRecord(out _),"Partial fill conserves solute and cannot be recorded as complete.");
            driver.SetPaused(true);Check(!wash.Stream.Flowing&&driver.DispenseManualWater(1,out _)==0,"Pause ends the active squeeze without rollback.");
            Check(driver.BeginManualWash(out why),why);driver.InputLost();Check(!wash.Stream.Flowing&&driver.DispenseManualWater(1,out _)==0,"Input loss stops stream and further transfer.");
            var checkpoint=session.Checkpoint();restoredRoot=new GameObject("C05 restored manual wash");
            var restored=Build(restoredRoot,ExperimentSession.Restore(checkpoint),new Vector3(103,10,100));var restoredDriver=(ChemistryDriver)restored.Driver;
            Check(Math.Abs(Item<VlabVolumetricFlask>(restored,"vol50").Liquid.VolumeMl-22)<1e-8&&Math.Abs(Item<VlabWashBottle>(restored,"wash").RemainingMl-238)<1e-8,"Restore retains partial flask and finite water stock.");
            var draft=JsonUtility.FromJson<ChemistryDriver.Draft>(restoredDriver.SaveState());
            Check(draft.samples.Count==0&&!Item<VlabWashBottle>(restored,"wash").Stream.Flowing,"Restore creates no completed sample or running flow.");
            Check(restoredDriver.Operate("waterMark",out why),"Button fallback still completes the restored partial: "+why);
            Check(Math.Abs(Item<VlabVolumetricFlask>(restored,"vol50").Liquid.VolumeMl-50)<1e-8,"Fallback fills only the remaining actual volume.");
            Check(driver.BeginManualWash(out why),why);Aim(wash,flask);
            Check(Math.Abs(driver.DispenseManualWater(27.99,out why)-27.99)<1e-7,"Fill to the real calibration band: "+why);
            Check(!wash.Stream.Flowing&&driver.DispenseManualWater(1,out _)==0,"Reaching the mark stops flow; no hidden extra dose.");
            Check(driver.Operate("invert",out why),why);Check(rig.PrepareNext(out why),why);Check(session.Start(out why),why);session.Advance(2);
            Check(driver.CanRecord(out why),why);
            Check(Math.Abs(driver.Observe().Get("concentration")-moles/.04999)<1e-9,"Concentration uses accepted 49.99 mL, not the nominal 50 mL parameter.");
            Debug.Log("C05_MANUAL_WASH_PASS: actual SDK pose/path/contact/volume, pause/loss, partial restore, mark and measured concentration; pointer/UI route needs its own runtime check.");
        }
        finally{UnityEngine.Object.DestroyImmediate(root);if(restoredRoot)UnityEngine.Object.DestroyImmediate(restoredRoot);if(block)UnityEngine.Object.DestroyImmediate(block);}
    }
}
#endif
