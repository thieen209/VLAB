using UnityEngine;
public sealed class VlabVolumetricFlask : MonoBehaviour
{
 [SerializeField] VlabFlask liquid;
 [SerializeField] Transform stopper,seat;
 public bool StopperOpen{get;private set;}
 public VlabFlask Liquid=>liquid;
 bool Ready=>isActiveAndEnabled&&(transform.lossyScale-Vector3.one).sqrMagnitude<1e-10f;
 public bool TrySetStopperOpen(bool open){if(!Ready)return false;stopper.position=seat.position+(open?transform.TransformDirection(Vector3.right)*.04f:Vector3.zero);StopperOpen=open;return true;}
 public bool TrySetVolume(double ml)=>Ready&&StopperOpen&&liquid.TrySetVolume(ml);
 // shortcut: explicit rigid stopper poses and upright fill; no automatic pouring, seal or meniscus model.
}
