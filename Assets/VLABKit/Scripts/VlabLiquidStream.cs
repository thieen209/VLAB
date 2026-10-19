using UnityEngine;
public sealed class VlabLiquidStream : MonoBehaviour
{
 [SerializeField] Transform body,endA,endB;
 [SerializeField] Vector3 localAxis;
 public bool Flowing{get;private set;}
 public Vector3 StartPoint=>endA.position;
 public Vector3 EndPoint=>endB.position;
 static bool Finite(Vector3 v)=>float.IsFinite(v.x)&&float.IsFinite(v.y)&&float.IsFinite(v.z);
 public bool TrySetEndpoints(Vector3 start,Vector3 end,float diameterM){var delta=end-start;float length=delta.magnitude;if(!isActiveAndEnabled||!Finite(start)||!Finite(end)||!float.IsFinite(length)||length<.005f-1e-7f||length>1+1e-6f||!float.IsFinite(diameterM)||diameterM<.0005f||diameterM>.01f||(transform.lossyScale-Vector3.one).sqrMagnitude>1e-10f)return false;
  transform.SetPositionAndRotation(start,Quaternion.FromToRotation(localAxis,delta));var axis=new Vector3(Mathf.Abs(localAxis.x),Mathf.Abs(localAxis.y),Mathf.Abs(localAxis.z));float radial=diameterM/.003f;body.localScale=Vector3.one*radial+axis*(length/.1f-radial);endA.localScale=endB.localScale=Vector3.one*radial+axis*(1-radial);endA.position=start;endB.position=end;Show(true);return true;
 }
 public void Hide()=>Show(false);
 void Show(bool value){body.gameObject.SetActive(value);endA.gameObject.SetActive(value);endB.gameObject.SetActive(value);Flowing=value;}
 void Awake()=>Hide();
 void OnDisable()=>Hide();
 // shortcut: straight illustrative stream; caller owns validated transfer amount and any curved flow trajectory.
}

