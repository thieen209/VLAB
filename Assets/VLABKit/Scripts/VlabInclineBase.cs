using UnityEngine;
public sealed class VlabInclineBase : MonoBehaviour
{
 [SerializeField] Transform pivot,leg,foot,shoe,pointer;
 [SerializeField] Quaternion frameCorrection,legCorrection,shoeCorrection,pointerCorrection;
 public float AngleDegrees{get;private set;}
 public float EndRiseM=>1.2f*Mathf.Sin(AngleDegrees*Mathf.Deg2Rad);
 public Pose RailPose=>new Pose(pivot.position,transform.rotation*frameCorrection*Quaternion.AngleAxis(AngleDegrees,Vector3.forward));
 public bool SetAngle(float degrees)
 {
  if(!isActiveAndEnabled||!float.IsFinite(degrees)||degrees<0||degrees>30||!pivot||!leg||!foot||!shoe||!pointer||(transform.lossyScale-Vector3.one).sqrMagnitude>1e-10f)return false;
  float t=degrees*Mathf.Deg2Rad,s=Mathf.Sin(t),c=Mathf.Cos(t);var frame=transform.rotation*frameCorrection;
  Vector3 World(Vector3 p)=>transform.position+frame*p;
  var top=new Vector3(c+.008f*s,.06f+s-.008f*c,0);float x=top.x-Mathf.Sqrt(.36f-(top.y-.016f)*(top.y-.016f));var bottom=new Vector3(x,.016f,0);
  leg.SetPositionAndRotation(World(bottom),frame*Quaternion.LookRotation(Vector3.forward,(top-bottom).normalized)*legCorrection);
  foot.position=World(new Vector3(x,.006f,0));
  var tilt=Quaternion.AngleAxis(degrees,Vector3.forward);
  shoe.SetPositionAndRotation(World(new Vector3(c+.004f*s,.06f+s-.004f*c,0)),frame*tilt*shoeCorrection);
  pointer.rotation=frame*tilt*pointerCorrection;pivot.SetPositionAndRotation(World(new Vector3(0,.06f,0)),frame*tilt);
  AngleDegrees=degrees;return true;
 }
 // shortcut: 1.2m track mounting contract only; actual track fit and P08 dynamics await the track asset.
}
