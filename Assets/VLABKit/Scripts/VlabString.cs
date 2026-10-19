using UnityEngine;
public sealed class VlabString:MonoBehaviour{
 [SerializeField]Transform anchorTop,anchorBottom;[SerializeField]LineRenderer line;public float EndpointSpanM{get;private set;}
 public Vector3 Top=>anchorTop.position;public Vector3 Bottom=>anchorBottom.position;
 void Awake(){TrySetEndpoints(Top,Bottom);}
 public bool TrySetEndpoints(Vector3 a,Vector3 b){float d=Vector3.Distance(a,b);if(!isActiveAndEnabled||!float.IsFinite(a.sqrMagnitude)||!float.IsFinite(b.sqrMagnitude)||d<.009999f||d>2.000001f||(transform.lossyScale-Vector3.one).sqrMagnitude>1e-10f)return false;var rotation=Quaternion.FromToRotation(transform.TransformDirection(Vector3.forward),(b-a).normalized)*transform.rotation;transform.SetPositionAndRotation(a,rotation);anchorTop.position=a;anchorBottom.position=b;line.SetPosition(0,transform.InverseTransformPoint(a));line.SetPosition(1,transform.InverseTransformPoint(b));EndpointSpanM=d;return true;}
 // shortcut: explicit straight taut endpoints and 2mm visual cord, no tension, slack, pulley wrapping or force solver; integrate those in the assembled lesson.
}
