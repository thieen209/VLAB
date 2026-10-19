using System;
using UnityEngine;
public sealed class VlabWashBottle:MonoBehaviour
{
 [SerializeField] Transform cap,seat,park,jet,direction;
 [SerializeField] VlabLiquidStream stream;
 public double RemainingMl{get;private set;}=250;
 public bool CapOpen{get;private set;}
 public Vector3 Outlet=>jet.position;
 public Vector3 OutletDirection=>(direction.position-jet.position).normalized;
 public VlabLiquidStream Stream=>stream;
 bool Ready=>isActiveAndEnabled&&(transform.lossyScale-Vector3.one).sqrMagnitude<1e-10f;
 public bool TrySetCapOpen(bool open){if(!Ready)return false;StopFlow();cap.position=(open?park:seat).position;CapOpen=open;return true;}
 public bool TrySetWaterAmount(double ml){if(!Ready||stream.Flowing||!double.IsFinite(ml)||ml<0||ml>250)return false;RemainingMl=ml;return true;}
 public double TryDispenseTo(VlabBeaker receiver,double requestedMl){
  if(!Ready||CapOpen||receiver==null||!receiver.isActiveAndEnabled||!double.IsFinite(requestedMl)||requestedMl<=0||(receiver.transform.lossyScale-Vector3.one).sqrMagnitude>1e-10f||Vector3.Dot(receiver.transform.TransformDirection(Vector3.back),Vector3.up)<.99999f)return 0;
  double amount=Math.Min(requestedMl,Math.Min(RemainingMl,receiver.CapacityMl-receiver.VolumeMl));if(amount<=0)return 0;
  var target=receiver.transform.position+Vector3.up*(float)(receiver.SurfaceHeightMeters+.002);var delta=target-Outlet;
  if(Vector3.Dot(delta.normalized,OutletDirection)<.98f)return 0;
  Physics.SyncTransforms();foreach(var wall in receiver.GetComponentsInChildren<MeshCollider>())if(wall.enabled&&!wall.isTrigger&&wall.Raycast(new Ray(Outlet,delta.normalized),out _,delta.magnitude))return 0;
  if(!stream.TrySetEndpoints(Outlet,target,.0015f))return 0;
  double moved=receiver.Add(amount);RemainingMl-=moved;return moved;
 }
 public double TryDispenseTo(VlabFlask receiver,double requestedMl)
 {
  if(!Ready||CapOpen||!receiver||!double.IsFinite(requestedMl)||requestedMl<=0)return 0;
  var target=receiver.CavityCentreAt(receiver.SurfaceHeightMeters+.001);
  if(!receiver.ContainsPoint(target)||Vector3.Dot((target-Outlet).normalized,OutletDirection)<.98f)return 0;
  double moved=Math.Min(requestedMl,Math.Min(RemainingMl,receiver.CapacityMl-receiver.VolumeMl));
  if(moved<=0||!stream.TrySetEndpoints(Outlet,target,.0015f)||!receiver.TrySetVolume(receiver.VolumeMl+moved))return 0;
  RemainingMl-=moved;return moved;
 }
 public void StopFlow()=>stream.Hide();
 void OnDisable(){if(stream!=null)StopFlow();}
 // shortcut: explicit water bookkeeping to an upright beaker and a straight illustrative jet; no squeeze pressure, timed flow or calibration model.
}
