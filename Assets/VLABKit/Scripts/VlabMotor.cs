using UnityEngine;
public sealed class VlabMotor : MonoBehaviour
{
 [SerializeField] Transform shaft,shaftTip,rotationAxis;
 [SerializeField] Vector3 localAxis;
 [SerializeField] Quaternion shaftRest;
 public Transform Shaft=>shaft;
 public Transform Tip=>shaftTip;
 public Vector3 Origin=>rotationAxis.position;
 public Vector3 Axis=>transform.TransformDirection(localAxis).normalized;
 public double Rpm{get;private set;}
 public double AngleDegrees{get;private set;}
 public bool Running{get;private set;}
 bool Ready=>isActiveAndEnabled&&(transform.lossyScale-Vector3.one).sqrMagnitude<1e-10f;
 public bool CanEditTransmission=>Ready&&!Running;
 public bool TrySetRpm(double rpm){if(!CanEditTransmission||!double.IsFinite(rpm)||System.Math.Abs(rpm)>120)return false;Rpm=rpm;return true;}
 public bool TrySetAngle(double angle){if(!CanEditTransmission||!double.IsFinite(angle))return false;Apply(angle);return true;}
 public bool TryStart(){if(!Ready||Rpm==0)return false;Running=true;return true;}
 public void Stop()=>Running=false;
 public bool Advance(double seconds){if(!Ready||!Running||!double.IsFinite(seconds)||seconds<0||seconds>3600)return false;double next=AngleDegrees+Rpm*6*seconds;if(!double.IsFinite(next))return false;Apply(next);return true;}
 void Apply(double angle){shaft.localRotation=Quaternion.AngleAxis((float)(angle%360),localAxis)*shaftRest;AngleDegrees=angle;}
 void OnDisable()=>Stop();
 // shortcut: prescribedRPM/angle only; caller ticks once and gates all transmission edits, with no electrical/torque/load solver.
}
