using System;
using System.Linq;
using UnityEngine;
public sealed class VlabPipetteBulb : MonoBehaviour
{
 [SerializeField] Transform entry,stop;
 VlabFlask pipette;Transform outlet,connection;
 public int Control{get;private set;}
 public bool Attached=>pipette&&pipette.isActiveAndEnabled&&transform.parent==pipette.transform&&(stop.position-connection.position).sqrMagnitude<1e-10f;
 bool Ready=>isActiveAndEnabled&&Attached&&(transform.lossyScale-Vector3.one).sqrMagnitude<1e-10f&&(pipette.transform.lossyScale-Vector3.one).sqrMagnitude<1e-10f;
 public bool TryAttach(VlabFlask tool){if(!isActiveAndEnabled||!tool||tool.transform.IsChildOf(transform)||!tool.isActiveAndEnabled||tool.CapacityMl!=10||(transform.lossyScale-Vector3.one).sqrMagnitude>1e-10f||(tool.transform.lossyScale-Vector3.one).sqrMagnitude>1e-10f)return false;var ts=tool.GetComponentsInChildren<Transform>(true);var top=ts.SingleOrDefault(t=>t.name=="VLAB_pipette_BulbConnection_SOCKET");var tip=ts.SingleOrDefault(t=>t.name=="VLAB_pipette_TipOutlet_SOCKET");if(!top||!tip)return false;transform.rotation=Quaternion.FromToRotation((stop.position-entry.position).normalized,(top.position-tip.position).normalized)*transform.rotation;transform.position+=top.position-stop.position;transform.SetParent(tool.transform,true);pipette=tool;outlet=tip;connection=top;Control=0;return true;}
 public bool TrySetControl(int value){if(!Ready||value<0||value>3)return false;Control=value;return true;}
 bool AtRegion(VlabBeaker cup,Collider region){if(!Ready||!cup||!cup.isActiveAndEnabled||(cup.transform.lossyScale-Vector3.one).sqrMagnitude>1e-10f||!region||!region.enabled||!region.gameObject.activeInHierarchy||!region.isTrigger||!region.transform.IsChildOf(cup.transform)||!(region is MeshCollider mesh&&mesh.convex||region is BoxCollider||region is SphereCollider||region is CapsuleCollider))return false;Physics.SyncTransforms();var axis=(connection.position-outlet.position).normalized;var right=pipette.transform.right;var front=Vector3.Cross(axis,right).normalized;for(int i=0;i<8;i++){float a=i*Mathf.PI/4;var p=outlet.position+.0011f*(right*Mathf.Cos(a)+front*Mathf.Sin(a));if((region.ClosestPoint(p)-p).sqrMagnitude>1e-12f)return false;}return true;}
 public bool TryAspirate(VlabBeaker source,Collider trueLiquidRegion,double requestedMl,out double movedMl){movedMl=0;if(Control!=2||!double.IsFinite(requestedMl)||requestedMl<=0||!AtRegion(source,trueLiquidRegion)||source.VolumeMl<=0)return false;double moved=Math.Min(requestedMl,Math.Min(source.VolumeMl,10-pipette.VolumeMl));if(!pipette.TrySetVolume(pipette.VolumeMl+moved))return false;source.Remove(moved);movedMl=moved;return true;}
 public bool TryDispense(VlabBeaker receiver,Collider receivingCavity,double requestedMl,out double movedMl){movedMl=0;if(Control!=3||!double.IsFinite(requestedMl)||requestedMl<=0||!AtRegion(receiver,receivingCavity))return false;double moved=Math.Min(requestedMl,Math.Min(pipette.VolumeMl,receiver.CapacityMl-receiver.VolumeMl));if(!pipette.TrySetVolume(pipette.VolumeMl-moved))return false;receiver.Add(moved);movedMl=moved;return true;}
 void OnDisable()=>Control=0;
 // Delivered flask receivers need no hidden beaker proxy. Validate the actual
 // outlet circumference against the flask's authored interior profile.
 bool AtFlask(VlabFlask flask,bool submerged)
 {
  if(!Ready||!flask)return false;
  var axis=(connection.position-outlet.position).normalized;
  var side=Vector3.Cross(axis,pipette.transform.right).normalized;
  for(int i=0;i<8;i++){float a=i*Mathf.PI/4;var p=outlet.position+.0011f*(pipette.transform.right*Mathf.Cos(a)+side*Mathf.Sin(a));if(!flask.ContainsPoint(p,submerged))return false;}
  return true;
 }
 public bool TryAspirate(VlabFlask source,double requestedMl,out double movedMl)
 {
  movedMl=0;if(Control!=2||!double.IsFinite(requestedMl)||requestedMl<=0||!AtFlask(source,true))return false;
  double moved=System.Math.Min(requestedMl,System.Math.Min(source.VolumeMl,10-pipette.VolumeMl));
  if(moved<=0||!pipette.TrySetVolume(pipette.VolumeMl+moved))return false;
  if(!source.TrySetVolume(source.VolumeMl-moved)){pipette.TrySetVolume(pipette.VolumeMl-moved);return false;}movedMl=moved;return true;
 }
 public bool TryDispense(VlabFlask receiver,double requestedMl,out double movedMl)
 {
  movedMl=0;if(Control!=3||!double.IsFinite(requestedMl)||requestedMl<=0||!AtFlask(receiver,false))return false;
  double moved=System.Math.Min(requestedMl,System.Math.Min(pipette.VolumeMl,receiver.CapacityMl-receiver.VolumeMl));
  if(moved<=0||!receiver.TrySetVolume(receiver.VolumeMl+moved))return false;
  if(!pipette.TrySetVolume(pipette.VolumeMl-moved)){receiver.TrySetVolume(receiver.VolumeMl-moved);return false;}movedMl=moved;return true;
 }
 // shortcut: explicit virtual valve/volume actions with caller-owned true convex regions; pressure, retained films, composition and contamination belong to lesson integration.
}
