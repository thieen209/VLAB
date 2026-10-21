using System.Collections.Generic;
using TMPro;
using UnityEngine;
namespace Vlab.Product
{
    // Reversible observation mats preserve white-precipitate contrast on the light worktop.
    public sealed class FlagshipStageLabels:MonoBehaviour
    {
        readonly List<(EquipmentHandle item,TextMeshPro label)> labels=new List<(EquipmentHandle,TextMeshPro)>();Camera view;Material mat;
        public void Initialize(ApparatusRig rig,Camera camera)
        {
            view=camera;mat=new Material(Shader.Find("Universal Render Pipeline/Lit")){color=new Color(.09f,.12f,.13f)};mat.SetFloat("_Smoothness",.08f);
            foreach(var key in new[]{"nacl","nitrate","water"})
            {
                var item=rig.items.Find(x=>x.assetId==key);if(!item)continue;
                var pad=GameObject.CreatePrimitive(PrimitiveType.Cube);pad.name="Observation mat · "+key;pad.transform.SetParent(transform,false);pad.transform.position=new Vector3(item.slot.x,rig.Worktop.y+.001f,item.slot.z);pad.transform.localScale=new Vector3(.13f,.002f,.13f);Destroy(pad.GetComponent<Collider>());pad.GetComponent<Renderer>().sharedMaterial=mat;
                var go=new GameObject("Sample identity · "+key,typeof(TextMeshPro));go.transform.SetParent(transform,false);var text=go.GetComponent<TextMeshPro>();text.font=Resources.Load<TMP_FontAsset>("UI/VlabBody");text.text=key=="nacl"?"NaCl":key=="nitrate"?"NaNO₃":"H₂O";text.fontSize=.20f;text.alignment=TextAlignmentOptions.Center;text.color=Color.white;text.textWrappingMode=TextWrappingModes.NoWrap;text.rectTransform.sizeDelta=new Vector2(.16f,.04f);labels.Add((item,text));
            }
        }
        void LateUpdate(){if(!view)return;foreach(var pair in labels){if(!pair.item||!pair.label)continue;var position=pair.item.transform.position+new Vector3(0,.13f,0);pair.label.transform.SetPositionAndRotation(position,Quaternion.LookRotation(position-view.transform.position,view.transform.up));}}
        void OnDestroy(){if(mat)Destroy(mat);}
    }
}
