using System.Globalization;using UnityEngine;
public sealed class VlabTachometer:MonoBehaviour{
 [SerializeField]TextMesh readout;[SerializeField]Transform marker;public Transform Marker=>marker;public double Rpm{get;private set;}=double.NaN;public string DisplayText=>readout.text;
 public bool TrySetRpm(double rpm){if(!isActiveAndEnabled||!double.IsFinite(rpm)||System.Math.Abs(rpm)>999999||(transform.lossyScale-Vector3.one).sqrMagnitude>1e-10f){Clear();return false;}Rpm=rpm;readout.text=rpm.ToString("+0.0;-0.0;0.0",CultureInfo.InvariantCulture)+" rpm";return true;}
 public void Clear(){Rpm=double.NaN;if(readout)readout.text="";}
 void OnDisable()=>Clear();
 // shortcut: caller-supplied signed RPM; counting pulses, optical detection and hardware calibration belong to the assembled lesson.
}
