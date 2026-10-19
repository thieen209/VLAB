using System.Globalization;
using UnityEngine;
public sealed class VlabPhProbe : MonoBehaviour
{
 [SerializeField] MeshFilter tip;
 [SerializeField] Transform probeAssembly;
 [SerializeField] GameObject cableGuide;
 [SerializeField] TextMesh readout;
 Vector3 restPosition;Quaternion restRotation;Collider liquid;bool hasReading;float ph;
 public bool BuffersConfigured{get;private set;}
 public Vector2 BufferPh{get;private set;}
 public float BufferTemperatureC{get;private set;}
 public string SampleId{get;private set;}
 public float Reading=>hasReading&&Ready&&Immersed(liquid)?ph:float.NaN;
 public Transform ProbeAssembly=>probeAssembly;
 public MeshFilter Tip=>tip;
 public TextMesh Readout=>readout;
 bool Ready=>isActiveAndEnabled&&(transform.lossyScale-Vector3.one).sqrMagnitude<1e-10f&&(probeAssembly.lossyScale-Vector3.one).sqrMagnitude<1e-10f;
 void Awake(){restPosition=probeAssembly.localPosition;restRotation=probeAssembly.localRotation;Clear();}
 public bool TryConfigureBuffers(float low,float high,float temperatureC){if(!Ready||!float.IsFinite(low)||!float.IsFinite(high)||low<0||high>14||low>=high||!float.IsFinite(temperatureC)||temperatureC<0||temperatureC>100)return false;BufferPh=new Vector2(low,high);BufferTemperatureC=temperatureC;BuffersConfigured=true;Clear();return true;}
 public bool Immersed(Collider region){if(!Ready||!region||!region.enabled||!region.gameObject.activeInHierarchy||!region.isTrigger||region.transform.IsChildOf(transform)||!(region is BoxCollider||region is SphereCollider||region is CapsuleCollider||region is MeshCollider mesh&&mesh.convex))return false;Physics.SyncTransforms();foreach(var v in tip.sharedMesh.vertices){var p=tip.transform.TransformPoint(v);if((region.ClosestPoint(p)-p).sqrMagnitude>1e-12f)return false;}return true;}
 public bool TryObserve(Collider region,string sampleId,float suppliedPh){if(!BuffersConfigured||string.IsNullOrWhiteSpace(sampleId)||sampleId.Length>128||!float.IsFinite(suppliedPh)||suppliedPh<0||suppliedPh>14||!Immersed(region)){Clear();return false;}foreach(char ch in sampleId)if(char.IsControl(ch)){Clear();return false;}liquid=region;ph=suppliedPh;SampleId=sampleId.Normalize();hasReading=true;readout.text=ph.ToString("0.00",CultureInfo.InvariantCulture)+" pH";return true;}
 void Clear(){hasReading=false;liquid=null;SampleId=null;if(readout)readout.text="---- pH";}
 void LateUpdate(){if(hasReading&&float.IsNaN(Reading))Clear();if(cableGuide)cableGuide.SetActive((probeAssembly.localPosition-restPosition).sqrMagnitude<1e-12f&&Quaternion.Angle(probeAssembly.localRotation,restRotation)<.001f);}
 void OnDisable()=>Clear();
 // shortcut: caller supplies the true convex liquid region and solver pH; buffer metadata does not apply calibration or temperature compensation.
}
