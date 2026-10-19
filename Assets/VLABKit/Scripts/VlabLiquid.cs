using UnityEngine;
[RequireComponent(typeof(VlabFlask))]
public sealed class VlabLiquid : MonoBehaviour
{
 [SerializeField] MeshFilter surface;
 [SerializeField] int sides=32;
 [SerializeField] Vector3 localUp;
 VlabFlask volume; Mesh owned; double last=-1;
 public VlabFlask Volume=>volume?volume:(volume=GetComponent<VlabFlask>());
 public MeshFilter Surface=>surface;
 public bool TrySetVolume(double ml){if(!isActiveAndEnabled||!Volume.TrySetVolume(ml))return false;RefreshSurface();return true;}
 void LateUpdate(){if(last!=Volume.VolumeMl)RefreshSurface();}
 void RefreshSurface(){last=Volume.VolumeMl;bool visible=Volume.Liquid.gameObject.activeSelf;surface.gameObject.SetActive(visible);if(!visible)return;
  float h=(float)Volume.SurfaceHeightMeters,r=0;
  foreach(var p in Volume.Liquid.sharedMesh.vertices)if(Mathf.Abs(Vector3.Dot(p,localUp)-h)<1e-7f)r=Mathf.Max(r,Vector3.ProjectOnPlane(p,localUp).magnitude);
  var v=new Vector3[1+2*sides];var t=new int[9*sides];var rot=Quaternion.FromToRotation(Vector3.forward,localUp);v[0]=rot*new Vector3(0,0,h+.00002f);
  // shortcut: 0.35mm optical rim only; upgrade for contact-angle or tilt optics, never add it to measured volume.
  float rise=Mathf.Min(.00035f,(h-Vector3.Dot(Volume.Liquid.sharedMesh.vertices[0],localUp))*.1f);
  for(int i=0;i<sides;i++){float a=2*Mathf.PI*i/sides;v[1+i]=rot*new Vector3(Mathf.Max(0,r-.0004f)*Mathf.Cos(a),Mathf.Max(0,r-.0004f)*Mathf.Sin(a),h+.00002f);v[1+sides+i]=rot*new Vector3((r-.00002f)*Mathf.Cos(a),(r-.00002f)*Mathf.Sin(a),h+rise);int j=(i+1)%sides,k=i*9;t[k]=0;t[k+1]=1+i;t[k+2]=1+j;t[k+3]=1+i;t[k+4]=1+sides+i;t[k+5]=1+sides+j;t[k+6]=1+i;t[k+7]=1+sides+j;t[k+8]=1+j;}
  if(!owned)owned=new Mesh{name="Liquid optical surface instance"};owned.Clear();owned.vertices=v;owned.triangles=t;owned.RecalculateNormals();owned.RecalculateBounds();surface.sharedMesh=owned;
 }
 void OnDestroy(){if(owned){if(Application.isPlaying)Destroy(owned);else DestroyImmediate(owned);}}
}
