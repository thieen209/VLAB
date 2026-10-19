using UnityEngine;
public sealed class VlabIndicatorPaper : MonoBehaviour
{
 [SerializeField] Renderer sheet;
 MaterialPropertyBlock block;
 public int State{get;private set;}
 public string IntervalLabel=>State==1?"pH <= 6.0":State==2?"6.0 < pH < 7.6":State==3?"pH >= 7.6":"No reading";
 public bool ObservePh(float ph){if(!float.IsFinite(ph))return false;return Apply(ph<=6?1:ph>=7.6f?3:2);}
 public bool ResetReading()=>Apply(0);
 bool Apply(int state){if(!isActiveAndEnabled||!sheet||(transform.lossyScale-Vector3.one).sqrMagnitude>1e-10f)return false;block??=new MaterialPropertyBlock();sheet.GetPropertyBlock(block);block.SetVector("_BaseMap_ST",new Vector4(1,1,state*.25f,0));sheet.SetPropertyBlock(block);State=state;return true;}
 // shortcut: explicit BTB observation from lesson pH; dipping, contamination and equilibration are separate.
}
