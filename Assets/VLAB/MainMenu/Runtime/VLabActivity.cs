using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using VLAB.PhysicsLab.Common;

namespace VLAB.MainMenu
{
    /// <summary>A supplementary hands-on lesson hosted by the existing lab shell.</summary>
    public abstract class VLabActivity : MonoBehaviour
    {
        private readonly List<Material> ownedMaterials=new List<Material>();
        private readonly Dictionary<Color,Material> surfaceMaterials=new Dictionary<Color,Material>();
        private Material textMaterial;
        public abstract string Title { get; }
        public abstract string Objective { get; }
        public abstract string Theory { get; }
        public string Instruction { get; protected set; }
        public string Result { get; protected set; }
        public bool Completed { get; protected set; }
        public event Action Changed;
        public abstract void Build();
        public abstract void ResetActivity();
        protected void NotifyChanged() => Changed?.Invoke();
        protected GameObject Part(string name,PrimitiveType shape,Vector3 position,Vector3 scale,Color color,Action action=null)
        {
            var part=GameObject.CreatePrimitive(shape);part.name=name;
            part.transform.SetParent(transform,false);part.transform.localPosition=position;part.transform.localScale=scale;
            if(!surfaceMaterials.TryGetValue(color,out var material))
            {material=new Material(Shader.Find("Standard")){color=color};material.SetFloat("_Glossiness",.35f);ownedMaterials.Add(material);surfaceMaterials.Add(color,material);}
            part.GetComponent<Renderer>().sharedMaterial=material;
            if(action!=null)MakeInteractive(part,action);
            return part;
        }
        protected void MakeInteractive(GameObject part,Action action)
        {
            var interactable=part.GetComponent<LabInteractable>()??part.AddComponent<LabInteractable>();
            interactable.Activated+=()=>{if(isActiveAndEnabled && Time.timeScale>0)action();};
            if(part.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>()==null)
                part.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
            if(part.GetComponent<VLAB.PhysicsLab.Interaction.XrSimpleInteractableBridge>()==null)
                part.AddComponent<VLAB.PhysicsLab.Interaction.XrSimpleInteractableBridge>();
        }
        protected TMP_Text Label(string name,string text,Vector3 position,float size=.07f)
        {
            var label=new GameObject(name).AddComponent<TextMeshPro>();
            label.transform.SetParent(transform,false);label.transform.localPosition=position;
            var font=Resources.Load<VLABMenuAssets>("VLABMenuAssets")?.activityFont;
            if(font!=null)label.font=font;
            if(textMaterial==null)
            {textMaterial=new Material(label.fontSharedMaterial);textMaterial.SetColor(ShaderUtilities.ID_FaceColor,Color.white);ownedMaterials.Add(textMaterial);}
            label.fontSharedMaterial=textMaterial;
            label.richText=true;
            label.text=text.Replace("₂","<sub>2</sub>").Replace("₃","<sub>3</sub>").Replace("₄","<sub>4</sub>");
            label.fontSize=size*10;label.color=new Color(.9f,.96f,1);
            label.alignment=TextAlignmentOptions.Center;label.rectTransform.sizeDelta=new Vector2(1.3f,.35f);
            label.textWrappingMode=TextWrappingModes.Normal;label.raycastTarget=false;
            return label;
        }
        protected virtual void OnDestroy(){foreach(var material in ownedMaterials)if(material!=null)Destroy(material);}
    }
}
