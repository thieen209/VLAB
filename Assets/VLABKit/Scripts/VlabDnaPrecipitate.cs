using UnityEngine;
public sealed class VlabDnaPrecipitate : MonoBehaviour
{
 [SerializeField] Renderer strands;
 [SerializeField] Quaternion restRotation;
 public bool Present=>strands.enabled;
 public bool SetPresent(bool present){if(!isActiveAndEnabled)return false;strands.enabled=present;return true;}
 public bool AttachTo(Transform anchor)
 {
  if(!isActiveAndEnabled||!anchor||!anchor.gameObject.activeInHierarchy||anchor.IsChildOf(transform)||(anchor.lossyScale-Vector3.one).sqrMagnitude>1e-10f||(transform.lossyScale-Vector3.one).sqrMagnitude>1e-10f)return false;
  var p=anchor.position;var q=anchor.rotation;
  if(!float.IsFinite(p.x+p.y+p.z)||!float.IsFinite(q.x+q.y+q.z+q.w))return false;
  transform.SetParent(anchor,false);transform.localPosition=Vector3.zero;transform.localRotation=restRotation;transform.localScale=Vector3.one;return true;
 }
 // shortcut: a rigid visual proxy; extraction chemistry and flexible rod spooling require the B07 lesson assembly.
}
