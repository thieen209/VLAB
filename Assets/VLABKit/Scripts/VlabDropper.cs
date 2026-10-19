using UnityEngine;
public sealed class VlabDropper : MonoBehaviour
{
 [SerializeField] Transform bulb,tip;
 [SerializeField] Vector3 tipLocalDirection;
 public Transform Tip=>tip;
 public Vector3 EmissionDirection=>tip.TransformDirection(tipLocalDirection).normalized;
 public float Squeeze{get;private set;}
 public bool SetSqueeze(float amount)
 {
  if(!isActiveAndEnabled||!float.IsFinite(amount)||amount<0||amount>1||(transform.lossyScale-Vector3.one).sqrMagnitude>1e-10f)return false;
  bulb.localScale=new Vector3(1,1,1-.2f*amount);Squeeze=amount;return true;
 }
 // shortcut: prescribed rubber compression and an emission socket; suction, calibrated volume and chemistry require lesson integration.
}
