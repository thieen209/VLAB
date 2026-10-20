using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vlab.Product
{
    public sealed class VlabUi
    {
        public readonly Canvas canvas;public readonly RectTransform surface;
        public TMP_FontAsset font;
        public bool LargeText,Vietnamese=true;
        public bool ReducedMotion{get;private set;}
        public PhoneUiViewport PhoneViewport{get;private set;}
        public void RefreshPhoneLayout(){if(PhoneViewport)PhoneViewport.Refresh();}
        public void SetReducedMotion(bool reduced)
        {
            ReducedMotion=reduced;
            foreach(var entrance in canvas.GetComponentsInChildren<UiEntrance>(true))entrance.SetReducedMotion(reduced);
            foreach(var feedback in canvas.GetComponentsInChildren<UiButtonMotion>(true))feedback.SetReducedMotion(reduced);
        }
        public static Color PanelColor=>UiTheme.Current.panel;public static Color Raised=>UiTheme.Current.raised;public static Color Stroke=>UiTheme.Current.stroke;public static Color TextColor=>UiTheme.Current.text;public static Color Secondary=>UiTheme.Current.secondary;
        public static Color Hex(string value){ColorUtility.TryParseHtmlString("#"+value,out var c);return c;}
        static Sprite rounded;
        static Sprite pointerRing;
        public static Sprite PointerRing()
        {
            if(pointerRing)return pointerRing;
            const int n=64;var texture=new Texture2D(n,n,TextureFormat.RGBA32,false);texture.name="VLAB pointer ring";
            for(int y=0;y<n;y++)for(int x=0;x<n;x++){float radius=Vector2.Distance(new Vector2(x+.5f,y+.5f),Vector2.one*(n*.5f));float alpha=Mathf.Clamp01(Mathf.Min(radius-22,28-radius));texture.SetPixel(x,y,new Color(1,1,1,alpha));}
            texture.Apply();pointerRing=Sprite.Create(texture,new Rect(0,0,n,n),Vector2.one*.5f);return pointerRing;
        }
        static Sprite Rounded()
        {
            if(rounded)return rounded;
            var source=Resources.Load<Texture2D>("UI/Skin/RoundedPanel");
            if(source){rounded=Sprite.Create(source,new Rect(0,0,source.width,source.height),Vector2.one*.5f,100,0,SpriteMeshType.FullRect,new Vector4(source.width*.15f,source.height*.15f,source.width*.15f,source.height*.15f));return rounded;}
            const int n=64;var tex=new Texture2D(n,n,TextureFormat.RGBA32,false);tex.name="VLAB rounded control";
            for(int y=0;y<n;y++)for(int x=0;x<n;x++){float dx=Mathf.Max(12-x,0,x-(n-13)),dy=Mathf.Max(12-y,0,y-(n-13));tex.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(12.5f-Mathf.Sqrt(dx*dx+dy*dy))));}
            tex.Apply();rounded=Sprite.Create(tex,new Rect(0,0,n,n),Vector2.one*.5f,100,0,SpriteMeshType.FullRect,new Vector4(14,14,14,14));return rounded;
        }
        public VlabUi(Transform parent)
        {
            var go=new GameObject("VLAB Interface",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));go.transform.SetParent(parent,false);
            canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=10;
            var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,1000);scaler.matchWidthOrHeight=.5f;
            surface=new GameObject("Safe Area",typeof(RectTransform)).GetComponent<RectTransform>();surface.SetParent(go.transform,false);surface.anchorMin=Vector2.zero;surface.anchorMax=Vector2.one;surface.offsetMin=surface.offsetMax=Vector2.zero;
            font=Resources.Load<TMP_FontAsset>("UI/VlabBody");
            if(!font)font=Resources.Load<TMP_FontAsset>("UI/VlabInter");
            if(!font){var source=Resources.Load<Font>("UI/Inter-Regular");if(source)font=TMP_FontAsset.CreateFontAsset(source,64,8,UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,2048,2048,AtlasPopulationMode.Dynamic);}
            PhoneViewport=go.AddComponent<PhoneUiViewport>();PhoneViewport.Initialize(canvas,surface);RefreshPhoneLayout();
            go.AddComponent<PhoneJoystickView>().Initialize(canvas,PhoneViewport);
        }
        public RectTransform Page(string name)
        {
            var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(surface,false);rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;return rect;
        }
        public RectTransform Rect(Transform parent,string name,float x,float y,float width,float height)
        {
            var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(width,height);return rect;
        }
        public RectTransform Panel(Transform parent,string name,float x,float y,float width,float height,Color? color=null)
        {
            var rect=Rect(parent,name,x,y,width,height);var image=rect.gameObject.AddComponent<UnityEngine.UI.Image>();image.sprite=Rounded();image.type=UnityEngine.UI.Image.Type.Sliced;image.pixelsPerUnitMultiplier=image.sprite.rect.width>64?image.sprite.rect.width/200f:1;image.color=color??PanelColor;image.raycastTarget=true;
            var edge=rect.gameObject.AddComponent<UnityEngine.UI.Outline>();edge.enabled=false;edge.effectColor=new Color(Stroke.r,Stroke.g,Stroke.b,.65f);edge.effectDistance=new Vector2(2,-2);
            if(!parent.GetComponent<VlabPanelOpacity>()){var shadow=rect.gameObject.AddComponent<UnityEngine.UI.Shadow>();shadow.effectColor=new Color(0,0,0,.2f);shadow.effectDistance=new Vector2(0,-6);}
            var opacity=rect.gameObject.AddComponent<VlabPanelOpacity>();opacity.FlatAlpha=image.color.a;opacity.Apply(canvas.renderMode==RenderMode.WorldSpace);
            if(parent.parent==surface)rect.gameObject.AddComponent<UiEntrance>().Initialize(ReducedMotion);
            return rect;
        }
        public RectTransform Rule(Transform parent,float x,float y,float width,float height,Color color)
        {var rect=Rect(parent,"Section rule",x,y,width,height);var graphic=rect.gameObject.AddComponent<UnityEngine.UI.Image>();graphic.color=color;graphic.raycastTarget=false;return rect;}
        public TextMeshProUGUI Text(Transform parent,string content,float x,float y,float width,float height,int size=22,Color? color=null,TextAlignmentOptions alignment=TextAlignmentOptions.TopLeft)
        {
            var rect=Rect(parent,"Text",x,y,width,height);var text=rect.gameObject.AddComponent<TextMeshProUGUI>();text.font=font;text.text=content;text.fontSize=LargeText?size*1.12f:size;text.color=color.HasValue?UiTheme.Readable(color.Value,PanelColor):TextColor;text.alignment=alignment;text.textWrappingMode=TextWrappingModes.Normal;text.raycastTarget=false;text.overflowMode=TextOverflowModes.Truncate;text.lineSpacing=4;
            var backing=rect.gameObject.AddComponent<VlabTextBacking>();backing.Target=text;return text;
        }
        public UnityEngine.UI.Button Button(Transform parent,string label,float x,float y,float width,float height,Action action,Color? accent=null)
        {
            var rect=Panel(parent,label,x,y,width,height,accent??Raised);var b=rect.gameObject.AddComponent<UnityEngine.UI.Button>();b.targetGraphic=rect.GetComponent<UnityEngine.UI.Image>();var face=(UnityEngine.UI.Image)b.targetGraphic;face.pixelsPerUnitMultiplier=face.sprite.rect.width>64?face.sprite.rect.width/120f:1;
            rect.GetComponent<VlabPanelOpacity>().Solid=true;rect.GetComponent<VlabPanelOpacity>().Apply(canvas.renderMode==RenderMode.WorldSpace);
            var c=b.colors;c.normalColor=Color.white;c.highlightedColor=new Color(1.14f,1.14f,1.14f);c.pressedColor=new Color(.75f,.8f,.9f);c.disabledColor=new Color(.5f,.5f,.5f);b.colors=c;
            b.onClick.AddListener(()=>action?.Invoke());var caption=Text(rect,label,12,6,width-24,height-12,22,null,TextAlignmentOptions.Center);caption.color=UiTheme.On(accent??Raised);caption.fontWeight=FontWeight.SemiBold;
            var outline=rect.GetComponent<UnityEngine.UI.Outline>();outline.effectColor=new Color(Stroke.r,Stroke.g,Stroke.b,.8f);outline.effectDistance=new Vector2(2,-2);outline.useGraphicAlpha=true;
            rect.gameObject.AddComponent<UiButtonMotion>().Initialize(b,caption.rectTransform,ReducedMotion);
            return b;
        }
        public RawImage Picture(Transform parent,Texture texture,float x,float y,float width,float height,bool contain=false)
        {
            var rect=Rect(parent,"Picture",x,y,width,height);var raw=rect.gameObject.AddComponent<RawImage>();raw.texture=texture;raw.raycastTarget=false;
            if(contain&&texture){var ar=rect.gameObject.AddComponent<AspectRatioFitter>();ar.aspectMode=AspectRatioFitter.AspectMode.FitInParent;ar.aspectRatio=(float)texture.width/texture.height;}
            return raw;
        }
        public RectTransform Scroll(Transform parent,string content,float x,float y,float width,float height,int size=22)
        {
            var host=Panel(parent,"Scrollable content",x,y,width,height,Raised);var scroll=host.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=45;
            float viewHeight=height-100;var viewport=Rect(host,"Viewport",12,12,width-24,viewHeight);viewport.gameObject.AddComponent<RectMask2D>();
            var body=Text(viewport,content,0,0,width-48,100,size);body.overflowMode=TextOverflowModes.Overflow;
            scroll.viewport=viewport;scroll.content=body.rectTransform;
            // Cardboard has a trigger, but no drag wheel. The same page controls are ray/dwell targets.
            var geometry=host.gameObject.AddComponent<VlabScrollContent>();geometry.Body=body;geometry.Scroll=scroll;
            geometry.Up=Button(host,Vietnamese?"Trang trước":"Page up",12,height-76,(width-40)/2,64,()=>geometry.Move(1));
            geometry.Down=Button(host,Vietnamese?"Trang sau":"Page down",28+(width-40)/2,height-76,(width-40)/2,64,()=>geometry.Move(-1));
            geometry.Refresh();return host;
        }
        public void SetWorld(bool world,Camera camera)
        {
            var scaler=canvas.GetComponent<CanvasScaler>();
            if(world)
            {
                canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;var rect=canvas.GetComponent<RectTransform>();rect.sizeDelta=new Vector2(1600,1000);rect.localScale=Vector3.one*.0016f;scaler.uiScaleMode=CanvasScaler.ScaleMode.ConstantPixelSize;canvas.GetComponent<GraphicRaycaster>().blockingObjects=GraphicRaycaster.BlockingObjects.None;Recenter(camera);
            }
            else{canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.transform.localScale=Vector3.one;scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,1000);}
            RefreshPhoneLayout();
            foreach(var opacity in canvas.GetComponentsInChildren<VlabPanelOpacity>(true))opacity.Apply(world);
        }
        public void Recenter(Camera camera)
        {if(canvas.renderMode!=RenderMode.WorldSpace)return;var dir=Vector3.ProjectOnPlane(camera.transform.forward,Vector3.up).normalized;if(dir.sqrMagnitude<.1f)dir=Vector3.forward;canvas.transform.position=camera.transform.position+dir*2.25f;canvas.transform.rotation=Quaternion.LookRotation(dir,Vector3.up);}
    }
    // Live instrument readings may add paragraphs after the scroll host was created.
    public sealed class VlabScrollContent:MonoBehaviour
    {
        public TextMeshProUGUI Body;public ScrollRect Scroll;public UnityEngine.UI.Button Up,Down;
        string measuredText;TMP_FontAsset measuredFont;float measuredSize=-1,measuredWidth=-1,measuredHeight=-1;
        void LateUpdate(){Refresh();}
        public void Refresh()
        {
            if(!Body||!Scroll||!Scroll.viewport)return;
            float width=Body.rectTransform.rect.width,height=Scroll.viewport.rect.height;
            if(measuredText==Body.text&&measuredFont==Body.font&&measuredSize==Body.fontSize&&measuredWidth==width&&measuredHeight==height)return;
            float offset=Body.rectTransform.anchoredPosition.y;
            float needed=Mathf.Max(height,Body.GetPreferredValues(Body.text,width,Mathf.Infinity).y+24);
            Body.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,needed);
            var position=Body.rectTransform.anchoredPosition;position.y=Mathf.Clamp(offset,0,Mathf.Max(0,needed-height));Body.rectTransform.anchoredPosition=position;
            if(Up)Up.interactable=needed>height+1;if(Down)Down.interactable=needed>height+1;
            measuredText=Body.text;measuredFont=Body.font;measuredSize=Body.fontSize;measuredWidth=width;measuredHeight=height;
        }
        public void Move(float direction)
        {
            Refresh();float height=Scroll.viewport.rect.height,range=Body.rectTransform.rect.height-height;Scroll.StopMovement();
            if(range>0)Scroll.verticalNormalizedPosition=Mathf.Clamp01(Scroll.verticalNormalizedPosition+direction*(height-32)/range);
        }
    }
    public sealed class VlabPanelOpacity:MonoBehaviour
    {
        public float FlatAlpha;public bool Solid;
        public void Apply(bool world){var graphic=GetComponent<UnityEngine.UI.Image>();var color=graphic.color;color.a=Solid?1:FlatAlpha*UiTheme.Opacity;graphic.color=color;}
    }
    // Only paragraph backings remain opaque at low panel opacity. Text and artwork never fade.
    public sealed class VlabTextBacking:MonoBehaviour
    {
        public TextMeshProUGUI Target;RectTransform plate;string measuredText;Vector2 measuredRect;float measuredFont;int measuredTheme=-1;VlabPanelOpacity parentPanel;
        void Start(){parentPanel=transform.parent?transform.parent.GetComponentInParent<VlabPanelOpacity>():null;}
        void LateUpdate()
        {
            if(!Target)return;
            bool freeWorldText=!parentPanel&&GetComponentInParent<Canvas>().renderMode==RenderMode.WorldSpace;
            bool needed=(UiTheme.Opacity<.75f||freeWorldText)&&!string.IsNullOrEmpty(Target.text)&&(!parentPanel||!parentPanel.Solid);
            if(!needed){if(plate)plate.gameObject.SetActive(false);return;}
            if(!plate)
            {
                var go=new GameObject("Opaque text backing",typeof(RectTransform),typeof(UnityEngine.UI.Image));plate=go.GetComponent<RectTransform>();plate.SetParent(transform.parent,false);plate.anchorMin=Target.rectTransform.anchorMin;plate.anchorMax=Target.rectTransform.anchorMax;plate.pivot=Target.rectTransform.pivot;plate.SetSiblingIndex(transform.GetSiblingIndex());go.GetComponent<UnityEngine.UI.Image>().raycastTarget=false;
            }
            plate.gameObject.SetActive(true);plate.anchoredPosition=Target.rectTransform.anchoredPosition+new Vector2(-5,5);
            Vector2 bounds=Target.rectTransform.rect.size;
            if(measuredText!=Target.text||measuredRect!=bounds||measuredFont!=Target.fontSize||measuredTheme!=UiTheme.Index)
            {
                var size=Target.GetPreferredValues(Target.text,bounds.x,Mathf.Infinity);
                // Full-width bands also cover centered/right-aligned text and do not affect its layout.
                plate.sizeDelta=new Vector2(bounds.x+10,Mathf.Min(bounds.y,size.y)+10);
                plate.GetComponent<UnityEngine.UI.Image>().color=UiTheme.Current.panel;
                measuredText=Target.text;measuredRect=bounds;measuredFont=Target.fontSize;measuredTheme=UiTheme.Index;
            }
        }
        void OnDestroy(){if(plate)Destroy(plate.gameObject);}
    }
    public sealed class PortalFocus:MonoBehaviour,UnityEngine.EventSystems.IPointerEnterHandler,UnityEngine.EventSystems.IPointerExitHandler,UnityEngine.EventSystems.ISelectHandler,UnityEngine.EventSystems.IDeselectHandler
    {
        public Action<bool> Focus;public bool ReducedMotion;public Color FocusColor=Color.white;Vector3 target=Vector3.one;
        public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData e){target=Vector3.one*(ReducedMotion?1:1.035f);Border(true);Focus?.Invoke(true);}
        public void OnPointerExit(UnityEngine.EventSystems.PointerEventData e){target=Vector3.one;Border(false);Focus?.Invoke(false);}
        void Border(bool focused){var edge=GetComponent<UnityEngine.UI.Outline>();if(!edge)return;edge.effectColor=focused?FocusColor:new Color(FocusColor.r,FocusColor.g,FocusColor.b,.35f);edge.effectDistance=focused?new Vector2(3,-3):new Vector2(2,-2);}
        public void OnSelect(UnityEngine.EventSystems.BaseEventData e){OnPointerEnter(null);}
        public void OnDeselect(UnityEngine.EventSystems.BaseEventData e){OnPointerExit(null);}
        void Update(){if(ReducedMotion){target=Vector3.one;transform.localScale=Vector3.one;return;}transform.localScale=Vector3.Lerp(transform.localScale,target,1-Mathf.Exp(-Time.unscaledDeltaTime/0.04f));}
    }
}
