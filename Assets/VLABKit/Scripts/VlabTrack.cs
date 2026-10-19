using System.Linq;using UnityEngine;
public sealed class VlabTrack:MonoBehaviour{
 [SerializeField]Transform inletTip,inletBase,railBottom,cartFrame;[SerializeField]GameObject feet;[SerializeField]Quaternion rootRest;VlabAirHose hose;Transform hoseTip,hoseBase;
 public Transform CartFrame=>cartFrame;public Vector3 InletTip=>inletTip.position;public Vector3 InletAxis=>(inletTip.position-inletBase.position).normalized;bool Ready=>isActiveAndEnabled&&(transform.lossyScale-Vector3.one).sqrMagnitude<1e-10f;
 bool BMatches=>Ready&&hose&&hose.isActiveAndEnabled&&(hose.transform.lossyScale-Vector3.one).sqrMagnitude<1e-10f&&hoseTip&&hoseBase&&Vector3.Distance(hoseTip.position,InletTip-InletAxis*.012f)<.0001f&&Vector3.Dot((hoseTip.position-hoseBase.position).normalized,-InletAxis)>.99999f;
 public bool AirEnabled=>BMatches&&hose.HasSupplyAtA;
 public bool TryBindHose(VlabAirHose value){if(!Ready||!value||!value.isActiveAndEnabled)return false;var ts=value.GetComponentsInChildren<Transform>();var tip=ts.SingleOrDefault(t=>t.name=="VLAB_air_hose_ConnectorB_SOCKET");var body=ts.SingleOrDefault(t=>t.name=="VLAB_air_hose_ConnectorB_VIS");if(!tip||!body)return false;var old=hose;var oldTip=hoseTip;var oldBase=hoseBase;hose=value;hoseTip=tip;hoseBase=body;if(BMatches)return true;hose=old;hoseTip=oldTip;hoseBase=oldBase;return false;}
 public void DisconnectHose(){hose=null;hoseTip=hoseBase=null;}
 public bool TryMountIncline(VlabInclineBase support){if(!Ready||!support||!support.isActiveAndEnabled||(support.transform.lossyScale-Vector3.one).sqrMagnitude>1e-10f)return false;var p=support.RailPose;feet.SetActive(false);transform.rotation=p.rotation*rootRest;transform.position+=p.position-railBottom.position;return true;}
 // shortcut: both-end geometry/power interlock and prescribed incline pose only; no CFD, pressure seal, automatic clock or coupled lesson dynamics.
}
