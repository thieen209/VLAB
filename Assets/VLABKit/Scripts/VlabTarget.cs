using UnityEngine;
public sealed class VlabTarget:MonoBehaviour{
 [SerializeField]Transform origin,xEnd,yEnd,marker;public bool HasHit{get;private set;}public Vector2 CoordinatesM{get;private set;}public Transform Marker=>marker;
 void Awake()=>Clear();
 public bool TryMarkWorld(Vector3 world){if(!isActiveAndEnabled||!float.IsFinite(world.sqrMagnitude)||(transform.lossyScale-Vector3.one).sqrMagnitude>1e-10f)return false;var delta=world-origin.position;var x=(xEnd.position-origin.position).normalized;var y=(yEnd.position-origin.position).normalized;float a=Vector3.Dot(delta,x),b=Vector3.Dot(delta,y);if(a<-.000001f||a>.500001f||b<-.000001f||b>.300001f||Mathf.Abs(Vector3.Dot(delta,Vector3.Cross(x,y)))>.001f)return false;a=Mathf.Clamp(a,0,.5f);b=Mathf.Clamp(b,0,.3f);CoordinatesM=new Vector2(a,b);marker.position=origin.position+x*a+y*b-Vector3.Cross(x,y)*.00015f;marker.gameObject.SetActive(true);HasHit=true;return true;}
 public void Clear(){HasHit=false;CoordinatesM=Vector2.zero;if(marker)marker.gameObject.SetActive(false);}
 void OnDisable()=>Clear();
 // shortcut: explicit supplied hits within1mm of the design plane, no projectile trajectory or physical calibration; rendered8mmring may extend beyond mat edges at boundary hits.
}
