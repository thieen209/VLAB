using System.Globalization;using UnityEngine;
public sealed class VlabWaterBath:MonoBehaviour
{
 [SerializeField] Transform lid,closed,opened,seat;
 [SerializeField] GameObject fullWater,occupiedWater;
 [SerializeField] TextMesh readout;
 public bool LidOpen{get;private set;}
 public bool HeaterOn{get;private set;}
 public float BathC{get;private set;}=float.NaN;
 public float SampleC{get;private set;}=float.NaN;
 public VlabBeaker Sample{get;private set;}
 public TextMesh Readout=>readout;
 bool Ready=>isActiveAndEnabled&&(transform.lossyScale-Vector3.one).sqrMagnitude<1e-10f;
 static bool Temperature(float t)=>float.IsFinite(t)&&t>=0&&t<=100;
 public bool TrySetLidOpen(bool open){if(!Ready||(!open&&Sample!=null))return false;lid.position=(open?opened:closed).position;LidOpen=open;return true;}
 public bool TrySetHeater(bool on){if(!Ready)return false;HeaterOn=on;Render();return true;}
 public bool TrySetBathC(float c){if(!Ready||!Temperature(c))return false;BathC=c;Render();return true;}
 public bool TrySetSampleC(float c){if(!Ready||Sample==null||!Temperature(c))return false;SampleC=c;Render();return true;}
 public bool TryMountSample(VlabBeaker sample){if(!Ready||!LidOpen||Sample!=null||sample==null||!sample.isActiveAndEnabled||sample.transform.IsChildOf(transform)||(sample.transform.lossyScale-Vector3.one).sqrMagnitude>1e-10f||Vector3.Dot(transform.TransformDirection(Vector3.back),Vector3.up)<.99999f)return false;
  var candidate=Matrix4x4.TRS(seat.position,transform.rotation,Vector3.one);foreach(var mf in sample.GetComponentsInChildren<MeshFilter>(true)){if(mf.sharedMesh==null||!mf.sharedMesh.isReadable)return false;var local=sample.transform.worldToLocalMatrix*mf.transform.localToWorldMatrix;foreach(var v in mf.sharedMesh.vertices){var p=transform.InverseTransformPoint(candidate.MultiplyPoint3x4(local.MultiplyPoint3x4(v)));if(p.x*p.x+p.y*p.y>.0426f*.0426f||-p.z<.02699f||-p.z>.15f)return false;}}
  sample.transform.SetParent(transform,true);sample.transform.SetPositionAndRotation(seat.position,transform.rotation);Sample=sample;SampleC=float.NaN;fullWater.SetActive(false);occupiedWater.SetActive(true);Render();return true;
 }
 public bool TryRemoveSample(){if(!Ready||Sample==null)return false;Sample.transform.SetParent(null,true);Sample=null;SampleC=float.NaN;fullWater.SetActive(true);occupiedWater.SetActive(false);Render();return true;}
 void Render(){string T(float c)=>float.IsFinite(c)?c.ToString("0.0",CultureInfo.InvariantCulture)+" C":"--";readout.text="BATH "+T(BathC)+"\nSAMPLE "+T(SampleC)+"\nHEATER "+(HeaterOn?"ON":"OFF");readout.characterSize=.003f;var size=readout.GetComponent<Renderer>().localBounds.size;readout.characterSize*=Mathf.Min(1,Mathf.Min(.115f/size.x,.035f/size.y));}
 void Awake()=>Render();
 void OnDisable(){HeaterOn=false;if(readout)Render();}
 // shortcut: independently supplied bath/sample temperatures and explicit heater state; no heat-transfer/equilibrium or thermostat calibration model.
}
