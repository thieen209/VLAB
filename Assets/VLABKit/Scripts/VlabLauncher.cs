using UnityEngine;
public sealed class VlabLauncher : MonoBehaviour
{
 [SerializeField] Transform barrel,trigger,load,muzzle;
 [SerializeField] Quaternion barrelRest;
 [SerializeField] Vector3 localHingeAxis,triggerRest,triggerAxis;
 VlabProjectile loaded,lastShot;
 public float AngleDeg { get; private set; }
 public VlabProjectile Loaded=>loaded;
 public Ray MuzzleRay=>new Ray(muzzle.position,(muzzle.position-load.position).normalized);
 bool Ready=>isActiveAndEnabled&&(transform.lossyScale-Vector3.one).sqrMagnitude<1e-8f;
 public bool SetAngle(float degrees){if(!Ready||!float.IsFinite(degrees)||degrees<0||degrees>90)return false;barrel.localRotation=Quaternion.AngleAxis(degrees,localHingeAxis)*barrelRest;AngleDeg=degrees;if(loaded!=null)loaded.transform.position=load.position;return true;}
 public bool TryLoad(VlabProjectile ball){if(!Ready||loaded!=null||lastShot!=null||ball==null||!ball.isActiveAndEnabled||ball.IsFlying||ball.Diameter<.012f||ball.Diameter>.020f||(ball.transform.lossyScale-Vector3.one*(ball.Diameter/.016f)).sqrMagnitude>1e-8f)return false;loaded=ball;ball.transform.position=load.position;trigger.localPosition=triggerRest;return true;}
 public bool Fire(float speed,double gravity=9.81,double landingCenterY=0){
  if(!Ready||loaded==null||!loaded.isActiveAndEnabled||(loaded.transform.lossyScale-Vector3.one*(loaded.Diameter/.016f)).sqrMagnitude>1e-8f||!float.IsFinite(speed)||speed<=0||double.IsNaN(gravity)||double.IsInfinity(gravity)||gravity<=0||double.IsNaN(landingCenterY)||double.IsInfinity(landingCenterY))return false;
  var ray=MuzzleRay;var center=ray.origin+ray.direction*(loaded.Diameter*.5f+.0001f);if(center.y<landingCenterY)return false;
  if(!loaded.Launch(center,ray.direction*speed,gravity,landingCenterY))return false;
  trigger.localPosition=triggerRest+triggerAxis*.002f;lastShot=loaded;loaded=null;return true;
 }
 public void ResetLauncher(){if(lastShot!=null){lastShot.ResetFlight();loaded=lastShot;lastShot=null;}if(loaded!=null)loaded.transform.position=load.position;trigger.localPosition=triggerRest;}
 // shortcut: prescribed muzzle speed reuses the analytic ball solver; a calibrated spring/trigger power model belongs to P02 integration.
}

