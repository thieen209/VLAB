using System.Linq;
using UnityEngine;

namespace Vlab.Product
{
    public sealed partial class ApparatusRig
    {
        bool carryingInstalled;Vector3 carryRest;Quaternion carryRestRotation;
        public string LastPlacementFailure{get;private set;}="";
        public bool InspectingInstalled=>carryingInstalled&&carrying&&Selected;
        bool MovableTool(EquipmentHandle item)
        {
            var spec=Driver?.Kit.FirstOrDefault(k=>k.key==item.assetId);
            if(item.installed&&spec!=null&&!spec.onSurface)return false;
            return new[]{"beaker","test_tube","test_tube_15ml","beaker_100ml","beaker_250ml","dropper","pipette","forceps","stir_rod","thermometer","pestle","flask","erlenmeyer"}.Contains(spec?.prefab??item.assetId);
        }
        public bool CanLiftForInspection(EquipmentHandle item)
        {
            if(!item||!item.installed||item.Owner!=EquipmentGrabOwner.None||session.Phase==ExperimentPhase.Paused)return false;
            return MovableTool(item);
        }
        // Cancellation restores the picked-up pose; explicit placement preserves a supported new pose.
        public bool LiftForInspection(EquipmentHandle item,Ray pointer)
        {
            if(!CanLiftForInspection(item)||IsCarrying||IsPullingBob)return false;
            var falling=item.GetComponent<ReleasedEquipment>();var rest=falling&&falling.IsSettling?falling.SupportedPosition:item.transform.position;
            Select(item,false);if(!item.TryAcquire(EquipmentGrabOwner.Legacy))return false;
            carryRest=rest;carryRestRotation=item.transform.rotation;
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
        public bool CanReleaseLoose=>Selected&&!Selected.installed&&Selected.Owner==EquipmentGrabOwner.Legacy&&carrying&&MovableTool(Selected);
        bool MovePhoneCarryOverSurface(Ray pointer)
        {
            if(!MovableTool(Selected)||!TryVisibleBounds(Selected,out var bounds))return false;
            float nearest=float.PositiveInfinity;Vector3 destination=default;
            foreach(var surface in storageSurfaces)
            {
                // shortcut: touch uses 8 cm support clearance; replace if explicit vertical touch placement is added.
                float height=surface.bounds.max.y+.08f-(bounds.min.y-Selected.transform.position.y);
                if(!new Plane(Vector3.up,new Vector3(0,height,0)).Raycast(pointer,out float distance)||!float.IsFinite(distance)||distance<.2f||distance>2.5f||distance>=nearest)continue;
                var position=pointer.GetPoint(distance);var candidate=bounds;candidate.center+=position-Selected.transform.position;
                if(!TryGetRestingSupport(Selected,candidate,out var support,out _)||Mathf.Abs(support.max.y-surface.bounds.max.y)>.001f)continue;
                nearest=distance;destination=position;
            }
            if(float.IsPositiveInfinity(nearest))return false;
            Selected.transform.position=destination;return true;
        }
        public bool ReleaseLooseOnSurface(out string message)
        {
            message="Place the complete item over a clear bench surface.";
            if(!CanReleaseLoose)return false;
            if(!TryPlaceSupported(Selected,out message))return false;
            var item=Selected;item.stowed=false;item.Release(EquipmentGrabOwner.Legacy);
            item.trayPosition=item.GetComponent<ReleasedEquipment>().SupportedPosition;item.trayRotation=item.transform.rotation;
            Selected=null;carrying=pullingBob=latchedCarry=carriedFromStorage=false;UpdateTargets();Changed?.Invoke();return true;
        }
        bool TryPlaceSupported(EquipmentHandle item,out string message)
        {
            message="Place the complete item over a clear bench surface.";
            if(!item||!MovableTool(item)||!TryVisibleBounds(item,out var bounds))return false;
            bool vessel=item.GetComponentInChildren<VlabBeaker>()||item.GetComponentInChildren<VlabFlask>();
            var liquid=item.GetComponentInChildren<VlabFlask>();
            if(liquid&&liquid.VolumeMl>0&&Vector3.Dot((liquid.CavityCentreAt(1)-liquid.CavityCentreAt(0)).normalized,Vector3.up)<.99999f)
            {message="Keep the vessel upright before setting it down.";return false;}
            if(vessel&&Vector3.Dot(item.transform.rotation*Quaternion.Inverse(item.rotation)*Vector3.up,Vector3.up)<.98f)
            {message="Keep the vessel upright before setting it down.";return false;}
            if(!TryGetRestingSupport(item,bounds,out var support,out message))return false;
            if(bounds.min.y<support.max.y-.001f){message="Lift the tool above the surface before letting go.";return false;}
            var swept=bounds;swept.Encapsulate(new Vector3(bounds.min.x,support.max.y+.002f,bounds.min.z));swept.Expand(-.001f);
            if(items.Any(other=>other&&other!=item&&other.gameObject.activeInHierarchy&&!other.transform.IsChildOf(item.transform)&&!item.transform.IsChildOf(other.transform)&&TryVisibleBounds(other,out var occupied)&&swept.Intersects(occupied)))
            {message="Move above a clear resting area before letting go.";return false;}
            var settling=item.GetComponent<ReleasedEquipment>()??item.gameObject.AddComponent<ReleasedEquipment>();
            if(!settling.Begin(bounds,support)){message="This mounted assembly cannot be dropped.";return false;}
            message="";return true;
        }
        void RestoreInspected()
        {if(carryingInstalled&&Selected&&Selected.Owner==EquipmentGrabOwner.Legacy)Selected.transform.SetPositionAndRotation(carryRest,carryRestRotation);carryingInstalled=false;}
    }
}
