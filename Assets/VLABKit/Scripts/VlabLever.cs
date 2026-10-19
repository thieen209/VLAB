using UnityEngine;
public sealed class VlabLever : MonoBehaviour
{
 [SerializeField] Quaternion bodyRotation;
 [SerializeField] Vector3 localLength;
 VlabFulcrum fulcrum;
 public int PivotMark {get;private set;}
 public float AngleDegrees {get;private set;}
 bool Ready=>isActiveAndEnabled&&(transform.lossyScale-Vector3.one).sqrMagnitude<1e-8f;
 public bool TryMount(VlabFulcrum support,int pivotMark,float angleDegrees){
  if(!Ready||support==null||pivotMark< -1||pivotMark>1||!support.TryLeverPose(pivotMark*.1f,angleDegrees,out var pose))return false;
  transform.SetPositionAndRotation(pose.position,pose.rotation*bodyRotation);fulcrum=support;PivotMark=pivotMark;AngleDegrees=angleDegrees;return true;
 }
 public bool TryMoments(int loadMark,int effortMark,double loadN,double effortN,double beamWeightN,out double netTorqueNm,out double supportForceN){
  netTorqueNm=supportForceN=0;
  if(!Ready||fulcrum==null||loadMark< -4||loadMark>4||effortMark< -4||effortMark>4||!double.IsFinite(loadN)||!double.IsFinite(effortN)||!double.IsFinite(beamWeightN)||loadN<0||effortN<0||beamWeightN<0||!fulcrum.TryLeverPose(0,0,out var frame))return false;
  var down=-(frame.rotation*Vector3.up);var shaft=frame.rotation*Vector3.forward;
  double arm(int mark)=>Vector3.Dot(Vector3.Cross(transform.TransformPoint(localLength*(mark*.1f))-fulcrum.PivotPoint,down),shaft);
  double torque=loadN*arm(loadMark)+effortN*arm(effortMark)+beamWeightN*arm(0),reaction=loadN+effortN+beamWeightN;
  if(!double.IsFinite(torque)||!double.IsFinite(reaction))return false;netTorqueNm=torque;supportForceN=reaction;return true;
 }
 // shortcut: static moments for two downward forces and explicit uniform beam weight; dynamics and real hanging masses belong to M02 integration.
}
