using System;
using UnityEngine;
using UnityEngine.UI;

namespace VLAB.MainMenu
{
    public sealed class VLABUI
    {
        public static readonly Color Background = new Color32(13,17,23,255);
        public static readonly Color Surface = new Color32(20,34,48,255);
        public static readonly Color Cyan = new Color32(0,180,216,255);
        public static readonly Color White = new Color32(248,249,250,255);
        public static readonly Color Muted = new Color32(171,194,207,255);
        private readonly Font font;
        private static Sprite rounded;
        private static Sprite ribbon;
        public VLABUI(Font font) { this.font = font; }
        public RectTransform Rect(Transform parent, string name, float x, float y, float w, float h)
        {
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            r.SetParent(parent, false);
            r.gameObject.layer=parent.gameObject.layer;
            r.anchorMin = new Vector2(x,y); r.anchorMax = new Vector2(x+w,y+h);
            r.offsetMin = r.offsetMax = Vector2.zero;
            return r;
        }
        public Image Panel(Transform parent, string name, float x,float y,float w,float h,Color color)
        {
            var image = Rect(parent,name,x,y,w,h).gameObject.AddComponent<Image>();
            image.color=color; image.raycastTarget=false;
            if(color==Surface) { image.sprite=Rounded();image.type=Image.Type.Sliced; }
            return image;
        }
        public void Ribbon(Transform parent,string title,float x,float y,float w,float h)
        {
            if(ribbon==null)
            {
                var texture=new Texture2D(128,1,TextureFormat.RGBA32,false){name="VLAB cyan ribbon",hideFlags=HideFlags.HideAndDontSave};
                for(int i=0;i<128;i++)texture.SetPixel(i,0,Color.Lerp(Cyan,new Color(.025f,.08f,.15f,.15f),i/127f));
                texture.Apply(false,true);
                ribbon=Sprite.Create(texture,new Rect(0,0,128,1),new Vector2(.5f,.5f));
                ribbon.hideFlags=HideFlags.HideAndDontSave;
            }
            var image=Panel(parent,"SectionRibbon",x,y,w,h,Color.white);image.sprite=ribbon;
            Text(image.transform,"Heading",title,0,0,1,1,31,White,TextAnchor.MiddleCenter).fontStyle=FontStyle.BoldAndItalic;
        }
        public Text Text(Transform parent,string name,string value,float x,float y,float w,float h,int size,Color? color=null,TextAnchor align=TextAnchor.MiddleLeft)
        {
            var label=Rect(parent,name,x,y,w,h).gameObject.AddComponent<Text>();
            label.font=font; label.text=value; label.fontSize=size;
            label.color=color??White; label.alignment=align;
            label.horizontalOverflow=HorizontalWrapMode.Wrap;
            label.verticalOverflow=VerticalWrapMode.Truncate;
            label.raycastTarget=false; label.supportRichText=false;
            return label;
        }
        public Button Button(Transform parent,string name,string value,float x,float y,float w,float h,Action action,bool primary=false)
        {
            var image=Panel(parent,name,x,y,w,h,primary?Cyan:Surface);
            image.sprite=Rounded();image.type=Image.Type.Sliced;
            image.raycastTarget=true;
            var b=image.gameObject.AddComponent<Button>();
            image.gameObject.AddComponent<VLABMenuCancelRelay>();
            b.targetGraphic=image;
            var colors=b.colors;
            colors.normalColor=Color.white; colors.highlightedColor=new Color(1.2f,1.2f,1.2f);
            colors.selectedColor=colors.highlightedColor; colors.pressedColor=new Color(.65f,.8f,.9f);
            colors.disabledColor=new Color(.48f,.48f,.48f,.8f); colors.fadeDuration=.1f;
            b.colors=colors;
            var title=Text(image.transform,"Label",value,.035f,.04f,.93f,.92f,23,primary?Background:White,TextAnchor.MiddleCenter);
            title.fontStyle=FontStyle.Bold;
            Panel(image.transform,"Accent",0,0,1,.025f,primary?new Color(.38f,.88f,1):new Color(.2f,.4f,.5f));
            b.onClick.AddListener(()=>action?.Invoke());
            return b;
        }
        private static Sprite Rounded()
        {
            if(rounded!=null)return rounded;
            const int size=48;const float radius=10;
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){name="VLAB rounded surface",hideFlags=HideFlags.HideAndDontSave};
            var pixels=new Color32[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float dx=Mathf.Max(radius-x-.5f,x+.5f-(size-radius));
                float dy=Mathf.Max(radius-y-.5f,y+.5f-(size-radius));
                float distance=new Vector2(Mathf.Max(0,dx),Mathf.Max(0,dy)).magnitude;
                pixels[y*size+x]=new Color(1,1,1,Mathf.Clamp01(radius-distance+.5f));
            }
            texture.SetPixels32(pixels);texture.Apply(false,true);
            rounded=Sprite.Create(texture,new Rect(0,0,size,size),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(12,12,12,12));
            rounded.hideFlags=HideFlags.HideAndDontSave;
            return rounded;
        }
    }
}
