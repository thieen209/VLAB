using System;
using System.Globalization;
using UnityEngine;
public sealed class VlabPowerSupply : MonoBehaviour
{
 [SerializeField] VlabElectricalTerminal[] ports;
 [SerializeField] Transform[] knobs;
 [SerializeField] Quaternion[] knobRest;
 [SerializeField] Transform rocker;
 [SerializeField] Quaternion rockerRest;
 [SerializeField] Vector3 knobAxis,rockerAxis;
 [SerializeField] TextMesh readout;
 bool controlsSet,hasReading;double voltage,current;
 public bool On{get;private set;}
 public double MaxCurrentA{get;private set;}=double.NaN;
 public double SetVoltageV{get;private set;}
 public double CurrentLimitA{get;private set;}
 public bool CurrentLimited{get;private set;}
 public bool OutputAvailable=>Ready&&On&&controlsSet&&!double.IsNaN(MaxCurrentA);
 bool ReadingValid=>hasReading&&OutputAvailable&&(current==0||(ports[0].Occupied&&ports[1].Occupied));
 public double VoltageV=>ReadingValid?voltage:double.NaN;
 public double CurrentA=>ReadingValid?current:double.NaN;
 public TextMesh Readout=>readout;
 public VlabElectricalTerminal Port(bool positive)=>ports[positive?1:0];
 bool Ready=>isActiveAndEnabled&&ports[0].Compatible&&ports[1].Compatible&&(transform.lossyScale-Vector3.one).sqrMagnitude<1e-10f;
 void Awake(){string scope=Guid.NewGuid().ToString("N");for(int i=0;i<2;i++)if(!ports[i].TrySetNodeId("power_supply:"+scope+":"+i))throw new InvalidOperationException("Source terminal identity unavailable");}
 public bool TryConfigureCurrentMax(double maxA){if(!Ready||On||!double.IsFinite(maxA)||maxA<=0)return false;MaxCurrentA=maxA;controlsSet=false;Clear();return true;}
 public bool TrySetControls(double volts,double limitA){if(!Ready||On||double.IsNaN(MaxCurrentA)||!double.IsFinite(volts)||volts<0||volts>6||!double.IsFinite(limitA)||limitA<=0||limitA>MaxCurrentA)return false;SetVoltageV=volts;CurrentLimitA=limitA;controlsSet=true;knobs[0].localRotation=Quaternion.AngleAxis((float)(volts/6*270),knobAxis)*knobRest[0];knobs[1].localRotation=Quaternion.AngleAxis((float)(limitA/MaxCurrentA*270),knobAxis)*knobRest[1];Clear();return true;}
 public bool TrySetPower(bool value){if(!Ready||value&&(!controlsSet||double.IsNaN(MaxCurrentA)))return false;On=value;rocker.localRotation=Quaternion.AngleAxis(value?-12:12,rockerAxis)*rockerRest;Clear();return true;}
 public bool TryObserveOperatingPoint(double volts,double amps,bool currentLimited){if(!OutputAvailable||!double.IsFinite(volts)||!double.IsFinite(amps)||volts<0||volts>SetVoltageV||amps<0||amps>CurrentLimitA||amps>0&&(!ports[0].Occupied||!ports[1].Occupied)||currentLimited&&Math.Abs(amps-CurrentLimitA)>Math.Max(1e-12,CurrentLimitA*1e-9)||!currentLimited&&Math.Abs(volts-SetVoltageV)>Math.Max(1e-12,SetVoltageV*1e-9)){Clear();return false;}voltage=volts;current=amps;CurrentLimited=currentLimited;hasReading=true;Text("V "+volts.ToString(volts!=0&&volts<.0005?"0.###E+0":"0.000",CultureInfo.InvariantCulture)+"\nA "+amps.ToString(amps!=0&&(amps<.0005||amps>=1000)?"0.###E+0":"0.000",CultureInfo.InvariantCulture)+(currentLimited?" CC":" CV"));return true;}
 void Text(string text){if(!readout)return;readout.text=text;readout.characterSize=.0018f;var size=readout.GetComponent<Renderer>().localBounds.size;if(size.x>.078f||size.y>.027f)readout.characterSize*=Mathf.Min(.077f/size.x,.026f/size.y);}
 void Clear(){hasReading=false;CurrentLimited=false;Text(On?"V ----\nA ----":"");}
 void LateUpdate(){if(hasReading&&!ReadingValid)Clear();}
 void OnDisable(){On=false;if(rocker)rocker.localRotation=Quaternion.AngleAxis(12,rockerAxis)*rockerRest;Clear();}
 // shortcut: bounded supplied CV/CC operating points, not a circuit solver; topology/load/transients and calibrated hardware limits belong to C03 integration.
}
