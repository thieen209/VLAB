#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using Vlab.Product;

public static class ReleasedEquipmentChecks
{
    public static IEnumerator Run(Action<string,bool> check)
    {
        var root=new GameObject("Released apparatus physics fixture");var rig=root.AddComponent<ApparatusRig>();
        try
        {
            var session=new ExperimentSession("C06",73);rig.Build(session,new Vector3(500,3,500));
            foreach(var handle in rig.items.ToArray()){rig.Select(handle);check("Release fixture installs "+handle.assetId,rig.InstallSelected());}
            var item=rig.items.Single(i=>i.assetId=="test");var liquid=item.GetComponentInChildren<VlabBeaker>();double volume=liquid.VolumeMl;
            var original=item.transform.position;var rotation=item.transform.rotation;
            check("A free beaker can be lifted",rig.LiftForInspection(item,new Ray(original+Vector3.up,Vector3.down)));
            item.transform.position=rig.Worktop+new Vector3(.85f,.35f,.35f);var release=item.transform.position;
            ApparatusRig.TryVisibleBounds(item,out var bounds);check("Release fixture has real identified support",rig.TryGetRestingSupport(item,bounds,out var support,out _));
            check("Explicit put-down releases ownership",rig.ReleaseInspected(out _)&&item.Owner==EquipmentGrabOwner.None&&!rig.IsCarrying);
            var settling=item.GetComponent<ReleasedEquipment>();
            check("Put-down starts gravity without snapping the model",settling&&settling.IsSettling&&Vector3.Distance(item.transform.position,release)<.00001f&&item.GetComponent<Rigidbody>().useGravity);
            yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();
            check("Physics steps move the released tool down at its chosen horizontal location",item.transform.position.y<release.y-.001f&&Mathf.Abs(item.transform.position.x-release.x)<.0001f&&Mathf.Abs(item.transform.position.z-release.z)<.0001f);
            for(int frame=0;frame<150&&settling.IsSettling;frame++)yield return new WaitForFixedUpdate();
            ApparatusRig.TryVisibleBounds(item,out bounds);
            check("Tool reaches physical support and restores native selectable colliders",!settling.IsSettling&&Mathf.Abs(bounds.min.y-support.max.y)<.006f&&item.GetComponentsInChildren<MeshCollider>().Any(c=>c.enabled));
            check("Settling preserves liquid, upright calibration and experiment progress",liquid.VolumeMl==volume&&Quaternion.Angle(item.transform.rotation,rotation)<.001f&&session.Operation==0&&session.Phase==ExperimentPhase.Preparing);
            var placed=item.transform.position;
            check("Placed tool can be picked up again",rig.LiftForInspection(item,new Ray(placed+Vector3.up,Vector3.down)));
            item.transform.rotation=Quaternion.AngleAxis(30,Vector3.forward)*rotation;
            check("Tilted liquid cannot silently snap upright or discard its load",!rig.ReleaseInspected(out _)&&item.Owner==EquipmentGrabOwner.Legacy&&liquid.VolumeMl==volume);
            rig.CancelGrab();check("Explicit cancellation returns to the most recent supported pickup location",Vector3.Distance(item.transform.position,placed)<.0001f);
            check("Fixture can return the item to uninstalled state",rig.ReturnToTray(item));
            rig.Select(item);check("A loose tool can be picked up",rig.TakeSelected(new Ray(item.transform.position+Vector3.up,Vector3.down),out _));
            item.transform.position=release;check("A mistaken loose pickup can be released without installation",rig.ReleaseLooseOnSurface(out _)&&!item.installed&&item.Owner==EquipmentGrabOwner.None);
            settling=item.GetComponent<ReleasedEquipment>();for(int frame=0;frame<150&&settling.IsSettling;frame++)yield return new WaitForFixedUpdate();
            check("Loose release keeps its new surface location and does not advance setup",!settling.IsSettling&&Vector2.Distance(new Vector2(item.transform.position.x,item.transform.position.z),new Vector2(release.x,release.z))<.001f&&!item.installed&&session.Operation==0);
            var freePosition=item.transform.position;var pose=new Pose(freePosition,Quaternion.identity);
            check("Controller can reacquire released tool",rig.BeginHandGrab(1,item,pose,freePosition));rig.EndHandGrab(1,false);
            check("Controller input loss restores the supported pickup pose",item.Owner==EquipmentGrabOwner.None&&Vector3.Distance(item.transform.position,freePosition)<.001f);
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
}
#endif
