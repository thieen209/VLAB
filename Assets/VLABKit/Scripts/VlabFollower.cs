using UnityEngine;
public sealed class VlabFollower : MonoBehaviour
{
 [SerializeField] float rollerRadius;
 [SerializeField] Transform roller;
 [SerializeField] Vector3 localStem,localShaft;
 [SerializeField] Quaternion rollerRest;
 public bool IsRoller=>rollerRadius>0;
 public Vector3 ContactPoint{get;private set;}
 public bool Follow(VlabCam cam)
 {
  if(!isActiveAndEnabled||cam==null||!cam.isActiveAndEnabled||(transform.lossyScale-Vector3.one).sqrMagnitude>1e-10f||(cam.transform.lossyScale-Vector3.one).sqrMagnitude>1e-10f||cam.Body.localScale!=Vector3.one)return false;
  var collider=cam.Body.GetComponent<MeshCollider>();if(collider==null||!collider.enabled)return false;var up=cam.FollowerDirection;var shaft=cam.ShaftAxis;var origin=cam.Center+up*.1f;Physics.SyncTransforms();RaycastHit contact=default;bool found=false;
  if(IsRoller){float top=float.NegativeInfinity;Vector2 touch=default;Vector2 Point(int i){float theta=(i*2-(float)cam.AngleDegrees)*Mathf.Deg2Rad;float r=(float)cam.RadiusAt(i*2);return new Vector2(-r*Mathf.Sin(theta),r*Mathf.Cos(theta));}void Candidate(float height,Vector2 p){if(height>top){top=height;touch=p;}}
   for(int i=0;i<180;i++){var a=Point(i);var b=Point((i+1)%180);if(Mathf.Abs(a.x)<=rollerRadius)Candidate(a.y+Mathf.Sqrt(Mathf.Max(0,rollerRadius*rollerRadius-a.x*a.x)),a);var edge=b-a;var normal=new Vector2(-edge.y,edge.x).normalized;if(normal.y<0)normal=-normal;if(Mathf.Abs(edge.x)<1e-9f)continue;float t=-(a.x+rollerRadius*normal.x)/edge.x;if(t>=0&&t<=1){var p=a+t*edge;Candidate(p.y+rollerRadius*normal.y,p);}}
   if(!float.IsFinite(top))return false;contact.point=cam.Center+Vector3.Cross(up,shaft)*touch.x+up*touch.y;contact.distance=.1f-top;found=true;
  }
  else {contact.point=cam.ContactPoint;found=true;}
  if(!found)return false;
  var position=IsRoller?origin-up*contact.distance:contact.point;var rotation=Quaternion.LookRotation(shaft,up)*Quaternion.Inverse(Quaternion.LookRotation(localShaft,localStem));transform.SetPositionAndRotation(position,rotation);ContactPoint=contact.point;return true;
 }
 public bool SetRollerAngle(float degrees){if(!isActiveAndEnabled||roller==null||!float.IsFinite(degrees)||(transform.lossyScale-Vector3.one).sqrMagnitude>1e-10f)return false;roller.localRotation=Quaternion.AngleAxis(degrees%360,localShaft)*rollerRest;return true;}
 // shortcut: geometric contact only; guide forces, springs, loss of contact and no-slip roller travel require M06 dynamics.
}
