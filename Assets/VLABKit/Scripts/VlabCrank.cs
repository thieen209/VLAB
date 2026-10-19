using UnityEngine;
public sealed class VlabCrank : MonoBehaviour
{
 [SerializeField] Transform pin,rodPin,shaft;
 [SerializeField] Vector3 pinBase,rodBase,radiusAxis,spinAxis;
 [SerializeField] Quaternion restRotation;
 public int RadiusMm{get;private set;}=30;
 public double AngleDegrees{get;private set;}
 public Transform Pin=>pin;
 public Transform RodPin=>rodPin;
 public Transform Shaft=>shaft;
 public Vector3 Axis=>transform.TransformDirection(spinAxis).normalized;
 public bool Configure(int radiusMm,double angle)
 {
  if(!isActiveAndEnabled||(radiusMm!=20&&radiusMm!=30&&radiusMm!=40)||double.IsNaN(angle)||double.IsInfinity(angle)||(transform.lossyScale-Vector3.one).sqrMagnitude>1e-10f)return false;
  double wrapped=(angle%360+360)%360;pin.localPosition=pinBase+radiusAxis*(radiusMm*.001f);rodPin.localPosition=rodBase+radiusAxis*(radiusMm*.001f);transform.localRotation=restRotation*Quaternion.AngleAxis((float)wrapped,spinAxis);RadiusMm=radiusMm;AngleDegrees=wrapped;return true;
 }
 void Start()=>Configure(30,0);
 // shortcut: indexed rigid rotation only; drive dynamics and slider constraints belong to the assembled M05 mechanism.
}
