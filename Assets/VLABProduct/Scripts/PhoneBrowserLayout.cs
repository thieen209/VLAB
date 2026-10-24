using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vlab.Product
{
    // The lesson list scrolls independently of its selected preview and start actions.
    public sealed class PhoneBrowserLayout:MonoBehaviour
    {
        sealed class Saved
        {
            public RectTransform rect;public Transform parent;public Vector2 position,size,min,max,pivot;public Vector3 scale;public int sibling;public Rect uv;public bool hasPhoto;public float fontSize;
        }
        readonly List<Saved> saved=new List<Saved>();VlabUi ui;RectTransform box,viewport,body;ScrollRect scroll;Saved frame;
        float tourFooter;bool captured,flat;Rect previousSafe;float previousUnits;RenderMode previousMode;
        public void Initialize(VlabUi owner,RectTransform panel,float reservedTourFooter=0){ui=owner;box=panel;tourFooter=reservedTourFooter;}
        static Saved Capture(RectTransform rect)=>new Saved{rect=rect,parent=rect.parent,fontSize=rect.GetComponent<TMP_Text>()?rect.GetComponent<TMP_Text>().fontSize:0,position=rect.anchoredPosition,size=rect.sizeDelta,min=rect.anchorMin,max=rect.anchorMax,pivot=rect.pivot,scale=rect.localScale,sibling=rect.GetSiblingIndex(),hasPhoto=rect.GetComponent<RawImage>(),uv=rect.GetComponent<RawImage>()?rect.GetComponent<RawImage>().uvRect:default};
        void LateUpdate()
        {
            if(ui==null||!box)return;
            if(!captured)
            {frame=Capture(box);foreach(var rect in box.GetComponentsInChildren<RectTransform>(true))if(rect!=box&&rect.name!="Opaque text backing")saved.Add(Capture(rect));captured=true;}
            var safe=ui.PhoneViewport.SafePixels;float units=Mathf.Max(.001f,ui.PhoneViewport.PixelsPerLogicalUnit);bool phone=ui.canvas.renderMode!=RenderMode.WorldSpace;
            if(phone==flat&&safe==previousSafe&&units==previousUnits&&previousMode==ui.canvas.renderMode)return;
            Restore();flat=phone;previousSafe=safe;previousUnits=units;previousMode=ui.canvas.renderMode;
            if(phone){Build(safe,units);VlabUi.AddPhoneScrollbar(scroll);}
        }
        RectTransform Find(string name)=>saved.FirstOrDefault(x=>x.rect&&x.rect.name==name)?.rect;
        static void Place(RectTransform rect,float x,float y,float width,float height)
        {if(!rect)return;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(width,height);rect.localScale=Vector3.one;}
        void Build(Rect safe,float units)
        {
            const float scale=1.25f,header=100,footer=86,gap=8;
            float safeWidth=safe.width/units,safeHeight=safe.height/units,width=Mathf.Min(1440,(safeWidth-40)/scale),height=(safeHeight-40-tourFooter)/scale;
            bool stacked=safe.width<safe.height*1.15f;
            var fitted=ui.PhoneViewport.ContentPixels;float left=(safe.xMin-fitted.xMin)/units,top=(fitted.yMax-safe.yMax)/units;
            Place(box,left+(safeWidth-width*scale)/2,top+20,width,height);box.localScale=Vector3.one*scale;
            var home=Find("Browser home");Place(home,22,20,190,60);StretchCaption(home);
            Place(Find("Browser subject"),238,20,width-430,62);Place(Find("Browser lesson count"),width-166,28,144,44);
            float listWidth=stacked?width-44:(width-68)*.34f;
            float viewHeight=stacked?Mathf.Min(344,(height-header-footer)*.4f):height-header-footer;
            var go=new GameObject("Phone browser lesson viewport",typeof(RectTransform),typeof(Image),typeof(RectMask2D),typeof(ScrollRect));viewport=(RectTransform)go.transform;viewport.SetParent(box,false);Place(viewport,22,header,listWidth,viewHeight);go.GetComponent<Image>().color=Color.clear;
            var content=new GameObject("Phone browser equal lesson tiles",typeof(RectTransform));body=(RectTransform)content.transform;body.SetParent(viewport,false);
            var lessons=saved.Where(x=>x.parent==box&&x.rect.GetComponent<Button>()&&x.rect.name.StartsWith("Browser lesson ",System.StringComparison.Ordinal)).OrderBy(x=>x.rect.name,System.StringComparer.Ordinal).ToArray();
            int columns=stacked?2:1;float tileWidth=(listWidth-18-gap*(columns-1))/columns,tileHeight=Mathf.Clamp((viewHeight-gap*(8/columns-1))/(8/columns),58,78);
            for(int i=0;i<lessons.Length;i++)
            {
                var tile=lessons[i].rect;tile.SetParent(body,false);Place(tile,i%columns*(tileWidth+gap),i/columns*(tileHeight+gap),tileWidth,tileHeight);StretchCaption(tile);
                var caption=tile.GetComponentInChildren<TMP_Text>();caption.fontSize=ui.LargeText?27:24;caption.margin=new Vector4(8,0,25,0);
                Place(tile.Find("Browser selected mark") as RectTransform,tileWidth-34,(tileHeight-40)/2,26,40);
            }
            float contentHeight=Mathf.Ceil(lessons.Length/(float)columns)*(tileHeight+gap)-gap;Place(body,0,0,listWidth-18,Mathf.Max(1,contentHeight));
            scroll=go.GetComponent<ScrollRect>();scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=42;scroll.viewport=viewport;scroll.content=body;
            float rightX=stacked?22:listWidth+46,rightY=stacked?header+viewHeight+24:header,rightWidth=width-rightX-22;
            float detailsHeight=height-footer-rightY;
            Place(Find("Browser selected title"),rightX,rightY,rightWidth,60);
            float photoHeight=Mathf.Max(80,detailsHeight-184);var preview=Find("Browser apparatus preview");Place(preview,rightX,rightY+70,rightWidth,photoHeight);
            if(preview){var photo=preview.GetComponentInChildren<RawImage>();if(photo&&photo.texture){var fit=photo.GetComponent<AspectRatioFitter>();if(fit)fit.enabled=false;float w=Mathf.Min(rightWidth,photoHeight*photo.texture.width/photo.texture.height),h=w*photo.texture.height/photo.texture.width;Place(photo.rectTransform,(rightWidth-w)/2,(photoHeight-h)/2,w,h);}}
            Place(Find("Browser selected goal"),rightX,rightY+photoHeight+84,rightWidth,Mathf.Max(82,detailsHeight-photoHeight-84));
            var start=Find("Browser start");Place(start,rightX,height-74,rightWidth,60);StretchCaption(start);
            var explore=Find("Browser explore");Place(explore,22,height-74,stacked?0:listWidth,60);if(explore)explore.gameObject.SetActive(!stacked);StretchCaption(explore);
            foreach(var divider in saved.Where(x=>x.parent==box&&x.rect.name=="Section rule")){Place(divider.rect,stacked?22:listWidth+34,stacked?rightY-12:header,stacked?width-44:1,stacked?1:viewHeight);}
            string title=Find("Browser selected title")?.GetComponent<TMP_Text>().text;
            int selectedIndex=System.Array.FindIndex(lessons,x=>x.rect.GetComponentInChildren<TMP_Text>().text==title);
            float selectedBottom=(Mathf.Max(0,selectedIndex)/columns)*(tileHeight+gap)+tileHeight;
            float offset=Mathf.Clamp(selectedBottom-viewHeight,0,Mathf.Max(0,contentHeight-viewHeight));
            scroll.verticalNormalizedPosition=contentHeight>viewHeight?1-offset/(contentHeight-viewHeight):1;
        }
        static void StretchCaption(RectTransform button)
        {if(!button)return;var text=button.GetComponentInChildren<TMP_Text>();if(!text)return;var rect=text.rectTransform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=new Vector2(10,6);rect.offsetMax=new Vector2(-10,-6);}
        static void Reset(Saved value)
        {if(value==null||!value.rect)return;value.rect.SetParent(value.parent,false);value.rect.SetSiblingIndex(value.sibling);value.rect.anchorMin=value.min;value.rect.anchorMax=value.max;value.rect.pivot=value.pivot;value.rect.anchoredPosition=value.position;value.rect.sizeDelta=value.size;value.rect.localScale=value.scale;if(value.fontSize>0&&value.rect.GetComponent<TMP_Text>())value.rect.GetComponent<TMP_Text>().fontSize=value.fontSize;if(value.hasPhoto&&value.rect.GetComponent<RawImage>())value.rect.GetComponent<RawImage>().uvRect=value.uv;}
        void Restore()
        {foreach(var value in saved)Reset(value);if(viewport){viewport.gameObject.SetActive(false);Destroy(viewport.gameObject);}viewport=body=null;scroll=null;Reset(frame);var explore=Find("Browser explore");if(explore)explore.gameObject.SetActive(true);}
        public bool AssertPhoneLayout(out string failure)
        {
            failure="";if(!captured||!flat)return true;
            if(!scroll||!body||viewport.rect.height<=0){failure="Browser lesson list is unavailable.";return false;}
            var camera=ui.canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:ui.canvas.worldCamera;
            var corners=new Vector3[4];box.GetWorldCorners(corners);var safe=ui.PhoneViewport.SafePixels;
            foreach(var corner in corners)
            {
                var point=RectTransformUtility.WorldToScreenPoint(camera,corner);
                if(point.x<safe.xMin-1||point.x>safe.xMax+1||point.y<safe.yMin-1||point.y>safe.yMax+1)
                {failure="Browser panel exceeds the actual safe screen.";return false;}
            }
            if(!scroll.verticalScrollbar||scroll.verticalScrollbar.direction!=Scrollbar.Direction.BottomToTop){failure="Browser has no functional scroll-position control.";return false;}
            Vector2 tile=Vector2.zero;
            foreach(RectTransform child in body)
            {if(tile==Vector2.zero)tile=child.sizeDelta;if(tile!=child.sizeDelta){failure="Browser lesson tile sizes differ.";return false;}if(child.anchoredPosition.x+child.rect.width>body.rect.width+.1f){failure="Browser lesson exceeds list width.";return false;}}
            return true;
        }
    }
}


