using UnityEngine;
public sealed class VlabSaltBridge : MonoBehaviour
{
 [SerializeField] MeshFilter plugL,plugR;
 [SerializeField] Renderer electrolyte;
 Collider left,right;
 public string ElectrolyteId{get;private set;}
 public double Concentration{get;private set;}=double.NaN;
 public string Unit{get;private set;}
 public bool Loaded{get;private set;}
 bool Ready=>isActiveAndEnabled&&(transform.lossyScale-Vector3.one).sqrMagnitude<1e-10f;
 static bool Region(Collider r)=>r&&r.enabled&&r.gameObject.activeInHierarchy&&r.isTrigger&&(r is BoxCollider||r is SphereCollider||r is CapsuleCollider||r is MeshCollider m&&m.convex);
 bool Inside(MeshFilter plug,Collider region){if(!Region(region)||!plug.gameObject.activeInHierarchy)return false;var cup=region.GetComponentInParent<VlabBeaker>();if(!cup||!cup.isActiveAndEnabled||(cup.transform.lossyScale-Vector3.one).sqrMagnitude>1e-10f)return false;foreach(var v in plug.sharedMesh.vertices){var p=plug.transform.TransformPoint(v);if(!float.IsFinite(p.sqrMagnitude)||(region.ClosestPoint(p)-p).sqrMagnitude>1e-12f)return false;}return true;}
 public bool BothEndsImmersed{get{if(!Ready||!Loaded||ElectrolyteId==null)return false;Physics.SyncTransforms();return Inside(plugL,left)&&Inside(plugR,right);}}
 public bool TryConfigure(string electrolyteId,double concentration,string unit){if(!Ready||string.IsNullOrWhiteSpace(electrolyteId)||electrolyteId.Length>64||string.IsNullOrWhiteSpace(unit)||unit.Length>24||!double.IsFinite(concentration)||concentration<=0)return false;foreach(char c in electrolyteId+unit)if(char.IsControl(c))return false;ElectrolyteId=electrolyteId.Trim().Normalize();Concentration=concentration;Unit=unit.Trim().Normalize();Loaded=false;electrolyte.enabled=false;return true;}
 public bool TrySetLoaded(bool loaded){if(!Ready||loaded&&ElectrolyteId==null)return false;Loaded=loaded;electrolyte.enabled=loaded;return true;}
 public bool TryAssignRegions(Collider a,Collider b){if(!Ready||!Region(a)||!Region(b)||a==b)return false;var cupA=a.GetComponentInParent<VlabBeaker>();var cupB=b.GetComponentInParent<VlabBeaker>();if(!cupA||!cupB||cupA==cupB||!cupA.isActiveAndEnabled||!cupB.isActiveAndEnabled)return false;left=a;right=b;return true;}
 void OnDisable(){if(electrolyte)electrolyte.enabled=false;}
 void OnEnable(){if(electrolyte)electrolyte.enabled=Loaded;}
 // shortcut: configured/loaded fullywet contact condition only; caller owns true liquid regions, placement and ionic chemistry, never an electron wire.
}
