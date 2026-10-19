using UnityEngine;
public sealed class VlabForceps : MonoBehaviour
{
 [SerializeField] Transform left,right,tips;
 [SerializeField] Quaternion leftOpen,rightOpen,leftClosed,rightClosed;
 [SerializeField] Vector3 tipsClosed;
 public float Squeeze{get;private set;}
 public Vector3 TipMidpoint=>tips.position;
 public bool SetSqueeze(float amount){
 if(!isActiveAndEnabled||!float.IsFinite(amount)||amount<0||amount>1||(transform.lossyScale-Vector3.one).sqrMagnitude>1e-10f)return false;
 left.localRotation=Quaternion.Slerp(leftOpen,leftClosed,amount);right.localRotation=Quaternion.Slerp(rightOpen,rightClosed,amount);
 tips.localPosition=tipsClosed*Mathf.Cos((1-amount)*4*Mathf.Deg2Rad);Squeeze=amount;return true;
 }
 // shortcut: rigid visual squeeze only; specimen grasp rules and force response belong to lesson integration.
}
