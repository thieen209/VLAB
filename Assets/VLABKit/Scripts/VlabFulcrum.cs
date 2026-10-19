using UnityEngine;
public sealed class VlabFulcrum : MonoBehaviour
{
 [SerializeField] Transform pivot;
 [SerializeField] Vector3 localUp,localShaft;
 public Vector3 PivotPoint=>pivot.position;
 public bool TryLeverPose(float pivotOffsetM,float angleDegrees,out Pose pose){
 pose=default;
 if(!isActiveAndEnabled||(transform.lossyScale-Vector3.one).sqrMagnitude>1e-10f||!float.IsFinite(pivotOffsetM)||!float.IsFinite(angleDegrees)||Mathf.Abs(pivotOffsetM)>.3968f||Mathf.Abs(angleDegrees)>45)return false;
 float a=angleDegrees*Mathf.Deg2Rad;
 float low=.044f+Mathf.Min((-.4f-pivotOffsetM)*Mathf.Sin(a),(.4f-pivotOffsetM)*Mathf.Sin(a))-.005f*Mathf.Abs(Mathf.Cos(a));
 if(low<.0005f)return false;
 var frame=Quaternion.LookRotation(transform.TransformDirection(localShaft),transform.TransformDirection(localUp));
 var rotation=frame*Quaternion.AngleAxis(angleDegrees,Vector3.forward);
 pose=new Pose(pivot.position-rotation*new Vector3(pivotOffsetM,0,0),rotation);return true;
 }
 // shortcut: pose for an800x30x10mm lever in a local X-length/Y-up frame on the base plane; actual lever bore fit and dynamics are separate.
}
