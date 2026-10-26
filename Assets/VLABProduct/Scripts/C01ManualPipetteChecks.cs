#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEngine;
using Vlab.Product;

public static class C01ManualPipetteChecks
{
    public static void DeliverIndicatorFixture(ApparatusRig rig)
    {
        var driver=(ChemistryDriver)rig.Driver;var item=rig.items.Single(i=>i.assetId=="indicator");var drop=item.GetComponentInChildren<VlabDropper>();
        var flask=rig.items.Single(i=>i.assetId=="flask");flask.transform.position=flask.slot+Vector3.back*.12f;
        var mouth=flask.GetComponentsInChildren<Transform>().Single(t=>t.name.EndsWith("_NeckSocket_SOCKET",StringComparison.Ordinal));
        var position=item.transform.position;var rotation=item.transform.rotation;
        try
        {
            item.transform.rotation=Quaternion.FromToRotation(drop.EmissionDirection,Vector3.down)*item.transform.rotation;
            item.transform.position+=mouth.position+Vector3.up*.04f-drop.Tip.position;
            if(!driver.BeginManualDropper(out var why))throw new Exception(why);
            for(int n=0;n<2;n++)if(driver.TransferManualDropper(out why)!=1)throw new Exception(why);
        }
        finally{driver.StopManualDropper();item.transform.SetPositionAndRotation(position,rotation);}
    }
    public static void Run(Action<string,bool> check)
    {
        var root=new GameObject("C01 manual pipette checks");
        try
        {
            var session=new ExperimentSession("C01");var rig=root.AddComponent<ApparatusRig>();rig.Build(session,new Vector3(100,10,100));
            foreach(var item in rig.items){rig.Select(item);check("C01 install "+item.assetId,rig.InstallSelected());}
            var driver=(ChemistryDriver)rig.Driver;
            T Item<T>(string key) where T:Component=>rig.items.Single(i=>i.assetId==key).GetComponentInChildren<T>();
            var source=Item<VlabReagentBottle>("sample");var pipette=Item<VlabFlask>("pipette");var flask=Item<VlabFlask>("flask");var waste=Item<VlabBeaker>("waste");
            check("C01 charge burette",rig.PrepareNext(out _));check("C01 prime burette",rig.PrepareNext(out _));check("C01 retain initial meniscus",driver.Operate("initial",out _));
            check("C01 preparation cannot silently rinse and transfer sample",!rig.PrepareNext(out _)&&source.Liquid.VolumeMl==120&&flask.VolumeMl==0);
            check("C01 pipette has a real separate parking support",driver.ValidatePipetteParking(out _));
            var tip=pipette.GetComponentsInChildren<Transform>().Single(t=>t.name.EndsWith("_TipOutlet_SOCKET",StringComparison.Ordinal));
            void Aim(Vector3 target){pipette.transform.position+=target-tip.position;Physics.SyncTransforms();}
            void Move(double ml){check("C01 accepts actual "+ml+" mL",Math.Abs(driver.TransferManualPipette(ml,out _)-ml)<1e-8);}
            void Stock()=>Aim(source.Liquid.CavityCentreAt(source.Liquid.SurfaceHeightMeters*.55));
            check("C01 manual pipette starts",driver.BeginManualPipette(out _));Stock();source.TrySetCapOpen(false);
            check("C01 closed sample bottle blocks aspiration",driver.TransferManualPipette(4,out _)==0&&pipette.VolumeMl==0);source.TrySetCapOpen(true);Move(4);
            driver.StopManualPipette();check("C01 partial contents survive release and real parking",driver.ParkManualPipette(out _)&&pipette.VolumeMl==4&&source.Liquid.VolumeMl==116);
            check("C01 manual pipette resumes",driver.BeginManualPipette(out _));Aim(waste.transform.position+Vector3.up*.02f);Move(4);Stock();Move(6);Aim(waste.transform.position+Vector3.up*.02f);Move(6);
            check("C01 rinse alone cannot complete aliquot",driver.PipetteNeedsRinse==false&&!rig.PrepareNext(out _)&&flask.VolumeMl==0);
            Stock();Move(10);check("C01 cannot overfill pipette",driver.TransferManualPipette(1,out _)==0&&pipette.VolumeMl==10);
            Aim(flask.CavityCentreAt(.02));Move(4);driver.StopManualPipette();check("C01 partial aliquot stays incomplete",!rig.PrepareNext(out _)&&flask.VolumeMl==4&&pipette.VolumeMl==6);
            check("C01 resume remaining aliquot",driver.BeginManualPipette(out _));Move(6);
            check("C01 manual aliquot completes the preparation stage",rig.PrepareNext(out _)&&session.Operation==3&&flask.VolumeMl==10&&source.Liquid.VolumeMl==100&&pipette.VolumeMl==0);
            check("C01 conserves actual volume including priming waste",Math.Abs(source.Liquid.VolumeMl+pipette.VolumeMl+flask.VolumeMl+waste.VolumeMl-124)<1e-8);
            check("C01 completion cannot draw another hidden aliquot",!driver.BeginManualPipette(out _)&&driver.TransferManualPipette(10,out _)==0&&source.Liquid.VolumeMl==100);
            check("C01 preparation cannot silently add indicator",!rig.PrepareNext(out _)&&session.Operation==3);
            var drop=Item<VlabDropper>("indicator");var neck=flask.GetComponentsInChildren<Transform>().Single(t=>t.name.EndsWith("_NeckSocket_SOCKET",StringComparison.Ordinal));
            drop.transform.rotation=Quaternion.FromToRotation(drop.EmissionDirection,Vector3.down)*drop.transform.rotation;
            drop.transform.position+=neck.position+Vector3.up*.04f+Vector3.right*.1f-drop.Tip.position;
            check("C01 indicator gesture starts",driver.BeginManualDropper(out _));
            check("C01 misses do not consume indicator",driver.TransferManualDropper(out _)==0&&driver.IndicatorDrops==0);
            drop.transform.position+=neck.position+Vector3.up*.04f-drop.Tip.position;
            var obstacle=GameObject.CreatePrimitive(PrimitiveType.Cube);obstacle.transform.position=neck.position+Vector3.up*.02f;obstacle.transform.localScale=new Vector3(.04f,.005f,.04f);
            check("C01 blocked mouth rejects indicator",driver.TransferManualDropper(out _)==0&&driver.IndicatorDrops==0);UnityEngine.Object.DestroyImmediate(obstacle);
            check("C01 accepts first drop at actual mouth",driver.TransferManualDropper(out _)==1&&driver.IndicatorDrops==1);
            driver.StopManualDropper();check("C01 release stops dispensing and partial dose cannot advance",driver.TransferManualDropper(out _)==0&&drop.Squeeze==0&&!rig.PrepareNext(out _)&&driver.IndicatorDrops==1);
            check("C01 regrab delivers remaining drop",driver.BeginManualDropper(out _)&&driver.TransferManualDropper(out _)==1&&driver.IndicatorDrops==2);
            check("C01 two-drop dose cannot overrun",driver.TransferManualDropper(out _)==0&&driver.IndicatorDrops==2);
            check("C01 actual completed indicator allows preparation",rig.PrepareNext(out _)&&session.Operation==4&&drop.Squeeze==0);
            check("C01 uncalibrated indicator does not invent liquid volume",flask.VolumeMl==10);
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
}
#endif
