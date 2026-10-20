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
            public RectTransform rect;public Transform parent;public Vector2 position,size,min,max,pivot;public Vector3 scale;public int sibling;public Rect uv;public bool hasPhoto;
        }
        readonly List<Saved> saved=new List<Saved>();VlabUi ui;RectTransform box,viewport,body;ScrollRect scroll;Saved frame;
        bool captured,flat;Rect previousSafe;float previousUnits;RenderMode previousMode;
        public void Initialize(VlabUi owner,RectTransform panel){ui=owner;box=panel;}
        static Saved Capture(RectTransform rect)=>new Saved{rect=rect,parent=rect.parent,position=rect.anchoredPosition,size=rect.sizeDelta,min=rect.anchorMin,max=rect.anchorMax,pivot=rect.pivot,scale=rect.localScale,sibling=rect.GetSiblingIndex(),hasPhoto=rect.GetComponent<RawImage>(),uv=rect.GetComponent<RawImage>()?rect.GetComponent<RawImage>().uvRect:default};
        void LateUpdate()
        {
            if(ui==null||!box)return;
            if(!captured)
            {frame=Capture(box);foreach(var rect in box.GetComponentsInChildren<RectTransform>(true))if(rect!=box&&rect.name!="Opaque text backing")saved.Add(Capture(rect));captured=true;}
            var safe=ui.PhoneViewport.SafePixels;float units=Mathf.Max(.001f,ui.PhoneViewport.PixelsPerLogicalUnit);bool phone=ui.canvas.renderMode!=RenderMode.WorldSpace;
            if(phone==flat&&safe==previousSafe&&units==previousUnits&&previousMode==ui.canvas.renderMode)return;
            Restore();flat=phone;previousSafe=safe;previousUnits=units;previousMode=ui.canvas.renderMode;
            if(phone)Build(safe,units);
        }
        RectTransform Find(string name)=>saved.FirstOrDefault(x=>x.rect&&x.rect.name==name)?.rect;
        static void Place(RectTransform rect,float x,float y,float width,float height)
        {if(!rect)return;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(width,height);rect.localScale=Vector3.one;}
        void Build(Rect safe,float units)
        {
            const float scale=1.5f,header=100,footer=96,gap=14;
            float safeWidth=safe.width/units,safeHeight=safe.height/units,width=Mathf.Min(1260,(safeWidth-48)/scale),height=(safeHeight-48)/scale;
            var fitted=ui.PhoneViewport.ContentPixels;float left=(safe.xMin-fitted.xMin)/units,top=(fitted.yMax-safe.yMax)/units;
            Place(box,left+(safeWidth-width*scale)/2,top+24,width,height);box.localScale=Vector3.one*scale;
            var home=Find("Browser home");Place(home,22,18,220,66);StretchCaption(home);
            var subject=saved.FirstOrDefault(x=>x.parent==box&&x.rect.GetComponent<TMP_Text>()&&x.rect.name!="Browser lesson count"&&x.rect.name!="Browser selected title"&&x.rect.name!="Browser selected goal")?.rect;
            Place(subject,264,20,width-460,64);Place(Find("Browser lesson count"),width-174,28,152,52);
            var study=Find("Browser study");var start=Find("Browser start");float actionWidth=(width-58)/2;
            Place(study,22,height-82,actionWidth,68);StretchCaption(study);Place(start,36+actionWidth,height-82,actionWidth,68);StretchCaption(start);
            float listWidth=(width-66)*.61f,rightX=listWidth+44,rightWidth=width-rightX-22,viewHeight=height-header-footer;
            var go=new GameObject("Phone browser lesson viewport",typeof(RectTransform),typeof(Image),typeof(RectMask2D),typeof(ScrollRect));viewport=(RectTransform)go.transform;viewport.SetParent(box,false);Place(viewport,22,header,listWidth,viewHeight);go.GetComponent<Image>().color=Color.clear;
            var content=new GameObject("Phone browser equal lesson tiles",typeof(RectTransform));body=(RectTransform)content.transform;body.SetParent(viewport,false);
            var lessons=saved.Where(x=>x.parent==box&&x.rect.GetComponent<Button>()&&x.rect.name.StartsWith("Browser lesson ",System.StringComparison.Ordinal)).OrderBy(x=>x.rect.name,System.StringComparer.Ordinal).ToArray();
            float tileWidth=(listWidth-gap)/2,tileHeight=112;
            for(int i=0;i<lessons.Length;i++)
            {lessons[i].rect.SetParent(body,false);Place(lessons[i].rect,i%2*(tileWidth+gap),i/2*(tileHeight+gap),tileWidth,tileHeight);StretchCaption(lessons[i].rect);}
            float contentHeight=Mathf.Ceil(lessons.Length/2f)*(tileHeight+gap)-gap;Place(body,0,0,listWidth,Mathf.Max(1,contentHeight));
            scroll=go.GetComponent<ScrollRect>();scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=42;scroll.viewport=viewport;scroll.content=body;
            var selected=Find("Browser selected title");Place(selected,rightX,header,rightWidth,78);
            var preview=Find("Browser explore");Place(preview,rightX,header+90,rightWidth,184);
            if(preview)
            {
                var photo=preview.GetComponentInChildren<RawImage>();if(photo){Place(photo.rectTransform,0,0,rightWidth,184);if(photo.texture){float crop=Mathf.Min(1,(float)photo.texture.width/photo.texture.height/(rightWidth/184));photo.uvRect=new Rect(0,(1-crop)/2,1,crop);}}
                foreach(RectTransform child in preview)
                {
                    if(child.GetComponent<TMP_Text>())Place(child,12,134,rightWidth-24,44);
                    else if(child.name=="Section rule")Place(child,0,128,rightWidth,56);
                }
            }
            Place(Find("Browser selected goal"),rightX,header+288,rightWidth,Mathf.Max(90,viewHeight-288));
            foreach(var divider in saved.Where(x=>x.parent==box&&x.rect.name=="Section rule"))Place(divider.rect,listWidth+32,header,1,viewHeight);
            // Rebuilding after selecting a lower lesson keeps that actual selected tile visible.
            string title=selected?selected.GetComponent<TMP_Text>().text:"";
            int selectedIndex=System.Array.FindIndex(lessons,x=>x.rect.GetComponentInChildren<TMP_Text>().text.StartsWith(title+"\n",System.StringComparison.Ordinal));
            float selectedBottom=(Mathf.Max(0,selectedIndex)/2)*(tileHeight+gap)+tileHeight;
            float offset=Mathf.Clamp(selectedBottom-viewHeight,0,Mathf.Max(0,contentHeight-viewHeight));
            scroll.verticalNormalizedPosition=contentHeight>viewHeight?1-offset/(contentHeight-viewHeight):1;
        }
        static void StretchCaption(RectTransform button)
        {if(!button)return;var text=button.GetComponentInChildren<TMP_Text>();if(!text)return;var rect=text.rectTransform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=new Vector2(10,6);rect.offsetMax=new Vector2(-10,-6);}
        static void Reset(Saved value)
        {if(value==null||!value.rect)return;value.rect.SetParent(value.parent,false);value.rect.SetSiblingIndex(value.sibling);value.rect.anchorMin=value.min;value.rect.anchorMax=value.max;value.rect.pivot=value.pivot;value.rect.anchoredPosition=value.position;value.rect.sizeDelta=value.size;value.rect.localScale=value.scale;if(value.hasPhoto&&value.rect.GetComponent<RawImage>())value.rect.GetComponent<RawImage>().uvRect=value.uv;}
        void Restore()
        {foreach(var value in saved)Reset(value);if(viewport){viewport.gameObject.SetActive(false);Destroy(viewport.gameObject);}viewport=body=null;scroll=null;Reset(frame);}
        public bool AssertPhoneLayout(out string failure)
        {
            failure="";if(!captured||!flat)return true;
            if(!scroll||!body||viewport.rect.height<=0){failure="Browser lesson list is unavailable.";return false;}
            Vector2 tile=Vector2.zero;
            foreach(RectTransform child in body)
            {if(tile==Vector2.zero)tile=child.sizeDelta;if(tile!=child.sizeDelta){failure="Browser lesson tile sizes differ.";return false;}if(child.anchoredPosition.x+child.rect.width>body.rect.width+.1f){failure="Browser lesson exceeds list width.";return false;}}
            return true;
        }
    }
}


