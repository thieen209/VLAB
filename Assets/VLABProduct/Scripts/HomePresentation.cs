using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vlab.Product
{
    public static class HomePresentation
    {
        static Sprite disc,gear;
        static Sprite Disc()
        {
            if(disc)return disc;const int size=128;var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){name="Home circular utility",filterMode=FilterMode.Bilinear};
            for(int y=0;y<size;y++)for(int x=0;x<size;x++){float distance=Vector2.Distance(new Vector2(x+.5f,y+.5f),Vector2.one*64);texture.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(1.5f-Mathf.Abs(distance-61.5f))));}
            texture.Apply(false,true);disc=Sprite.Create(texture,new Rect(0,0,size,size),Vector2.one*.5f);return disc;
        }
        static Sprite Gear()
        {
            if(gear)return gear;const int size=128;var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){name="Home settings gear",filterMode=FilterMode.Bilinear};
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                var p=new Vector2((x+.5f-64)*.5f,(y+.5f-64)*.5f);float distance=p.magnitude-18;
                for(int tooth=0;tooth<8;tooth++){float angle=tooth*Mathf.PI/4;var radial=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));var tangential=new Vector2(-radial.y,radial.x);var q=new Vector2(Mathf.Abs(Vector2.Dot(p,tangential))-3.5f,Mathf.Abs(Vector2.Dot(p,radial)-22)-6);float box=Vector2.Max(q,Vector2.zero).magnitude+Mathf.Min(Mathf.Max(q.x,q.y),0);distance=Mathf.Min(distance,box);}
                float stroke=Mathf.Min(Mathf.Abs(distance)-1.15f,Mathf.Abs(p.magnitude-7.5f)-1.15f);texture.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(.5f-stroke)));
            }
            texture.Apply(false,true);gear=Sprite.Create(texture,new Rect(0,0,size,size),Vector2.one*.5f);return gear;
        }
        public static void Backdrop(VlabUi ui,RectTransform page)
        {
            var veil=ui.Rect(page,"Home room veil",-2000,-2000,5600,5000).gameObject.AddComponent<Image>();veil.color=new Color(.025f,.045f,.075f,.52f);veil.raycastTarget=false;
        }
        public static RectTransform Card(VlabUi ui,RectTransform page,string name,string imageName,string title,string description,Color accent,int index)
        {
            var card=ui.Panel(page,name,192+index*316,252,268,432,Color.clear);var opacity=card.GetComponent<VlabPanelOpacity>();opacity.Solid=false;opacity.FlatAlpha=0;
            var edge=card.GetComponent<Outline>();edge.effectColor=accent;edge.effectDistance=new Vector2(1,-1);edge.enabled=false;
            // Retain the focus contract, but render an open vector frame instead of
            // a full outline. The subject's strokes interrupt its lower/right edges.
            var plate=ui.Rect(card,"Cut portal plate",0,0,268,432).gameObject.AddComponent<SciencePortalGraphic>();plate.ConfigurePlate(index,VlabUi.Hex("243342"),edge,accent);
            var frame=ui.Rect(card,"Interrupted portal frame",0,0,268,432).gameObject.AddComponent<SciencePortalGraphic>();frame.Configure(index,VlabUi.Hex("84969F"),true,edge);
            var shape=ui.Rect(card,"Scientific subject shape",0,0,268,432).gameObject.AddComponent<SciencePortalGraphic>();shape.Configure(index,VlabUi.Hex("E9E4D9"));
            var heading=ui.Text(card,title,16,20,236,60,36,null,TextAlignmentOptions.Top);var titleFont=Resources.Load<TMP_FontAsset>("UI/VlabBodyStrong");if(!titleFont)titleFont=Resources.Load<TMP_FontAsset>("UI/VlabTitle");
            if(titleFont){heading.font=titleFont;heading.fontStyle=FontStyles.Normal;heading.fontWeight=FontWeight.Regular;}else{heading.fontStyle=FontStyles.Bold;heading.fontWeight=FontWeight.Bold;}
            heading.color=VlabUi.Hex("F3EFE6");heading.name="Home subject title";heading.GetComponent<VlabTextBacking>().enabled=false;
            card.pivot=Vector2.one*.5f;card.anchoredPosition+=new Vector2(134,-216);
            return card;
        }
        public static Button Utility(VlabUi ui,RectTransform page,string label,int index,Action action)
        {
            var button=ui.Button(page,label,512+index*152,770,120,120,action);button.name="Home utility "+index;
            var host=button.GetComponent<Image>();host.color=Color.clear;var opacity=button.GetComponent<VlabPanelOpacity>();opacity.Solid=false;opacity.FlatAlpha=0;foreach(var effect in button.GetComponents<Shadow>())effect.enabled=false;
            var face=ui.Rect(button.transform,"Circular utility disc",18,0,84,84).gameObject.AddComponent<Image>();face.sprite=Disc();face.color=VlabUi.Hex("F1F5F9");face.raycastTarget=true;
            // Keep the full transparent120px hit area, including its label, usable in VR.
            // Image's default alpha threshold is already zero. Setting it explicitly
            // validates CPU readability and throws for this intentionally unreadable texture.
            button.targetGraphic=face;
            var outline=face.gameObject.AddComponent<Outline>();outline.effectColor=VlabUi.Hex("101D32");outline.effectDistance=new Vector2(.5f,-.5f);
            var caption=button.GetComponentInChildren<TextMeshProUGUI>();caption.rectTransform.anchoredPosition=new Vector2(0,-86);caption.rectTransform.sizeDelta=new Vector2(120,34);caption.alignment=TextAlignmentOptions.Center;caption.fontStyle=FontStyles.Normal;caption.color=VlabUi.Hex("F1F5F9");
            UtilityIcon(ui,face.transform,index);return button;
        }
        public static void UtilityIcon(VlabUi ui,Transform parent,int kind)
        {
            Color ink=VlabUi.Hex("F1F5F9");
            if(kind==0)
            {
                var icon=ui.Rect(parent,"Settings gear icon",16,16,52,52).gameObject.AddComponent<Image>();icon.sprite=Gear();icon.color=ink;icon.raycastTarget=false;
            }
            else if(kind<3)
            {
                var text=ui.Text(parent,kind==1?"?":"i",12,12,60,60,36,ink,TextAlignmentOptions.Center);text.color=ink;text.fontStyle=FontStyles.Bold;text.GetComponent<VlabTextBacking>().enabled=false;
            }
            else if(kind==4)
            {
                ui.Rule(parent,20,29,44,3,ink);ui.Rule(parent,20,29,3,24,ink);ui.Rule(parent,61,29,3,24,ink);
                ui.Rule(parent,20,51,17,3,ink);ui.Rule(parent,47,51,17,3,ink);ui.Rule(parent,37,45,10,3,ink);
                ui.Rule(parent,35,45,3,9,ink);ui.Rule(parent,46,45,3,9,ink);
                ui.Rule(parent,12,34,8,3,ink);ui.Rule(parent,64,34,8,3,ink);
            }
            else
            {
                ui.Rule(parent,26,21,34,2,ink);ui.Rule(parent,26,61,34,2,ink);ui.Rule(parent,26,21,2,42,ink);ui.Rule(parent,58,21,2,42,ink);ui.Rule(parent,33,23,2,38,ink);
                for(int row=0;row<3;row++){ui.Rule(parent,22,29+row*12,8,2,ink);ui.Rule(parent,39,31+row*9,14,2,ink);}
            }
        }
        public static void Helper(VlabUi ui,RectTransform page,string text,float top)
        {
            var caption=ui.Text(page,text,400,top,800,32,18,null,TextAlignmentOptions.Center);caption.name="Home selection hint";caption.color=VlabUi.Hex("CAD5DF");caption.GetComponent<VlabTextBacking>().enabled=false;
        }
    }
    public sealed class HomePortalArc:MonoBehaviour
    {
        public RectTransform[] Cards;Canvas canvas;RenderMode? previous;Vector3[] rest;
        void LateUpdate()
        {
            if(!canvas)canvas=GetComponentInParent<Canvas>();if(!canvas||previous==canvas.renderMode)return;previous=canvas.renderMode;
            if(rest==null){rest=new Vector3[Cards.Length];for(int i=0;i<Cards.Length;i++)rest[i]=Cards[i].anchoredPosition3D;}
            for(int index=0;index<Cards.Length;index++)
            {
                var card=Cards[index];card.localRotation=Quaternion.identity;card.anchoredPosition3D=rest[index];
            }
        }
    }
}
