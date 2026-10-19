using UnityEngine;
public sealed class VlabPencil : MonoBehaviour
{
 [SerializeField] Transform tip,direction;
 public Transform Tip=>tip;
 public Vector3 WritingDirection=>(tip.position-direction.position).normalized;
 public VlabChromatography LastMarkedPaper{get;private set;}
 public Vector3 LastMarkPoint{get;private set;}
 bool Ready=>isActiveAndEnabled&&(transform.lossyScale-Vector3.one).sqrMagnitude<1e-10f;
 public bool TryContact(Collider paper,out Vector3 point){point=default;if(!Ready||paper==null||!paper.enabled||!paper.gameObject.activeInHierarchy||paper.isTrigger||paper.transform.IsChildOf(transform))return false;var axis=WritingDirection;if(!paper.Raycast(new Ray(tip.position-axis*.0005f,axis),out var hit,.0015f)||Vector3.Distance(hit.point,tip.position)>.001f||Vector3.Dot(hit.normal,axis)>-.2f)return false;point=hit.point;return true;}
 public bool TryMarkOrigin(VlabChromatography paper,Collider surface){if(paper==null||!paper.isActiveAndEnabled||!surface||!surface.transform.IsChildOf(paper.transform)||!TryContact(surface,out var point))return false;var line=paper.Origin.GetComponent<MeshFilter>();if(!line||Mathf.Abs(Vector3.Dot(point-paper.Origin.position,paper.Axis))>.0003f)return false;var local=paper.Origin.InverseTransformPoint(point);if(local.x<line.sharedMesh.bounds.min.x||local.x>line.sharedMesh.bounds.max.x)return false;paper.Origin.GetComponent<Renderer>().enabled=true;LastMarkedPaper=paper;LastMarkPoint=point;return true;}
 // shortcut: validates physical contact on the existing origin-line region; arbitrary drawing, solvent front, Rf and chemistry remain separate lesson data.
}
