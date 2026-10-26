using System.Linq;
using UnityEngine;

namespace Vlab.Product
{
    public sealed partial class ApparatusRig
    {
        bool carryingInstalled;Vector3 carryRest;Quaternion carryRestRotation;
        public bool InspectingInstalled=>carryingInstalled&&carrying&&Selected;
        public bool CanLiftForInspection(EquipmentHandle item)
        {
            if(!item||!item.installed||item.Owner!=EquipmentGrabOwner.None||session.Phase==ExperimentPhase.Paused)return false;
            string prefab=Driver?.Kit.FirstOrDefault(k=>k.key==item.assetId)?.prefab??item.assetId;
            return new[]{"beaker","test_tube","test_tube_15ml","beaker_100ml","beaker_250ml","dropper","pipette","forceps","stir_rod","thermometer","pestle","flask","erlenmeyer"}.Contains(prefab);
        }
        // Cancellation restores the picked-up pose; explicit placement preserves a supported new pose.
        public bool LiftForInspection(EquipmentHandle item,Ray pointer)
        {
            if(!CanLiftForInspection(item)||IsCarrying||IsPullingBob)return false;
            Select(item,false);if(!item.TryAcquire(EquipmentGrabOwner.Legacy))return false;
            carryRest=item.transform.position;carryRestRotation=item.transform.rotation;
            carryingInstalled=carrying=latchedCarry=true;carryDistance=.65f;
            item.transform.position=pointer.GetPoint(carryDistance);Changed?.Invoke();return true;
        }
        public void RotateInspected(float degrees)
        {if(InspectingInstalled&&Selected.Owner==EquipmentGrabOwner.Legacy&&float.IsFinite(degrees))Selected.transform.Rotate(Vector3.up,Mathf.Clamp(degrees,-30,30),Space.World);}
        public bool ReleaseInspected(out string message)
        {
            message="";if(!InspectingInstalled||Selected.Owner!=EquipmentGrabOwner.Legacy)return false;
            if(!TryPlaceSupported(Selected,out message))return false;
            carryingInstalled=false;CancelGrab();return true;
        }
        bool TryPlaceSupported(EquipmentHandle item,out string message)
        {
            message="Place the complete item over a clear bench surface.";
            if(!item||!TryVisibleBounds(item,out var bounds))return false;
            bool vessel=item.GetComponentInChildren<VlabBeaker>()||item.GetComponentInChildren<VlabFlask>();
            if(vessel&&Vector3.Dot(item.transform.rotation*Quaternion.Inverse(item.rotation)*Vector3.up,Vector3.up)<.98f)
            {message="Keep the vessel upright before setting it down.";return false;}
            foreach(var surface in storageSurfaces.OrderBy(s=>Mathf.Abs(s.bounds.max.y-bounds.min.y)))
            {
                if(!ContainsRect(Footprint(surface.bounds,-.005f),Footprint(bounds))||bounds.min.y<surface.bounds.max.y-.08f||bounds.min.y>surface.bounds.max.y+.9f)continue;
                var delta=Vector3.up*(surface.bounds.max.y+StorageLift-bounds.min.y);var placed=bounds;placed.center+=delta;placed.Expand(-.002f);
                if(items.Any(other=>other&&other!=item&&other.gameObject.activeInHierarchy&&!other.transform.IsChildOf(item.transform)&&!item.transform.IsChildOf(other.transform)&&TryVisibleBounds(other,out var occupied)&&placed.Intersects(occupied)))continue;
                // shortcut: settle on the existing measured bench bounds; free falling and spill simulation need calibrated vessel dynamics.
                item.transform.position+=delta;Physics.SyncTransforms();message="";return true;
            }
            return false;
        }
        void RestoreInspected()
        {if(carryingInstalled&&Selected&&Selected.Owner==EquipmentGrabOwner.Legacy)Selected.transform.SetPositionAndRotation(carryRest,carryRestRotation);carryingInstalled=false;}
    }
}
