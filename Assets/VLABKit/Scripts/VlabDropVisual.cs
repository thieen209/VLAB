using UnityEngine;
public sealed class VlabDropVisual : MonoBehaviour
{
 [SerializeField] Renderer dropMesh,atlas;
 MaterialPropertyBlock properties;
 public int Mode{get;private set;}=1;
 public int Frame{get;private set;}
 public bool SetPresentation(int mode,int frame=0)
 {
  if(!isActiveAndEnabled||mode<0||mode>2||frame<0||frame>3)return false;
  properties??=new MaterialPropertyBlock();atlas.GetPropertyBlock(properties);properties.SetVector("_BaseMap_ST",new Vector4(1,1,(frame%2)*.5f,(frame/2)*.5f));atlas.SetPropertyBlock(properties);
  dropMesh.enabled=mode==1;atlas.enabled=mode==2;Mode=mode;Frame=frame;return true;
 }
 // shortcut: static illustrative frames; emission, billboarding and measured liquid transfer belong to the lesson effect system.
}
