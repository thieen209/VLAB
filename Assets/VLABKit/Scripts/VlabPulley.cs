using UnityEngine;
public sealed class VlabPulley : MonoBehaviour
{
 [SerializeField] Transform sheave,front,back;
 [SerializeField] Quaternion rest;
 [SerializeField] Vector3 localAxis;
 public double Turns{get;private set;}
 public double RopeTravelMeters=>Turns*(2*System.Math.PI*.0185);
 public Vector3 Axis=>(back.position-front.position).normalized;
 public Transform Sheave=>sheave;
 public bool TrySetTurns(double turns){if(!isActiveAndEnabled||!double.IsFinite(turns)||(transform.lossyScale-Vector3.one).sqrMagnitude>1e-10f)return false;Turns=turns;sheave.localRotation=rest*Quaternion.AngleAxis(-(float)(turns%1*360),localAxis);return true;}
 // shortcut: prescribed no-slip travel/rotation only; fixed/moving rope topology, hanging masses, friction and force solving belong to M03/P07.
}
