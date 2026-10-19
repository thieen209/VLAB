using System;
using System.Globalization;
using UnityEngine;
public sealed class VlabMultimeter : MonoBehaviour
{
 [SerializeField] Transform dial;
 [SerializeField] Quaternion dialRest;
 [SerializeField] Vector3 localFront;
 [SerializeField] VlabElectricalTerminal[] ports;
 [SerializeField] VlabElectricalTerminal probeARear,probeBRear;
 [SerializeField] TextMesh readout;
 VlabCable commonLead,positiveLead;bool hasReading;double value;
 public int Mode{get;private set;}
 void Awake(){string scope=Guid.NewGuid().ToString("N");var all=new[]{ports[0],ports[1],ports[2],probeARear,probeBRear};for(int i=0;i<all.Length;i++)if(!all[i].TrySetNodeId("multimeter:"+scope+":"+i))throw new InvalidOperationException("Meter terminal identity unavailable");}
 bool Ready=>isActiveAndEnabled&&(transform.lossyScale-Vector3.one).sqrMagnitude<1e-10f;
 static bool Link(VlabCable c,VlabElectricalTerminal a,VlabElectricalTerminal b)=>c&&c.IsConducting&&((c.Terminal(0)==a&&c.Terminal(1)==b)||(c.Terminal(1)==a&&c.Terminal(0)==b));
 public bool ConnectionsValid=>Ready&&Mode>0&&Link(commonLead,ports[0],probeARear)&&Link(positiveLead,ports[Mode],probeBRear);
 public double Reading=>hasReading&&ConnectionsValid?value:double.NaN;
 public TextMesh Readout=>readout;
 public VlabElectricalTerminal Port(int index)=>index>=0&&index<3?ports[index]:null;
 public VlabElectricalTerminal ProbeRear(bool positive)=>positive?probeBRear:probeARear;
 public bool TryAssignLeads(VlabCable common,VlabCable positive){if(!Ready||Mode!=0||!common||!positive||common==positive)return false;commonLead=common;positiveLead=positive;Clear();return true;}
 public bool TrySetMode(int mode){if(!Ready||mode<0||mode>2)return false;dial.localRotation=Quaternion.AngleAxis(mode*60,localFront)*dialRest;Mode=mode;Clear();return true;}
 public bool TrySetReading(double suppliedValue){if(!Ready||!double.IsFinite(suppliedValue))return false;if(!ConnectionsValid){Clear();return false;}value=suppliedValue;hasReading=true;Text(suppliedValue.ToString(Math.Abs(suppliedValue)<1000?"0.000":"0.###E+0",CultureInfo.InvariantCulture)+(Mode==1?" V":" A"));return true;}
 void Clear(){hasReading=false;Text(Mode==0?"":Mode==1?"---- V":"---- A");}
 void Text(string text){readout.text=text;readout.characterSize=.002f;var size=readout.GetComponent<Renderer>().localBounds.size;if(size.x>.052f||size.y>.020f)readout.characterSize*=Mathf.Min(.051f/size.x,.019f/size.y);}
 void LateUpdate(){if(hasReading&&!ConnectionsValid)Clear();}
 void OnDisable()=>Clear();
 // shortcut: validates real lead routes/ports and displays suppliedsolverdata; circuit topology, probe contact and electrochemistry belong to C02/C03.
}

