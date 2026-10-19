using UnityEngine;
public sealed class VlabCoverslip : MonoBehaviour
{
 [SerializeField] Transform loweringEdge;
 [SerializeField] Vector3 localEdge,edgeInParent,localHinge;
 [SerializeField] Quaternion restRotation;
 public Transform LoweringEdge=>loweringEdge;
 public float AngleDegrees{get;private set;}
 public bool SetAngle(float degrees)
 {
  if(!isActiveAndEnabled||!float.IsFinite(degrees)||degrees<0||degrees>60||(transform.lossyScale-Vector3.one).sqrMagnitude>1e-10f)return false;
  var rotation=restRotation*Quaternion.AngleAxis(degrees,localHinge);transform.localRotation=rotation;transform.localPosition=edgeInParent-rotation*localEdge;AngleDegrees=degrees;return true;
 }
 void Start()=>SetAngle(0);
 // shortcut: rigid lowering about a fixed edge; specimen mounting, liquid films and trapped bubbles await the slide assembly.
}
