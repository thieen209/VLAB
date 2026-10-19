using System;
using System.Globalization;
using UnityEngine;
public sealed class VlabReagentBottle : MonoBehaviour
{
 [SerializeField] VlabFlask liquid;
 [SerializeField] Transform cap,closed,opened;
 [SerializeField] TextMesh label;
 [SerializeField] Vector2 labelSize;
 public string ReagentId{get;private set;}
 public string ConcentrationUnit{get;private set;}
 public double Concentration{get;private set;}=double.NaN;
 public bool CapOpen{get;private set;}
 public VlabFlask Liquid=>liquid;
 public TextMesh Label=>label;
 bool Ready=>isActiveAndEnabled&&(transform.lossyScale-Vector3.one).sqrMagnitude<1e-10f;
 static bool Valid(string text,int max){if(string.IsNullOrWhiteSpace(text)||text.Length>max)return false;foreach(char c in text)if(char.IsControl(c))return false;return true;}
 public bool TryConfigure(string reagentId,double concentration,string unit){if(!Ready||!Valid(reagentId,64)||!Valid(unit,24)||!double.IsFinite(concentration)||concentration<0)return false;string id=reagentId.Trim().Normalize(),u=unit.Trim().Normalize();if(liquid.VolumeMl>0&&(id!=ReagentId||u!=ConcentrationUnit||concentration!=Concentration))return false;ReagentId=id;Concentration=concentration;ConcentrationUnit=u;string lines="";for(int i=0;i<id.Length;i+=18)lines+=(i>0?"\n":"")+id.Substring(i,Math.Min(18,id.Length-i));label.text=lines+"\n"+concentration.ToString("G5",CultureInfo.InvariantCulture)+" "+u;label.characterSize=.0015f;var b=label.GetComponent<Renderer>().localBounds.size;if(b.x>labelSize.x||b.y>labelSize.y)label.characterSize*=Mathf.Min(labelSize.x/b.x,labelSize.y/b.y);return true;}
 public bool TrySetCapOpen(bool open){if(!Ready)return false;cap.position=(open?opened:closed).position;CapOpen=open;return true;}
 public bool TrySetVolume(double ml)=>Ready&&CapOpen&&ReagentId!=null&&liquid.TrySetVolume(ml);
 // shortcut: supplied reagent metadata/explicit upright amount, not solution composition/transfer/chemical safety validation or a screw-thread simulation.
}
