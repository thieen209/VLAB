using UnityEngine;
public sealed class VlabPlasmolysis:MonoBehaviour{
 [SerializeField] Renderer protoplast;[SerializeField] Texture2D[] states;[SerializeField] Texture2D cellIDs;MaterialPropertyBlock properties;
 public int StateIndex{get;private set;}
 public Texture2D CurrentTexture=>states!=null&&states.Length==4?states[StateIndex]:null;
 public bool TrySetState(int index){if(!isActiveAndEnabled||!protoplast||states==null||states.Length!=4||index<0||index>=4||!states[index])return false;properties??=new MaterialPropertyBlock();protoplast.GetPropertyBlock(properties);properties.SetTexture("_BaseMap",states[index]);protoplast.SetPropertyBlock(properties);StateIndex=index;return true;}
 public bool TryGetCell(Vector2 uv,out int id){id=0;if(!isActiveAndEnabled||!cellIDs||!float.IsFinite(uv.x)||!float.IsFinite(uv.y)||uv.x<0||uv.x>=1||uv.y<0||uv.y>=1)return false;int key=((Color32)cellIDs.GetPixel((int)(uv.x*cellIDs.width),(int)(uv.y*cellIDs.height))).r;if(key<1||key>32)return false;id=key;return true;}
 void Awake()=>TrySetState(0);
 // shortcut: explicit untimed illustrative texture states; wall and cell IDs remain fixed, real osmosis kinetics/concentration and viability require a calibrated lesson model.
}
