#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEngine;
using Vlab.Product;

public static class C07ManualAcidChecks
{
    public static void DeliverAcidFixture(ApparatusRig rig)
    {
        var driver=(ChemistryDriver)rig.Driver;var item=rig.items.Single(i=>i.assetId=="acid");
        var source=item.GetComponentInChildren<VlabReagentBottle>().Liquid;var receiver=rig.items.Single(i=>i.assetId=="flask").GetComponentInChildren<VlabFlask>();
        // SDK fixture only: the native UI journey separately tests opening and supported stopper placement.
        rig.items.Single(i=>i.assetId=="stopper").transform.position+=Vector3.right;
        var position=item.transform.position;var rotation=item.transform.rotation;
        try
        {
            item.transform.rotation=Quaternion.AngleAxis(100,Vector3.forward)*rotation;item.transform.position+=receiver.Mouth+Vector3.up*.10f-source.PourOutlet;
            if(!driver.BeginManualPour("acid",out var why)||driver.TransferManualPour(80,out why)<=0||!driver.GasAcidComplete)throw new Exception("C07 manual dose fixture: "+why);
        }
        finally{driver.StopManualPour();item.transform.SetPositionAndRotation(position,rotation);}
    }
    public static void RunTransferCore(Action<string,bool> check)
    {
        var root=new GameObject("C07 acid transfer core");
        try
        {
            var session=new ExperimentSession("C07");var rig=root.AddComponent<ApparatusRig>();rig.Build(session,new Vector3(100,10,100));
            foreach(var item in rig.items){rig.Select(item);check("C07 core install "+item.assetId,rig.InstallSelected());}
            var bottle=rig.items.Single(i=>i.assetId=="acid").GetComponentInChildren<VlabReagentBottle>();var source=bottle.Liquid;
            var receiver=rig.items.Single(i=>i.assetId=="flask").GetComponentInChildren<VlabFlask>();
            var original=source.transform.rotation;source.transform.rotation=Quaternion.AngleAxis(100,Vector3.forward)*original;
            source.transform.position+=receiver.Mouth+Vector3.up*.10f-source.PourOutlet;
            check("C07 tilted public setter remains rejected",!source.TrySetVolume(79)&&source.VolumeMl==80);
            check("C07 closed receiver blocks transfer",!source.TryPourInto(receiver,5,bottle.CapOpen,false,out _)&&source.VolumeMl==80&&receiver.VolumeMl==0);
            rig.items.Single(i=>i.assetId=="stopper").transform.position+=Vector3.right;
            // The authored stopper has an open gas bore; closure is an explicit apparatus state, not a solid disk.
            var obstruction=GameObject.CreatePrimitive(PrimitiveType.Cube);obstruction.transform.SetParent(root.transform);obstruction.transform.position=receiver.Mouth+Vector3.up*.05f;obstruction.transform.localScale=new Vector3(.04f,.005f,.04f);
            check("C07 solid obstruction blocks the actual stream",!source.TryPourInto(receiver,5,bottle.CapOpen,true,out _)&&source.VolumeMl==80&&receiver.VolumeMl==0);UnityEngine.Object.DestroyImmediate(obstruction);
            check("C07 closed source blocks transfer",!source.TryPourInto(receiver,5,false,true,out _));
            check("C07 actual tilted mouth transfers finite partial amount",source.TryPourInto(receiver,5,bottle.CapOpen,true,out var moved)&&moved==5&&source.VolumeMl==75&&receiver.VolumeMl==5);
            check("C07 source stays tilted during accepted transfer",Quaternion.Angle(source.transform.rotation,Quaternion.AngleAxis(100,Vector3.forward)*original)<.01f);
            source.transform.position+=Vector3.right*.1f;
            check("C07 missed mouth does not consume source",!source.TryPourInto(receiver,5,true,true,out _)&&source.VolumeMl==75&&receiver.VolumeMl==5);
            source.transform.position-=Vector3.right*.1f;
            check("C07 nonfinite dose rejected",!source.TryPourInto(receiver,double.NaN,true,true,out _));
            check("C07 request is capped to finite source",source.TryPourInto(receiver,100,true,true,out moved)&&moved==75&&source.VolumeMl==0&&receiver.VolumeMl==80);
            check("C07 empty source cannot transfer again",!source.TryPourInto(receiver,1,true,true,out _));
            source.transform.rotation=original;check("C07 upright rendering resumes without a hidden refill",source.TrySetVolume(source.VolumeMl)&&source.VolumeMl==0);
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    public static void Run(Action<string,bool> check)
    {
        var root=new GameObject("C07 manual acid regression");
        try
        {
            var session=new ExperimentSession("C07");var rig=root.AddComponent<ApparatusRig>();rig.Build(session,new Vector3(100,10,100));
            foreach(var item in rig.items){rig.Select(item);check("C07 install "+item.assetId,rig.InstallSelected());}
            var source=rig.items.Single(i=>i.assetId=="acid").GetComponentInChildren<VlabReagentBottle>();
            var receiver=rig.items.Single(i=>i.assetId=="flask").GetComponentInChildren<VlabFlask>();
            var stopper=rig.items.Single(i=>i.assetId=="stopper");
            var neck=receiver.GetComponentsInChildren<Transform>().Single(t=>t.name.EndsWith("_NeckSocket_SOCKET",StringComparison.Ordinal));
            var seat=stopper.GetComponentsInChildren<Transform>().Single(t=>t.name.EndsWith("_Seat_SOCKET",StringComparison.Ordinal));
            check("C07 starts with finite source and empty receiver",source.Liquid.VolumeMl==80&&receiver.VolumeMl==0);
            check("C07 stopper is physically seated before preparation",Vector3.Distance(neck.position,seat.position)<.001f);
            double initialSource=source.Liquid.VolumeMl,initialReceiver=receiver.VolumeMl;
            bool advanced=rig.PrepareNext(out _);
            check("C07 preparation must not silently fill a stoppered flask",!advanced&&session.Operation==0&&source.Liquid.VolumeMl==initialSource&&receiver.VolumeMl==initialReceiver);
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
}
#endif
