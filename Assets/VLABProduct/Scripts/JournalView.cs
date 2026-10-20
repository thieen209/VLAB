using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vlab.Product
{
    public sealed class JournalState {public string subject="P",experiment="P01";public int indexPage,attempt,contentTab,contentPage,studyChapter;public bool notesMode=true,noteChooseLesson;public int notePage,noteLinkPage;public string noteLinkSubject="P",noteStatus="";public readonly HashSet<string> dirtyNotes=new HashSet<string>();}
    public static class JournalView
    {
        // A local paper treatment: changing the notebook never changes instrument or menu themes.
        internal static readonly Color Paper=VlabUi.Hex("F5F0E3"),Ink=VlabUi.Hex("253846"),Pencil=VlabUi.Hex("576571"),BookBlue=VlabUi.Hex("305C86");
        static Sprite paperTexture,bindingTexture;
        static Sprite PaperTexture()
        {
            if(paperTexture)return paperTexture;
            const int size=128;var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){name="Notebook paper grain",filterMode=FilterMode.Bilinear};
            // Deterministic, subtle fibre; never touches Unity's simulation random state.
            for(int y=0;y<size;y++)for(int x=0;x<size;x++){float value=.994f+((x*73+y*151+x*y*3)%23)*.00025f;texture.SetPixel(x,y,new Color(value,value,value,1));}
            texture.Apply(false,true);paperTexture=Sprite.Create(texture,new Rect(0,0,size,size),Vector2.one*.5f);return paperTexture;
        }
        static void Flat(RectTransform rect,Color color,bool grain=false)
        {
            var image=rect.GetComponent<Image>();image.sprite=grain?PaperTexture():null;image.type=Image.Type.Simple;image.color=color;
            var opacity=rect.GetComponent<VlabPanelOpacity>();if(opacity){opacity.Solid=true;opacity.FlatAlpha=1;}
            foreach(var effect in rect.GetComponents<Shadow>())effect.enabled=false;
        }
        internal static RectTransform PaperPage(VlabUi ui,Transform parent,string name,float x,float y,float width,float height)
        {
            var page=ui.Panel(parent,name,x,y,width,height,Paper);Flat(page,Paper,true);return page;
        }
        internal static void Ruling(VlabUi ui,Transform parent,float x,float y,float width,float height,float line=40)
        {
            for(float top=y+line;top<y+height;top+=line)ui.Rule(parent,x,top,width,2,VlabUi.Hex("CBD8DE"));
            ui.Rule(parent,x+10,y,2,height,VlabUi.Hex("D2A39C"));
        }
        static void Cover(VlabUi ui,RectTransform page)
        {
            // The wide writing area is one landscape spiral pad. Its slim contents
            // card is attached alongside it, rather than pretending to be an unequal leaf.
            var shadow=ui.Panel(page,"Journal book shadow",398,230,1140,670,VlabUi.Hex("12222F"));Flat(shadow,VlabUi.Hex("12222F"));shadow.GetComponent<Image>().raycastTarget=false;
            var cover=ui.Panel(page,"Journal continuous cover",392,220,1138,670,VlabUi.Hex("294359"));Flat(cover,VlabUi.Hex("294359"),true);cover.GetComponent<Image>().raycastTarget=false;
            var index=ui.Panel(page,"Journal attached index backing",75,225,310,661,VlabUi.Hex("C9C1AE"));Flat(index,VlabUi.Hex("C9C1AE"));index.GetComponent<Image>().raycastTarget=false;
            var stack=ui.Panel(page,"Journal page stack",417,227,1106,659,VlabUi.Hex("D6CEBD"));Flat(stack,VlabUi.Hex("D6CEBD"));stack.GetComponent<Image>().raycastTarget=false;
            ui.Rule(page,421,882,1096,1,VlabUi.Hex("B9B09E"));ui.Rule(page,421,885,1096,1,VlabUi.Hex("E7E0CF"));
        }
        internal static void Binding(VlabUi ui,RectTransform writingPage)
        {
            if(!bindingTexture)
            {
                const int width=64,height=28;var texture=new Texture2D(width,height,TextureFormat.RGBA32,false){name="Notebook wire binding",filterMode=FilterMode.Bilinear};
                for(int y=0;y<height;y++)for(int x=0;x<width;x++)
                {
                    float dx=(x-(width-1)*.5f)/28,dy=(y-(height-1)*.5f)/9;float distance=Mathf.Sqrt(dx*dx+dy*dy);
                    float alpha=Mathf.Clamp01((.15f-Mathf.Abs(distance-1))*8);float shade=y<height/2?.32f:.12f;texture.SetPixel(x,y,new Color(shade,shade+.02f,shade+.03f,alpha));
                }
                texture.Apply(false,true);bindingTexture=Sprite.Create(texture,new Rect(0,0,width,height),Vector2.one*.5f);
            }
            var binding=ui.Rect(writingPage,"Journal spine",-26,0,44,650);
            for(int y=16;y<630;y+=38){var ring=ui.Rect(binding,"Wire loop",0,y,44,18);var image=ring.gameObject.AddComponent<Image>();image.sprite=bindingTexture;image.raycastTarget=false;}
        }
        // Call only on book leaves. Preserve layout, names, fields and reading bindings.
        internal static void FinishPaper(Transform root)
        {
            foreach(var text in root.GetComponentsInChildren<TextMeshProUGUI>(true)){text.color=Ink;var backing=text.GetComponent<VlabTextBacking>();if(backing)backing.enabled=false;}
            foreach(var panel in root.GetComponentsInChildren<VlabPanelOpacity>(true))
            {
                var rect=(RectTransform)panel.transform;var button=rect.GetComponent<Button>();var field=rect.GetComponent<TMP_InputField>();
                if(button)
                {
                    bool marked=button.GetComponent<Image>().color!=VlabUi.Raised;
                    bool indexEntry=button.name.StartsWith("Journal note page ",StringComparison.Ordinal)||button.name.StartsWith("Journal lesson ",StringComparison.Ordinal);
                    bool annotation=button.name=="Journal note lesson";
                    Flat(rect,marked?BookBlue:indexEntry||annotation?Paper:VlabUi.Hex("EAE5D8"));
                    var caption=button.GetComponentInChildren<TextMeshProUGUI>();if(caption)caption.color=marked?Color.white:Ink;
                    if(annotation&&caption){caption.alignment=TextAlignmentOptions.Left;caption.color=Pencil;}
                    var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(.93f,.96f,1);colors.pressedColor=new Color(.8f,.86f,.92f);colors.disabledColor=new Color(.7f,.7f,.7f);button.colors=colors;
                }
                else if(field){Flat(rect,Paper);field.caretColor=Ink;((TextMeshProUGUI)field.placeholder).color=Pencil;}
                else if(rect!=root)Flat(rect,VlabUi.Hex("EEE9DB"));
            }
        }
        static void BookTab(Button button,bool active)
        {
            Flat((RectTransform)button.transform,active?BookBlue:VlabUi.Hex("EAE5D8"));var text=button.GetComponentInChildren<TextMeshProUGUI>();text.color=active?Color.white:Ink;var backing=text.GetComponent<VlabTextBacking>();if(backing)backing.enabled=false;
        }
        static string L(bool vi,string a,string b)=>vi?a:b;
        public static string Status(LocalProfile profile,string id,bool vi)
        {
            int count=profile.attempts.Count(record=>record.experimentId==id);string status=count==0?L(vi,"Chưa có lượt đã ghi","No saved attempts"):count+L(vi," lượt đã lưu",count==1?" saved attempt":" saved attempts");
            if(profile.checkpoint?.experimentId==id)status+=L(vi," · có điểm chuẩn bị"," · preparation checkpoint");return status;
        }
        public static string Interpretation(AttemptRecord record,bool vi)
        {
            string notes=vi?record.modelNotesVi:record.modelNotesEn;
            return !string.IsNullOrEmpty(notes)?notes:L(vi,"Bản ghi cũ chưa kèm diễn giải. Tham khảo hiện tại dưới đây; số đo và mô hình gốc vẫn được giữ.\n\n","This older record has no attached interpretation. Current reference guidance follows; its original readings and model are retained.\n\n")+LessonContext.Explanation(record.experimentId,vi);
        }
        public static string RecordText(AttemptRecord record,bool vi,Func<IEnumerable<Reading>,string> readings)
        {
            var body=new StringBuilder();body.AppendLine(L(vi,"SỐ ĐO ĐÃ LƯU","SAVED OBSERVATIONS")).AppendLine(readings(ObservationCards.PresentationReadings(record.readings,record.experimentId)));
            body.AppendLine().AppendLine(L(vi,"ĐIỀU KIỆN CỦA LƯỢT","RECORDED CONDITIONS"));var spec=LessonSpec.All[record.experimentId];
            for(int i=0;i<spec.parameters.Length;i++){var parameter=spec.parameters[i];string value=record.parameterValues!=null&&i<record.parameterValues.Length?record.parameterValues[i].ToString("0.###",CultureInfo.InvariantCulture):L(vi,"Không có trong bản ghi","Not recorded");body.AppendLine((vi?parameter.vi:parameter.en)+": "+value+" "+parameter.unit);}
            if(spec.parameters.Length==0)body.AppendLine(L(vi,"Không có tham số số học cho bài này.","This lesson has no numeric parameters."));
            body.AppendLine().AppendLine(L(vi,"CÁCH HIỂU KẾT QUẢ","HOW TO INTERPRET")).AppendLine(Interpretation(record,vi));
            if(!string.IsNullOrEmpty(record.observation))body.AppendLine().AppendLine(record.observation);
            body.AppendLine().AppendLine(L(vi,"TIẾP TỤC KHẢO SÁT","CONTINUE THE INVESTIGATION")).AppendLine(LessonSeries.FollowUp(record.experimentId,vi));
            return body.ToString();
        }
        public static void Render(VlabUi ui,RectTransform page,LocalProfile profile,bool vi,JournalState state,Action refresh,Action compare,Func<IEnumerable<Reading>,string> readings,Func<bool> saveNotes=null,Action<string,Action> confirmDelete=null,Action<TMP_InputField> openKeyboard=null)
        {
            Color bookAccent=VlabUi.Hex("4C86FF");
            var notesTab=ui.Button(page,L(vi,"Ghi chép","Write notes"),80,140,310,64,()=>{state.notesMode=true;refresh();},state.notesMode?bookAccent:(Color?)null);notesTab.name="Journal mode notes";BookTab(notesTab,state.notesMode);
            var recordsTab=ui.Button(page,L(vi,"Bài học & số đo","Lessons & readings"),410,140,310,64,()=>{state.notesMode=false;refresh();},!state.notesMode?bookAccent:(Color?)null);recordsTab.name="Journal mode records";BookTab(recordsTab,!state.notesMode);
            Cover(ui,page);
            if(state.notesMode){JournalNotesView.Render(ui,page,profile,vi,state,refresh,saveNotes,confirmDelete,openKeyboard);return;}
            string[] codes={"P","C","B","M"},names=vi?new[]{"Vật lí","Hóa học","Sinh học","Kỹ thuật"}:new[]{"Physics","Chemistry","Biology","Engineering"};
            Color accent=VlabUi.Hex(state.subject=="P"?"4C86FF":state.subject=="C"?"D85BEA":state.subject=="B"?"56BF88":"F0A34D");
            for(int i=0;i<4;i++){string code=codes[i];var button=ui.Button(page,names[i],760+i*195,140,180,64,()=>{state.subject=code;state.experiment=code+"01";state.contentTab=state.contentTab==4?4:0;state.indexPage=state.attempt=state.contentPage=state.studyChapter=0;refresh();},state.subject==code?accent:(Color?)null);button.name="Journal subject "+code;BookTab(button,state.subject==code);}
            var left=PaperPage(ui,page,"Journal index page",80,230,300,650);var right=PaperPage(ui,page,"Journal record page",420,230,1100,650);
            Binding(ui,right);Ruling(ui,left,14,128,272,408,42);ui.Rule(right,32,20,1036,3,BookBlue);
            ui.Text(left,L(vi,"Mục lục","Contents"),24,24,252,48,30);ui.Text(left,L(vi,"Chọn bài thực hành","Choose a practical"),24,78,252,44,22,VlabUi.Secondary);
            var ids=LessonSpec.All.Keys.Where(id=>id.StartsWith(state.subject,StringComparison.Ordinal)).OrderBy(id=>id).ToArray();int pages=Math.Max(1,(ids.Length+2)/3);state.indexPage=Mathf.Clamp(state.indexPage,0,pages-1);
            if(!ids.Contains(state.experiment))state.experiment=ids[0];
            for(int row=0;row<3;row++)
            {
                int index=state.indexPage*3+row;if(index>=ids.Length)break;string id=ids[index];int count=profile.attempts.Count(record=>record.experimentId==id);
                string status=count==0?L(vi,"Chưa ghi số đo","No saved readings"):count+L(vi," lượt đã lưu",count==1?" saved attempt":" saved attempts");if(profile.checkpoint?.experimentId==id)status+=L(vi," · đang dở"," · in progress");
                var button=ui.Button(left,LessonGuide.ShortTitle(id,vi)+"\n<size=20>"+status+"</size>",24,140+row*126,252,112,()=>{state.experiment=id;state.contentTab=state.contentTab==4?4:0;state.attempt=state.contentPage=state.studyChapter=0;refresh();});button.name="Journal lesson "+id;var caption=button.GetComponentInChildren<TextMeshProUGUI>();caption.fontSize=ui.LargeText?26.88f:24;caption.alignment=TextAlignmentOptions.Left;
                if(state.experiment==id)ui.Rule(button.transform,0,8,4,96,accent);
            }
            ui.Button(left,"‹",24,552,72,64,()=>{state.indexPage=Math.Max(0,state.indexPage-1);refresh();}).interactable=state.indexPage>0;left.GetComponentsInChildren<UnityEngine.UI.Button>().Last().name="Journal index previous";
            ui.Text(left,(state.indexPage+1)+" / "+pages,106,570,88,40,24,VlabUi.Secondary,TextAlignmentOptions.Center);
            ui.Button(left,"›",204,552,72,64,()=>{state.indexPage=Math.Min(pages-1,state.indexPage+1);refresh();}).interactable=state.indexPage+1<pages;left.GetComponentsInChildren<UnityEngine.UI.Button>().Last().name="Journal index next";
            string selected=state.experiment;ui.Text(right,LessonGuide.ShortTitle(selected,vi),32,40,state.contentTab==4?740:1036,62,36).name="Journal lesson title";
            var records=profile.attempts.Where(record=>record.experimentId==selected).OrderByDescending(record=>record.savedAtUtc,StringComparer.Ordinal).ToArray();state.attempt=Mathf.Clamp(state.attempt,0,Math.Max(0,records.Length-1));
            var record=records.Length>0?records[state.attempt]:null;string timestamp=record?.savedAtUtc;
            if(record!=null&&DateTime.TryParse(timestamp,CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind,out var utc))timestamp=utc.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss zzz",CultureInfo.InvariantCulture);
            ui.Text(right,state.contentTab==4?L(vi,"Tài liệu học tập · không cần có số đo đã lưu","Vietnamese study guide · available before saving any readings"):record!=null?L(vi,"Đã ghi lúc ","Saved ")+timestamp:Status(profile,selected,vi),32,108,1036,44,24,VlabUi.Secondary).name="Journal timestamp";
            string[] tabs=vi?new[]{"Số đo","Điều kiện","Giải thích","Khảo sát tiếp","Ôn bài"}:new[]{"Readings","Conditions","Interpretation","Continue","Study"};
            if(state.contentTab!=4)
                for(int i=0;i<5;i++){int tab=i;var button=ui.Button(right,tabs[i],32+i*209,164,200,64,()=>{state.contentTab=tab;state.contentPage=0;refresh();},state.contentTab==i?accent:(Color?)null);button.name="Journal tab "+i;}
            if(state.contentTab==4)
            {
                var measured=ui.Button(right,L(vi,"Xem số đo","View readings"),790,36,278,64,()=>{state.contentTab=0;state.contentPage=0;refresh();});measured.name="Journal tab 0";
                LearningGuide.Render(ui,right,selected,vi,state,refresh,accent);
                ui.Text(page,L(vi,"Đọc bài, tự làm và tự kiểm tra; số đo của bạn vẫn được giữ riêng.","Read, practise and self-check; your saved observations remain separate."),80,902,1440,62,22,VlabUi.Secondary);
                FinishPaper(left);FinishPaper(right);return;
            }
            int contentPages=1;
            if(records.Length==0)
            {
                string empty=state.contentTab==3?ContinueText(profile,selected,vi):L(vi,"Chưa có số đo đã lưu cho bài này.\n\nSau khi thực hành, chọn Ghi số đo. Mỗi lượt được giữ riêng trong sổ để bạn đọc lại và so sánh.","No observations have been saved for this lesson.\n\nAfter practising, choose Record measurement. Each saved attempt stays separate for review and comparison.");
                if(profile.checkpoint?.experimentId==selected&&state.contentTab!=3)empty+="\n\n"+L(vi,"Có bài đang dở; đây chưa phải một lượt đã ghi số đo.","There is preparation in progress; it is not a saved measured attempt.");contentPages=TextPage(ui,right,empty,state,28);
            }
            else
            {
                if(state.contentTab==0){contentPages=ObservationCards.PageCount(record.readings,2);state.contentPage=Mathf.Clamp(state.contentPage,0,contentPages-1);ObservationCards.Render(ui,right,ObservationCards.PresentationReadings(record.readings,record.experimentId),vi,state.contentPage,32,246,1036,242,2);}
                else if(state.contentTab==1)contentPages=ConditionsPage(ui,right,record,vi,state);
                else if(state.contentTab==2){string explanation=Interpretation(record,vi);if(!string.IsNullOrWhiteSpace(record.observation))explanation+="\n\n"+record.observation;contentPages=TextPage(ui,right,explanation,state,28);}
                else contentPages=TextPage(ui,right,ContinueText(profile,selected,vi),state,28);
            }
            ui.Button(right,L(vi,"‹ Trang trước","‹ Previous page"),32,498,240,64,()=>{state.contentPage=Math.Max(0,state.contentPage-1);refresh();}).interactable=state.contentPage>0;right.GetComponentsInChildren<UnityEngine.UI.Button>().Last().name="Journal content previous";
            ui.Text(right,L(vi,"Trang ","Page ")+(state.contentPage+1)+" / "+contentPages,302,514,432,44,24,VlabUi.Secondary,TextAlignmentOptions.Center);
            ui.Button(right,L(vi,"Trang sau ›","Next page ›"),828,498,240,64,()=>{state.contentPage=Math.Min(contentPages-1,state.contentPage+1);refresh();}).interactable=state.contentPage+1<contentPages;right.GetComponentsInChildren<UnityEngine.UI.Button>().Last().name="Journal content next";
            ui.Rule(right,32,575,1036,1,VlabUi.Stroke);
            var newer=ui.Button(right,L(vi,"Lượt mới hơn","Newer attempt"),32,584,248,64,()=>{state.attempt=Math.Max(0,state.attempt-1);state.contentPage=0;refresh();});newer.name="Journal newer attempt";newer.interactable=records.Length>0&&state.attempt>0;
            ui.Text(right,records.Length>0?L(vi,"Lượt ","Attempt ")+(state.attempt+1)+" / "+records.Length:L(vi,"Chưa có lượt đã ghi","No saved attempt"),302,600,432,40,24,VlabUi.Secondary,TextAlignmentOptions.Center);
            var older=ui.Button(right,L(vi,"Lượt cũ hơn","Older attempt"),820,584,248,64,()=>{state.attempt=Math.Min(records.Length-1,state.attempt+1);state.contentPage=0;refresh();});older.name="Journal older attempt";older.interactable=state.attempt+1<records.Length;
            ui.Text(page,L(vi,"Sổ lưu lại việc bạn đã đo; số lượt không phải điểm đánh giá.","The journal retains your measurements; attempt counts are not grades."),80,902,840,62,22,VlabUi.Secondary);
            var comparison=ui.Button(page,L(vi,"So sánh số đo và đồ thị","Compare readings and plots"),980,900,540,64,compare);comparison.name="Journal compare";comparison.interactable=records.Length>0;
            FinishPaper(left);FinishPaper(right);BookTab(comparison,false);
        }
        static string ContinueText(LocalProfile profile,string id,bool vi)
        {
            string checkpoint=profile.checkpoint?.experimentId==id?L(vi,"Bài đang dở được mở lại ở bước chuẩn bị an toàn. Bản lưu này chưa phải số đo hoàn thành.\n\n","An interrupted lesson restarts with safe preparation. This checkpoint is not a completed measurement.\n\n"):"";
            return checkpoint+LessonSeries.FollowUp(id,vi);
        }
        static int ConditionsPage(VlabUi ui,Transform parent,AttemptRecord record,bool vi,JournalState state)
        {
            var parameters=LessonSpec.All[record.experimentId].parameters;int pages=Math.Max(1,(parameters.Length+1)/2);state.contentPage=Mathf.Clamp(state.contentPage,0,pages-1);
            if(parameters.Length==0)ui.Text(parent,L(vi,"Bài này không có tham số số học được lưu.","This lesson has no saved numeric parameters."),32,260,1036,180,28);
            for(int row=0;row<2;row++)
            {
                int i=state.contentPage*2+row;if(i>=parameters.Length)break;var parameter=parameters[i];var card=ui.Panel(parent,"Journal condition "+parameter.key,32,246+row*124,1036,112,VlabUi.Raised);
                ui.Text(card,vi?parameter.vi:parameter.en,24,16,520,74,28).name="Journal condition label "+parameter.key;
                string value=record.parameterValues!=null&&i<record.parameterValues.Length?record.parameterValues[i].ToString("R",CultureInfo.InvariantCulture):L(vi,"Không được lưu","Not recorded");
                ui.Text(card,value+(string.IsNullOrWhiteSpace(parameter.unit)?"":" "+parameter.unit),568,18,444,82,32).name="Journal condition value "+parameter.key;
            }
            return pages;
        }
        static int TextPage(VlabUi ui,Transform parent,string source,JournalState state,int fontSize)
        {
            var text=ui.Text(parent,"",32,250,1036,232,fontSize);text.name="Journal narrative page";var pages=Paginate(text,source??"");state.contentPage=Mathf.Clamp(state.contentPage,0,pages.Length-1);text.text=pages[state.contentPage];return pages.Length;
        }
        public static string[] Paginate(TextMeshProUGUI template,string source)
        {
            var pages=new List<string>();string remaining=source??"";float width=template.rectTransform.rect.width,height=template.rectTransform.rect.height;
            while(remaining.Length>0)
            {
                int lo=1,hi=remaining.Length,best=1;
                while(lo<=hi){int mid=lo+(hi-lo)/2;if(template.GetPreferredValues(remaining.Substring(0,mid),width,Mathf.Infinity).y<=height){best=mid;lo=mid+1;}else hi=mid-1;}
                int end=best;if(end<remaining.Length){int word=remaining.LastIndexOfAny(new[]{' ','\n','\t'},Math.Max(0,end-1),end);if(word>0)end=word;}
                pages.Add(remaining.Substring(0,end).Trim());remaining=remaining.Substring(end).TrimStart();
            }
            return pages.Count>0?pages.ToArray():new[]{""};
        }
    }
}
