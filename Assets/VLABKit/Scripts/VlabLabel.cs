using System;
using System.Text;
using UnityEngine;
public sealed class VlabLabel : MonoBehaviour
{
 [SerializeField] TextMesh readout;
 [SerializeField] Renderer background;
 public string ReagentID { get; private set; }="";
 public string SampleID { get; private set; }="";
 public bool SetIds(string reagentID,string sampleID)
 {
  if(!isActiveAndEnabled||reagentID==null||sampleID==null)return false;
  try{reagentID=reagentID.Normalize(NormalizationForm.FormC);sampleID=sampleID.Normalize(NormalizationForm.FormC);}catch(ArgumentException){return false;}
  if(reagentID.Length>24||sampleID.Length>24)return false;
  string all=reagentID+sampleID;readout.font.RequestCharactersInTexture(all,readout.fontSize,FontStyle.Normal);
  foreach(char c in all)if(char.IsControl(c)||(!char.IsWhiteSpace(c)&&!readout.font.HasCharacter(c)))return false;
  ReagentID=reagentID;SampleID=sampleID;
  readout.text=all.Length==0?"":"ReagentID\n"+ReagentID+"\nSampleID\n"+SampleID;
  readout.characterSize=.0006f;
  var size=readout.GetComponent<Renderer>().localBounds.size;
  if(size.x>.044f||size.y>.022f)readout.characterSize*=Mathf.Min(.043f/size.x,.021f/size.y);
  var block=new MaterialPropertyBlock();background.GetPropertyBlock(block);block.SetVector("_BaseMap_ST",new Vector4(1,1,ReagentID.Length>0?.5f:0,0));background.SetPropertyBlock(block);return true;
 }
 public bool ClearIds()=>SetIds("","");
}
