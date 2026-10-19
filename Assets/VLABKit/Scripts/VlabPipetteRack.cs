using System.Linq;
using UnityEngine;
public sealed class VlabPipetteRack : MonoBehaviour
{
 [SerializeField] Transform[] tips,tops;
 readonly VlabFlask[] stored=new VlabFlask[3];
 bool Ready=>isActiveAndEnabled&&(transform.lossyScale-Vector3.one).sqrMagnitude<1e-10f;
 public VlabFlask Stored(int slot){if(slot<0||slot>=3)return null;var p=stored[slot];if(!p||!p.isActiveAndEnabled||p.transform.parent!=transform||p.VolumeMl!=0)return null;return p;}
 public bool TryStore(VlabFlask pipette,int slot){if(!Ready||!pipette||!pipette.isActiveAndEnabled||slot<0||slot>=3||Stored(slot)||pipette.CapacityMl!=10||pipette.VolumeMl!=0||pipette.transform.IsChildOf(transform)||transform.IsChildOf(pipette.transform)||(pipette.transform.lossyScale-Vector3.one).sqrMagnitude>1e-10f||pipette.GetComponentsInChildren<VlabPipetteBulb>().Any(c=>c.isActiveAndEnabled))return false;var refs=pipette.GetComponentsInChildren<Transform>(true);var tip=refs.SingleOrDefault(t=>t.name=="VLAB_pipette_TipOutlet_SOCKET");var top=refs.SingleOrDefault(t=>t.name=="VLAB_pipette_BulbConnection_SOCKET");if(!tip||!top||Mathf.Abs(Vector3.Distance(tip.position,top.position)-Vector3.Distance(tips[slot].position,tops[slot].position))>1e-6f)return false;pipette.transform.rotation=Quaternion.FromToRotation((top.position-tip.position).normalized,(tops[slot].position-tips[slot].position).normalized)*pipette.transform.rotation;pipette.transform.position+=tips[slot].position-tip.position;pipette.transform.SetParent(transform,true);stored[slot]=pipette;return true;}
 public bool TryTake(int slot,out VlabFlask pipette){pipette=null;if(!Ready||!Stored(slot))return false;pipette=stored[slot];pipette.transform.SetParent(null,true);stored[slot]=null;return true;}
 // shortcut: bare empty accepted280mm pipettes only; different glass families and filled tilted storage require new fit/fluid validation.
}
