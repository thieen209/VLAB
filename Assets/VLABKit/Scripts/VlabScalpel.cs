using UnityEngine;
public sealed class VlabScalpel : MonoBehaviour
{
 [SerializeField] Transform tip,directionRef,planeNormal;
 public Vector3 Tip=>tip.position;
 public Vector3 Axis=>(tip.position-directionRef.position).normalized;
 public Vector3 PlaneNormal=>(planeNormal.position-transform.position).normalized;
 public bool TryContact(Collider allowedPlantSurface,out Vector3 contact){contact=default;if(!isActiveAndEnabled||(transform.lossyScale-Vector3.one).sqrMagnitude>1e-10f||!allowedPlantSurface||!allowedPlantSurface.enabled||!allowedPlantSurface.gameObject.activeInHierarchy||allowedPlantSurface.isTrigger||allowedPlantSurface.transform.IsChildOf(transform)||!float.IsFinite(Tip.sqrMagnitude)||!float.IsFinite(Axis.sqrMagnitude))return false;if(!allowedPlantSurface.Raycast(new Ray(Tip-Axis*.001f,Axis),out var hit,.002f)||Vector3.Distance(hit.point,Tip)>.001001f||Vector3.Dot(hit.normal,Axis)>-.2f)return false;contact=hit.point;return true;}
 // shortcut: caller-whitelisted plant-surface tip contact only; cutting states/mesh changes/forces require the preparation controller.
}
