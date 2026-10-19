using UnityEngine;
public sealed class VlabPestle : MonoBehaviour
{
 [SerializeField] Vector3 localBottom;
 [SerializeField] Quaternion bodyRotation;
 [SerializeField] SphereCollider head;
 bool Ready=>isActiveAndEnabled&&(transform.lossyScale-Vector3.one).sqrMagnitude<1e-10f;
 public bool TryPlaceOn(VlabCuttingBoard floor,Vector2 offset,float liftMeters){if(!Ready||!floor||!float.IsFinite(liftMeters)||liftMeters<0||liftMeters>.020f||!floor.TryGetPlacement(offset,Vector2.one*.0201f,out var pose))return false;var rotation=pose.rotation*bodyRotation;transform.SetPositionAndRotation(pose.position+pose.rotation*Vector3.up*liftMeters-rotation*localBottom,rotation);return true;}
 public bool TouchesSample(Collider sample){if(!Ready||sample==null||!sample.enabled||!sample.gameObject.activeInHierarchy||sample.transform.IsChildOf(transform))return false;Physics.SyncTransforms();return Physics.ComputePenetration(head,head.transform.position,head.transform.rotation,sample,sample.transform.position,sample.transform.rotation,out _,out float depth)&&depth>0;}
 // shortcut: upright guided pose and head-overlap signal only; pressure, freehand tilt, deformation and extraction are separate lesson logic.
}
