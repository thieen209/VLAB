using System;
using System.Collections.Generic;
using UnityEngine;
public sealed class VlabFlask : MonoBehaviour
{
 [SerializeField] MeshFilter liquid;
 [SerializeField] Vector2[] cavity;
 [SerializeField] double capacityMl;
 [SerializeField] int sides=32;
 [SerializeField] Vector3 localUp;
 Mesh ownedMesh;
 public double CapacityMl=>capacityMl;
 public double VolumeMl{get;private set;}
 public double SurfaceHeightMeters{get;private set;}
 public MeshFilter Liquid=>liquid;
 // Product adapter: test the delivered cavity profile, never an exterior bounds proxy.
 public bool ContainsPoint(Vector3 world,bool liquidOnly=false,float margin=0)
 {
  if(!isActiveAndEnabled||(transform.lossyScale-Vector3.one).sqrMagnitude>1e-10f)return false;
  var p=Quaternion.Inverse(Quaternion.FromToRotation(Vector3.forward,localUp))*transform.InverseTransformPoint(world);
  if(p.z<cavity[0].x+margin||p.z>cavity[cavity.Length-1].x-margin||liquidOnly&&p.z>SurfaceHeightMeters-margin)return false;
  for(int i=0;i<cavity.Length-1;i++)if(p.z>=cavity[i].x&&p.z<=cavity[i+1].x)
  {float r=Mathf.Lerp(cavity[i].y,cavity[i+1].y,(p.z-cavity[i].x)/(cavity[i+1].x-cavity[i].x))-margin;return r>0&&p.x*p.x+p.y*p.y<=r*r;}
  return false;
 }
 public Vector3 CavityCentreAt(double height)=>transform.TransformPoint(Quaternion.FromToRotation(Vector3.forward,localUp)*Vector3.forward*(float)height);
 public Vector3 Mouth=>CavityCentreAt(cavity[cavity.Length-1].x);
 public Vector3 PourOutlet
 {
  get {var radial=Vector3.ProjectOnPlane(Vector3.down,transform.TransformDirection(localUp)).normalized;return Mouth+radial*Mathf.Max(0,cavity[cavity.Length-1].y-.001f);}
 }
 public bool TryPourInto(VlabFlask receiver,double requestedMl,bool sourceOpen,bool receiverOpen,out double moved)
 {
  moved=0;
  if(!sourceOpen||!receiverOpen||!receiver||receiver==this||!isActiveAndEnabled||!receiver.isActiveAndEnabled||!double.IsFinite(requestedMl)||requestedMl<=0||VolumeMl<=0||receiver.VolumeMl>=receiver.capacityMl)return false;
  if((transform.lossyScale-Vector3.one).sqrMagnitude>1e-10f||(receiver.transform.lossyScale-Vector3.one).sqrMagnitude>1e-10f||Vector3.Dot(transform.TransformDirection(localUp),Vector3.up)>.35f||Vector3.Dot(receiver.transform.TransformDirection(receiver.localUp),Vector3.up)<.99999f)return false;
  const float radius=.001f;
  var origin=PourOutlet;var mouth=receiver.Mouth;float fall=origin.y-mouth.y;
  if(!float.IsFinite(fall)||fall<.005f||fall>.45f)return false;
  var landing=new Vector3(origin.x,mouth.y-.002f,origin.z);
  for(int n=0;n<8;n++){float a=n*Mathf.PI/4;if(!receiver.ContainsPoint(landing+radius*new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))))return false;}
  Physics.SyncTransforms();
  foreach(var contact in Physics.OverlapSphere(origin,radius,~0,QueryTriggerInteraction.Ignore))if(!contact.transform.IsChildOf(transform))return false;
  foreach(var hit in Physics.SphereCastAll(origin,radius,Vector3.down,fall-radius*2,~0,QueryTriggerInteraction.Ignore))if(!hit.collider.transform.IsChildOf(transform))return false;
  double accepted=Math.Min(requestedMl,Math.Min(VolumeMl,receiver.capacityMl-receiver.VolumeMl));
  if(!receiver.TrySetVolume(receiver.VolumeMl+accepted))return false;
  // shortcut: conserved teaching transfer with a local-axis liquid mesh; upgrade rendering for gravity-level tilted surfaces and spilling.
  SetVolumeMesh(VolumeMl-accepted);moved=accepted;return true;
 }
 double VolumeAt(double z){double sum=0,factor=sides*.5*Math.Sin(2*Math.PI/sides);for(int i=0;i<cavity.Length-1;i++){double z0=cavity[i].x,z1=cavity[i+1].x,r0=cavity[i].y,r1=cavity[i+1].y,h=Math.Max(0,Math.Min(z,z1)-z0),r=r0+(r1-r0)*h/(z1-z0);sum+=factor*h*(r0*r0+r0*r+r*r)/3;}return sum*1e6;}
 public bool TrySetVolume(double ml)
 {
  if(!isActiveAndEnabled||!double.IsFinite(ml)||ml<0||ml>capacityMl||(transform.lossyScale-Vector3.one).sqrMagnitude>1e-10f||Vector3.Dot(transform.TransformDirection(localUp),Vector3.up)<.99999f)return false;
  return SetVolumeMesh(ml);
 }
 bool SetVolumeMesh(double ml)
 {
  if(ml==0){liquid.gameObject.SetActive(false);VolumeMl=0;SurfaceHeightMeters=cavity[0].x;return true;}
  double lo=cavity[0].x,hi=cavity[cavity.Length-1].x;for(int i=0;i<50;i++){double mid=(lo+hi)/2;if(VolumeAt(mid)<ml)lo=mid;else hi=mid;}float height=(float)((lo+hi)/2);
  // Float meshes cannot resolve a film below 10nm; retain its volume without a degenerate surface.
  if(height-cavity[0].x<1e-8f){liquid.gameObject.SetActive(false);VolumeMl=ml;SurfaceHeightMeters=(lo+hi)/2;return true;}
  var rings=new List<Vector2>();foreach(var p in cavity)if(p.x<height)rings.Add(p);for(int i=0;i<cavity.Length-1;i++)if(height>=cavity[i].x&&height<=cavity[i+1].x){rings.Add(new Vector2(height,Mathf.Lerp(cavity[i].y,cavity[i+1].y,(height-cavity[i].x)/(cavity[i+1].x-cavity[i].x))));break;}
  var vertices=new List<Vector3>();var triangles=new List<int>();foreach(var p in rings)for(int i=0;i<sides;i++){float angle=2*Mathf.PI*i/sides;vertices.Add(new Vector3(p.y*Mathf.Cos(angle),p.y*Mathf.Sin(angle),p.x));}
  for(int j=0;j<rings.Count-1;j++)for(int i=0;i<sides;i++){int a=j*sides+i,b=j*sides+(i+1)%sides,c=b+sides,d=a+sides;triangles.AddRange(new[]{a,b,c,a,c,d});}
  for(int end=0;end<2;end++){int ring=end==0?0:rings.Count-1,start=vertices.Count;for(int i=0;i<sides;i++)vertices.Add(vertices[ring*sides+i]);for(int i=1;i<sides-1;i++)triangles.AddRange(end==0?new[]{start,start+i+1,start+i}:new[]{start,start+i,start+i+1});}
  var axisRotation=Quaternion.FromToRotation(Vector3.forward,localUp);for(int i=0;i<vertices.Count;i++)vertices[i]=axisRotation*vertices[i];
  if(ownedMesh==null)ownedMesh=new Mesh{name="Flask liquid instance"};ownedMesh.Clear();ownedMesh.SetVertices(vertices);ownedMesh.SetTriangles(triangles,0);ownedMesh.RecalculateNormals();ownedMesh.RecalculateBounds();liquid.sharedMesh=ownedMesh;liquid.gameObject.SetActive(true);VolumeMl=ml;SurfaceHeightMeters=height;return true;
 }
 void Start()=>TrySetVolume(VolumeMl);
 void OnDestroy(){if(ownedMesh!=null){if(Application.isPlaying)Destroy(ownedMesh);else DestroyImmediate(ownedMesh);}}
 // shortcut: upright flat liquid surface; tilt, pouring, composition and meniscus optics need the lesson fluid system.
}
