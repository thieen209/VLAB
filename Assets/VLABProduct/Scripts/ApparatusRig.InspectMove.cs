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
        // Inspection returns to the authored pose: moving a vessel does not silently transfer its contents.
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
        void RestoreInspected()
        {if(carryingInstalled&&Selected&&Selected.Owner==EquipmentGrabOwner.Legacy)Selected.transform.SetPositionAndRotation(carryRest,carryRestRotation);carryingInstalled=false;}
    }
}
