using System;
using UnityEngine;
public sealed class VlabMicrometer : MonoBehaviour
{
 [SerializeField] Transform spindle,thimble,ratchet,fixedFace,movingFace;
 [SerializeField] Vector3 localAxis,spindleRest,thimbleRest;
 [SerializeField] Quaternion spindleRotation,thimbleRotation,ratchetRotation;
 public Vector3 Axis=>transform.TransformDirection(localAxis).normalized;
 public Transform FixedFace=>fixedFace;
 public Transform MovingFace=>movingFace;
 public bool RatchetEngaged{get;private set;}
 bool Ready=>isActiveAndEnabled&&(transform.lossyScale-Vector3.one).sqrMagnitude<1e-10f;
 public double OpeningMm=>Vector3.Dot(movingFace.position-fixedFace.position,Axis)*1000d;
 public double ReadingMm=>Ready?Math.Round(OpeningMm,2,MidpointRounding.AwayFromZero):double.NaN;
 public double SleeveMm=>Ready?Math.Floor(ReadingMm*2+1e-7)*.5:double.NaN;
 public int ThimbleDivision=>Ready?(int)Math.Round((ReadingMm-SleeveMm)*100):-1;
 public bool TrySetOpening(double mm){if(!Ready||!double.IsFinite(mm)||mm<0||mm>25)return false;// FBX changes handedness, so positive Blender screw turns rotate negatively in Unity.
  float shift=(float)(mm*.001),angle=-(float)((mm/.5%1)*360);spindle.localPosition=spindleRest+localAxis*shift;thimble.localPosition=thimbleRest+localAxis*shift;spindle.localRotation=Quaternion.AngleAxis(angle,localAxis)*spindleRotation;thimble.localRotation=Quaternion.AngleAxis(angle,localAxis)*thimbleRotation;return true;}
 public bool TrySetRatchet(bool engaged){if(!Ready)return false;var axis=ratchet.parent.InverseTransformDirection(Axis);ratchet.localRotation=Quaternion.AngleAxis(engaged?12:0,axis)*ratchetRotation;RatchetEngaged=engaged;return true;}
 public bool TryMeasureOutside(Collider sample){if(!Ready||sample==null||!sample.enabled||!sample.gameObject.activeInHierarchy||sample.isTrigger||sample.transform.IsChildOf(transform))return false;Physics.SyncTransforms();var reference=fixedFace.position;var axis=Axis;if(!sample.Raycast(new Ray(reference-axis*.001f,axis),out var left,.027f)||!sample.Raycast(new Ray(reference+axis*.026f,-axis),out var right,.027f)||Vector3.Distance(left.point,reference)>.00001f)return false;double mm=Vector3.Dot(right.point-reference,axis)*1000d;return mm>0&&TrySetOpening(mm);}
 // shortcut: ideal aligned contact and screw pitch; ratchet state is visual, not calibrated contact force or certified measurement uncertainty.
}

