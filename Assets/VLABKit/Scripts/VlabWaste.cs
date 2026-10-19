using System;
using UnityEngine;
public sealed class VlabWaste:MonoBehaviour
{
 [SerializeField] Transform cap,seat,park;
 [SerializeField] TextMesh label;
 [SerializeField] double capacityMl;
 [SerializeField] Vector2 labelSize;
 public string GroupId{get;private set;}
 public double AmountMl{get;private set;}
 public double CapacityMl=>capacityMl;
 public bool CapOpen{get;private set;}
 public TextMesh Label=>label;
 bool Ready=>isActiveAndEnabled&&(transform.lossyScale-Vector3.one).sqrMagnitude<1e-10f;
 public bool TrySetGroup(string group){if(!Ready||AmountMl>0||string.IsNullOrWhiteSpace(group)||group.Length>24)return false;foreach(char c in group)if(char.IsControl(c))return false;GroupId=group.Trim().Normalize();label.text="WASTE\n"+GroupId;label.characterSize=.003f;var size=label.GetComponent<Renderer>().localBounds.size;label.characterSize*=Mathf.Min(1,Mathf.Min(labelSize.x/size.x,labelSize.y/size.y));return true;}
 public bool TrySetCapOpen(bool open){if(!Ready)return false;cap.position=(open?park:seat).position;CapOpen=open;return true;}
 public double TryReceive(string group,double ml){if(!Ready||!CapOpen||GroupId==null||group!=GroupId||!double.IsFinite(ml)||ml<=0)return 0;double accepted=Math.Min(ml,capacityMl-AmountMl);AmountMl+=accepted;return accepted;}
 // shortcut: caller-supplied group and quantity bookkeeping; no compatibility chemistry, fluid surface, disposal instruction or automatic emptying.
}
