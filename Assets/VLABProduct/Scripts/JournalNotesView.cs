using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Vlab.Product
{
    public static class JournalNotesView
    {
        static string L(bool vi,string a,string b)=>vi?a:b;
        public static void Render(VlabUi ui,RectTransform page,LocalProfile profile,bool vi,JournalState state,Action refresh,Func<bool> save,Action<string,Action> confirmDelete,Action<TMP_InputField> keyboard)
        {
            var left=JournalView.PaperPage(ui,page,"Journal notes contents",80,230,300,650);var right=JournalView.PaperPage(ui,page,"Journal writing page",420,230,1100,650);
            JournalView.Binding(ui,right);JournalView.Ruling(ui,left,14,160,272,360,40);
            ui.Text(left,L(vi,"Trang của tôi","My pages"),24,24,252,52,30);ui.Button(left,L(vi,"+ Trang mới","+ New page"),24,88,252,64,()=>
            {
                string now=DateTime.UtcNow.ToString("O");var next=new NotebookPage{id=Guid.NewGuid().ToString("N"),experimentId="",title="",body="",createdAtUtc=now,updatedAtUtc=now};profile.notebookPages.Add(next);state.notePage=profile.notebookPages.Count-1;state.dirtyNotes.Add(next.id);state.noteStatus="";refresh();
            }).name="Journal note new";
            state.notePage=Mathf.Clamp(state.notePage,0,Math.Max(0,profile.notebookPages.Count-1));int start=state.notePage/3*3;
            for(int row=0;row<3;row++)
            {
                int index=start+row;if(index>=profile.notebookPages.Count)break;var note=profile.notebookPages[index];string title=string.IsNullOrWhiteSpace(note.title)?L(vi,"Trang chưa đặt tên","Untitled page"):note.title;
                var button=ui.Button(left,title,24,180+row*108,252,92,()=>{state.notePage=index;state.noteStatus="";refresh();});button.name="Journal note page "+index;button.GetComponentInChildren<TextMeshProUGUI>().richText=false;
                if(index==state.notePage)ui.Rule(button.transform,0,8,4,76,VlabUi.Hex("4C86FF"));
            }
            var previous=ui.Button(left,"‹",24,552,72,64,()=>{state.notePage--;state.noteStatus="";refresh();});previous.name="Journal note previous";previous.interactable=state.notePage>0;
            ui.Text(left,(profile.notebookPages.Count==0?0:state.notePage+1)+" / "+profile.notebookPages.Count,104,568,92,44,24,VlabUi.Secondary,TextAlignmentOptions.Center);
            var nextPage=ui.Button(left,"›",204,552,72,64,()=>{state.notePage++;state.noteStatus="";refresh();});nextPage.name="Journal note next";nextPage.interactable=state.notePage+1<profile.notebookPages.Count;
            if(profile.notebookPages.Count==0)
            {
                ui.Text(right,L(vi,"Viết điều bạn quan sát được","Write what you observe"),32,42,1036,70,36);
                ui.Text(right,L(vi,"Đây là sổ của bạn.\n\nChọn Trang mới, đặt tiêu đề rồi ghi câu hỏi, dự đoán hoặc kết luận bằng lời của mình. Nội dung được giữ trên thiết bị khi bạn chọn Lưu trang.","This is your own notebook.\n\nChoose New page, add a title and write your questions, predictions or conclusions. Save page keeps your writing on this device."),32,160,1036,320,28,VlabUi.Secondary);JournalView.FinishPaper(left);JournalView.FinishPaper(right);return;
            }
            var current=profile.notebookPages[state.notePage];TextMeshProUGUI statusText=null;
            Action changed=()=>{current.updatedAtUtc=DateTime.UtcNow.ToString("O");state.dirtyNotes.Add(current.id);state.noteStatus="";if(statusText)statusText.text=L(vi,"Chưa lưu thay đổi","Changes not saved");};
            Input(ui,right,"Journal note title",current.title,L(vi,"Tiêu đề trang…","Page title…"),32,28,1036,78,32,120,false,value=>{current.title=value;changed();},keyboard);
            string related=string.IsNullOrEmpty(current.experimentId)?L(vi,"Gắn với một bài học (tùy chọn)","Link a lesson (optional)"):L(vi,"Bài học: ","Lesson: ")+LessonGuide.ShortTitle(current.experimentId,vi);
            ui.Button(right,related,32,122,1036,64,()=>{state.noteChooseLesson=true;state.noteLinkPage=0;refresh();}).name="Journal note lesson";
            var body=Input(ui,right,"Journal note body",current.body,L(vi,"Ghi chép bằng lời của bạn…","Write in your own words…"),32,208,1036,326,28,12000,true,value=>{current.body=value;changed();},keyboard);
            var up=ui.Button(right,L(vi,"Cuộn lên","Scroll up"),32,548,190,64,()=>body.OnScroll(new PointerEventData(EventSystem.current){scrollDelta=new Vector2(0,4)}));up.name="Journal note scroll up";
            var down=ui.Button(right,L(vi,"Cuộn xuống","Scroll down"),240,548,190,64,()=>body.OnScroll(new PointerEventData(EventSystem.current){scrollDelta=new Vector2(0,-4)}));down.name="Journal note scroll down";
            var delete=ui.Button(right,L(vi,"Xóa trang","Delete page"),500,548,220,64,()=>confirmDelete?.Invoke(L(vi,"Xóa trang ghi chép này?","Delete this notebook page?"),()=>
            {
                int index=profile.notebookPages.IndexOf(current);profile.notebookPages.Remove(current);
                bool committed=save!=null&&save();if(!committed)profile.notebookPages.Insert(index,current);
                else{state.dirtyNotes.Remove(current.id);state.notePage=Mathf.Clamp(index,0,Math.Max(0,profile.notebookPages.Count-1));}
                state.noteStatus=committed?L(vi,"Đã xóa trang","Page deleted"):L(vi,"Chưa xóa được; nội dung vẫn còn.","Could not delete; your writing is retained.");refresh();
            }));delete.name="Journal note delete";delete.interactable=confirmDelete!=null&&save!=null;
            var saveButton=ui.Button(right,L(vi,"Lưu trang","Save page"),748,548,320,64,()=>
            {
                bool committed=save!=null&&save();if(committed)state.dirtyNotes.Clear();state.noteStatus=committed?L(vi,"Đã lưu trên thiết bị","Saved on this device"):L(vi,"Chưa lưu được; giữ nguyên trang để thử lại.","Could not save; your draft is retained.");if(statusText)statusText.text=state.noteStatus;
            },VlabUi.Hex("4C86FF"));saveButton.name="Journal note save";saveButton.interactable=save!=null;
            statusText=ui.Text(page,string.IsNullOrEmpty(state.noteStatus)?(state.dirtyNotes.Contains(current.id)?L(vi,"Chưa lưu thay đổi","Changes not saved"):L(vi,"Ghi chép riêng của bạn · lưu ngoại tuyến","Your own writing · saved offline")):state.noteStatus,80,902,1440,62,24,VlabUi.Secondary);statusText.name="Journal note status";
            if(state.noteChooseLesson)LessonPicker(ui,page,profile,vi,state,current,refresh,changed);
            JournalView.FinishPaper(left);JournalView.FinishPaper(right);
        }
        static TMP_InputField Input(VlabUi ui,Transform parent,string name,string value,string placeholder,float x,float y,float width,float height,int size,int limit,bool multiline,Action<string> changed,Action<TMP_InputField> keyboard)
        {
            var panel=ui.Panel(parent,name,x,y,width,height,VlabUi.Raised);panel.GetComponent<VlabPanelOpacity>().Solid=true;panel.GetComponent<VlabPanelOpacity>().Apply(ui.canvas.renderMode==RenderMode.WorldSpace);
            // The page itself is the writing surface; rules are behind the editable ink,
            // with a red margin outside the text inset rather than a separate form box.
            if(multiline)ui.Rule(panel,10,12,2,height-24,VlabUi.Hex("D2A39C"));
            else ui.Rule(panel,0,height-6,width,1,JournalView.BookBlue);
            var viewport=ui.Rect(panel,"Writing viewport",18,12,width-36,height-24);viewport.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            var text=ui.Text(viewport,value??"",0,0,width-36,height-24,size);text.overflowMode=TextOverflowModes.Overflow;text.richText=false;
            var hint=ui.Text(viewport,placeholder,0,0,width-36,height-24,size,VlabUi.Secondary);hint.richText=false;
            var input=panel.gameObject.AddComponent<TMP_InputField>();input.textViewport=viewport;input.textComponent=text;input.placeholder=hint;input.targetGraphic=panel.GetComponent<UnityEngine.UI.Image>();input.characterLimit=limit;input.contentType=TMP_InputField.ContentType.Standard;input.lineType=multiline?TMP_InputField.LineType.MultiLineNewline:TMP_InputField.LineType.SingleLine;input.richText=false;input.customCaretColor=true;input.caretColor=VlabUi.TextColor;input.selectionColor=new Color(.3f,.55f,1,.35f);input.scrollSensitivity=20;input.text=value??"";
            // Reopening a handwritten page must not select its entire body: the first VR key
            // would otherwise replace all existing writing rather than edit at the caret.
            // Edit the journal field directly; the Android overlay can discard its separate buffer on Back.
            input.restoreOriginalTextOnEscape=false;input.onFocusSelectAll=false;input.shouldHideSoftKeyboard=keyboard!=null;input.shouldHideMobileInput=true;
            if(multiline)viewport.gameObject.AddComponent<NotebookRuling>().Bind(input);
            string draft=value??"";
            input.onValueChanged.AddListener(edited=>
            {
                // TMP copies Android's canceled keyboard buffer before firing its status event.
                if(input.touchScreenKeyboard!=null&&input.touchScreenKeyboard.status==TouchScreenKeyboard.Status.Canceled){input.SetTextWithoutNotify(draft);return;}
                draft=edited;changed(edited);
            });if(keyboard!=null)input.onSelect.AddListener(_=>keyboard(input));return input;
        }
        static void LessonPicker(VlabUi ui,RectTransform page,LocalProfile profile,bool vi,JournalState state,NotebookPage note,Action refresh,Action changed)
        {
            var panel=JournalView.PaperPage(ui,page,"Journal note lesson picker",420,230,1100,650);string[] codes={"P","C","B","M"},names=vi?new[]{"Vật lí","Hóa học","Sinh học","Kỹ thuật"}:new[]{"Physics","Chemistry","Biology","Engineering"};
            for(int i=0;i<4;i++){string code=codes[i];ui.Button(panel,names[i],32+i*260,28,248,64,()=>{state.noteLinkSubject=code;state.noteLinkPage=0;refresh();}).name="Journal note link subject "+code;}
            var ids=LessonSpec.All.Keys.Where(id=>id.StartsWith(state.noteLinkSubject,StringComparison.Ordinal)).OrderBy(id=>id).ToArray();
            for(int i=0;i<6;i++){int index=state.noteLinkPage*6+i;if(index>=ids.Length)break;string id=ids[index];ui.Button(panel,LessonGuide.ShortTitle(id,vi),32+i%2*532,130+i/2*108,504,88,()=>{note.experimentId=id;changed();state.noteChooseLesson=false;refresh();}).name="Journal note link "+id;}
            ui.Button(panel,L(vi,"Trang trước","Previous"),32,488,248,64,()=>{state.noteLinkPage=Math.Max(0,state.noteLinkPage-1);refresh();}).interactable=state.noteLinkPage>0;
            ui.Button(panel,L(vi,"Trang sau","Next"),820,488,248,64,()=>{state.noteLinkPage=Math.Min((ids.Length-1)/6,state.noteLinkPage+1);refresh();}).interactable=(state.noteLinkPage+1)*6<ids.Length;
            ui.Button(panel,L(vi,"Không gắn bài học","No linked lesson"),32,578,504,64,()=>{note.experimentId="";changed();state.noteChooseLesson=false;refresh();});ui.Button(panel,L(vi,"Quay lại trang viết","Back to writing"),564,578,504,64,()=>{state.noteChooseLesson=false;refresh();});
            JournalView.FinishPaper(panel);
        }
    }
}
