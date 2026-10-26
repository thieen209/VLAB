#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Vlab.Product;

public static class JournalNoteChecks
{
    static VlabApp app;static Action<string> capture;static Action<string,bool> report;static readonly List<string> lines=new List<string>();
    public static IEnumerator Run(VlabApp application,Action<string> screenshot,Action<string,bool> check)
    {
        app=application;capture=screenshot;report=check;lines.Clear();Write("IN PROGRESS");var stack=new Stack<IEnumerator>();stack.Push(Scenario());
        while(stack.Count>0)
        {
            object next=null;bool more=false;Exception failure=null;try{more=stack.Peek().MoveNext();if(more)next=stack.Peek().Current;}catch(Exception error){failure=error;}
            if(failure!=null){lines.Add(failure.ToString());Write("FAIL");report("Writable offline notebook journey",false);throw new InvalidOperationException("Writable notebook checks failed",failure);}
            if(!more){(stack.Pop() as IDisposable)?.Dispose();continue;}if(next is IEnumerator nested&&!(next is CustomYieldInstruction)){stack.Push(nested);continue;}yield return next;
        }
        Write("PASS");
    }
    static IEnumerator Scenario()
    {
        string folder=SessionState.GetString("VLAB.TestProfile","");Verify("Writing tests use an isolated smoke profile",!string.IsNullOrEmpty(folder));Verify("Writable journal starts at Home",app.Route=="home");bool vi=app.Profile.locale!="en";
        string[] attempts=app.Profile.attempts.Select(attempt=>JsonUtility.ToJson(attempt)).ToArray();int initial=app.Profile.notebookPages.Count;
        yield return Open();yield return Press("Journal mode notes");yield return Press("Journal note new");
        Verify("New page contains no fabricated learner writing",Field("Journal note title").text==""&&Field("Journal note body").text=="");
        const string title="Câu hỏi về con lắc";const string body="Điều gì thay đổi khi tăng chiều dài?\nTôi sẽ so sánh các lượt đo trước khi kết luận.\nTiếng Việt: ă â đ ê ô ơ ư, ắ ằ ẳ ẵ ặ.";
        yield return Type("Journal note title",title);yield return Type("Journal note body",body);
        var first=app.Profile.notebookPages.Last();string firstId=first.id;Verify("Unicode typing updates only the explicit learner draft",first.title==title&&first.body==body);
        Verify("Draft does not claim successful saving",Status().Contains(vi?"Chưa lưu":"not saved"));
        yield return Drawing(first,body);
        yield return Press("Journal mode records");yield return Press("Journal mode notes");Verify("Switching book modes retains unsaved writing",Field("Journal note title").text==title&&Field("Journal note body").text==body);
        yield return Press("Journal note new");yield return Type("Journal note title","Lượt khảo sát tiếp theo");yield return Type("Journal note body","Tôi sẽ giữ điều kiện khác như cũ và chỉ đổi một yếu tố.");string secondId=app.Profile.notebookPages.Last().id;
        yield return Press("Journal note previous");Verify("Page navigation retains the first draft",Field("Journal note body").text==body);yield return Press("Journal note next");
        yield return Press("Journal note save");Verify("Save status follows actual atomic-save success",Status().Contains(vi?"Đã lưu":"Saved"));
        var reloaded=new ProfileStore(folder).Load();Verify("Reloading actual profile recovers both authored pages and page-local ink",!reloaded.writeProtected&&reloaded.profile.notebookPages.Any(note=>note.id==firstId&&note.title==title&&note.body==body&&note.inkStrokes.Count==2)&&reloaded.profile.notebookPages.Any(note=>note.id==secondId&&note.inkStrokes.Count==0));
        yield return Press("Journal note previous");yield return Press("Journal note lesson");yield return Press("Journal note link subject B");yield return Press("Journal note link B01");Verify("Optional lesson link changes metadata without rewriting notes",app.Profile.notebookPages.Single(note=>note.id==firstId).experimentId=="B01"&&Field("Journal note body").text==body);
        Verify("Notebook linked to microscopy uses Biology room background rather than the previous room",app.Preview.CurrentRoom=="Biology");
        yield return Press("Journal note lesson");yield return Press("Journal note link subject P");yield return Press("Journal note link P01");Verify("Pendulum showcase note links its matching lesson through the actual picker",app.Profile.notebookPages.Single(note=>note.id==firstId).experimentId=="P01"&&Field("Journal note body").text==body);
        Verify("Notebook follows the actual pendulum lesson link back to Physics room background",app.Preview.CurrentRoom=="Physics");
        yield return Press("Journal note save");
        string primary=Path.Combine(folder,"profile.json"),blockedTemp=Path.Combine(folder,"profile.json.tmp");byte[] originalBytes=File.ReadAllBytes(primary);
        string recoveryDraft=body+"\nTôi muốn giữ lại ý tưởng chưa lưu này.";Field("Journal note body").text=recoveryDraft;
        yield return Press("Journal note draw");yield return Press("Journal ink pen");var ink=Board();ink.OnPointerClick(InkPoint(ink,new Vector2(.8f,.15f)));yield return Press("Journal note write");string recoveryInk=JsonUtility.ToJson(first);var recoveryField=Field("Journal note body");
        Verify("Failure fixture begins with a genuinely unsaved changed body",Field("Journal note body").text!=body&&app.Profile.notebookPages.Single(note=>note.id==firstId).body==recoveryDraft);
        Verify("Failure fixture owns a previously absent temp path",!File.Exists(blockedTemp)&&!Directory.Exists(blockedTemp));Directory.CreateDirectory(blockedTemp);
        try
        {
            yield return Press("Journal note save");
            Verify("Save failure exposes localized Retry and Back actions",Buttons().Any(button=>button.interactable&&button.GetComponentInChildren<TextMeshProUGUI>()?.text==(vi?"Thử lưu lại":"Retry save"))&&Buttons().Any(button=>button.interactable&&button.GetComponentInChildren<TextMeshProUGUI>()?.text==(vi?"Quay lại trang viết":"Back to writing")));
            Verify("Save failure retains both typed notes and unsaved drawing",recoveryField&&recoveryField.text==recoveryDraft&&JsonUtility.ToJson(app.Profile.notebookPages.Single(note=>note.id==firstId))==recoveryInk);
            Verify("Failed temp write leaves committed primary bytes unchanged",originalBytes.SequenceEqual(File.ReadAllBytes(primary)));
        }
        finally{if(Directory.Exists(blockedTemp))Directory.Delete(blockedTemp,false);}
        yield return Label(vi?"Thử lưu lại":"Retry save");Verify("Actual Retry reports successful local saving",Status().Contains(vi?"Đã lưu":"Saved"));
        reloaded=new ProfileStore(folder).Load();Verify("Retried save reopens with authored text, strokes and scientific attempts intact",!reloaded.writeProtected&&reloaded.profile.notebookPages.Any(note=>note.id==firstId&&JsonUtility.ToJson(note)==recoveryInk)&&attempts.SequenceEqual(reloaded.profile.attempts.Select(attempt=>JsonUtility.ToJson(attempt))));
        Field("Journal note body").text=body;yield return Press("Journal note save");Verify("Showcase restores its original body through the real field and Save",new ProfileStore(folder).Load().profile.notebookPages.Any(note=>note.id==firstId&&note.body==body));
        bool wasWorld=app.Input.SimulatorMode;yield return new WaitForSecondsRealtime(.3f);var background=app.Preview.CurrentTexture;
        Verify("Linked notebook background has fully settled before mode change",app.Preview.Alpha>.99f);
        app.Input.SetSimulator(true);yield return new WaitForSecondsRealtime(.24f);
        Verify("Notebook paper and ink reach full contrast after the UI entrance",app.GetComponentsInChildren<UiEntrance>().All(entrance=>entrance.GetComponent<CanvasGroup>().alpha>.999f));
        Verify("Switching notebook view preserves its background without a second fade",app.Preview.CurrentTexture==background&&app.Preview.Alpha>.99f);
        CheckRuling(Field("Journal note body"),3);capture("44-so-ghi-chep-tieng-Viet");app.Input.SetSimulator(wasWorld);yield return null;yield return null;
        yield return Label(vi?"‹ Quay lại":"‹ Back");yield return Open();yield return Press("Journal mode notes");Verify("Returning Home and reopening preserves written page",Field("Journal note title").text==title&&Field("Journal note body").text==body);
        yield return Press("Journal note delete");yield return Label(vi?"Hủy":"Cancel");Verify("Cancelling deletion retains the page",app.Profile.notebookPages.Any(note=>note.id==firstId));
        yield return Press("Journal note delete");yield return Label(vi?"Xác nhận":"Confirm");Verify("Confirmed deletion removes only selected notebook page",!app.Profile.notebookPages.Any(note=>note.id==firstId)&&app.Profile.notebookPages.Any(note=>note.id==secondId));
        reloaded=new ProfileStore(folder).Load();Verify("Deleted page stays deleted after disk reload",!reloaded.profile.notebookPages.Any(note=>note.id==firstId));
        Verify("Writing and deletion preserve every scientific attempt",attempts.SequenceEqual(app.Profile.attempts.Select(attempt=>JsonUtility.ToJson(attempt)))&&app.Profile.notebookPages.Count==initial+1);
        yield return Label(vi?"‹ Quay lại":"‹ Back");
    }
    static VlabWhiteboard Board()=>app.GetComponentsInChildren<VlabWhiteboard>().Single(board=>board.isActiveAndEnabled&&board.name=="Journal note ink");
    static PointerEventData InkPoint(VlabWhiteboard board,Vector2 normalized,int pointer=-1)
    {
        var rect=board.rectTransform;var world=rect.TransformPoint(rect.rect.min+Vector2.Scale(normalized,rect.rect.size));
        return new PointerEventData(EventSystem.current){pointerId=pointer,button=PointerEventData.InputButton.Left,eligibleForClick=true,position=RectTransformUtility.WorldToScreenPoint(board.canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:board.canvas.worldCamera,world)};
    }
    static IEnumerator Drawing(NotebookPage note,string body)
    {
        yield return Press("Journal note draw");var board=Board();Canvas.ForceUpdateCanvases();
        Verify("Notebook drawing replaces only the body area and retains the authored text",board.raycastTarget&&note.body==body&&!app.GetComponentsInChildren<TMP_InputField>().Any(field=>field.name=="Journal note body"));
        board.OnPointerDown(InkPoint(board,new Vector2(.1f,.3f),23));board.OnPointerDown(InkPoint(board,new Vector2(.9f,.8f),24));board.OnDrag(InkPoint(board,new Vector2(.3f,.7f),23));
        var release=InkPoint(board,new Vector2(.3f,.7f),23);release.dragging=true;release.eligibleForClick=false;board.OnPointerUp(release);
        Verify("Phone-style press and drag form one stroke while a second touch is ignored",note.inkStrokes.Count==1&&note.inkStrokes[0].points.Count==2);
        yield return Press("Journal ink pen");board.OnPointerClick(InkPoint(board,new Vector2(.5f,.5f)));board.OnPointerClick(InkPoint(board,new Vector2(.6f,.6f)));
        Verify("VR-style point selection joins points and New stroke separates lines",note.inkStrokes.Count==2&&note.inkStrokes[1].points.Count==2);
        yield return Press("Journal ink undo");Verify("Notebook Undo removes the last stroke",note.inkStrokes.Count==1);
        yield return Press("Journal ink erase");board.OnPointerClick(InkPoint(board,new Vector2(.2f,.5f)));Verify("Notebook eraser removes a touched segment's whole stroke",note.inkStrokes.Count==0);
        yield return Press("Journal ink undo");Verify("Notebook Undo restores accidentally erased ink",note.inkStrokes.Count==1&&note.inkStrokes[0].points.Count==2);
        yield return Press("Journal ink pen");board.OnPointerClick(InkPoint(board,new Vector2(.7f,.8f)));
        board.OnPointerDown(InkPoint(board,new Vector2(.8f,.2f),23));board.OnPointerUp(InkPoint(board,new Vector2(.8f,.2f),23));board.OnPointerClick(InkPoint(board,new Vector2(.85f,.25f),23));
        Verify("A phone tap is not applied again by its following click event",note.inkStrokes.Count==3&&note.inkStrokes[2].points.Count==1);yield return Press("Journal ink undo");
        CheckVisibleTitle(note.title);capture("44a-so-ve-tren-trang");yield return null;yield return Press("Journal note write");CheckVisibleTitle(note.title);Verify("Switching back to writing preserves all text and drawn strokes",Field("Journal note body").text==body&&note.inkStrokes.Count==2);
    }
    static IEnumerator Type(string name,string value)
    {
        var input=Field(name);Verify("Writing field uses readable Unicode-capable TMP input",input.characterLimit==(name.EndsWith("title",StringComparison.Ordinal)?120:12000)&&input.textComponent.fontSize>=(name.EndsWith("title",StringComparison.Ordinal)?32:28)&&!input.richText);
        input.ActivateInputField();yield return null;yield return null;input.MoveTextEnd(false);
        foreach(char character in value)input.ProcessEvent(new Event{type=EventType.KeyDown,keyCode=KeyCode.None,character=character});
        // ProcessEvent only calls KeyPressed; the native EventSystem path updates the label
        // after its event batch. Reproduce that required final step before measuring pixels.
        input.ForceLabelUpdate();
        yield return null;yield return null;Verify("Actual TMP text events preserve typed Vietnamese and newlines",input.text==value);input.DeactivateInputField();
        var done=Buttons().LastOrDefault(button=>button.name=="Notebook key Done");if(done)done.onClick.Invoke();yield return null;yield return null;
        if(name=="Journal note body")CheckRuling(input);
        else CheckVisibleTitle(value);
    }
    static void CheckVisibleTitle(string expected)
    {
        Canvas.ForceUpdateCanvases();var field=Field("Journal note title");var label=field.textComponent;label.ForceMeshUpdate();
        int visible=0;for(int i=0;i<label.textInfo.characterCount;i++)
        {
            var glyph=label.textInfo.characterInfo[i];if(!glyph.isVisible)continue;
            var centre=field.textViewport.InverseTransformPoint(label.rectTransform.TransformPoint((glyph.bottomLeft+glyph.topRight)*.5f));
            if(field.textViewport.rect.Contains(centre))visible++;
        }
        Verify("Notebook title has visible ink inside its viewport after typing and mode switches",label.text.Contains(expected)&&visible>=expected.Count(c=>!char.IsWhiteSpace(c))&&label.color.a>.9f&&!label.canvasRenderer.cull);
        Verify("The selected page caption updates while typing without reopening the page",Buttons().Any(button=>button.name.StartsWith("Journal note page ",StringComparison.Ordinal)&&button.GetComponentInChildren<TextMeshProUGUI>().text==expected));
    }
    static void CheckRuling(TMP_InputField input,int minimumVisibleLines=1)
    {
        Canvas.ForceUpdateCanvases();var text=input.textComponent;text.ForceMeshUpdate(true,true);var viewport=input.textViewport;
        var rules=viewport.GetComponentsInChildren<Image>().Where(image=>image.isActiveAndEnabled&&image.name=="Writing baseline rule").ToArray();
        Verify("Writing rules are masked, behind ink and do not intercept selection",viewport.GetComponent<RectMask2D>()&&viewport.GetComponent<NotebookRuling>()&&rules.Length>0&&rules.All(rule=>!rule.raycastTarget&&rule.transform.parent==viewport&&rule.transform.GetSiblingIndex()<text.transform.GetSiblingIndex()));
        int checkedLines=0;
        for(int i=0;i<text.textInfo.lineCount;i++)
        {
            float baseline=viewport.InverseTransformPoint(text.rectTransform.TransformPoint(new Vector3(0,text.textInfo.lineInfo[i].baseline,0))).y;
            if(baseline-2<=viewport.rect.yMin||baseline-2>=viewport.rect.yMax)continue;
            float nearest=rules.Select(rule=>baseline-viewport.InverseTransformPoint(rule.rectTransform.TransformPoint(rule.rectTransform.rect.center)).y).OrderBy(distance=>Mathf.Abs(distance-2)).First();
            Verify("Visible writing baseline aligns with its paper rule: "+i,Mathf.Abs(nearest-2)<.75f);checkedLines++;
        }
        Verify("Native typed body has at least "+minimumVisibleLines+" measured visible ruled baselines",checkedLines>=minimumVisibleLines);
        if(minimumVisibleLines>=3)
        {
            var positions=rules.Select(rule=>viewport.InverseTransformPoint(rule.rectTransform.TransformPoint(rule.rectTransform.rect.center)).y).OrderByDescending(y=>y).ToArray();
            var spacing=Enumerable.Range(1,positions.Length-1).Select(i=>positions[i-1]-positions[i]).ToArray();
            Debug.Log("VLAB notebook visible rule spacing: "+string.Join(", ",spacing.Select(value=>value.ToString("R",System.Globalization.CultureInfo.InvariantCulture))));
            Verify("Showcase paper rules continue across blank rows without skipped geometry",spacing.Length>=3&&spacing.All(value=>value>0)&&spacing.Max()-spacing.Min()<1.5f);
        }
    }
    static TMP_InputField Field(string name)=>app.GetComponentsInChildren<TMP_InputField>().Single(field=>field.isActiveAndEnabled&&field.name==name);
    static string Status()=>app.GetComponentsInChildren<TextMeshProUGUI>().Single(text=>text.isActiveAndEnabled&&text.name=="Journal note status").text;
    static IEnumerable<Button> Buttons()=>app.GetComponentsInChildren<Button>().Where(button=>button.isActiveAndEnabled);
    static IEnumerator Open(){yield return Click(()=>Buttons().LastOrDefault(button=>button.name=="Open journal"||button.GetComponentInChildren<TextMeshProUGUI>()?.text=="Sổ thực hành"),"Open journal");}
    static IEnumerator Press(string name){yield return Click(()=>Buttons().LastOrDefault(button=>button.name==name),name);}
    static IEnumerator Label(string text){yield return Click(()=>Buttons().LastOrDefault(button=>button.GetComponentInChildren<TextMeshProUGUI>()?.text==text),text);}
    static IEnumerator Click(Func<Button> find,string name){yield return new WaitForSecondsRealtime(.28f);var button=find();Verify("Notebook writing UI: "+name,button&&button.interactable);button.onClick.Invoke();yield return new WaitForSecondsRealtime(.28f);}
    static void Verify(string name,bool passed){lines.Add((passed?"PASS ":"FAIL ")+name);report(name,passed);if(!passed)throw new InvalidOperationException(name);}
    static void Write(string status){Directory.CreateDirectory("Evidence/Runtime");File.WriteAllLines("Evidence/Runtime/journal-notes.txt",new[]{status,"Explicit Vietnamese text typed by automated fixture into native TMP input; no fabricated measurements or learner progress. Disk reload checks do not replace an Android relaunch test."}.Concat(lines));}
}
#endif
