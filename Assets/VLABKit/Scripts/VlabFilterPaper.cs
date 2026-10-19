using UnityEngine;
public sealed class VlabFilterPaper : MonoBehaviour
{
 [SerializeField] Renderer sheet;
 [SerializeField] bool wet;
 MaterialPropertyBlock block;
 public bool IsWet=>wet;
 public bool SetWet(bool value){if(!isActiveAndEnabled||(transform.lossyScale-Vector3.one).sqrMagnitude>1e-10f)return false;block??=new MaterialPropertyBlock();sheet.GetPropertyBlock(block);block.SetVector("_BaseMap_ST",new Vector4(1,1,value?.5f:0,0));sheet.SetPropertyBlock(block);wet=value;return true;}
 void Start()=>SetWet(wet);
 // shortcut: explicit visual wet state; filtration, absorbed volume and mass require lesson simulation.
}
