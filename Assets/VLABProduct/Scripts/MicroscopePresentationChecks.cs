#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Vlab.Product;

public static class MicroscopePresentationChecks
{
    static VlabApp app;static Action<string> capture;static Action<string,bool> report;static readonly List<string> lines=new List<string>();static long pointerSequence=60000;static bool readingsRayVerified;
    static bool Vi=>app.Profile.locale!="en";
    public static IEnumerator Run(VlabApp application,Action<string> screenshot,Action<string,bool> check)
    {
        app=application;capture=screenshot;report=check;lines.Clear();readingsRayVerified=false;Write("IN PROGRESS");
        var stack=new Stack<IEnumerator>();stack.Push(Scenario());
        while(stack.Count>0)
        {
            object yielded=null;bool more=false;Exception failure=null;
            try{more=stack.Peek().MoveNext();if(more)yielded=stack.Peek().Current;}catch(Exception error){failure=error;}
            if(failure!=null){lines.Add(failure.ToString());Write("FAIL");report("B01 presentation native UI journey completed",false);throw new InvalidOperationException("B01 microscope presentation checks failed",failure);}
            if(!more){(stack.Pop() as IDisposable)?.Dispose();continue;}
            if(yielded is IEnumerator nested&&!(yielded is CustomYieldInstruction)){stack.Push(nested);continue;}yield return yielded;
        }
        Write("PASS");
    }
    static IEnumerator Scenario()
    {
        Verify("B01 presentation starts at actual Home",app.Route=="home");bool originalSimulator=app.Input.SimulatorMode,originalMock=app.Input.UseMockController;string originalLocale=app.Profile.locale;
        yield return PressName("Subject B");yield return WaitRoute("browser");yield return PressName("Browser lesson B01");yield return Press("Start experiment");yield return WaitRoute("readiness");
        var readyTitle=app.GetComponentsInChildren<TextMeshProUGUI>().FirstOrDefault(text=>text.isActiveAndEnabled&&text.name=="Readiness title");Verify("Student readiness uses the new code-free title",readyTitle&&!readyTitle.text.Contains("B01")&&!readyTitle.isTextOverflowing);
        yield return Press("Enter station");yield return WaitRoute("activity");
        Verify("B01 actual native session loaded",app.Session?.spec.id=="B01"&&app.Apparatus.Driver is BiologyDriver);
        yield return Press("Open equipment");for(int p=0;p<3;p++)if(Find("Previous")&&Find("Previous").interactable)yield return Press("Previous");int page=0;
        for(int i=0;i<app.Apparatus.items.Count;i++)
        {
            while(page<i/6){yield return Press("Next");page++;}var item=app.Apparatus.items[i];yield return PressRaw(app.Apparatus.EquipmentLabel(item,Vi));yield return Press("Place at installation anchor");Verify("B01 installed "+item.assetId,item.installed);
        }
        yield return Press("Done");for(int i=0;i<4;i++){int before=app.Session.Operation;yield return Press("Perform current action");Verify("B01 native preparation "+(i+1),app.Session.Operation==before+1);}
        app.Input.SetSimulator(true);yield return new WaitForSecondsRealtime(.35f);
        // A forward-facing VR gaze otherwise dwells on the inspected image during captures,
        // silently rebuilding its modal and rearming the input guard between scripted clicks.
        // Park only the test adapter ray; the real camera/head orientation stays unchanged.
        Verify("B01 test pointer adapter initializes",app.Input.Controller.Submit(new ControllerSignal{sequence=pointerSequence++,receivedAt=Time.realtimeSinceStartupAsDouble,orientation=Quaternion.identity}));app.Input.Controller.Calibrate(Quaternion.identity);app.Input.UseMockController=true;ParkPointer();yield return null;yield return null;
        yield return Press("Station tools");yield return Press("Equipment");for(int p=0;p<3;p++)if(Find("Previous")&&Find("Previous").interactable)yield return Press("Previous");yield return Press("Fitted · Microscope");
        var driver=(BiologyDriver)app.Apparatus.Driver;var sdk=app.Apparatus.items.First(item=>item.assetId=="microscope").GetComponentInChildren<VlabMicroscope>();
        yield return CheckLiveReadingPages();yield return PressName("Instrument tab view");
        MicroscopePresentation.CheckGeometry(Verify);MicroscopePresentation.CheckBinding(Image(),driver,Verify);
        Vector2 source=Vector2.zero;int expectedCell=0,expectedRegion=0;
        for(int y=0;y<40&&expectedCell==0;y++)for(int x=0;x<40&&expectedCell==0;x++)
        {var candidate=new Vector2(.47f+x*.0015f,.47f+y*.0015f);if(driver.TryB01SourceIdentity(candidate,out int cell,out int region)){source=candidate;expectedCell=cell;expectedRegion=region;}}
        Verify("B01 fixture finds source structure near the common field center",expectedCell>0);
        var beginning=State();int preparation=app.Session.Operation;int classificationCount=beginning.structures.Count;
        foreach(string objective in new[]{"4× objective","10× objective","40× objective"})
        {
            int index=objective.StartsWith("4×",StringComparison.Ordinal)?0:objective.StartsWith("10",StringComparison.Ordinal)?1:2;
            yield return Control(index==0?"obj4":index==1?"obj10":"obj40");var image=Image();Verify("Actual microscope SDK objective changed to "+objective,sdk.ObjectiveIndex==index);
            float width=index==0?1:index==1?.4f:.1f;Verify(objective+" updates actual RawImage crop",Mathf.Abs(image.uvRect.width-width)<.00001f);
            string before=driver.SaveState();yield return null;yield return null;Verify(objective+" rendering does not mutate lesson state",before==driver.SaveState());
            yield return ImageClick(source);var state=State();Verify(objective+" real image pointer preserves global cell/structure ID",state.selectedCell==expectedCell&&state.selectedStructure==expectedRegion);
            Verify(objective+" source selection does not classify or advance preparation",state.structures.Count==classificationCount&&app.Session.Operation==preparation);
            Verify(objective+" selected source marker remains visible",Image().GetComponentsInChildren<Transform>().Any(value=>value.name=="Selected image location"&&value.gameObject.activeInHierarchy));
            yield return null;yield return null;capture("30-B01-objective-"+(index==0?4:index==1?10:40)+"-"+(Vi?"VI":"EN")+"-VR");
        }
        Verify("Higher power centers on the learner selected source point",Vector2.Distance(Image().uvRect.center,source)<.0001f);
        var presentation=Image().GetComponent<MicroscopePresentation>();presentation.RefreshNow();Verify("Existing target6.5mm produces sharp illustrative view",Mathf.Abs(sdk.FocusMm-6.5f)<.0001f&&presentation.BlurRadiusSourcePixels<.0001f);
        FocusDiagnostic("before focus increase",sdk,driver,presentation);
        yield return Control("focus+");presentation=Image().GetComponent<MicroscopePresentation>();presentation.RefreshNow();FocusDiagnostic("after first +0.5mm",sdk,driver,presentation);
        Verify("First focus UI increment reaches SDK and lesson state",Mathf.Abs(sdk.FocusMm-7)<.0001f&&Mathf.Abs(State().focus-7)<.0001f);
        yield return Control("focus+");presentation=Image().GetComponent<MicroscopePresentation>();presentation.RefreshNow();FocusDiagnostic("after second +0.5mm",sdk,driver,presentation);
        Verify("Focus SDK movement creates visible illustrative defocus",Mathf.Abs(sdk.FocusMm-7.5f)<.0001f&&presentation.BlurRadiusSourcePixels>1);
        string defocused=driver.SaveState();yield return null;yield return null;Verify("Defocus rendering does not change scientific state",defocused==driver.SaveState());capture("31-B01-illustrative-defocus-"+(Vi?"VI":"EN")+"-VR");
        yield return Control("focus-");yield return Control("focus-");presentation=Image().GetComponent<MicroscopePresentation>();presentation.RefreshNow();Verify("Focus returning to existing target clears blur",presentation.BlurRadiusSourcePixels<.0001f);
        Canvas.ForceUpdateCanvases();var status=app.GetComponentsInChildren<TextMeshProUGUI>().FirstOrDefault(text=>text.isActiveAndEnabled&&text.text.Contains(MicroscopePresentation.Notice(Vi)));
        Verify("Localized illustrative notice is present and fits actual text field",status&&!status.isTextOverflowing);
        if(status)foreach(bool vi in new[]{false,true})Verify((vi?"VI":"EN")+" illustrative notice fits the actual template",status.GetPreferredValues(MicroscopePresentation.Notice(vi),status.rectTransform.rect.width,Mathf.Infinity).y<=status.rectTransform.rect.height+1);
        // One explicit classification button demonstrates that selection alone never records an answer.
        yield return Control("structure:"+(expectedRegion-1));
        Verify("Actual classification button records exactly one manually selected source structure",State().structures.Count==classificationCount+1&&State().structures.Last().cell==expectedCell);
        yield return Press("Return to station");yield return Press("‹ Back");yield return WaitRoute("pause");yield return Press("Leave laboratory");yield return Press("Confirm");yield return WaitRoute("browser");yield return Press("‹ Home");yield return WaitRoute("home");
        Verify("Native inspection journey retains the review language",app.Profile.locale==originalLocale);
        app.Input.UseMockController=originalMock;app.Input.SetSimulator(originalSimulator);yield return null;yield return null;
    }
    static BiologyLessonState State()=>JsonUtility.FromJson<BiologyLessonState>(app.Apparatus.Driver.SaveState());
    static RawImage Image()=>app.GetComponentsInChildren<InstrumentInspection>().Single(value=>value.isActiveAndEnabled).GetComponent<RawImage>();
    static IEnumerator ImageClick(Vector2 source)
    {
        yield return new WaitForSecondsRealtime(.26f);var image=Image();Rect uv=image.uvRect,rect=image.rectTransform.rect;float x=(source.x-uv.x)/uv.width,y=(source.y-uv.y)/uv.height;Verify("Source fixture is inside displayed crop",x>=0&&x<=1&&y>=0&&y<=1);
        var canvas=image.GetComponentInParent<Canvas>();Camera camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;Vector3 world=image.rectTransform.TransformPoint(new Vector3(rect.xMin+x*rect.width,rect.yMin+y*rect.height));
        image.GetComponent<InstrumentInspection>().OnPointerClick(new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(camera,world)});yield return null;yield return null;
    }
    static IEnumerable<Button> Buttons()=>app.GetComponentsInChildren<Button>().Where(button=>button.isActiveAndEnabled);
    static string Label(Button button)=>button.GetComponentInChildren<TextMeshProUGUI>()?.text??"";
    static Button Find(string label)=>Buttons().LastOrDefault(button=>Label(button)==Localized(label));
    static IEnumerator PressStarts(string label){yield return PressButton(()=>Buttons().FirstOrDefault(button=>Label(button).StartsWith(label,StringComparison.Ordinal)),label);}
    static IEnumerator PressName(string name){yield return PressButton(()=>Buttons().FirstOrDefault(button=>button.name==name),name);}
    static IEnumerator Press(string label){yield return PressButton(()=>Find(label),label);}
    static IEnumerator PressRaw(string label){yield return PressButton(()=>Buttons().LastOrDefault(button=>Label(button)==label),label);}
    static string Localized(string label)
    {
        if(!Vi)return label;
        switch(label)
        {
            case "Start experiment":return "Bắt đầu thí nghiệm";case "Enter station":return "Vào trạm";case "Open equipment":return "Mở dụng cụ";case "Previous":return "Trang trước";case "Next":return "Trang sau";case "Place at installation anchor":return "Đặt vào vị trí lắp";case "Done":return "Xong";case "Perform current action":return "Thực hiện bước này";case "Station tools":return "Dụng cụ & hướng dẫn";case "Equipment":return "Dụng cụ";case "Fitted · Microscope":return "Đã lắp · Kính hiển vi";case "Return to station":return "Trở lại trạm";case "‹ Back":return "‹ Quay lại";case "Leave laboratory":return "Rời phòng";case "Confirm":return "Xác nhận";case "‹ Home":return "‹ Trang chủ";case "More readings":return "Số đo tiếp";case "Previous readings":return "Số đo trước";default:return label;
        }
    }
    static IEnumerator Control(string id)
    {
        var control=app.Apparatus.Driver.Controls("microscope").Single(value=>value.id==id);string label=Vi?control.vi:control.en;
        // Biology has five optics controls on page1; labels may occupy more than one later page.
        for(int guard=0;guard<8&&!Buttons().Any(value=>Label(value)==label);guard++)
        {var previous=Find("Previous");if(!previous||!previous.interactable)break;yield return Press("Previous");}
        for(int guard=0;guard<8&&!Buttons().Any(value=>Label(value)==label);guard++)
        {var next=Find("Next");Verify("Requested biology control page is reachable: "+id,next&&next.interactable);yield return Press("Next");}
        yield return PressRaw(label);
    }
    public static IEnumerator CheckLiveReadingPages(VlabApp application,Action<string> screenshot,Action<string,bool> check)
    {app=application;capture=screenshot;report=check;yield return CheckLiveReadingPages();}
    public static IEnumerator CheckLiveReadingPages()
    {
        if(app.Input.UseMockController&&app.Input.SimulatorMode&&!readingsRayVerified)
        {
            yield return RayPressReadings();readingsRayVerified=true;
        }
        else yield return PressName("Instrument tab readings");
        var expected=app.Session.Observe().readings.ToArray();var visited=new List<string>();int page=0;
        do
        {
            Canvas.ForceUpdateCanvases();var cards=app.GetComponentsInChildren<ObservationCardBinding>().Where(value=>value.isActiveAndEnabled).ToArray();
            Verify("Live reading page contains the actual science slice",cards.Select(card=>card.Key).SequenceEqual(expected.Skip(page*4).Take(4).Select(reading=>reading.key)));
            foreach(var card in cards)
            {
                var source=expected.Single(reading=>reading.key==card.Key);string value=ObservationCards.FormatValue(source.value,Vi);
                string unit=source.unit??"";
                if(unit=="presence"){value=source.value>.5?(Vi?"Có":"Present"):(Vi?"Không":"Absent");unit="";}
                if(source.key=="BTBstate"&&unit=="interval"){value=source.value==1?(Vi?"≤ 6,0":"≤ 6.0"):source.value==2?(Vi?"6,0–7,6":"6.0–7.6"):source.value==3?(Vi?"≥ 7,6":"≥ 7.6"):(Vi?"Chưa đo":"Unread");unit="pH";}
                Verify("Live value matches Session.Observe: "+card.Key,card.Value.text==value&&card.Unit.text==unit);
                Verify("Live value/label/unit are large and fit: "+card.Key,card.Value.fontSize>=48&&card.Label.fontSize>=26&&card.Unit.fontSize>=26&&!card.Value.isTextOverflowing&&!card.Label.isTextOverflowing&&!card.Unit.isTextOverflowing);
                Verify("Live label uses current locale: "+card.Key,card.Label.text==VlabApp.ReadingCaption(source,Vi));visited.Add(card.Key);
            }
            if(page==0){yield return null;yield return null;capture("32-"+app.Session.spec.id+"-so-do-truc-tiep-"+(Vi?"VI":"EN")+"-VR");}
            var next=Find("More readings");if(!next||!next.interactable)break;yield return Press("More readings");page++;
        }while(page<12);
        Verify("All live readings remain reachable without omissions",visited.SequenceEqual(expected.Select(reading=>reading.key)));
        if(expected.Length==4)Verify("B01 shows all four actual observations on one page",page==0&&visited.Count==4);
        while(Find("Previous readings")&&Find("Previous readings").interactable)yield return Press("Previous readings");
    }
    static IEnumerator PressButton(Func<Button> lookup,string description)
    {if(app.Input.UseMockController)ParkPointer();yield return new WaitForSecondsRealtime(.26f);var button=lookup();Verify("B01 UI target: "+description,button&&button.interactable);button.onClick.Invoke();if(app.Input.UseMockController)ParkPointer();yield return new WaitForSecondsRealtime(.26f);}
    static void ParkPointer()=>app.Input.Controller.Submit(new ControllerSignal{sequence=pointerSequence++,receivedAt=Time.realtimeSinceStartupAsDouble,orientation=Quaternion.LookRotation(Vector3.up,Vector3.forward)});
    static IEnumerator RayPressReadings()
    {
        yield return new WaitForSecondsRealtime(.26f);Canvas.ForceUpdateCanvases();
        var button=Buttons().Single(value=>value.name=="Instrument tab readings");var rect=button.GetComponent<RectTransform>();Vector3 center=rect.TransformPoint(rect.rect.center);
        Quaternion orientation=Quaternion.LookRotation((center-app.View.transform.position).normalized);
        Verify("Actual VR tab target is enabled",button.interactable);
        Verify("Controller primes released signal for actual VR tab ray",app.Input.Controller.Submit(new ControllerSignal{sequence=pointerSequence++,receivedAt=Time.realtimeSinceStartupAsDouble,orientation=orientation,select=false}));yield return null;yield return null;
        Verify("Controller sends fresh select edge through actual VR tab ray",app.Input.Controller.Submit(new ControllerSignal{sequence=pointerSequence++,receivedAt=Time.realtimeSinceStartupAsDouble,orientation=orientation,select=true}));yield return null;yield return null;
        app.Input.Controller.Submit(new ControllerSignal{sequence=pointerSequence++,receivedAt=Time.realtimeSinceStartupAsDouble,orientation=orientation,select=false});yield return null;yield return null;
        Verify("Foreground live-reading tab opens through product VR ray despite cabinet",app.GetComponentsInChildren<ObservationCardBinding>().Any(value=>value.isActiveAndEnabled)&&!app.GetComponentsInChildren<InstrumentInspection>().Any(value=>value.isActiveAndEnabled));
        ParkPointer();yield return null;yield return null;
    }
    static void FocusDiagnostic(string stage,VlabMicroscope sdk,BiologyDriver driver,MicroscopePresentation presentation)
    {
        var state=State();string parameters=string.Join(",",app.Session.parameters.Select(pair=>pair.Key+"="+pair.Value.ToString("R",System.Globalization.CultureInfo.InvariantCulture)));
        lines.Add("DIAGNOSTIC "+stage+" sdkFocus="+sdk.FocusMm.ToString("R")+" stateFocus="+state.focus.ToString("R")+" quality="+driver.MicroscopeFocusQuality.ToString("R")+" blur="+presentation.BlurRadiusSourcePixels.ToString("R")+" crop="+Image().uvRect+" selectedCell="+state.selectedCell+" parameters="+parameters);Write("IN PROGRESS");
    }
    static IEnumerator WaitRoute(string route){double end=Time.realtimeSinceStartupAsDouble+30;while(app.Route!=route&&Time.realtimeSinceStartupAsDouble<end)yield return null;Verify("B01 route: "+route,app.Route==route);yield return new WaitForSecondsRealtime(.26f);}
    static void Verify(string name,bool passed){lines.Add((passed?"PASS ":"FAIL ")+name);report(name,passed);if(!passed)throw new InvalidOperationException(name);}
    static void Write(string status){Directory.CreateDirectory("Evidence/Runtime");File.WriteAllLines("Evidence/Runtime/microscope-presentation.txt",new[]{status,"Actual B01 SDK controls, pointer-handler UV mapping and Editor world-space captures; no physical headset/phone evidence."}.Concat(lines));}
}
#endif
