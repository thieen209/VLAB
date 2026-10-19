using UnityEngine;
public sealed class VlabGasSyringe : MonoBehaviour
{
 [SerializeField] float capacityMl=100,boreRadiusM=.016f;
 [SerializeField] Transform plunger,seal,face;
 [SerializeField] Vector3 plungerZero,sealZero,faceZero,localAxis;
 public float CapacityMl=>capacityMl;
 public float VolumeMl{get;private set;}
 public Vector3 PistonFace=>face.position;
 public bool SetVolume(float ml)
 {
  if(!isActiveAndEnabled||!float.IsFinite(ml)||ml<0||ml>capacityMl||!float.IsFinite(boreRadiusM)||boreRadiusM<=0||!plunger||!seal||!face||(transform.lossyScale-Vector3.one).sqrMagnitude>1e-10f)return false;
  var shift=localAxis*(ml*1e-6f/(Mathf.PI*boreRadiusM*boreRadiusM));
  plunger.localPosition=plungerZero+shift;seal.localPosition=sealZero+shift;face.localPosition=faceZero+shift;VolumeMl=ml;return true;
 }
 // shortcut: prescribed gas displacement excludes inlet deadspace; pressure, leaks and lesson kinetics require integration.
}
