using UnityEngine;
public sealed class VlabPhotogate : MonoBehaviour
{
 [SerializeField] Transform beamStart,beamEnd,beamUp;
 [SerializeField] BoxCollider opticalTrigger;
 bool canEnter,measuring;VlabFlag current;double previous=double.NaN,entered;
 public bool Blocked{get;private set;}
 public bool HasMeasurement{get;private set;}
 public double DurationSeconds{get;private set;}
 public double MeanSpeedMps{get;private set;}
 public Vector3 TravelAxis=>Vector3.Cross((beamEnd.position-beamStart.position).normalized,(beamUp.position-beamStart.position).normalized).normalized;
 public BoxCollider OpticalTrigger=>opticalTrigger;
 bool Ready=>isActiveAndEnabled&&(transform.lossyScale-Vector3.one).sqrMagnitude<1e-10f;
 public void ResetMeasurement(){Blocked=false;HasMeasurement=false;canEnter=false;measuring=false;current=null;previous=double.NaN;DurationSeconds=0;MeanSpeedMps=0;}
 public bool TrySample(VlabFlag flag,double timestampSeconds){if(!Ready||!flag||!flag.TryGetMeanSpeed(1,out _)||!double.IsFinite(timestampSeconds)||timestampSeconds<0||(!double.IsNaN(previous)&&timestampSeconds<=previous)||Mathf.Abs(Vector3.Dot(flag.transform.right,TravelAxis))<.9999f||(current&&current!=flag)){ResetMeasurement();return false;}var shape=flag.GetComponentInChildren<MeshCollider>();if(!shape||!shape.enabled||shape.isTrigger||!shape.gameObject.activeInHierarchy){ResetMeasurement();return false;}Physics.SyncTransforms();bool blocked=shape.Raycast(new Ray(beamStart.position,(beamEnd.position-beamStart.position).normalized),out _,Vector3.Distance(beamStart.position,beamEnd.position));previous=timestampSeconds;if(blocked&&!Blocked){entered=timestampSeconds;measuring=canEnter;current=flag;HasMeasurement=false;DurationSeconds=0;MeanSpeedMps=0;}else if(!blocked&&Blocked&&measuring){DurationSeconds=timestampSeconds-entered;if(!flag.TryGetMeanSpeed(DurationSeconds,out double speed)){ResetMeasurement();return false;}MeanSpeedMps=speed;HasMeasurement=true;current=null;}if(!blocked){canEnter=true;current=null;}Blocked=blocked;return true;}
 void OnDisable()=>ResetMeasurement();
 // shortcut: supplied monotonic samples timestamp actual ray transitions; subframe crossing interpolation, automatic sampling and hardware clock calibration belong to the lesson.
}
