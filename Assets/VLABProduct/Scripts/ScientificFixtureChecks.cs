#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEngine;
using Vlab.Product;

public static class ScientificFixtureChecks
{
    // Invoke on an actually prepared UI rig, before C03 cathode removal.
    // C02: preparation 1..3. C03: 1..3. B06: 1..2. B07: 1..3.
    public static void RunPrepared(ApparatusRig rig,Action<string,bool> check)
    {
        string id=rig.Session.spec.id;
        bool chemistry=id=="C02"||id=="C03",biology=id=="B06"||id=="B07";
        if(!chemistry&&!biology)return;
        var fixtures=rig.GetComponentsInChildren<ScientificFixtureSupport>();
        check(id+" actual prepared derived fixture count",fixtures.Length==(chemistry?2:1));
        check(id+" holders are insulating polymer",fixtures.All(x=>x.Insulating));
        if(chemistry)check(id+" one physical rim bridge per beaker",rig.GetComponentsInChildren<Renderer>().Count(x=>x.name=="Insulating rim bridge")== (id=="C02"?2:1));
        Func<bool> valid=()=>chemistry?((ChemistryDriver)rig.Driver).ValidateScientificFixtures(out _):((BiologyDriver)rig.Driver).ValidateScientificFixtures(out _);
        check(id+" actual support and measurement path",valid());
        if(biology)
        {
            var cup=rig.items.Single(x=>x.assetId=="receiver").GetComponentInChildren<VlabBeaker>();double before=cup.VolumeMl;
            var outlet=rig.items.Single(x=>x.assetId=="funnel").GetComponentInChildren<VlabFunnel>().Outflow.origin;
            double fullSurface=cup.transform.position.y+cup.InteriorBottom+cup.CapacityMl*1e-6/cup.CrossSectionArea;
            check(id+" prospective full receiver clearance",fixtures[0].ValidateProspectiveVolume(cup.CapacityMl,out _)==(outlet.y>fullSurface+.005));
            check(id+" prospective overflow rejected without mutation",!fixtures[0].ValidateProspectiveVolume(cup.CapacityMl+1,out _)&&Math.Abs(cup.VolumeMl-before)<1e-8);
        }
        foreach(var item in rig.items)check(id+" source scale preserved "+item.assetId,(item.transform.lossyScale-Vector3.one).sqrMagnitude<1e-8f);
        var probe=rig.items.Single(x=>x.assetId==(chemistry?(id=="C02"?"zn":"cathode"):"funnel"));
        void DetachAndRestore(Transform target,Vector3 shift,string label)
        {
            var position=target.position;
            try{target.position+=shift;Physics.SyncTransforms();check(id+" rejects "+label,!valid());}
            finally{target.position=position;Physics.SyncTransforms();}
            check(id+" restores "+label,valid());
        }
        DetachAndRestore(probe.transform,(chemistry?Vector3.forward:Vector3.right)*.012f,"real detached instrument");
        DetachAndRestore(probe.transform,Vector3.right*.012f,"instrument X detachment");
        DetachAndRestore(fixtures[0].transform,Vector3.right*.012f,"fixture X detachment");
    }
    // Run after B06 preparation1 / B07 preparation2, before successful filtering.
    public static void RejectFilterCapacity(ApparatusRig rig,Action<string,bool> check)
    {
        string id=rig.Session.spec.id;if(id!="B06"&&id!="B07")return;
        var source=rig.items.Single(x=>x.assetId=="extract").GetComponentInChildren<VlabBeaker>();
        var receiver=rig.items.Single(x=>x.assetId=="receiver").GetComponentInChildren<VlabBeaker>();
        double original=receiver.VolumeMl,sourceBefore=source.VolumeMl;
        check(id+" capacity failure fixture starts before filtering",sourceBefore>0);
        try
        {
            receiver.Add(receiver.CapacityMl-receiver.VolumeMl);double filled=receiver.VolumeMl;
            check(id+" rejects filtering without receiving capacity",!rig.Driver.Prepare(id=="B06"?2:3,out _));
            check(id+" rejected filtering preserves both volumes",Math.Abs(source.VolumeMl-sourceBefore)<1e-8&&Math.Abs(receiver.VolumeMl-filled)<1e-8);
        }
        finally{receiver.Remove(receiver.VolumeMl);receiver.Add(original);}
        check(id+" capacity failure fixture restores receiver amount",Math.Abs(receiver.VolumeMl-original)<1e-8&&Math.Abs(source.VolumeMl-sourceBefore)<1e-8);
    }
}
#endif
