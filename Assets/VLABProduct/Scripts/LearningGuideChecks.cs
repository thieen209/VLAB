#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Vlab.Product;

public static class LearningGuideChecks
{
    static VlabApp app;static Action<string> capture;static Action<string,bool> report;static readonly List<string> log=new List<string>();
    public static IEnumerator Run(VlabApp application,Action<string> screenshot,Action<string,bool> check)
    {
        app=application;capture=screenshot;report=check;log.Clear();Write("IN PROGRESS");var stack=new Stack<IEnumerator>();stack.Push(Scenario());
        while(stack.Count>0)
        {
            object value=null;bool more=false;Exception failure=null;try{more=stack.Peek().MoveNext();if(more)value=stack.Peek().Current;}catch(Exception error){failure=error;}
            if(failure!=null){log.Add(failure.ToString());Write("FAIL");throw new InvalidOperationException("Learning guide native checks failed",failure);}
            if(!more){(stack.Pop() as IDisposable)?.Dispose();continue;}if(value is IEnumerator nested&&!(value is CustomYieldInstruction)){stack.Push(nested);continue;}yield return value;
        }
        Write("PASS");
    }
    static void Templates()
    {
        Verify("Study catalogue contains exactly the32 authoritative lessons",LearningGuide.All.Count==32&&LearningGuide.All.Keys.OrderBy(id=>id).SequenceEqual(LessonSpec.All.Keys.OrderBy(id=>id)));
        foreach(string subject in new[]{"P","C","B","M"})Verify("Study catalogue has eight lessons for "+subject,LearningGuide.All.Keys.Count(id=>id.StartsWith(subject,StringComparison.Ordinal))==8);
        foreach(var entry in LearningGuide.All.Values)
        {
            Verify("Authored study sections are all available: "+entry.id,new[]{entry.titleVi,entry.learnVi,entry.practiceVi,entry.pitfallsVi,entry.examplesVi,entry.examVi,entry.checkVi}.All(value=>!string.IsNullOrWhiteSpace(value)));
            Verify("Resolved offline source titles and URLs are present: "+entry.id,entry.sources!=null&&entry.sources.Length>0&&entry.sources.All(source=>!string.IsNullOrWhiteSpace(source.title)&&!string.IsNullOrWhiteSpace(source.url)));
        }
        var owner=new GameObject("Temporary learning-guide native template checks"){hideFlags=HideFlags.HideAndDontSave};
        try
        {
            var ui=new VlabUi(owner.transform);ui.canvas.enabled=false;var host=ui.Rect(ui.surface,"Learning template",0,0,1600,1000);
            string corpus=string.Concat(LearningGuide.All.Keys.SelectMany(id=>Enumerable.Range(0,4).Select(chapter=>LearningGuide.Chapter(id,chapter,true))));
            ui.font.TryAddCharacters(new string(corpus.Where(character=>!char.IsControl(character)).Distinct().ToArray()),out string missing);
            Verify("Native study font contains all formula, unit and Vietnamese source glyphs: "+missing,string.IsNullOrEmpty(missing));
            var template=ui.Text(host,"",0,0,1036,292,28);template.richText=false;int pagesChecked=0;
            foreach(bool vi in new[]{true,false})foreach(bool large in new[]{false,true})
            {
                template.fontSize=28*(large?1.12f:1);bool fits=true,exact=true;string failure="";
                foreach(string id in LessonSpec.All.Keys)for(int chapter=0;chapter<4;chapter++)
                {
                    string source=LearningGuide.Chapter(id,chapter,vi);var pages=LearningGuide.Pages(template,id,chapter,vi,out int answerPage);exact&=Characters(string.Concat(pages))==Characters(source);
                    if(chapter==2)Verify("Self-check hides the authored answer until deliberate page reveal "+id+" "+vi+" "+large,answerPage>0&&pages.Take(answerPage).All(page=>!page.Contains("Đáp:"))&&pages[answerPage].StartsWith("Đáp:",StringComparison.Ordinal));
                    foreach(string page in pages){template.text=page;template.ForceMeshUpdate(true,true);pagesChecked++;float needed=template.GetPreferredValues(page,1036,Mathf.Infinity).y;if(template.isTextOverflowing||needed>293){fits=false;failure=id+" chapter="+chapter+" needed="+needed;}}
                    if(!vi)exact&=source.StartsWith("VIETNAMESE STUDY GUIDE",StringComparison.Ordinal);
                }
                Verify("All32 four-chapter pages preserve exact non-whitespace source characters "+(vi?"VI":"EN")+" large="+large,exact);
                Verify("All32 study pages fit native1036x292 at28px or larger "+(vi?"VI":"EN")+" large="+large+" "+failure,fits);
            }
            log.Add("Native study page geometries checked="+pagesChecked);
            UnityEngine.Object.DestroyImmediate(host.gameObject);
            // A separate empty profile exercises real book callbacks. It never
            // clears or temporarily substitutes the learner's actual profile.
            var empty=new LocalProfile();string before=JsonUtility.ToJson(empty);var state=new JournalState{notesMode=false};RectTransform pageRoot=null;
            Action refresh=null;refresh=()=>{if(pageRoot)UnityEngine.Object.DestroyImmediate(pageRoot.gameObject);pageRoot=ui.Rect(ui.surface,"Empty-profile learning book",0,0,1600,1000);JournalView.Render(ui,pageRoot,empty,ui.Vietnamese,state,refresh,()=>{},_=>"");};
            foreach(bool vi in new[]{true,false})foreach(bool large in new[]{false,true})
            {
                ui.Vietnamese=vi;ui.LargeText=large;state.contentTab=0;refresh();pageRoot.GetComponentsInChildren<Button>().Single(button=>button.name=="Journal tab 4").onClick.Invoke();
                Verify("Study opens through actual tab callback with zero attempts "+vi+" "+large,state.contentTab==4&&empty.attempts.Count==0&&pageRoot.GetComponentsInChildren<TextMeshProUGUI>().Any(text=>text.name=="Journal study page"));
                Verify("Study hides unavailable attempt and comparison controls",!pageRoot.GetComponentsInChildren<Button>().Any(button=>button.name=="Journal compare"||button.name=="Journal older attempt"||button.name=="Journal newer attempt"));
                Verify("Study prioritises reading space over secondary navigation",pageRoot.GetComponentsInChildren<Button>().Count(button=>button.name.StartsWith("Journal tab ",StringComparison.Ordinal))==1&&pageRoot.GetComponentsInChildren<TextMeshProUGUI>().Single(text=>text.name=="Journal study page").rectTransform.rect.height>=292);
                for(int chapter=0;chapter<4;chapter++)
                {
                    pageRoot.GetComponentsInChildren<Button>().Single(button=>button.name=="Journal study chapter "+chapter).onClick.Invoke();
                    Verify("Chapter callback selects authored content and resets its page",state.studyChapter==chapter&&state.contentPage==0&&pageRoot.GetComponentsInChildren<TextMeshProUGUI>().Single(text=>text.name=="Journal study page").text.Length>0);
                }
                Canvas.ForceUpdateCanvases();foreach(var text in pageRoot.GetComponentsInChildren<Button>().Select(button=>button.GetComponentInChildren<TextMeshProUGUI>())){text.ForceMeshUpdate(true,true);Verify("Book navigation label fits "+text.text+" large="+large,!text.isTextOverflowing);}
            }
            Verify("Zero-attempt study callbacks leave the isolated profile unchanged",JsonUtility.ToJson(empty)==before);
        }
        finally{UnityEngine.Object.DestroyImmediate(owner);}
    }
    static IEnumerator Scenario()
    {
        Verify("Learning guide UI review starts at Vietnamese Home",app.Route=="home"&&app.Profile.locale=="vi");string profile=JsonUtility.ToJson(app.Profile);Templates();
        yield return Press("Open journal");yield return Press("Journal mode records");yield return Press("Journal tab 4");
        foreach(var sample in new[]{new{subject="P",id="P01",chapter=0,file="60-so-hoc-tap-con-lac-kien-thuc-VI"},new{subject="C",id="C01",chapter=1,file="61-so-hoc-tap-chuan-do-van-dung-VI"},new{subject="B",id="B08",chapter=2,file="62-so-hoc-tap-nguyen-phan-tu-kiem-tra-VI"}})
        {
            yield return Press("Journal subject "+sample.subject);for(int p=0;p<(int.Parse(sample.id.Substring(1))-1)/3;p++)yield return Press("Journal index next");yield return Press("Journal lesson "+sample.id);yield return Press("Journal study chapter "+sample.chapter);
            var pages=new List<string>();int guard=0;
            do
            {
                var text=Named<TextMeshProUGUI>("Journal study page");text.ForceMeshUpdate(true,true);Verify("Live book learning page is readable and fits",text.fontSize>=28&&!text.isTextOverflowing);pages.Add(text.text);var next=Find("Journal content next");if(!next.interactable)break;Verify("Learning page navigation remains bounded",++guard<100);yield return Press("Journal content next");
            }while(true);
            Verify("Actual book page callbacks reach the entire authored chapter "+sample.id,Characters(string.Concat(pages))==Characters(LearningGuide.Chapter(sample.id,sample.chapter,true)));
            yield return Press("Journal study chapter "+sample.chapter);yield return null;yield return null;capture(sample.file);
        }
        yield return Press("Journal study chapter 3");Verify("Sources are available through actual book navigation",Named<TextMeshProUGUI>("Journal study page").text.Contains("NGUỒN"));
        yield return Click(()=>app.GetComponentsInChildren<Button>().LastOrDefault(button=>button.isActiveAndEnabled&&button.GetComponentInChildren<TextMeshProUGUI>()?.text=="‹ Quay lại"),"Return Home");
        Verify("Study navigation returns Home without fabricating or altering learner data",app.Route=="home"&&JsonUtility.ToJson(app.Profile)==profile);
    }
    static string Characters(string value)=>Regex.Replace(value??"",@"\s","");
    static Button Find(string name)=>app.GetComponentsInChildren<Button>().LastOrDefault(button=>button.isActiveAndEnabled&&button.name==name);
    static T Named<T>(string name)where T:Component=>app.GetComponentsInChildren<T>().Single(value=>value.gameObject.activeInHierarchy&&value.name==name);
    static IEnumerator Press(string name){yield return Click(()=>Find(name),name);}
    static IEnumerator Click(Func<Button> lookup,string name){yield return new WaitForSecondsRealtime(.28f);var button=lookup();Verify("Learning UI target: "+name,button&&button.interactable);button.onClick.Invoke();yield return new WaitForSecondsRealtime(.28f);}
    static void Verify(string name,bool okay){log.Add((okay?"PASS ":"FAIL ")+name);report(name,okay);if(!okay)throw new InvalidOperationException(name);}
    static void Write(string status){Directory.CreateDirectory("Evidence/Runtime");File.WriteAllLines("Evidence/Runtime/learning-guides.txt",new[]{status,"Actual metadata and native TMP/book callbacks; template geometry and Editor screenshots do not establish physical headset readability."}.Concat(log));}
}
#endif
