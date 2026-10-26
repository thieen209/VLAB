using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Vlab.Product
{
    public enum EquipmentGrabOwner {None,LeftHand,RightHand,Legacy,Flagship}
    public sealed partial class EquipmentHandle
    {
        public EquipmentGrabOwner Owner{get;private set;}
        internal bool TryAcquire(EquipmentGrabOwner owner){if(owner==EquipmentGrabOwner.None||Owner!=EquipmentGrabOwner.None)return false;GetComponent<ReleasedEquipment>()?.StopSettling();Owner=owner;return true;}
        internal bool Release(EquipmentGrabOwner owner){if(owner==EquipmentGrabOwner.None||Owner!=owner)return false;Owner=EquipmentGrabOwner.None;return true;}
    }
    public sealed partial class ApparatusRig
    {
        sealed class HandHold
        {
            public EquipmentHandle item;public Vector3 rest,offset;public Quaternion restRotation,relativeRotation;
            public VlabDropper dropper;public VlabForceps forceps;
        }
        readonly Dictionary<int,HandHold> handHolds=new Dictionary<int,HandHold>();
        public EquipmentHandle HandItem(int hand)=>handHolds.TryGetValue(hand,out var hold)?hold.item:null;
        public bool IsHeldByHand(EquipmentHandle item)=>item&&(item.Owner==EquipmentGrabOwner.LeftHand||item.Owner==EquipmentGrabOwner.RightHand);
        static EquipmentGrabOwner HandOwner(int hand)=>hand==1?EquipmentGrabOwner.LeftHand:EquipmentGrabOwner.RightHand;
        public bool BeginFlagshipGrab(EquipmentHandle item)=>item&&items.Contains(item)&&item.TryAcquire(EquipmentGrabOwner.Flagship);
        public bool EndFlagshipGrab(EquipmentHandle item)=>item&&item.Release(EquipmentGrabOwner.Flagship);
        public bool BeginHandGrab(int hand,EquipmentHandle item,Pose pose,Vector3 hitPoint)
        {
            if(hand<1||hand>2||!item||!items.Contains(item)||handHolds.ContainsKey(hand)||item.Owner!=EquipmentGrabOwner.None||!ValidPose(pose))return false;
            if(session==null||session.Phase==ExperimentPhase.Paused||session.Phase==ExperimentPhase.Measured)return false;
            if(!item.installed&&session.Phase!=ExperimentPhase.Preparing&&session.Phase!=ExperimentPhase.Ready)return false;
            if(item.installed&&!MovableTool(item))return false;
            var falling=item.GetComponent<ReleasedEquipment>();var rest=falling&&falling.IsSettling?falling.SupportedPosition:item.transform.position;
            if(!item.TryAcquire(HandOwner(hand)))return false;
            if(Selected==item)Select(null,false);
            handHolds.Add(hand,new HandHold{item=item,rest=rest,restRotation=item.transform.rotation,offset=Vector3.ClampMagnitude(Quaternion.Inverse(pose.rotation)*(item.transform.position-hitPoint),.12f),relativeRotation=Quaternion.Inverse(pose.rotation)*item.transform.rotation,dropper=item.GetComponentInChildren<VlabDropper>(),forceps=item.GetComponentInChildren<VlabForceps>()});
            Changed?.Invoke();return true;
        }
        static bool ValidPose(Pose p)=>float.IsFinite(p.position.x)&&float.IsFinite(p.position.y)&&float.IsFinite(p.position.z)&&float.IsFinite(Quaternion.Dot(p.rotation,p.rotation))&&Quaternion.Dot(p.rotation,p.rotation)>.8f&&Quaternion.Dot(p.rotation,p.rotation)<1.2f;
        public void UpdateHandGrab(int hand,Pose pose,float trigger)
        {
            if(!handHolds.TryGetValue(hand,out var hold))return;
            if(!hold.item||hold.item.Owner!=HandOwner(hand)||!ValidPose(pose)||!float.IsFinite(trigger)||trigger<0||trigger>1){EndHandGrab(hand,false);return;}
            hold.item.transform.SetPositionAndRotation(pose.position+pose.rotation*(Vector3.forward*.08f+hold.offset),pose.rotation*hold.relativeRotation);
            if(hold.dropper)hold.dropper.SetSqueeze(trigger);if(hold.forceps)hold.forceps.SetSqueeze(trigger);
        }
        public void EndHandGrab(int hand,bool place)
        {
            if(!handHolds.TryGetValue(hand,out var hold))return;handHolds.Remove(hand);
            if(!hold.item||!hold.item.Release(HandOwner(hand)))return;
            if(hold.dropper)hold.dropper.SetSqueeze(0);if(hold.forceps)hold.forceps.SetSqueeze(0);
            bool fitted=false;LastPlacementFailure="";
            var releasedPosition=hold.item.transform.position;var releasedRotation=hold.item.transform.rotation;
            if(place&&!hold.item.installed&&Vector3.Distance(hold.item.transform.position,hold.item.slot)<.22f&&!Selected) {
                Select(hold.item,false);fitted=InstallSelected();
                if(!fitted){CancelGrab();hold.item.transform.SetPositionAndRotation(releasedPosition,releasedRotation);}
            }
            if(place&&!fitted&&MovableTool(hold.item))
            {
                fitted=TryPlaceSupported(hold.item,out var why);if(!fitted)LastPlacementFailure=why;
                else if(!hold.item.installed){hold.item.stowed=false;hold.item.trayPosition=hold.item.GetComponent<ReleasedEquipment>().SupportedPosition;hold.item.trayRotation=hold.item.transform.rotation;}
            }
            if(!fitted&&hold.item.Owner==EquipmentGrabOwner.None)hold.item.transform.SetPositionAndRotation(hold.rest,hold.restRotation);
            if(place&&!fitted&&LastPlacementFailure=="")LastPlacementFailure="Fit this supported assembly at its marked position.";
            Changed?.Invoke();
        }
        public void CancelHandGrabs(){EndHandGrab(1,false);EndHandGrab(2,false);}
        void OnDisable(){CancelHandGrabs();CancelGrab();}
    }
}
