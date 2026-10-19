using UnityEngine;
public sealed class VlabFlag : MonoBehaviour
{
 [SerializeField] double blockingWidthM=.05;
 public double BlockingWidthM=>blockingWidthM;
 public bool TryGetMeanSpeed(double blockingSeconds,out double metresPerSecond){metresPerSecond=0;if(!isActiveAndEnabled||(transform.lossyScale-Vector3.one).sqrMagnitude>1e-10f||!double.IsFinite(blockingSeconds)||blockingSeconds<=0)return false;double speed=blockingWidthM/blockingSeconds;if(!double.IsFinite(speed))return false;metresPerSecond=speed;return true;}
 // shortcut: mean speed for aligned translation through the full flag width; photogate timing and alignment checks belong to the lesson assembly.
}
