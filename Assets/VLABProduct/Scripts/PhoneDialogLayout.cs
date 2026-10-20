using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vlab.Product
{
    // Dialogs reflow inside a scrolling phone sheet. Authored world-space VR geometry is restored verbatim.
    public sealed class PhoneDialogLayout:MonoBehaviour
    {
        sealed class Item
        {
            public RectTransform rect;public Transform parent;public Vector2 position,size,anchorMin,anchorMax,pivot;public Vector3 scale;public int sibling;public float fontSize;public bool hasText;
            public float Top=>-position.y;
        }
        readonly List<Item> items=new List<Item>();
        readonly List<Item> originals=new List<Item>();
        readonly List<VlabScrollContent> nativeDocuments=new List<VlabScrollContent>();
        VlabUi ui;RectTransform dialog,viewport,ownedViewport,body,footer,readingOutput;ScrollRect scroll;
        float readingWidth,readingHeight;
        Vector2 authoredSize,authoredPosition,authoredMin,authoredMax,authoredPivot;Vector3 authoredScale;bool captured,phone;
        float lastWidth,lastHeight,restoredScroll=1;Rect lastSafe;RenderMode lastMode;
        public bool ParameterPanel,CompactControls;
        public float ScrollPosition=>scroll?scroll.verticalNormalizedPosition:restoredScroll;
        public void RestoreScroll(float value){restoredScroll=Mathf.Clamp01(value);if(scroll)scroll.verticalNormalizedPosition=restoredScroll;}
        public void Initialize(VlabUi owner){ui=owner;dialog=(RectTransform)transform;}
        void LateUpdate()
        {
            if(ui==null)return;
            if(!captured)
            {
                authoredSize=dialog.sizeDelta;authoredPosition=dialog.anchoredPosition;authoredScale=dialog.localScale;authoredMin=dialog.anchorMin;authoredMax=dialog.anchorMax;authoredPivot=dialog.pivot;
                foreach(var child in dialog.GetComponentsInChildren<RectTransform>(true))
                {
                    if(child==dialog||child.name=="Opaque text backing")continue;
                    var item=new Item{rect=child,parent=child.parent,position=child.anchoredPosition,size=child.sizeDelta,anchorMin=child.anchorMin,anchorMax=child.anchorMax,pivot=child.pivot,scale=child.localScale,sibling=child.GetSiblingIndex(),hasText=child.GetComponent<TMP_Text>(),fontSize=child.GetComponent<TMP_Text>()?child.GetComponent<TMP_Text>().fontSize:0};
                    originals.Add(item);if(child.parent==dialog)items.Add(item);
                }
                captured=true;
            }
            bool flat=ui.canvas.renderMode!=RenderMode.WorldSpace;
            float units=Mathf.Max(.001f,ui.PhoneViewport.PixelsPerLogicalUnit);
            var safe=ui.PhoneViewport.SafePixels;
            float width=flat&&safe.width>0?safe.width/units:ui.surface.rect.width,height=flat&&safe.height>0?safe.height/units:ui.surface.rect.height;
            if(flat==phone&&lastWidth==width&&lastHeight==height&&lastMode==ui.canvas.renderMode&&lastSafe==safe){if(readingOutput)LayoutReadingCards(readingOutput,readingWidth,readingHeight);return;}
            lastWidth=width;lastHeight=height;lastMode=ui.canvas.renderMode;lastSafe=safe;
            if(scroll)restoredScroll=scroll.verticalNormalizedPosition;
            Restore();phone=flat;if(!flat)return;
            Build(width,height);if(viewport==ownedViewport)VlabUi.AddPhoneScrollbar(scroll);
        }
        RectTransform Rect(string label,Transform parent)
        {
            var go=new GameObject(label,typeof(RectTransform));go.transform.SetParent(parent,false);
            var rect=(RectTransform)go.transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);return rect;
        }
        void Build(float safeWidth,float safeHeight)
        {
            const float gap=14,header=86,footerHeight=84;
            bool parameterSide=(ParameterPanel||CompactControls)&&safeWidth>safeHeight;
            bool side=items.Any(x=>x.rect.name=="Instrument observation output")&&!GetComponentInChildren<RawImage>()&&!GetComponentInChildren<VlabScrollContent>()&&safeWidth>safeHeight;
            float scale=side||parameterSide?1.1f:GetComponentsInChildren<ObservationCardBinding>(true).Length>0?1.4f:1.65f;
            float width=side||parameterSide?(safeWidth*.48f-24)/scale:Mathf.Min(1100,(safeWidth-48)/scale),height=(safeHeight-48)/scale;
            if(CompactControls)height=Mathf.Min(height,490);
            dialog.anchorMin=dialog.anchorMax=dialog.pivot=new Vector2(0,1);dialog.sizeDelta=new Vector2(width,height);dialog.localScale=Vector3.one*scale;
            float units=Mathf.Max(.001f,ui.PhoneViewport.PixelsPerLogicalUnit);
            var safe=ui.PhoneViewport.SafePixels;var fitted=ui.PhoneViewport.ContentPixels;
            float left=(safe.xMin-fitted.xMin)/units,top=(fitted.yMax-safe.yMax)/units;
            dialog.anchoredPosition=new Vector2(left+(side||parameterSide?safeWidth-width*scale-24:(safeWidth-width*scale)/2),-top-24);
            var title=items.FirstOrDefault(x=>x.rect.GetComponent<TMP_Text>());
            var close=items.FirstOrDefault(x=>x.rect.name=="Close dialog");
            if(title!=null){Place(title.rect,22,16,width-128,60);}
            if(close!=null)Place(close.rect,width-90,10,76,68);
            viewport=ownedViewport=Rect("Phone dialog scroll viewport",dialog);Place(viewport,14,header,width-28,Mathf.Max(120,height-header-footerHeight-12));
            var hit=viewport.gameObject.AddComponent<Image>();hit.color=Color.clear;viewport.gameObject.AddComponent<RectMask2D>();
            body=Rect("Phone dialog flowing content",viewport);body.sizeDelta=new Vector2(width-56,0);
            scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=body;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=42;
            footer=Rect("Phone dialog pinned actions",dialog);Place(footer,22,height-footerHeight,width-44,footerHeight-12);
            var actions=items.Where(x=>x!=close&&x.rect.GetComponent<Button>()&&(x.Top>=authoredSize.y-106||x.rect.name=="Station return")).OrderBy(x=>x.position.x).ToArray();
            float actionWidth=actions.Length==0?0:(width-44-gap*(actions.Length-1))/actions.Length;
            for(int i=0;i<actions.Length;i++){actions[i].rect.SetParent(footer,false);Place(actions[i].rect,i*(actionWidth+gap),4,actionWidth,68);FitCaption(actions[i].rect);}
            var content=items.Where(x=>x!=title&&x!=close&&!actions.Contains(x)).OrderBy(x=>x.Top).ThenBy(x=>x.position.x).ToArray();
            if(CompactControls)
            {
                float y=0,available=width-56;
                foreach(var row in content.GroupBy(x=>x.Top))
                {
                    var controls=row.OrderBy(x=>x.position.x).ToArray();float cell=(available-gap*(controls.Length-1))/controls.Length;
                    for(int i=0;i<controls.Length;i++){var item=controls[i];item.rect.SetParent(body,false);Place(item.rect,i*(cell+gap),y,cell,88);FitCaption(item.rect);}
                    y+=102;
                }
                body.sizeDelta=new Vector2(available,y);scroll.verticalNormalizedPosition=restoredScroll;return;
            }
            if(ParameterPanel){Flow(content,width-56);return;}
            if(side){BuildInstrumentSide(content,width,height,header,footerHeight);return;}
            if(BuildDocument(content,width,height,header,footerHeight))return;
            if(width>=820&&BuildEquipment(content,width,height,header,footerHeight))return;
            bool explanation=content.Any(x=>x.rect.name=="Instrument observation output"&&x.rect.GetComponentInChildren<VlabScrollContent>());
            if((width>=1000||explanation)&&BuildInstrument(content,width,height,header,footerHeight))return;
            Flow(content,width-56);
        }
        void BuildInstrumentSide(Item[] content,float width,float height,float header,float footerHeight)
        {
            var output=content.First(x=>x.rect.name=="Instrument observation output");
            var tabs=content.Where(x=>x.rect.name.StartsWith("Instrument tab ",System.StringComparison.Ordinal)).ToArray();
            float tabWidth=(width-44-14*(tabs.Length-1))/tabs.Length;
            for(int i=0;i<tabs.Length;i++)Place(tabs[i].rect,22+i*(tabWidth+14),header,tabWidth,68);
            float top=header+82;
            bool readings=output.rect.GetComponentInChildren<ObservationCardBinding>();
            float outputHeight=readings?336:110;
            Place(output.rect,22,top,width-44,outputHeight);
            if(readings){readingOutput=output.rect;readingWidth=width-44;readingHeight=outputHeight;LayoutReadingCards(readingOutput,readingWidth,readingHeight);}
            else foreach(RectTransform child in output.rect)if(child.GetComponent<TMP_Text>())Place(child,8,8,width-60,94);
            top+=outputHeight+14;
            Place(viewport,22,top,width-44,Mathf.Max(100,height-top-footerHeight-12));
            Flow(content.Where(x=>x!=output&&!tabs.Contains(x)&&(!x.rect.GetComponent<TMP_Text>()||!string.IsNullOrWhiteSpace(x.rect.GetComponent<TMP_Text>().text))),width-70);
        }
        void Flow(IEnumerable<Item> content,float available)
        {
            const float gap=14;
            float y=0;
            var remaining=content.OrderBy(x=>x.Top).ThenBy(x=>x.position.x).ToList();
            while(remaining.Count>0)
            {
                // +/- belong to their label even when authored baseline/track offsets cross a 24-unit bucket.
                float top=remaining[0].Top;
                var fixedValue=remaining.FirstOrDefault(x=>x.rect.name.StartsWith("Parameter fixed ",System.StringComparison.Ordinal)&&x.Top>=top&&x.Top<=top+16);
                var fixedLabel=fixedValue==null?null:remaining.FirstOrDefault(x=>x!=fixedValue&&x.rect.GetComponent<TMP_Text>()&&Mathf.Abs(x.Top-top)<1&&x.position.x<fixedValue.position.x);
                if(fixedLabel!=null)
                {
                    float statusWidth=136,labelWidth=available-statusWidth-gap;
                    var text=fixedLabel.rect.GetComponent<TMP_Text>();float height=Mathf.Max(64,text.GetPreferredValues(text.text,labelWidth,Mathf.Infinity).y+8);
                    fixedLabel.rect.SetParent(body,false);fixedValue.rect.SetParent(body,false);
                    Place(fixedLabel.rect,0,y,labelWidth,height);Place(fixedValue.rect,available-statusWidth,y,statusWidth,height);
                    remaining.Remove(fixedLabel);remaining.Remove(fixedValue);y+=height+gap;continue;
                }
                var buttons=remaining.Where(x=>Mathf.Abs(x.Top-top)<1&&x.rect.GetComponent<Button>()).OrderBy(x=>x.position.x).ToArray();
                var label=remaining.FirstOrDefault(x=>x.rect.GetComponent<TMP_Text>()&&x.Top>=top&&x.Top<=top+16&&buttons.Length==2&&x.position.x<buttons[0].position.x);
                if(label!=null&&buttons.Length==2&&IsStep(buttons[0],false)&&IsStep(buttons[1],true))
                {
                    float buttonWidth=80,labelWidth=available-2*(buttonWidth+gap);
                    var text=label.rect.GetComponent<TMP_Text>();
                    float textHeight=Mathf.Max(64,text.GetPreferredValues(text.text,labelWidth,Mathf.Infinity).y+8);
                    var bars=remaining.Where(x=>(x.rect.name=="Settings value track"||x.rect.name=="Settings value fill")&&Mathf.Abs(x.Top-top-34)<1).ToArray();
                    label.rect.SetParent(body,false);Place(label.rect,0,y,labelWidth,textHeight);
                    for(int i=0;i<2;i++){buttons[i].rect.SetParent(body,false);Place(buttons[i].rect,available-(2-i)*buttonWidth-(1-i)*gap,y,buttonWidth,80);FitCaption(buttons[i].rect);remaining.Remove(buttons[i]);}
                    float trackWidth=bars.FirstOrDefault(x=>x.rect.name=="Settings value track")?.size.x??1;
                    foreach(var bar in bars){bar.rect.SetParent(body,false);Place(bar.rect,0,y+textHeight+4,labelWidth*bar.size.x/Mathf.Max(1,trackWidth),8);remaining.Remove(bar);}
                    remaining.Remove(label);y+=Mathf.Max(80,textHeight+(bars.Length>0?12:0))+gap;continue;
                }
                var row=remaining.Where(x=>Mathf.Round(x.Top/24)==Mathf.Round(top/24)).ToArray();
                foreach(var item in row)remaining.Remove(item);
                float x=0,rowHeight=0;
                foreach(var item in row)
                {
                    item.rect.SetParent(body,false);float w=Mathf.Min(item.size.x,available),h=item.size.y,fit=w/Mathf.Max(1,item.size.x);
                    bool button=item.rect.GetComponent<Button>();var text=item.rect.GetComponent<TMP_Text>();
                    if(button){w=Mathf.Min(Mathf.Max(w,240),available);h=Mathf.Max(68,h);fit=1;}
                    else if(text){fit=1;h=Mathf.Max(h,text.GetPreferredValues(text.text,w,h).y+12);}
                    else h*=fit;
                    if(x>0&&x+w>available){y+=rowHeight+gap;x=0;rowHeight=0;}
                    if(button||text){Place(item.rect,x,y,w,h);FitCaption(item.rect);}
                    else{Place(item.rect,x,y,item.size.x,item.size.y);item.rect.localScale=Vector3.one*fit;}
                    x+=w+gap;rowHeight=Mathf.Max(rowHeight,h);
                }
                y+=rowHeight+gap;
            }
            body.sizeDelta=new Vector2(available,y);body.anchoredPosition=Vector2.zero;scroll.verticalNormalizedPosition=restoredScroll;
        }
        static bool IsStep(Item item,bool plus)
        {var caption=item.rect.GetComponentInChildren<TMP_Text>();return caption&&(plus?caption.text=="+":caption.text=="−"||caption.text=="-");}
        bool BuildDocument(Item[] content,float width,float height,float header,float footerHeight)
        {
            var documents=content.Where(x=>x.rect.GetComponent<VlabScrollContent>()).ToArray();
            if(documents.Length!=1||content.Any(x=>x.rect.GetComponent<Button>()))return false;
            var host=documents[0];float top=header;
            foreach(var item in content.Where(x=>x!=host))
            {
                var text=item.rect.GetComponent<TMP_Text>();if(!text)return false;
                float h=Mathf.Max(42,text.GetPreferredValues(text.text,width-44,Mathf.Infinity).y+8);
                Place(item.rect,22,top,width-44,h);top+=h+10;
            }
            float available=height-top-footerHeight-12;
            Place(host.rect,22,top,width-44,available);
            var document=host.rect.GetComponent<VlabScrollContent>();ResizeScroll(document,width-44,available);
            // Use the existing document ScrollRect directly; no outer scroll can swallow its drag/page buttons.
            ownedViewport.gameObject.SetActive(false);
            scroll=document.Scroll;viewport=scroll.viewport;body=scroll.content;
            scroll.verticalNormalizedPosition=restoredScroll;
            return true;
        }
        void ResizeScroll(VlabScrollContent document,float width,float height)
        {
            var scroll=document.Scroll;if(!scroll||!scroll.viewport||!document.Body)return;
            if(!nativeDocuments.Contains(document))nativeDocuments.Add(document);
            float position=scroll.verticalNormalizedPosition;
            var host=(RectTransform)document.transform;host.sizeDelta=new Vector2(width,height);host.localScale=Vector3.one;
            Place(scroll.viewport,12,12,width-24,Mathf.Max(70,height-100));
            Place(document.Body.rectTransform,0,0,width-48,Mathf.Max(70,height-100));
            if(document.Up){Place((RectTransform)document.Up.transform,12,height-76,(width-40)/2,64);FitCaption((RectTransform)document.Up.transform);}
            if(document.Down){Place((RectTransform)document.Down.transform,28+(width-40)/2,height-76,(width-40)/2,64);FitCaption((RectTransform)document.Down.transform);}
            document.Refresh();scroll.verticalNormalizedPosition=position;
        }

        bool BuildEquipment(Item[] content,float width,float height,float header,float footerHeight)
        {
            var instruments=content.Where(x=>x.rect.name.StartsWith("Equipment item ",System.StringComparison.Ordinal)).ToArray();
            var take=content.FirstOrDefault(x=>x.rect.name=="Equipment take selected");
            var place=content.FirstOrDefault(x=>x.rect.name=="Equipment place selected");
            var putBack=content.FirstOrDefault(x=>x.rect.name=="Equipment return selected");
            if(instruments.Length==0||take==null||place==null||putBack==null)return false;
            float listWidth=width*.56f-28,sideX=listWidth+44,sideWidth=width-sideX-22;
            float availableHeight=height-header-footerHeight-12;
            Place(viewport,22,header,listWidth,availableHeight);
            Flow(instruments,listWidth-12);
            float y=header;
            foreach(var action in new[]{take,place,putBack})
            {Place(action.rect,sideX,y,sideWidth,72);FitCaption(action.rect);y+=84;}
            var information=content.Where(x=>!instruments.Contains(x)&&x!=take&&x!=place&&x!=putBack).ToArray();
            for(int i=0;i<information.Length;i++)
            {
                var item=information[i];float reserved=(information.Length-i-1)*44;
                float textHeight=Mathf.Min(item.size.y,Mathf.Max(36,header+availableHeight-y-reserved));
                Place(item.rect,sideX,y,sideWidth,textHeight);y+=textHeight+8;
            }
            return true;
        }

        bool BuildInstrument(Item[] content,float width,float height,float header,float footerHeight)
        {
            var output=content.FirstOrDefault(x=>x.rect.name=="Instrument observation output");
            var controls=content.Where(x=>x.rect.name.StartsWith("Instrument control ",System.StringComparison.Ordinal)).ToArray();
            if(output==null)return false;
            var explanation=output.rect.GetComponentInChildren<VlabScrollContent>();
            bool hasControls=controls.Length>0;if(!hasControls&&!explanation)return false;
            var tabs=content.Where(x=>x.rect.name.StartsWith("Instrument tab ",System.StringComparison.Ordinal)).ToArray();
            float tabWidth=(width-44-14*Mathf.Max(0,tabs.Length-1))/Mathf.Max(1,tabs.Length);
            for(int i=0;i<tabs.Length;i++)Place(tabs[i].rect,22+i*(tabWidth+14),header,tabWidth,68);
            float top=header+82,availableHeight=height-top-footerHeight-12,rightWidth=hasControls?400:0,leftWidth=hasControls?width-rightWidth-66:width-44;
            if(hasControls)Place(viewport,width-rightWidth-22,top,rightWidth,availableHeight);
            var status=content.FirstOrDefault(x=>x.rect.GetComponent<TMP_Text>()&&x.Top>730);
            var summary=content.FirstOrDefault(x=>x.rect.GetComponent<TMP_Text>()&&x.Top<100);
            bool nativeReadings=output.rect.GetComponentsInChildren<ObservationCardBinding>().Length>0;
            float statusHeight=status!=null?62:0;
            if(status!=null)Place(status.rect,22,top+availableHeight-statusHeight,width-44,statusHeight);
            availableHeight-=statusHeight;
            if(hasControls)Place(viewport,width-rightWidth-22,top,rightWidth,availableHeight);
            bool hasSummary=summary!=null&&!string.IsNullOrWhiteSpace(summary.rect.GetComponent<TMP_Text>().text);
            if(summary!=null)
            {
                if(hasSummary)
                {
                    Place(summary.rect,width-402,18,290,60);
                    var title=items.FirstOrDefault(x=>x.rect.GetComponent<TMP_Text>());
                    if(title!=null)Place(title.rect,22,16,width-456,60);
                }
                else Place(summary.rect,0,0,0,0);
            }
            if(nativeReadings)
            {
                Place(output.rect,22,top,leftWidth,availableHeight);
                readingOutput=output.rect;readingWidth=leftWidth;readingHeight=availableHeight;
                LayoutReadingCards(readingOutput,readingWidth,readingHeight);
            }
            else
            {
                var document=explanation;
                if(document)
                {
                    Place(output.rect,22,top,leftWidth,availableHeight);
                    Place((RectTransform)document.transform,0,0,leftWidth,availableHeight);
                    ResizeScroll(document,leftWidth,availableHeight);
                }
                else
                {
                    float fit=Mathf.Min(leftWidth/output.size.x,Mathf.Max(80,availableHeight)/output.size.y);
                    Place(output.rect,22,top,output.size.x,output.size.y);output.rect.localScale=Vector3.one*fit;
                }
            }
            if(!hasControls&&explanation)
            {
                foreach(var item in content.Where(x=>x!=output&&x!=summary&&x!=status&&!tabs.Contains(x)))item.rect.SetParent(ownedViewport,false);
                ownedViewport.gameObject.SetActive(false);scroll=explanation.Scroll;viewport=scroll.viewport;body=scroll.content;
                scroll.verticalNormalizedPosition=restoredScroll;return true;
            }
            // Only controls scroll; the specimen/live readings remain in view during repeated adjustments.
            Flow(content.Where(x=>x!=output&&x!=summary&&x!=status&&!tabs.Contains(x)),rightWidth-26);
            return true;
        }

        void LayoutReadingCards(RectTransform output,float width,float height)
        {
            var cards=output.GetComponentsInChildren<ObservationCardBinding>();if(cards.Length==0)return;
            // Readings can be rebuilt while the same instrument stays open; capture new authored children once.
            foreach(var child in output.GetComponentsInChildren<RectTransform>(true))
            {
                if(child.name=="Opaque text backing"||originals.Any(x=>x.rect==child))continue;
                originals.Add(new Item{rect=child,parent=child.parent,position=child.anchoredPosition,size=child.sizeDelta,anchorMin=child.anchorMin,anchorMax=child.anchorMax,pivot=child.pivot,scale=child.localScale,sibling=child.GetSiblingIndex(),hasText=child.GetComponent<TMP_Text>(),fontSize=child.GetComponent<TMP_Text>()?child.GetComponent<TMP_Text>().fontSize:0});
            }
            var pagination=output.GetComponentsInChildren<Button>();float reserved=pagination.Length>0?78:0;
            float cardHeight=(height-reserved-14*(cards.Length-1))/cards.Length;
            for(int i=0;i<cards.Length;i++)
            {
                var card=cards[i];Place((RectTransform)card.transform,0,i*(cardHeight+14),width,cardHeight);
                card.Label.fontSize=ui.LargeText?26.88f:24;Place(card.Label.rectTransform,18,10,width-36,48);
                card.Value.fontSize=ui.LargeText?44.8f:40;card.Unit.fontSize=ui.LargeText?26.88f:24;
                float unitWidth=string.IsNullOrWhiteSpace(card.Unit.text)?0:card.Unit.GetPreferredValues(card.Unit.text,float.PositiveInfinity,36).x+4;
                Place(card.Value.rectTransform,18,cardHeight-78,Mathf.Max(1,width-36-(unitWidth>0?unitWidth+14:0)),74);
                Place(card.Unit.rectTransform,width-18-unitWidth,cardHeight-48,unitWidth,36);
                foreach(RectTransform child in card.transform)if(child.name=="Section rule")Place(child,0,16,4,Mathf.Max(1,cardHeight-32));
            }
            if(pagination.Length>0)
            {
                float buttonWidth=(width-94)/2;
                for(int i=0;i<pagination.Length;i++){Place((RectTransform)pagination[i].transform,i==0?0:width-buttonWidth,height-64,buttonWidth,64);FitCaption((RectTransform)pagination[i].transform);}
                foreach(RectTransform child in output)if(child.GetComponent<TMP_Text>())Place(child,buttonWidth+6,height-55,82,46);
            }
        }

        public bool AssertPhoneLayout(out string failure)
        {
            failure="";
            if(!captured||!phone)return true;
            if(!scroll||!viewport||!body||scroll.content!=body||scroll.viewport!=viewport){failure="Phone dialog lost its scrolling body.";return false;}
            if(viewport.rect.width<=0||viewport.rect.height<=0){failure="Phone dialog has no usable content viewport.";return false;}
            foreach(RectTransform child in body)
            {
                float right=child.anchoredPosition.x+child.rect.width*child.localScale.x;
                if(child.anchoredPosition.x<-.1f||right>body.rect.width+.1f){failure="Phone dialog child exceeds body width: "+child.name;return false;}
            }
            foreach(var document in nativeDocuments)
            {
                if(!document||!document.Scroll||!document.Body)continue;
                if(document.Scroll.content!=document.Body.rectTransform||document.Body.rectTransform.rect.height+1<document.Scroll.viewport.rect.height){failure="Document content does not cover its scroll viewport.";return false;}
                float scale=document.Body.rectTransform.lossyScale.x/dialog.lossyScale.x;
                if(Mathf.Abs(scale-1)>.01f){failure="Document body text was scaled down.";return false;}
                if(document.Up&&((RectTransform)document.Up.transform).rect.height<64||document.Down&&((RectTransform)document.Down.transform).rect.height<64){failure="Document page control is too short.";return false;}
            }
            if(readingOutput)
                foreach(var card in readingOutput.GetComponentsInChildren<ObservationCardBinding>())
                    if(((RectTransform)card.transform).rect.height<118){failure="Native reading cards need a two-card page or more viewport height.";return false;}
            foreach(RectTransform child in footer)
                if(child.rect.height<68){failure="Phone action target is too short.";return false;}
            return true;
        }

        static void FitCaption(RectTransform rect)
        {
            if(!rect.GetComponent<Button>())return;
            var text=rect.GetComponentInChildren<TMP_Text>();if(!text)return;
            var caption=text.rectTransform;caption.anchorMin=Vector2.zero;caption.anchorMax=Vector2.one;caption.offsetMin=new Vector2(10,5);caption.offsetMax=new Vector2(-10,-5);

        }
        static void Place(RectTransform rect,float x,float y,float width,float height)
        {rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(width,height);rect.localScale=Vector3.one;}
        void Restore()
        {
            nativeDocuments.Clear();
            foreach(var item in originals)
            {
                if(!item.rect||!item.parent)continue;item.rect.SetParent(item.parent,false);item.rect.SetSiblingIndex(item.sibling);item.rect.anchorMin=item.anchorMin;item.rect.anchorMax=item.anchorMax;item.rect.pivot=item.pivot;item.rect.anchoredPosition=item.position;item.rect.sizeDelta=item.size;item.rect.localScale=item.scale;if(item.hasText&&item.rect.GetComponent<TMP_Text>())item.rect.GetComponent<TMP_Text>().fontSize=item.fontSize;
            }
            if(ownedViewport){ownedViewport.gameObject.SetActive(false);Destroy(ownedViewport.gameObject);}if(footer){footer.gameObject.SetActive(false);Destroy(footer.gameObject);}
            viewport=ownedViewport=body=footer=readingOutput=null;scroll=null;dialog.anchorMin=authoredMin;dialog.anchorMax=authoredMax;dialog.pivot=authoredPivot;dialog.sizeDelta=authoredSize;dialog.anchoredPosition=authoredPosition;dialog.localScale=authoredScale;
        }
    }
}

