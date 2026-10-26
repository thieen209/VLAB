using System;
using UnityEngine;

namespace Vlab.Product
{
    // Integration boundary: NOT WIRED to VlabApp input or any lesson yet.
    // The lesson must supply authored geometry, open/cap state, liquid identity and
    // a justified requested dose, then update composition from the returned amount.
    // No flow-rate, reaction kinetics, spilling or tilted liquid rendering is implied.
    public static class LiquidPourTransfer
    {
        public struct Vessel
        {
            public VlabBeaker liquid;
            public Vector3 localUp, localMouth;
            public Transform outlet;
            public float mouthRadiusM;
            public bool open;
        }

        static readonly RaycastHit[] hits=new RaycastHit[32];
        static readonly Collider[] contacts=new Collider[32];
        const float StreamRadius=.001f;
        static bool Finite(Vector3 v)=>float.IsFinite(v.x)&&float.IsFinite(v.y)&&float.IsFinite(v.z);
        static bool Valid(Vessel v)=>v.liquid&&v.liquid.isActiveAndEnabled&&(v.liquid.transform.lossyScale-Vector3.one).sqrMagnitude<1e-8f&&Finite(v.localUp)&&v.localUp.sqrMagnitude>.99f&&v.localUp.sqrMagnitude<1.01f&&Finite(v.localMouth)&&float.IsFinite(v.mouthRadiusM)&&v.mouthRadiusM>StreamRadius;

        // A conservative interaction gate, not a hydraulic flow model. Dose timing
        // belongs to the caller; the delivered SDK performs the conserved mutation.
        public static double Transfer(Vessel source,Vessel target,double requestedMl,out string reason)
        {
            reason="";
            if(!Valid(source)||!Valid(target)||!source.outlet||!source.outlet.IsChildOf(source.liquid.transform)||source.liquid==target.liquid||double.IsNaN(requestedMl)||double.IsInfinity(requestedMl)||requestedMl<=0)
            {reason="Invalid vessel geometry or requested volume.";return 0;}
            if(!source.open||!target.open){reason="Open both vessels first.";return 0;}
            if(source.liquid.VolumeMl<=0||target.liquid.VolumeMl>=target.liquid.CapacityMl){reason="Source empty or receiver full.";return 0;}
            Vector3 sourceUp=source.liquid.transform.TransformDirection(source.localUp).normalized;
            Vector3 targetUp=target.liquid.transform.TransformDirection(target.localUp).normalized;
            Vector3 origin=source.outlet.position,mouth=target.liquid.transform.TransformPoint(target.localMouth);
            Vector3 sourceMouth=source.liquid.transform.TransformPoint(source.localMouth);
            if(!Finite(origin)||!Finite(mouth)||Vector3.Dot(sourceUp,Vector3.up)>.35f||Vector3.Dot(targetUp,Vector3.up)<.985f||origin.y>sourceMouth.y+.0001f)
            {reason="Tilt the pouring spout downward and keep the receiver upright.";return 0;}
            float drop=Vector3.Dot(mouth-origin,targetUp)/Vector3.Dot(Vector3.down,targetUp);
            if(!float.IsFinite(drop)||drop<.005f||drop>.45f){reason="Place the outlet above the receiving mouth.";return 0;}
            Vector3 landing=origin+Vector3.down*drop;
            if((landing-mouth).magnitude>target.mouthRadiusM-StreamRadius){reason="The stream misses the receiving mouth.";return 0;}
            Physics.SyncTransforms();
            // Include contact at the outlet: casts alone miss a collider containing
            // their origin. Saturated queries fail closed instead of skipping objects.
            int count=Physics.OverlapSphereNonAlloc(origin,StreamRadius,contacts,~0,QueryTriggerInteraction.Ignore);
            if(count==contacts.Length){reason="The outlet is obstructed.";return 0;}
            for(int i=0;i<count;i++)if(!contacts[i].transform.IsChildOf(source.liquid.transform)){reason="The outlet is obstructed.";return 0;}
            count=Physics.SphereCastNonAlloc(origin,StreamRadius,Vector3.down,hits,drop-StreamRadius*2,~0,QueryTriggerInteraction.Ignore);
            if(count==hits.Length){reason="The pouring path is obstructed.";return 0;}
            for(int i=0;i<count;i++)if(!hits[i].collider.transform.IsChildOf(source.liquid.transform)){reason="The pouring path is obstructed.";return 0;}
            return source.liquid.TransferTo(target.liquid,requestedMl);
        }
    }
}
