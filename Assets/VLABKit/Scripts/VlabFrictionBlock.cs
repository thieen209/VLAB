using System;
using UnityEngine;
public sealed class VlabFrictionBlock : MonoBehaviour
{
 [SerializeField] Renderer body;
 [SerializeField] Material wood,rubber,aluminium;
 [SerializeField] Transform forceSocket;
 [SerializeField] string surface="wood";
 public string Surface=>surface;
 public Vector3 PullPoint=>forceSocket.position;
 public bool SetSurface(string id){
 if(!isActiveAndEnabled||(transform.lossyScale-Vector3.one).sqrMagnitude>1e-10f)return false;
 Material m=id=="wood"?wood:id=="rubber"?rubber:id=="aluminium"?aluminium:null;
 if(!m)return false;body.sharedMaterial=m;surface=id;return true;
 }
 public bool TryFriction(double normalN,double appliedN,double velocity,double staticMu,double kineticMu,out double frictionN,out bool sliding){
 frictionN=0;sliding=false;
 if(!isActiveAndEnabled||(transform.lossyScale-Vector3.one).sqrMagnitude>1e-10f||!double.IsFinite(normalN)||!double.IsFinite(appliedN)||!double.IsFinite(velocity)||!double.IsFinite(staticMu)||!double.IsFinite(kineticMu)||normalN<0||staticMu<0||kineticMu<0||kineticMu>staticMu)return false;
 double limit=normalN*staticMu,kinetic=normalN*kineticMu;if(!double.IsFinite(limit)||!double.IsFinite(kinetic))return false;
 sliding=velocity!=0||Math.Abs(appliedN)>limit;
 frictionN=sliding?-Math.Sign(velocity!=0?velocity:appliedN)*kinetic:-appliedN;return true;
 }
 // shortcut: one-dimensional Coulomb force law with caller-supplied coefficients; motion integration, contact state and calibrated lesson values are separate.
}
