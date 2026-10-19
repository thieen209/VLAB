using UnityEngine;
public sealed class VlabSlider : MonoBehaviour
{
 [SerializeField] Transform travelFrame;
 public const float LimitM=.0794f;
 public float TravelM{get;private set;}
 public Transform TravelFrame=>travelFrame;
 bool Ready=>isActiveAndEnabled&&travelFrame&&travelFrame.gameObject.activeInHierarchy&&(transform.lossyScale-Vector3.one).sqrMagnitude<1e-10f&&(travelFrame.lossyScale-Vector3.one).sqrMagnitude<1e-10f;
 public bool TryBind(Transform frame){if(!isActiveAndEnabled||!frame||!frame.gameObject.activeInHierarchy||frame==transform||frame.IsChildOf(transform)||(frame.lossyScale-Vector3.one).sqrMagnitude>1e-10f||(transform.lossyScale-Vector3.one).sqrMagnitude>1e-10f)return false;travelFrame=frame;transform.SetParent(frame,false);TravelM=0;transform.localPosition=Vector3.zero;transform.localRotation=Quaternion.identity;return true;}
 public bool TrySetTravel(float metres){if(!Ready||!float.IsFinite(metres)||metres< -LimitM||metres>LimitM)return false;transform.localPosition=Vector3.right*metres;transform.localRotation=Quaternion.identity;TravelM=metres;return true;}
 // shortcut: prescribed +-79.4mm travel in the caller's matching200mm rail frame; no gravity, friction, linkage forces or motor binding.
}
