using System.Globalization;using UnityEngine;
public sealed class VlabTimer:MonoBehaviour{
 [SerializeField]TextMesh readout,modeLabel;VlabPhotogate gateA,gateB;double started,last,accumulated;public double ElapsedSeconds{get;private set;}public bool Running{get;private set;}public int Mode{get;private set;}public string DisplayText=>readout.text;bool Ready=>isActiveAndEnabled&&(transform.lossyScale-Vector3.one).sqrMagnitude<1e-10f;static bool Stamp(double t)=>double.IsFinite(t)&&t>=0&&t<=1e9;
 void Awake()=>ResetTimer();
 public bool TrySetMode(int mode){if(!Ready||mode<0||mode>1)return false;Mode=mode;ResetTimer();return true;}
 public void ResetTimer(){Running=false;accumulated=0;ElapsedSeconds=Mode==0?0:double.NaN;if(gateA)gateA.ResetMeasurement();if(gateB)gateB.ResetMeasurement();last=started=0;if(readout)readout.text=Mode==0?"0.000 s":"";if(modeLabel)modeLabel.text=Mode==0?"STOPWATCH":"PHOTOGATE";}
 public bool TryStart(double t){if(!Ready||Mode!=0||Running||!Stamp(t)||t<last)return false;started=last=t;Running=true;return true;}
 public bool TrySample(double t){if(!Ready||Mode!=0||!Running||!Stamp(t)||t<last)return false;ElapsedSeconds=accumulated+t-started;last=t;readout.text=ElapsedSeconds.ToString("0.000",CultureInfo.InvariantCulture)+" s";return true;}
 public bool TryStop(double t){if(!TrySample(t))return false;accumulated=ElapsedSeconds;Running=false;return true;}
 public bool TryAssignGates(VlabPhotogate a,VlabPhotogate b){if(!Ready||Running||!a||!b||a==b||!a.isActiveAndEnabled||!b.isActiveAndEnabled)return false;gateA=a;gateB=b;a.ResetMeasurement();b.ResetMeasurement();ResetTimer();return true;}
 public bool TryReadGate(int channel){var gate=channel==0?gateA:channel==1?gateB:null;if(!Ready||Mode!=1||!gate||!gate.isActiveAndEnabled||(gate.transform.lossyScale-Vector3.one).sqrMagnitude>1e-10f||!gate.HasMeasurement||!double.IsFinite(gate.DurationSeconds)||gate.DurationSeconds<=0){ElapsedSeconds=double.NaN;if(readout)readout.text="";return false;}ElapsedSeconds=gate.DurationSeconds;readout.text=(channel==0?"A ":"B ")+ElapsedSeconds.ToString("0.000",CultureInfo.InvariantCulture)+" s";return true;}
 void OnDisable(){ResetTimer();if(readout)readout.text="";}
 // shortcut: explicit monotonic clock ticks and existing gate blocked durations, no automatic clock, subframe interpolation, intergate-flight solver or hardware precision claim.
}
