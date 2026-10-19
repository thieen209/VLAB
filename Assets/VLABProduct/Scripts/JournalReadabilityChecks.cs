#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Vlab.Product;

public static class JournalReadabilityChecks
{
    static VlabApp app;static Action<string> capture;static Action<string,bool> report;static readonly List<string> log=new List<string>();
    public static IEnumerator Run(VlabApp application,Action<string> screenshot,Action<string,bool> check)
    {
        app=application;capture=screenshot;report=check;log.Clear();Write("IN PROGRESS");var stack=new Stack<IEnumerator>();stack.Push(Scenario());
        while(stack.Count>0)
        {
            object value=null;bool more=false;Exception failure=null;try{more=stack.Peek().MoveNext();if(more)value=stack.Peek().Current;}catch(Exception error){failure=error;}
            if(failure!=null){log.Add(failure.ToString());Write("FAIL");report("Readable book journey completed",false);throw new InvalidOperationException("Journal readability checks failed",failure);}
            if(!more){(stack.Pop() as IDisposable)?.Dispose();continue;}if(value is IEnumerator nested&&!(value is CustomYieldInstruction)){stack.Push(nested);continue;}yield return value;
        }
        Write("PASS");
    }
    static IEnumerator Scenario()
    {
        Verify("Book review begins at Home",app.Route=="home");Verify("Book review uses the actual saved47-record dataset",app.Profile.attempts.Count>=47);int count=app.Profile.attempts.Count;string originalLocale=app.Profile.locale;bool originalWorld=app.Input.SimulatorMode;
        string saved=JsonUtility.ToJson(app.Profile);var originalModelIdentities=app.Profile.attempts.Select(attempt=>attempt.model).ToArray();if(originalLocale=="en"){yield return Label("Settings");yield return Label("Language: English");yield return Label("Áp dụng");}
        Verify("Book review is Vietnamese first",app.Profile.locale=="vi");
        yield return PressOpen();yield return Route("notebook");yield return Press("Journal mode records");
        var visited=new HashSet<string>();
        foreach(string subject in new[]{"P","C","B","M"})
        {
            yield return Press("Journal subject "+subject);
            do
            {
                foreach(var button in Buttons().Where(value=>value.name.StartsWith("Journal lesson ",StringComparison.Ordinal)))
                {visited.Add(button.name.Substring("Journal lesson ".Length));Verify("Contents hide internal code: "+button.name,!Regex.IsMatch(LabelOf(button),@"\b[PCBM]0[1-8]\b"));Canvas.ForceUpdateCanvases();Verify("Contents title and saved-count caption fit",!button.GetComponentInChildren<TextMeshProUGUI>().isTextOverflowing);}
                var next=Find("Journal index next");if(!next||!next.interactable)break;yield return Press("Journal index next");
            }while(true);
        }
        Verify("Actual contents navigation reaches all32 lessons",visited.Count==32&&LessonSpec.All.Keys.All(visited.Contains));
        string id=app.Profile.attempts.GroupBy(record=>record.experimentId).OrderByDescending(group=>group.Count()).First().Key;yield return SelectLesson(id);
        var records=app.Profile.attempts.Where(record=>record.experimentId==id).OrderByDescending(record=>record.savedAtUtc,StringComparer.Ordinal).ToArray();var record=records[0];
        Verify("Reading page dominates book contents",Named<RectTransform>("Journal record page").rect.width>=1000&&Named<RectTransform>("Journal index page").rect.width<=320);
        Verify("Book title hides experiment codes",!Regex.IsMatch(Named<TextMeshProUGUI>("Journal lesson title").text,@"\b[PCBM]0[1-8]\b"));
        var observed=new List<string>();
        do
        {
            Canvas.ForceUpdateCanvases();foreach(var card in app.GetComponentsInChildren<ObservationCardBinding>().Where(value=>value.isActiveAndEnabled))
            {observed.Add(card.Key);Verify("Saved metric value is large and fits: "+card.Key,card.Value.fontSize>=48&&!card.Value.isTextOverflowing);Verify("Saved metric label and unit are legible: "+card.Key,card.Label.fontSize>=26&&card.Unit.fontSize>=26&&!card.Label.isTextOverflowing&&!card.Unit.isTextOverflowing);}
            var next=Find("Journal content next");if(!next.interactable)break;yield return Press("Journal content next");
        }while(true);
        Verify("Measurement pages retain every original reading exactly once",observed.SequenceEqual(record.readings.Select(reading=>reading.key)));
        yield return Press("Journal tab 0");yield return Capture("40-so-thuc-hanh-so-do-VI");
        if(records.Length>1)
        {
            string newest=Named<TextMeshProUGUI>("Journal timestamp").text;yield return Press("Journal older attempt");Verify("Older record navigation retains actual saved timestamp",Named<TextMeshProUGUI>("Journal timestamp").text.Contains(LocalTime(records[1].savedAtUtc)));yield return Press("Journal newer attempt");Verify("Newer record navigation returns to original attempt",Named<TextMeshProUGUI>("Journal timestamp").text==newest);
        }
        yield return Press("Journal tab 1");var parameters=LessonSpec.All[id].parameters;int parameterIndex=0;
        do
        {
            foreach(var text in app.GetComponentsInChildren<TextMeshProUGUI>().Where(value=>value.isActiveAndEnabled&&value.name.StartsWith("Journal condition value ",StringComparison.Ordinal)))
            {
                var parameter=parameters[parameterIndex];string exact=record.parameterValues!=null&&parameterIndex<record.parameterValues.Length?record.parameterValues[parameterIndex].ToString("R",CultureInfo.InvariantCulture):"Không được lưu";
                Verify("Condition retains exact saved value: "+parameter.key,text.text==exact+(string.IsNullOrWhiteSpace(parameter.unit)?"":" "+parameter.unit)&&text.fontSize>=32&&!text.isTextOverflowing);parameterIndex++;
            }
            if(!Find("Journal content next").interactable)break;yield return Press("Journal content next");
        }while(true);
        Verify("All original parameters remain reachable",parameterIndex==parameters.Length);yield return Press("Journal tab 1");yield return Capture("41-so-thuc-hanh-dieu-kien-VI");
        yield return Press("Journal tab 2");var pages=new List<string>();
        do{var text=Named<TextMeshProUGUI>("Journal narrative page");Canvas.ForceUpdateCanvases();Verify("Interpretation page uses readable text without clipping",text.fontSize>=28&&!text.isTextOverflowing);pages.Add(text.text);if(!Find("Journal content next").interactable)break;yield return Press("Journal content next");}while(true);
        string explanation=JournalView.Interpretation(record,true);if(!string.IsNullOrWhiteSpace(record.observation))explanation+="\n\n"+record.observation;
        Verify("Paged interpretation retains attached historical notes without technical model identity",Normalize(string.Join(" ",pages))==Normalize(explanation));
        Verify("Technical model identity remains untouched in the saved records",originalModelIdentities.SequenceEqual(app.Profile.attempts.Select(attempt=>attempt.model)));
        yield return Press("Journal tab 2");yield return Capture("42-so-thuc-hanh-giai-thich-VI");
        yield return Press("Journal tab 3");yield return Capture("43-so-thuc-hanh-khao-sat-tiep-VI");
        yield return Press("Journal compare");yield return Route("notebook-analysis");yield return Label("‹ Quay lại");yield return Route("notebook");Verify("Comparison returns to book without discarding records",app.Profile.attempts.Count==count);
        string empty=LessonSpec.All.Keys.FirstOrDefault(candidate=>!app.Profile.attempts.Any(attempt=>attempt.experimentId==candidate));if(empty!=null){yield return SelectLesson(empty);Verify("Unrecorded lesson is not presented as complete",!Find("Journal compare").interactable&&Named<TextMeshProUGUI>("Journal timestamp").text.StartsWith("Chưa có lượt",StringComparison.Ordinal));}
        if(app.Profile.checkpoint!=null){yield return SelectLesson(app.Profile.checkpoint.experimentId);yield return Press("Journal tab 3");Verify("Checkpoint is explicitly distinguished from completed measurement",Named<TextMeshProUGUI>("Journal narrative page").text.Contains("chưa phải số đo"));}
        yield return Label("‹ Quay lại");yield return Route("home");if(originalLocale=="en"){yield return Label("Cài đặt");yield return Label("Ngôn ngữ: Tiếng Việt");yield return Label("Apply");}
        app.Input.SetSimulator(originalWorld);yield return null;yield return null;Verify("Technical model identities survive all book navigation",originalModelIdentities.SequenceEqual(app.Profile.attempts.Select(attempt=>attempt.model)));Verify("Book navigation does not fabricate or alter records",saved==JsonUtility.ToJson(app.Profile));
    }
    static string LocalTime(string timestamp)=>DateTime.TryParse(timestamp,CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind,out var date)?date.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss zzz",CultureInfo.InvariantCulture):timestamp;
    static string Normalize(string value)=>Regex.Replace(value,@"\s+"," ").Trim();
    static IEnumerator SelectLesson(string id){yield return Press("Journal subject "+id.Substring(0,1));for(int p=0;p<(int.Parse(id.Substring(1))-1)/3;p++)yield return Press("Journal index next");yield return Press("Journal lesson "+id);}
    static IEnumerator Capture(string name){bool previous=app.Input.SimulatorMode;app.Input.SetSimulator(true);yield return new WaitForSecondsRealtime(.24f);capture(name);app.Input.SetSimulator(previous);yield return null;yield return null;}
    static IEnumerable<Button> Buttons()=>app.GetComponentsInChildren<Button>().Where(value=>value.isActiveAndEnabled);
    static string LabelOf(Button button)=>button.GetComponentInChildren<TextMeshProUGUI>()?.text??"";
    static Button Find(string name)=>Buttons().LastOrDefault(value=>value.name==name);
    static T Named<T>(string name)where T:Component=>app.GetComponentsInChildren<T>().Single(value=>value.gameObject.activeInHierarchy&&value.name==name);
    static IEnumerator PressOpen(){yield return Click(()=>Find("Open journal")??Buttons().LastOrDefault(value=>LabelOf(value)=="Sổ tay"||LabelOf(value)=="Sổ thực hành"||LabelOf(value)=="Mở sổ thực hành"),"Open journal");}
    static IEnumerator Press(string name){yield return Click(()=>Find(name),name);}
    static IEnumerator Label(string label){yield return Click(()=>Buttons().LastOrDefault(value=>LabelOf(value)==label),label);}
    static IEnumerator Click(Func<Button> lookup,string name){yield return new WaitForSecondsRealtime(.28f);var button=lookup();Verify("Book UI target: "+name,button&&button.interactable);button.onClick.Invoke();yield return new WaitForSecondsRealtime(.28f);}
    static IEnumerator Route(string route){double end=Time.realtimeSinceStartupAsDouble+20;while(app.Route!=route&&Time.realtimeSinceStartupAsDouble<end)yield return null;Verify("Book route: "+route,app.Route==route);yield return null;yield return null;}
    static void Verify(string name,bool passed){log.Add((passed?"PASS ":"FAIL ")+name);report(name,passed);if(!passed)throw new InvalidOperationException(name);}
    static void Write(string status){Directory.CreateDirectory("Evidence/Runtime");File.WriteAllLines("Evidence/Runtime/journal-readability.txt",new[]{status,"Actual existing offline records and UI callbacks; Editor world-space captures do not prove physical headset readability."}.Concat(log));}
}
#endif
