using System.Globalization;using UnityEngine;
public sealed class VlabThermometer:MonoBehaviour{
 [SerializeField]Vector3[] sensorPoints;[SerializeField]TextMesh readout;Collider liquid;float temperature;bool valid;public float ReadingC=>valid&&Immersed(liquid)?temperature:float.NaN;public TextMesh Readout=>readout;
 public bool Immersed(Collider region){if(!isActiveAndEnabled||(transform.lossyScale-Vector3.one).sqrMagnitude>1e-10f||!region||!region.enabled||!region.gameObject.activeInHierarchy||!region.isTrigger||region.transform.IsChildOf(transform)||!(region is BoxCollider||region is SphereCollider||region is CapsuleCollider||region is MeshCollider m&&m.convex))return false;Physics.SyncTransforms();foreach(var p in sensorPoints)if((region.ClosestPoint(transform.TransformPoint(p))-transform.TransformPoint(p)).sqrMagnitude>1e-12f)return false;return true;}
 public bool TryObserve(Collider region,float suppliedC){if(!float.IsFinite(suppliedC)||suppliedC<0||suppliedC>100||!Immersed(region)){Clear();return false;}liquid=region;temperature=suppliedC;valid=true;readout.text=suppliedC.ToString("0.0",CultureInfo.InvariantCulture)+" C";return true;}
 public void Clear(){valid=false;liquid=null;if(readout)readout.text="";}
 void LateUpdate(){if(valid&&float.IsNaN(ReadingC))Clear();}void OnDisable()=>Clear();
 // shortcut: explicit supplied sample temperature and actual sensor-vertex containment; response time and calibration remain unconfigured until lesson data is supplied.
}
