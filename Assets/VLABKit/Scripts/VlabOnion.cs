using System;
using System.Text;
using UnityEngine;
public sealed class VlabOnion : MonoBehaviour
{
 [SerializeField] GameObject[] parts;
 [SerializeField] Bounds[] bounds;
 [SerializeField] BoxCollider shape;
 public int State{get;private set;}
 public string SampleID{get;private set;}="";
 Texture2D microscopeImage;
 public BoxCollider Shape=>shape;
 bool Ready=>isActiveAndEnabled&&(transform.lossyScale-Vector3.one).sqrMagnitude<1e-10f;
 public bool TrySetState(int state){if(!Ready||state<0||state>2)return false;for(int i=0;i<parts.Length;i++)parts[i].SetActive(i==state);shape.center=bounds[state].center;shape.size=bounds[state].size;State=state;return true;}
 public bool TryConfigureSample(string id,Texture2D image){if(!Ready||string.IsNullOrWhiteSpace(id))return false;try{id=id.Normalize(NormalizationForm.FormC);}catch(ArgumentException){return false;}if(id.Length>128)return false;foreach(char ch in id)if(char.IsControl(ch))return false;SampleID=id;microscopeImage=image;return true;}
 public bool TryGetMicroscopeView(out string id,out Texture2D image){id=null;image=null;if(!Ready||State!=2||SampleID.Length==0||!microscopeImage)return false;id=SampleID;image=microscopeImage;return true;}
 // shortcut: symbolic macro preparation states and explicit sample-image mapping; no inferred portion mass, cell atlas or microscopy outcome.
}
