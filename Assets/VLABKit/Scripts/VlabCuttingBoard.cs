using UnityEngine;
public sealed class VlabCuttingBoard : MonoBehaviour
{
 [SerializeField] Vector3 localCenter,localRight,localForward,localNormal;
 [SerializeField] Vector2 workSize;
 public Vector2 WorkSize=>workSize;
 public bool TryGetPlacement(Vector2 center,Vector2 footprint,out Pose pose)
 {
  pose=default;
  if(!isActiveAndEnabled||(transform.lossyScale-Vector3.one).sqrMagnitude>1e-10f||!float.IsFinite(center.x)||!float.IsFinite(center.y)||!float.IsFinite(footprint.x)||!float.IsFinite(footprint.y)||footprint.x<=0||footprint.y<=0)return false;
  if(Mathf.Abs(center.x)+footprint.x*.5f>workSize.x*.5f||Mathf.Abs(center.y)+footprint.y*.5f>workSize.y*.5f)return false;
  pose=new Pose(transform.TransformPoint(localCenter+localRight*center.x+localForward*center.y),Quaternion.LookRotation(transform.TransformDirection(localForward),transform.TransformDirection(localNormal)));return true;
 }
 // shortcut: footprint is aligned with the work area; callers must supply a containing footprint for rotated specimens and place their bottom pivot at the returned pose.
}
