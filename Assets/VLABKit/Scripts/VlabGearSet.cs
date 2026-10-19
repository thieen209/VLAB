using UnityEngine;
public sealed class VlabGearSet : MonoBehaviour
{
 [SerializeField] Transform gear12,gear24,gear36;
 [SerializeField] Quaternion rest12,rest24,rest36;
 [SerializeField] Vector3 axis12,axis24,axis36;
 public double AngleDegrees{get;private set;}
 public bool SetDriverAngle(double degrees)
 {
  if(!isActiveAndEnabled||!double.IsFinite(degrees)||!gear12||!gear24||!gear36||(transform.lossyScale-Vector3.one).sqrMagnitude>1e-10f)return false;
  // Full train repeats after six driver revolutions; wrapping at 360 would jump the driven gears.
  double angle=degrees%2160;
  gear12.localRotation=rest12*Quaternion.AngleAxis((float)angle,axis12);
  gear24.localRotation=rest24*Quaternion.AngleAxis((float)(-angle/2),axis24);
  gear36.localRotation=rest36*Quaternion.AngleAxis((float)(angle/3),axis36);
  AngleDegrees=degrees;return true;
 }
 // shortcut: fixed 12-24-36 kinematic train; torque, friction and interactive lesson assembly are separate.
}
