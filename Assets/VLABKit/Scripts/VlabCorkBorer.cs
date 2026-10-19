using UnityEngine;
public sealed class VlabCorkBorer : MonoBehaviour
{
 [SerializeField] Transform ejector,ejectorTip,cuttingTip;
 [SerializeField] Vector3 ejectorRest,tipRest,localAxis;
 public float StrokeM{get;private set;}
 public Transform Ejector=>ejector;
 public Transform EjectorTip=>ejectorTip;
 public Transform CuttingTip=>cuttingTip;
 public Vector3 Axis=>ejector.parent.TransformDirection(localAxis).normalized;
 public bool SetStroke(float metres)
 {
  if(!isActiveAndEnabled||!float.IsFinite(metres)||metres<0||metres>.035f||(transform.lossyScale-Vector3.one).sqrMagnitude>1e-10f)return false;
  ejector.localPosition=ejectorRest-localAxis*metres;ejectorTip.localPosition=tipRest-localAxis*metres;StrokeM=metres;return true;
 }
 void Start()=>SetStroke(0);
 // shortcut: prescribed plunger motion; sample cutting and transfer await the potato_sample and preparation assembly.
}
