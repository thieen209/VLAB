using UnityEngine;
public sealed class VlabForceMeter : MonoBehaviour
{
 [SerializeField] float capacityN=5;
 [SerializeField] Transform indicator,hook,load;
 [SerializeField] Vector3 indicatorZero,hookZero,loadZero,localPull;
 public float CapacityN=>capacityN;
 public float ForceN{get;private set;}
 public Vector3 LoadPoint=>load.position;
 public bool SetForce(float newtons)
 {
  if(!isActiveAndEnabled||!float.IsFinite(newtons)||newtons<0||newtons>capacityN||capacityN<=0||(transform.lossyScale-Vector3.one).sqrMagnitude>1e-10f||!indicator||!hook||!load)return false;
  var shift=localPull*(.1f*newtons/capacityN);
  indicator.localPosition=indicatorZero+shift;hook.localPosition=hookZero+shift;load.localPosition=loadZero+shift;ForceN=newtons;return true;
 }
 // shortcut: prescribed axial force visualization; full lesson tension dynamics and calibration are separate.
}
