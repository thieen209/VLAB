#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEngine;
using Vlab.Product;

public static class C01ManualPipetteChecks
{
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
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
}
#endif
