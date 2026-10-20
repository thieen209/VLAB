using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vlab.Product
{
    // The phone keeps navigation fixed while real settings rows scroll; VR keeps authored geometry.
    public sealed class PhoneSettingsLayout:MonoBehaviour
    {
        sealed class Saved
        {
            public RectTransform rect;public Transform parent;public Vector2 position,size,min,max,pivot;public Vector3 scale;public int sibling;
            public float Top=>-position.y;
        }
        readonly List<Saved> saved=new List<Saved>();
        readonly List<Saved> children=new List<Saved>();
        VlabUi ui;RectTransform box,viewport,body;ScrollRect scroll;Saved frame;
        bool captured,flat;Rect previousSafe;RenderMode previousMode;float previousUnits;
        public void Initialize(VlabUi value,RectTransform panel){ui=value;box=panel;}
        static Saved Capture(RectTransform rect)=>new Saved{rect=rect,parent=rect.parent,position=rect.anchoredPosition,size=rect.sizeDelta,min=rect.anchorMin,max=rect.anchorMax,pivot=rect.pivot,scale=rect.localScale,sibling=rect.GetSiblingIndex()};
        void LateUpdate()
        {
            if(ui==null||!box)return;
            if(!captured)
            {
                frame=Capture(box);
                foreach(var rect in box.GetComponentsInChildren<RectTransform>(true))
                {if(rect==box||rect.name=="Opaque text backing")continue;var value=Capture(rect);saved.Add(value);if(rect.parent==box)children.Add(value);}
                captured=true;
            }
            var safe=ui.PhoneViewport.SafePixels;float units=Mathf.Max(.001f,ui.PhoneViewport.PixelsPerLogicalUnit);bool phone=ui.canvas.renderMode!=RenderMode.WorldSpace;
            if(phone==flat&&previousSafe==safe&&previousUnits==units&&previousMode==ui.canvas.renderMode)return;
            float position=scroll?scroll.verticalNormalizedPosition:1;
            Restore();flat=phone;previousSafe=safe;previousUnits=units;previousMode=ui.canvas.renderMode;
            if(!phone)return;
            Build(safe,units,position);VlabUi.AddPhoneScrollbar(scroll);
        }
        RectTransform Create(string name,Transform parent)
        {var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);return (RectTransform)go.transform;}
        static void Place(RectTransform rect,float x,float y,float width,float height)
        {rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(width,height);rect.localScale=Vector3.one;}
        void Build(Rect safe,float units,float position)
        {
            const float scale=1.5f;float safeWidth=safe.width/units,safeHeight=safe.height/units;
            float width=Mathf.Min(1140,(safeWidth-48)/scale),height=(safeHeight-48)/scale;
            var fitted=ui.PhoneViewport.ContentPixels;float left=(safe.xMin-fitted.xMin)/units,top=(fitted.yMax-safe.yMax)/units;
            Place(box,left+(safeWidth-width*scale)/2,top+24,width,height);box.localScale=Vector3.one*scale;
            var title=children.FirstOrDefault(x=>x.rect.name=="Settings title");var back=children.FirstOrDefault(x=>x.rect.name=="Settings back");var rail=children.FirstOrDefault(x=>x.rect.name=="Settings category rail");
            if(title!=null)Place(title.rect,24,16,width-230,62);
            if(back!=null){Place(back.rect,width-202,14,178,66);StretchCaption(back.rect);}
            foreach(var hint in children.Where(x=>x.Top<90&&x!=title&&x!=back))Place(hint.rect,width-440,36,220,40);
            if(rail!=null)LayoutRail(rail.rect,width);
            var actions=children.Where(x=>x.Top>=670&&x.rect.GetComponent<Button>()).OrderBy(x=>x.position.x).ToArray();
            float actionWidth=(width-48-14*Mathf.Max(0,actions.Length-1))/Mathf.Max(1,actions.Length);
            for(int i=0;i<actions.Length;i++){Place(actions[i].rect,24+i*(actionWidth+14),height-88,actionWidth,72);StretchCaption(actions[i].rect);}
            foreach(var separator in children.Where(x=>x.Top>=650&&x.Top<670))Place(separator.rect,24,height-100,width-48,2);
            viewport=Create("Phone settings scroll viewport",box);Place(viewport,24,174,width-48,Mathf.Max(120,height-280));
            var hit=viewport.gameObject.AddComponent<Image>();hit.color=Color.clear;viewport.gameObject.AddComponent<RectMask2D>();
            body=Create("Phone settings content",viewport);Place(body,0,0,width-64,0);
            scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.viewport=viewport;scroll.content=body;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=42;
            var content=children.Where(x=>x.Top>=190&&x.Top<650).OrderBy(x=>x.Top).ThenBy(x=>x.position.x).ToList();
            float y=0,available=width-64;
            while(content.Count>0)
            {
                var row=new List<Saved>{content[0]};content.RemoveAt(0);float rowTop=row[0].Top,end=rowTop+row[0].size.y;
                while(content.Count>0&&content[0].Top<end-8)
                {var item=content[0];content.RemoveAt(0);row.Add(item);end=Mathf.Max(end,item.Top+item.size.y);}
                foreach(var item in row)item.rect.SetParent(body,false);
                var track=row.FirstOrDefault(x=>x.rect.name=="Settings value track");
                if(track!=null){LayoutValueRow(row,track,available,y);y+=148;continue;}
                float rowHeight=end-rowTop,factor=available/1004;
                foreach(var item in row)
                {
                    float x=Mathf.Max(0,item.position.x-48)*factor,w=Mathf.Min(available,item.size.x*factor),h=item.size.y;
                    var text=item.rect.GetComponent<TMP_Text>();if(text)h=Mathf.Max(h,text.GetPreferredValues(text.text,w,h).y+6);
                    Place(item.rect,x,y+item.Top-rowTop,w,h);LayoutRowDetails(item.rect,w);rowHeight=Mathf.Max(rowHeight,item.Top-rowTop+h);
                }
                y+=rowHeight+14;
            }
            body.sizeDelta=new Vector2(available,y);scroll.verticalNormalizedPosition=position;
        }
        static void LayoutRail(RectTransform rail,float width)
        {
            Place(rail,24,94,width-48,72);var tabs=rail.GetComponentsInChildren<Button>(true);float tabWidth=(width-48)/Mathf.Max(1,tabs.Length);
            for(int i=0;i<tabs.Length;i++)
            {
                var tab=(RectTransform)tabs[i].transform;Place(tab,i*tabWidth,0,tabWidth,72);StretchCaption(tab);
                var selected=tab.Find("Settings active category surface") as RectTransform;if(selected)Place(selected,3,4,tabWidth-6,60);
            }
            foreach(RectTransform child in rail)
            {
                if(child.name=="Settings category baseline")Place(child,0,70,width-48,2);
                if(child.name=="Settings selected category")
                {float index=Mathf.Round((child.anchoredPosition.x-18)/(1036f/6));Place(child,index*tabWidth+18,68,tabWidth-36,4);}
            }
        }
        static void LayoutValueRow(List<Saved> row,Saved track,float width,float top)
        {
            foreach(var item in row)
            {
                if(item.rect.GetComponent<TMP_Text>()){Place(item.rect,0,top,width,56);continue;}
                if(item.rect.GetComponent<Button>()){Place(item.rect,item.position.x<800?0:width-144,top+62,144,72);StretchCaption(item.rect);continue;}
                float fraction=item.size.x/192;Place(item.rect,160,top+92,(width-320)*fraction,12);
            }
        }
        static void LayoutRowDetails(RectTransform rect,float width)
        {
            if(!rect.GetComponent<Button>())return;
            var track=rect.Find("Settings switch track") as RectTransform;var language=rect.Find("Settings language value") as RectTransform;
            if(track)Place(track,width-136,15,124,42);if(language)Place(language,width-136,12,124,48);
            var caption=rect.GetComponentInChildren<TMP_Text>();if(!caption)return;
            if(track||language)Place(caption.rectTransform,0,10,Mathf.Max(80,width-160),52);
            else
            {
                float left=caption.rectTransform.anchoredPosition.x;Place(caption.rectTransform,left,-caption.rectTransform.anchoredPosition.y,Mathf.Max(80,width-left-12),caption.rectTransform.sizeDelta.y);
            }
        }
        static void StretchCaption(RectTransform button)
        {var text=button.GetComponentInChildren<TMP_Text>();if(!text)return;var rect=text.rectTransform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=new Vector2(8,5);rect.offsetMax=new Vector2(-8,-5);}
        static void Reset(Saved value)
        {if(!value.rect)return;value.rect.SetParent(value.parent,false);value.rect.SetSiblingIndex(value.sibling);value.rect.anchorMin=value.min;value.rect.anchorMax=value.max;value.rect.pivot=value.pivot;value.rect.anchoredPosition=value.position;value.rect.sizeDelta=value.size;value.rect.localScale=value.scale;}
        void Restore()
        {
            foreach(var value in saved)Reset(value);if(viewport){viewport.gameObject.SetActive(false);Destroy(viewport.gameObject);}viewport=body=null;scroll=null;Reset(frame);
        }
        public bool AssertPhoneLayout(out string failure)
        {
            failure="";if(!captured||!flat)return true;
            if(!scroll||scroll.content!=body||viewport.rect.height<=0){failure="Settings scrolling content is unavailable.";return false;}
            foreach(RectTransform child in body)
                if(child.anchoredPosition.x<-.1f||child.anchoredPosition.x+child.rect.width>body.rect.width+.1f){failure="Settings row exceeds available width: "+child.name;return false;}
            return true;
        }
    }
}

