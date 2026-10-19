using UnityEngine;
public sealed class VlabConnectingRod : MonoBehaviour
{
 [SerializeField] Transform pinA,pinB;
 [SerializeField] Vector3 localA,localB,localNormal;
 public Transform PinA=>pinA;
 public Transform PinB=>pinB;
 public float LengthM=>(localB-localA).magnitude;
 static bool Finite(Vector3 v)=>float.IsFinite(v.x)&&float.IsFinite(v.y)&&float.IsFinite(v.z);
 public bool SetEndpoints(Vector3 a,Vector3 b,Vector3 normal)
 {
  if(!isActiveAndEnabled||!Finite(a)||!Finite(b)||!Finite(normal)||(transform.lossyScale-Vector3.one).sqrMagnitude>1e-10f)return false;
  var direction=b-a;float length=direction.magnitude;
  if(!float.IsFinite(length)||Mathf.Abs(length-LengthM)>1e-6f||length<1e-6f||normal.sqrMagnitude<1e-12f||!float.IsFinite(normal.sqrMagnitude))return false;
  var n=Vector3.ProjectOnPlane(normal.normalized,direction.normalized);
  if(n.sqrMagnitude<1e-8f)return false;
  var rotation=Quaternion.LookRotation(n.normalized,direction.normalized)*Quaternion.Inverse(Quaternion.LookRotation(localNormal,(localB-localA).normalized));
  transform.SetPositionAndRotation(a-rotation*localA,rotation);return true;
 }
 // shortcut: rigid prescribed poses only; forces and crank-slider constraints belong to the assembled mechanism.
}
