#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Vlab.Product;

// Test-only observer. Product actions are exclusively queued touchscreen events.
public sealed class SecondTestUiProbe : MonoBehaviour
{
    [Serializable] public sealed class LessonResult
    {
        public string id,status="NOT RUN",blocker="",lastTask="",phase="",screenshot="";
        public bool entered,assembled,started,recorded,reopened,instrumentCaptured,observingCueCaptured;
        public int taps,drags,holds,navigationRetries,operations,installed,entryWorldChecks,assembledWorldChecks;
        public string[] worldFailures=Array.Empty<string>();
    }
    [Serializable] sealed class Report
    {
        public string scope="Editor queued touchscreen through actual UGUI. Not physical Android or VR certification.";
        public string profile,selection;public float lessonTimeout;public int screenWidth,screenHeight;public bool visualReview;public string safeArea;public LessonResult[] lessons;public string[] runtimeErrors,scopedRegressions;
    }
    [Serializable] sealed class VisualButton {public string name,caption;public Rect bounds;public bool centreTouchable;}
    [Serializable] sealed class VisualImage {public string name;public Rect bounds,uvRect;public int width,height;}
    [Serializable] sealed class VisualRequest
    {
        public string requestId,experimentId,task,screenshot,instructions;
        public int screenWidth,screenHeight;public VisualButton[] buttons;public VisualImage[] images;public Rect[] scrollViewports;
    }
    [Serializable] sealed class VisualAction {public string kind,button;public float x,y,toX,toY,seconds;}
    [Serializable] sealed class VisualCommand {public string requestId,stopReason;public VisualAction[] actions;}
    [Serializable] sealed class VisualResponse {public string requestId,status;public int actionsApplied;}
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    readonly List<LessonResult> results=new List<LessonResult>();
    readonly List<string> trace=new List<string>(),errors=new List<string>(),scopedRows=new List<string>();
    Touchscreen touch,previous;string folder,selection;LessonResult current;int serial=99600;float lessonTimeout=180;bool visualReview;
    VlabApp App=>VlabApp.Instance;
    void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception)errors.Add(message+"\n"+stack);}
    static string Caption(Button button)=>button.GetComponentInChildren<TextMeshProUGUI>()?.text??"";
    Button[] Buttons()=>App?App.GetComponentsInChildren<Button>().Where(b=>b.isActiveAndEnabled&&b.interactable).ToArray():Array.Empty<Button>();
    Button Find(params string[] keys)=>Buttons().FirstOrDefault(b=>keys.Contains(b.name)||keys.Contains(Caption(b)));
    Button Required(params string[] keys)=>Find(keys)??throw new Exception("Visible enabled button missing: "+string.Join(" / ",keys));
    T Read<T>(string field)=>(T)typeof(VlabApp).GetField(field,Private).GetValue(App);
    GuideTask Task()=>typeof(VlabApp).GetMethod("CurrentGuide",Private).Invoke(App,null) as GuideTask;
    bool Modal()=>Read<RectTransform>("modal");
    void Touch(UnityEngine.InputSystem.TouchPhase phase,Vector2 position,int id)
    {InputSystem.QueueStateEvent(touch,new TouchState{touchId=id,phase=phase,position=position});}
    static Vector2 Point(RectTransform rect)
    {
        var canvas=rect.GetComponentInParent<Canvas>();
        return RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,rect.TransformPoint(rect.rect.center));
    }
    static Rect Bounds(RectTransform rect)
    {
        var corners=new Vector3[4];rect.GetWorldCorners(corners);var canvas=rect.GetComponentInParent<Canvas>();
        var points=corners.Select(c=>RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,c)).ToArray();
        return Rect.MinMaxRect(points.Min(p=>p.x),points.Min(p=>p.y),points.Max(p=>p.x),points.Max(p=>p.y));
    }
    static Button HitButton(Vector2 point)
    {
        var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},hits);
        var first=hits.FirstOrDefault();return first.gameObject?first.gameObject.GetComponentInParent<Button>():null;
    }
    IEnumerator Reveal(Button button)
    {
        var scroll=button.GetComponentInParent<ScrollRect>();if(!scroll||!scroll.viewport||!scroll.isActiveAndEnabled)yield break;
        for(int attempt=0;attempt<24;attempt++)
        {
            Canvas.ForceUpdateCanvases();var point=Point((RectTransform)button.transform);var viewport=Bounds(scroll.viewport);
            if(HitButton(point)==button)yield break;
            if(viewport.Contains(point))yield break; // A visible target blocked by another control is a real UI failure.
            if(viewport.height<40||viewport.width<40)throw new Exception("Scroll viewport too small for touch drag");
            // Screen Y grows upward: drag upward to reveal content below the viewport.
            float direction=point.y<viewport.yMin?1:-1;
            var from=new Vector2(viewport.center.x,viewport.center.y-direction*viewport.height*.25f);
            var to=from+Vector2.up*(direction*viewport.height*.5f);int id=++serial;
            var before=scroll.content.anchoredPosition;
            trace.Add((current?.id??"navigation")+" SCROLL "+button.name+" target="+point+" viewport="+viewport+" from "+from+" to "+to+" contentBefore="+before);if(current!=null)current.drags++;
            Touch(UnityEngine.InputSystem.TouchPhase.Began,from,id);yield return null;yield return null;
            for(int step=1;step<=8;step++){Touch(UnityEngine.InputSystem.TouchPhase.Moved,Vector2.Lerp(from,to,step/8f),id);yield return null;}
            Touch(UnityEngine.InputSystem.TouchPhase.Stationary,to,id);yield return new WaitForSecondsRealtime(.25f);
            Touch(UnityEngine.InputSystem.TouchPhase.Ended,to,id);yield return null;yield return null;
            yield return new WaitForSecondsRealtime(.25f);
            trace.Add((current?.id??"navigation")+" SCROLL RESULT "+button.name+" contentAfter="+scroll.content.anchoredPosition+" targetAfter="+Point((RectTransform)button.transform));
        }
    }
    IEnumerator Tap(Button button)
    {
        yield return new WaitForSecondsRealtime(.3f);
        if(!button||!button.isActiveAndEnabled||!button.interactable)throw new Exception("Button disappeared or disabled before touch");
        yield return Reveal(button);
        Canvas.ForceUpdateCanvases();var rect=(RectTransform)button.transform;var canvas=button.GetComponentInParent<Canvas>();
        Vector2 point=RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,rect.TransformPoint(rect.rect.center));
        var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},hits);
        var first=hits.FirstOrDefault();var reached=first.gameObject?first.gameObject.GetComponentInParent<Button>():null;
        if(reached!=button)throw new Exception("Touch blocked for "+button.name+" at "+point+" by "+(first.gameObject?first.gameObject.name:"no UGUI hit"));
        trace.Add((current?.id??"navigation")+" TAP "+button.name+" | "+Caption(button).Replace('\n',' ')+" @ "+point);
        if(current!=null)current.taps++;int id=++serial;
        Touch(UnityEngine.InputSystem.TouchPhase.Began,point,id);yield return null;yield return null;
        Touch(UnityEngine.InputSystem.TouchPhase.Ended,point,id);yield return null;yield return null;
        yield return new WaitForSecondsRealtime(.12f);
    }
    IEnumerator Press(params string[] keys){yield return Tap(Required(keys));}
    IEnumerator Navigate(string expected,string[] buttons,float seconds=3,string marker=null)
    {
        string before=App.Route;yield return Press(buttons);
        for(int attempt=0;attempt<2;attempt++)
        {
            double deadline=Time.realtimeSinceStartupAsDouble+seconds;
            while(Time.realtimeSinceStartupAsDouble<deadline)
            {
                if(App.Route==expected&&(marker==null||Find(marker)))yield break;
                yield return null;
            }
            if(attempt==0&&App.Route==before&&Find(buttons))
            {
                trace.Add((current?.id??"presentation")+" NAVIGATION MISS observed route still "+App.Route+" after actual touch; retrying actual touch once: "+string.Join(" / ",buttons));
                if(current!=null)current.navigationRetries++;
                yield return Press(buttons);
            }
            else break;
        }
        throw new Exception("Navigation not acknowledged after bounded actual touch retry: expected="+expected+", actual="+App.Route+", marker="+marker);
    }
    string VisibleInstrumentValues()=>Read<TextMeshProUGUI>("inspectionReadings")?.text??"No visible readings text";
    IEnumerator Hold(Button button,float seconds)
    {
        yield return new WaitForSecondsRealtime(.3f);yield return Reveal(button);Canvas.ForceUpdateCanvases();
        if(!button||!button.isActiveAndEnabled||!button.interactable)throw new Exception("Hold target is unavailable");
        var point=Point((RectTransform)button.transform);if(HitButton(point)!=button)throw new Exception("Hold target is not touchable: "+button.name);
        string before=VisibleInstrumentValues();int id=++serial;current.holds++;
        trace.Add(current.id+" HOLD "+button.name+" "+seconds+" seconds @ "+point+" visibleBefore="+before.Replace('\n',' '));
        Touch(UnityEngine.InputSystem.TouchPhase.Began,point,id);yield return null;yield return null;
        double until=Time.realtimeSinceStartupAsDouble+seconds;
        try
        {
            while(Time.realtimeSinceStartupAsDouble<until&&button&&button.isActiveAndEnabled&&button.interactable)
            {Touch(UnityEngine.InputSystem.TouchPhase.Stationary,point,id);yield return null;}
        }
        finally{Touch(UnityEngine.InputSystem.TouchPhase.Ended,point,id);}
        yield return null;yield return null;yield return new WaitForSecondsRealtime(.15f);
        trace.Add(current.id+" HOLD RESULT visibleAfter="+VisibleInstrumentValues().Replace('\n',' '));
    }
    static Vector2 ValidPoint(float x,float y)
    {
        if(!float.IsFinite(x)||!float.IsFinite(y)||x<0||y<0||x>=Screen.width||y>=Screen.height)throw new Exception("Visual-review coordinate outside observed screen");
        return new Vector2(x,y);
    }
    static void RequireUiHit(Vector2 point)
    {
        var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},hits);
        if(hits.Count==0||!hits[0].gameObject.GetComponentInParent<Canvas>())throw new Exception("Visual-review coordinate does not hit actual UGUI: "+point);
    }
    IEnumerator VisualTap(Vector2 point)
    {
        yield return new WaitForSecondsRealtime(.3f);RequireUiHit(point);int id=++serial;
        trace.Add(current.id+" VISUAL TAP "+point);current.taps++;
        Touch(UnityEngine.InputSystem.TouchPhase.Began,point,id);yield return null;yield return null;
        Touch(UnityEngine.InputSystem.TouchPhase.Ended,point,id);yield return null;yield return null;yield return new WaitForSecondsRealtime(.12f);
    }
    IEnumerator VisualDrag(Vector2 from,Vector2 to)
    {
        yield return new WaitForSecondsRealtime(.3f);RequireUiHit(from);int id=++serial;
        trace.Add(current.id+" VISUAL DRAG "+from+" to "+to);current.drags++;
        Touch(UnityEngine.InputSystem.TouchPhase.Began,from,id);yield return null;yield return null;
        for(int step=1;step<=12;step++){Touch(UnityEngine.InputSystem.TouchPhase.Moved,Vector2.Lerp(from,to,step/12f),id);yield return null;}
        Touch(UnityEngine.InputSystem.TouchPhase.Stationary,to,id);yield return new WaitForSecondsRealtime(.25f);
        Touch(UnityEngine.InputSystem.TouchPhase.Ended,to,id);yield return null;yield return null;yield return new WaitForSecondsRealtime(.25f);
    }
    IEnumerator VisualReview(GuideTask task)
    {
        yield return new WaitForSecondsRealtime(.3f);Canvas.ForceUpdateCanvases();
        string requestId=Guid.NewGuid().ToString("N"),stem=current.id+"-visual-"+requestId;Capture(stem);
        var request=new VisualRequest
        {
            requestId=requestId,experimentId=current.id,task=task.action,screenshot=Path.Combine(folder,stem+".png"),screenWidth=Screen.width,screenHeight=Screen.height,
            instructions="Inspect screenshot pixels. Write visual-command.json with matching requestId and actions (kind=button with exact button name or caption, kind=tap with x/y, kind=drag with x/y/toX/toY, kind=wait with seconds within 0.1–30). Coordinates use Unity screen bottom-left origin. No answer/model-state information is exposed. Wait limit 120 seconds. stopReason records unresolved visual judgment.",
            buttons=Buttons().Select(b=>new VisualButton{name=b.name,caption=Caption(b),bounds=Bounds((RectTransform)b.transform),centreTouchable=HitButton(Point((RectTransform)b.transform))==b}).ToArray(),
            images=App.GetComponentsInChildren<RawImage>().Where(i=>i.isActiveAndEnabled).Select(i=>new VisualImage{name=i.name,bounds=Bounds((RectTransform)i.transform),uvRect=i.uvRect,width=i.texture?i.texture.width:0,height=i.texture?i.texture.height:0}).ToArray(),
            scrollViewports=App.GetComponentsInChildren<ScrollRect>().Where(s=>s.isActiveAndEnabled&&s.viewport).Select(s=>Bounds(s.viewport)).ToArray()
        };
        File.WriteAllText(Path.Combine(folder,"visual-request.json"),JsonUtility.ToJson(request,true));
        File.WriteAllText(Path.Combine(folder,stem+".json"),JsonUtility.ToJson(request,true));
        current.status="AWAITING VISUAL REVIEW";trace.Add(current.id+" VISUAL REVIEW REQUEST "+requestId+" screenshot="+request.screenshot);Write(SessionState.GetString("VLAB.TestProfile",""));
        Debug.Log("VLAB VISUAL REVIEW REQUEST "+requestId+" "+Path.Combine(folder,"visual-request.json"));
        double deadline=Time.realtimeSinceStartupAsDouble+120;VisualCommand command=null;string commandPath=Path.Combine(folder,"visual-command.json");
        while(Time.realtimeSinceStartupAsDouble<deadline)
        {
            if(File.Exists(commandPath))
            {
                if(new FileInfo(commandPath).Length>65536)throw new Exception("Visual command exceeds 64 KiB limit");
                VisualCommand candidate=null;
                try{candidate=JsonUtility.FromJson<VisualCommand>(File.ReadAllText(commandPath));}catch(IOException){}catch(ArgumentException){}
                if(candidate!=null&&candidate.requestId==requestId){command=candidate;break;}
            }
            yield return new WaitForSecondsRealtime(.2f);
        }
        if(command==null)throw new Exception("VISUAL REVIEW TIMEOUT: no matching manual command within 120 seconds; request="+requestId);
        if(!string.IsNullOrWhiteSpace(command.stopReason))throw new Exception("VISUAL REVIEW UNRESOLVED: "+command.stopReason);
        if(command.actions==null||command.actions.Length==0||command.actions.Length>128)throw new Exception("Visual command requires 1–128 actual UI actions");
        File.WriteAllText(Path.Combine(folder,stem+"-command.json"),JsonUtility.ToJson(command,true));int applied=0;
        foreach(var action in command.actions)
        {
            if(action==null)throw new Exception("Null visual action");
            if(action.kind=="button")
            {if(string.IsNullOrWhiteSpace(action.button))throw new Exception("Visual button name missing");yield return Tap(Required(action.button));}
            else if(action.kind=="tap")yield return VisualTap(ValidPoint(action.x,action.y));
            else if(action.kind=="drag")yield return VisualDrag(ValidPoint(action.x,action.y),ValidPoint(action.toX,action.toY));
            else if(action.kind=="wait")
            {
                if(!float.IsFinite(action.seconds)||action.seconds<.1f||action.seconds>30)throw new Exception("Visual wait must be finite within 0.1–30 seconds");
                trace.Add(current.id+" VISUAL WAIT "+action.seconds+" seconds");yield return new WaitForSecondsRealtime(action.seconds);
            }
            else throw new Exception("Unsupported visual action kind: "+action.kind);
            applied++;
        }
        current.status="RUNNING";Capture(stem+"-after");
        File.WriteAllText(Path.Combine(folder,"visual-response.json"),JsonUtility.ToJson(new VisualResponse{requestId=requestId,status="UI ACTIONS APPLIED; classification correctness not presumed",actionsApplied=applied},true));
        Write(SessionState.GetString("VLAB.TestProfile",""));
    }
    IEnumerator Close()
    {
        if(!Modal())yield break;
        yield return Press("Close dialog","Trở lại trạm","Return to station","Xong","Done","Quay lại","Back");
        if(Modal())throw new Exception("Dialog remains after close touch");
    }
    IEnumerator Guard(IEnumerator routine,Action<Exception> failed)
    {
        var stack=new Stack<IEnumerator>();stack.Push(routine);
        while(stack.Count>0)
        {
            bool next=false;object value=null;Exception error=null;
            try{next=stack.Peek().MoveNext();if(next)value=stack.Peek().Current;}catch(Exception ex){error=ex;}
            if(error!=null){while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();failed(error);yield break;}
            if(!next){(stack.Pop() as IDisposable)?.Dispose();continue;}
            if(value is IEnumerator nested&&!(value is CustomYieldInstruction)){stack.Push(nested);continue;}
            yield return value;
        }
    }
    TextMeshProUGUI B03DurationLabel()=>App.GetComponentsInChildren<TextMeshProUGUI>().FirstOrDefault(t=>t.isActiveAndEnabled&&(t.text.StartsWith("Thời gian ngâm",StringComparison.Ordinal)||t.text.StartsWith("Soak duration",StringComparison.Ordinal)))??throw new Exception("Visible B03 soak-duration label missing");
    static double ShownDuration(TextMeshProUGUI label)
    {
        var match=System.Text.RegularExpressions.Regex.Match(label.text,@"([0-9]+(?:[.,][0-9]+)?)\s+s\s*$");
        if(!match.Success||!double.TryParse(match.Groups[1].Value.Replace(',','.'),System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out var seconds))throw new Exception("Cannot read displayed soak duration: "+label.text);
        return seconds;
    }
    IEnumerator ConfigureShortB03()
    {
        var duration=App.Session.spec.parameters.Single(p=>p.key=="duration");
        if(Math.Abs(duration.min-60)>.001||Math.Abs(duration.step-60)>.001||App.Session.Phase!=ExperimentPhase.Preparing||App.Session.Operation!=0)
            throw new Exception("B03 short-soak UI fixture requires source-supported min/step60 and untouched preparation");
        yield return Press("Dụng cụ & hướng dẫn","Station tools");yield return Press("Thông số","Variables");
        for(int attempt=0;attempt<10;attempt++)
        {
            var label=B03DurationLabel();double before=ShownDuration(label);
            if(Math.Abs(before-60)<.001)break;
            if(before<60||before>600)throw new Exception("B03 visible duration outside bounded fresh-fixture expectation: "+before);
            var next=label.transform.GetSiblingIndex()+1;var parent=label.transform.parent;
            var minus=next<parent.childCount?parent.GetChild(next).GetComponent<Button>():null;
            if(!minus||Caption(minus)!="−")throw new Exception("B03 duration row has no adjacent visible minus button");
            yield return Tap(minus);double after=ShownDuration(B03DurationLabel());
            trace.Add(current.id+" UI SOAK CONFIG "+before+" → "+after+" seconds via displayed duration minus button");
            if(Math.Abs(after-Math.Max(60,before-60))>.001)throw new Exception("B03 duration minus touch did not produce expected displayed decrement");
        }
        if(Math.Abs(ShownDuration(B03DurationLabel())-60)>.001)throw new Exception("B03 displayed soak did not reach valid60-second minimum");
        Capture("B03-variables-actual-60s");yield return Close();
        if(App.Session.Phase!=ExperimentPhase.Preparing||App.Session.Operation!=0||Math.Abs(App.Session.parameters["duration"]-60)>.001)
            throw new Exception("B03 UI configuration failed to preserve untouched preparation or actual duration");
    }
    IEnumerator Lesson(string id)
    {
        if(!App||App.Route!="home")throw new Exception("Lesson must start at actual Home");
        int before=App.Profile.attempts.Count(a=>a.experimentId==id);
        yield return Navigate("browser",new[]{"Subject "+id[0]},3,"Browser lesson "+id);
        if(App.Route!="browser")throw new Exception("Subject tap did not reach browser");
        yield return Press("Browser lesson "+id);yield return Navigate("readiness",new[]{"Browser start"});yield return Navigate("activity",new[]{"Readiness enter"},45);
        double timeout=Time.realtimeSinceStartupAsDouble+45;
        while(App.Route=="loading"&&Time.realtimeSinceStartupAsDouble<timeout)yield return null;
        if(App.Route!="activity"||App.Session?.spec.id!=id)throw new Exception("Entry failed, route="+App.Route+" lesson="+App.Session?.spec.id);
        current.entered=true;Write(SessionState.GetString("VLAB.TestProfile",""));yield return new WaitForSecondsRealtime(.4f);Capture(id+"-entry");WorldChecks(false);
                if(Environment.GetEnvironmentVariable("VLAB_UI_ENTRY_ONLY")=="1")
        {
            yield return Tap(Read<Button>("liveAction"));
            if(!Modal())throw new Exception("Inventory did not open through touchscreen");
            for(int equipmentPage=0;equipmentPage<5;equipmentPage++)
            {
                var sheet=Read<RectTransform>("modal");var layout=sheet.GetComponentInChildren<PhoneDialogLayout>();string why="missing layout";if(!layout||!layout.AssertPhoneLayout(out why))throw new Exception("Inventory layout: "+(layout?why:"missing layout"));
                Capture(id+"-inventory-"+equipmentPage);var next=Find("Equipment next");if(!next)break;yield return Tap(next);
            }
            yield return Press("Equipment done","Xong","Done");if(Modal())throw new Exception("Done did not close inventory");current.status="UI ENTRY + INVENTORY ONLY";yield break;
        }
if(id=="B03")yield return ConfigureShortB03();
        timeout=Time.realtimeSinceStartupAsDouble+lessonTimeout;int repeats=0;string previousSignature="";
        for(int action=0;action<150&&Time.realtimeSinceStartupAsDouble<timeout;action++)
        {
            current.phase=App.Session.Phase.ToString();current.operations=App.Session.Operation;current.installed=App.Apparatus.items.Count(i=>i.installed);
            current.assembled=App.Apparatus.Ready;
            if(current.assembled&&current.assembledWorldChecks==0){WorldChecks(true);Capture(id+"-assembled");Write(SessionState.GetString("VLAB.TestProfile",""));}
            if(App.Route=="results")
            {
                current.recorded=App.Profile.attempts.Count(a=>a.experimentId==id)>before;
                var store=Read<ProfileStore>("store");var reload=new ProfileStore(store.Folder).Load();
                current.reopened=reload.profile.attempts.Count(a=>a.experimentId==id)>before;
                if(!current.recorded||!current.reopened)throw new Exception("Results reached but matching new saved attempt not verified on reopen");
                current.status="UI JOURNEY RECORDED";Capture(id+"-results");yield break;
            }
            var task=Task();if(task==null)throw new Exception("Current guide missing");
            current.lastTask=task.kind+":"+task.key+":"+task.control+" | "+task.action;
            string signature=current.phase+":"+current.operations+":"+current.installed+":"+current.lastTask;
            repeats=signature==previousSignature?repeats+1:0;previousSignature=signature;
            // A repeated guide can still progress (P03 pull+ needs roughly 60 physical UI presses).
            if(repeats>100&&task.kind!="observe")throw new Exception("Same guide task exceeded 100 attempts; inspect visible readings before judging science progress: "+current.lastTask);
            if(task.kind=="image"||task.control.EndsWith(":",StringComparison.Ordinal)||task.control=="cell")
            {
                if(!Modal())yield return Press("Chỉ tôi chỗ này","Show me where");Capture(id+"-visual-classification");
                if(!visualReview)throw new Exception("VISUAL CLASSIFICATION REQUIRED: image and labels need human visual judgment; no model-answer lookup performed");
                yield return VisualReview(task);continue;
            }
            // Preserve the visible scroll/selection between manual review commands.
            if(visualReview&&task.kind=="variables"&&Modal()){yield return VisualReview(task);continue;}
            if(Modal())yield return Close();
            if(task.kind=="install")
            {
                yield return Press("Chỉ tôi chỗ này","Show me where");
                yield return Press("Equipment item "+task.key);
                yield return Press("Equipment take selected");
                if(!App.Apparatus.IsCarrying&&Modal()&&Find("Equipment take selected"))
                {trace.Add(current.id+" RETRY actual Take tap after first touch left dialog open");yield return Press("Equipment take selected");}
                if(!App.Apparatus.IsCarrying)throw new Exception("Take UI did not retain equipment "+task.key);
                yield return Tap(Read<Button>("liveAction"));
                if(!App.Apparatus.items.Any(i=>i.assetId==task.key&&i.installed)&&App.Apparatus.IsCarrying&&App.Apparatus.CanInstallSelected(out _))
                {trace.Add(id+" PLACE touch not acknowledged; retrying actual primary touch once");yield return Tap(Read<Button>("liveAction"));}
                if(!App.Apparatus.items.Any(i=>i.assetId==task.key&&i.installed))throw new Exception("Place UI did not install "+task.key);
            }
            else if(task.kind=="instrument")
            {
                yield return Press("Chỉ tôi chỗ này","Show me where");
                if(!current.instrumentCaptured){yield return new WaitForSecondsRealtime(.3f);PresentationLayout("PhoneDialogLayout",true);PresentationText(id+" instrument");Capture(id+"-instrument");current.instrumentCaptured=true;}
                var controls=Read<Dictionary<string,Button>>("inspectionControls");
                var pair=controls.FirstOrDefault(p=>p.Key==task.control);
                Button control=pair.Value;
                if(!control)
                {
                    var metadata=App.Apparatus.Driver.Controls(task.key).FirstOrDefault(c=>c.id==task.control);
                    if(metadata!=null)control=Find(metadata.vi,metadata.en);
                }
                if(!control||!control.isActiveAndEnabled||!control.interactable)throw new Exception("Guided control absent/disabled: "+task.key+"/"+task.control);
                if(id=="P03"&&task.control=="pull+")
                {
                    for(int pull=0;pull<80;pull++)
                    {
                        var steady=Find("Instrument control steady");
                        if(steady)
                        {
                            var block=App.Apparatus.items.Single(item=>item.assetId=="block");
                            yield return Tap(steady);
                            double beforeSteady=App.Apparatus.Driver.Observe().Get("steadyInterval");
                            if(beforeSteady<=0)throw new Exception("Actual steady-pull touch was not accepted before the block reached the tile end");
                            var beforeMove=block.transform.position;
                            yield return new WaitForSecondsRealtime(.6f);
                            double afterSteady=App.Apparatus.Driver.Observe().Get("steadyInterval");
                            float travel=Vector3.Distance(beforeMove,block.transform.position);
                            if(afterSteady<.5||travel<.001f||travel>.05*(afterSteady-beforeSteady)+.002)
                                throw new Exception("Actual steady-pull UI did not retain slow measured motion: travel="+travel+", steady="+beforeSteady+" -> "+afterSteady);
                            trace.Add(id+" actual steady-pull UI moves original block "+travel+" metres with measured steady interval "+afterSteady+" seconds");break;
                        }
                        yield return Tap(Required("Instrument control pull+"));
                    }
                }
                else yield return Tap(control);
                yield return Close();
            }
            else if(task.kind=="observe")
            {
                if(id=="P01"&&!current.observingCueCaptured&&App.Session.Elapsed>=13)
                {Capture(id+"-observing-guidance");current.observingCueCaptured=true;}
                yield return new WaitForSecondsRealtime(1);
            }
            else if(task.kind=="variables")
            {
                if(!visualReview)throw new Exception("Guide requires manual visible-variable configuration: "+current.lastTask);
                yield return Press("Chỉ tôi chỗ này","Show me where");yield return VisualReview(task);
            }
            else if(task.kind=="pause")throw new Exception("Guide requires manual recovery: "+current.lastTask);
            else
            {
                var primary=Read<Button>("liveAction");
                if(!primary||!primary.interactable)throw new Exception("Primary task control disabled: "+current.lastTask);
                yield return Tap(primary);
                bool wasStarted=current.started;current.started|=App.Session.Phase==ExperimentPhase.Running||App.Session.Phase==ExperimentPhase.Measured;if(current.started&&!wasStarted)Write(SessionState.GetString("VLAB.TestProfile",""));
            }
        }
        throw new Exception("Bounded UI journey timed out before saved result; "+current.lastTask);
    }
    void WorldChecks(bool assembled)
    {
        string stage=assembled?"assembled":"entry";
        void Check(string name,bool okay)
        {
            if(assembled)current.assembledWorldChecks++;else current.entryWorldChecks++;
            trace.Add(current.id+" SCOPED WORLD "+stage+" "+(okay?"PASS ":"FAIL ")+name);
            if(!okay)current.worldFailures=current.worldFailures.Concat(new[]{stage+": "+name}).ToArray();
        }
        try{SecondTestWorldChecks.Run(App,Check);if(assembled&&current.id=="P03")StepFocusChecks.Run(App,Check);}catch(Exception ex){Check("Regression check exception: "+ex,false);}
    }
    IEnumerator Home()
    {
        if(!App)throw new Exception("App missing during recovery");
        if(Modal())yield return Close();
        if(App.Route=="activity"||App.Route=="explore")yield return Navigate("pause",new[]{"‹ Quay lại","‹ Back"});
        if(App.Route=="pause"){yield return Press("Rời phòng","Leave laboratory");yield return Press("Xác nhận","Confirm");}
        if(App.Route=="results")yield return Press("Danh sách thí nghiệm","Back to experiments");
        if(App.Route=="browser")yield return Navigate("home",new[]{"Browser home"});
        if(App.Route=="readiness")yield return Press("Readiness back");
        if(App.Route=="browser")yield return Navigate("home",new[]{"Browser home"});
        if(App.Route!="home")throw new Exception("UI recovery could not return Home: "+App.Route);
    }
    readonly List<string> presentationRows=new List<string>();
    void PresentationCheck(string name,bool okay)
    {
        presentationRows.Add((okay?"PASS ":"FAIL ")+name);
        File.WriteAllLines(Path.Combine(folder,"presentation.txt"),new[]{"Actual queued touchscreen presentation checks; no Android hardware claim.","Observed resolution "+Screen.width+"x"+Screen.height}.Concat(presentationRows));
    }
    void PresentationLayout(string componentName,bool required)
    {
        var layout=App.GetComponentsInChildren<MonoBehaviour>().FirstOrDefault(c=>c.GetType().Name==componentName);
        if(!layout)
        {
            if(required)PresentationCheck(componentName+" exists on current phone page",false);
            else presentationRows.Add("NOT CHECKED "+componentName+" is not installed on current page");
            return;
        }
        var method=layout.GetType().GetMethod("AssertPhoneLayout",BindingFlags.Instance|BindingFlags.Public);object[] args={null};
        bool okay=method!=null&&(bool)method.Invoke(layout,args);PresentationCheck(componentName+" geometry: "+(args[0]??""),okay);
    }
    void PresentationText(string context)
    {
        Canvas.ForceUpdateCanvases();
        var texts=App.GetComponentsInChildren<TextMeshProUGUI>().Where(t=>t.isActiveAndEnabled).ToArray();
        foreach(var text in texts)text.ForceMeshUpdate();
        var overflow=texts.Where(t=>t.isTextOverflowing).Select(t=>t.name+" | "+t.text.Replace('\n',' ')).ToArray();
        PresentationCheck(context+" TMP overflow: "+string.Join("; ",overflow),overflow.Length==0);
    }
    IEnumerator Presentation()
    {
        if(App.Route!="home")throw new Exception("Presentation checks require Home");
        var original=App.Profile.settings.Copy();string originalJson=JsonUtility.ToJson(original);
        yield return Press("Home utility 0","Cài đặt","Settings");
        PresentationCheck("Actual Home Settings touch opens Settings",App.Route=="settings");
        string[][] tabs={new[]{"Chung","General"},new[]{"Âm thanh","Audio"},new[]{"Đồ họa","Graphics"},new[]{"Điều khiển","Controls"},new[]{"Giao diện","Appearance"},new[]{"Tương tác","Interaction"}};
        for(int i=0;i<tabs.Length;i++)
        {
            yield return Press(tabs[i]);yield return new WaitForSecondsRealtime(.3f);
            PresentationCheck("Actual Settings category tap "+i,Read<int>("settingsTab")==i);
            PresentationLayout("PhoneSettingsLayout",true);PresentationText("Settings category "+i);Capture("presentation-settings-"+i);
        }
        yield return Press(tabs[0]);yield return Press("Settings UI animation");
        yield return Press(tabs[4]);yield return Press("Appearance theme "+((original.uiTheme+1)%3));yield return Press("Settings apply");
        PresentationCheck("Animation and theme Apply change actual profile",App.Profile.settings.reducedMotion!=original.reducedMotion&&App.Profile.settings.uiTheme==(original.uiTheme+1)%3);
        var stored=new ProfileStore(Read<ProfileStore>("store").Folder).Load().profile;
        PresentationCheck("Animation and theme persist on fresh store reopen",stored.settings.reducedMotion!=original.reducedMotion&&stored.settings.uiTheme==(original.uiTheme+1)%3);
        Capture("presentation-home-applied-theme");
        yield return Press("Home utility 0","Cài đặt","Settings");yield return Press(tabs[0]);yield return Press("Settings UI animation");
        yield return Press(tabs[4]);yield return Press("Appearance theme "+original.uiTheme);yield return Press("Settings apply");
        PresentationCheck("Restore original settings through actual UI",JsonUtility.ToJson(App.Profile.settings)==originalJson);
        stored=new ProfileStore(Read<ProfileStore>("store").Folder).Load().profile;
        PresentationCheck("Restored settings persist on fresh store reopen",JsonUtility.ToJson(stored.settings)==originalJson);
        yield return Navigate("browser",new[]{"Subject P"},3,"Browser lesson P08");
        PresentationCheck("Actual Home subject card reaches browser",App.Route=="browser");
        PresentationCheck("Browser exposes exactly eight Physics lesson buttons",Buttons().Count(b=>b.name.StartsWith("Browser lesson P",StringComparison.Ordinal))==8);
        yield return Press("Browser lesson P08");yield return new WaitForSecondsRealtime(.3f);
        PresentationCheck("Actual scroll/tap selects final P08 tile",Read<ExperimentBrief>("selected").id=="P08");
        PresentationLayout("PhoneBrowserLayout",false);PresentationText("Browser P08");Capture("presentation-browser-P08");
        yield return Press("Browser lesson P01");yield return Navigate("readiness",new[]{"Browser start"});yield return Navigate("activity",new[]{"Readiness enter"},45);
        yield return new WaitForSecondsRealtime(.4f);
        PresentationCheck("Actual P01 entry for camera/HUD checks",App.Session?.spec.id=="P01"&&App.Route=="activity");
        float initialFov=App.View.fieldOfView;
        yield return Press("Phone zoom in");PresentationCheck("Phone zoom-in changes actual camera FOV by -8",Mathf.Abs(App.View.fieldOfView-Mathf.Clamp(initialFov-8,20,90))<.01f);
        float beforeOut=App.View.fieldOfView;
        yield return Press("Phone zoom out");PresentationCheck("Phone zoom-out changes actual camera FOV by +8",Mathf.Abs(App.View.fieldOfView-Mathf.Clamp(beforeOut+8,20,90))<.01f);
        for(int i=0;i<9&&App.View.fieldOfView>20.01f;i++)yield return Press("Phone zoom in");
        PresentationCheck("Repeated actual zoom-in clamps at 20 degrees",Mathf.Abs(App.View.fieldOfView-20)<.01f);
        for(int i=0;i<9&&App.View.fieldOfView<89.99f;i++)yield return Press("Phone zoom out");
        PresentationCheck("Repeated actual zoom-out clamps at 90 degrees",Mathf.Abs(App.View.fieldOfView-90)<.01f);
        yield return Press("Focus workstation");PresentationCheck("Focus station restores saved overview FOV",Mathf.Abs(App.View.fieldOfView-App.Profile.settings.cameraFov)<.01f);
        yield return Press("Phone zoom in");yield return Press("Phone zoom in");
        if(Find("Thu gọn bảng","Hide panel"))yield return Press("Thu gọn bảng","Hide panel");
        var compact=Find("Compact current action");
        PresentationCheck("Hidden placard leaves compact primary action touchable",!Read<RectTransform>("activityCard").gameObject.activeInHierarchy&&compact&&HitButton(Point((RectTransform)compact.transform))==compact);
        if(compact)yield return Tap(compact);
        PresentationCheck("Compact primary action actually opens equipment",Modal()&&Find("Equipment take selected"));
        if(Modal())yield return Close();
        Capture("presentation-P01-free-observation");
        yield return Press("Hiện bảng","Show panel");
        PresentationCheck("Show panel restores real action card",Read<RectTransform>("activityCard").gameObject.activeInHierarchy&&!Read<Button>("compactAction").gameObject.activeInHierarchy);
        Capture("presentation-P01-panel-restored");
        float priorLessonZoom=App.View.fieldOfView,savedOverview=App.Profile.settings.cameraFov;
        PresentationCheck("First lesson has an actual temporary zoom distinct from saved overview",Mathf.Abs(priorLessonZoom-savedOverview)>.01f);
        yield return Home();PresentationCheck("Camera/HUD check returns Home by actual UI",App.Route=="home");
        yield return Navigate("browser",new[]{"Subject P"},3,"Browser lesson P02");yield return Press("Browser lesson P02");
        yield return Navigate("readiness",new[]{"Browser start"});
        trace.Add("presentation ZOOM TRANSITION priorLesson="+priorLessonZoom+", beforeNextEntry="+App.View.fieldOfView+", savedOverview="+savedOverview);
        yield return Navigate("activity",new[]{"Readiness enter"},45);yield return new WaitForSecondsRealtime(.4f);
        PresentationCheck("Next actual lesson P02 resets prior temporary zoom to saved overview",App.Session?.spec.id=="P02"&&Mathf.Abs(App.View.fieldOfView-savedOverview)<.01f);
        Capture("presentation-P02-entry-zoom-reset");
        yield return Home();PresentationCheck("Zoom-reset regression returns Home by actual UI",App.Route=="home");
        yield return PhoneDocumentUiChecks.Run(App.transform,App.View,Tap,PresentationCheck);
        presentationRows.Add("NOT CHECKED legal, feedback, science completion, phone hardware and VR; this mode covers Settings, browser, actual camera FOV controls and compact/restored activity UI.");
    }
    IEnumerator Start()
    {
        DontDestroyOnLoad(gameObject);folder=Path.GetFullPath("Evidence/SecondTestUI/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));Directory.CreateDirectory(folder);
        Screen.SetResolution(1600,900,false);
        Application.logMessageReceived+=Log;
        InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        yield return new WaitForSecondsRealtime(1);
        string profile=SessionState.GetString("VLAB.TestProfile","");
        if(Screen.width!=1600||Screen.height!=900){File.WriteAllText(Path.Combine(folder,"ABORT.txt"),"Requested fixed Game View 1600x900; observed "+Screen.width+"x"+Screen.height+". No phone UI journey evidence produced. "+SecondTestGameView.Diagnostic);SecondTestGameView.Restore();EditorApplication.Exit(2);yield break;}
        if(!App||string.IsNullOrEmpty(profile)||Path.GetFullPath(Read<ProfileStore>("store").Folder)!=Path.GetFullPath(profile))
        {File.WriteAllText(Path.Combine(folder,"ABORT.txt"),"Pre-Awake isolated QA profile was not verified. No actions performed.");SecondTestGameView.Restore();EditorApplication.Exit(2);yield break;}
        previous=Touchscreen.current;touch=InputSystem.AddDevice<Touchscreen>();yield return null;yield return null;
        if(Environment.GetEnvironmentVariable("VLAB_UI_PRESENTATION_ONLY")=="1")
        {
            yield return Guard(Presentation(),ex=>PresentationCheck("Presentation exception: "+ex,false));
            PresentationCheck("No runtime errors during presentation",errors.Count==0);
            File.WriteAllLines(Path.Combine(folder,"presentation.txt"),new[]{"Actual queued touchscreen presentation checks; no Android hardware claim.","Observed resolution "+Screen.width+"x"+Screen.height}.Concat(presentationRows));
            File.WriteAllLines(Path.Combine(folder,"touch-trace.txt"),trace);
            Application.logMessageReceived-=Log;if(touch!=null&&touch.added)InputSystem.RemoveDevice(touch);if(previous!=null&&previous.added)previous.MakeCurrent();
            SessionState.SetBool("VLAB.SecondTestUI",false);SessionState.SetString("VLAB.TestProfile","");SecondTestGameView.Restore();
            EditorApplication.Exit(presentationRows.Any(row=>row.StartsWith("FAIL ",StringComparison.Ordinal))?1:0);yield break;
        }
        if(Environment.GetEnvironmentVariable("VLAB_UI_WITH_PRESENTATION")=="1")yield return Guard(Presentation(),ex=>PresentationCheck("Presentation exception: "+ex,false));
        var allIds=new[]{"P","C","B","M"}.SelectMany(prefix=>Enumerable.Range(1,8).Select(n=>prefix+n.ToString("00"))).ToArray();
        string requested=Environment.GetEnvironmentVariable("VLAB_UI_IDS");
        visualReview=Environment.GetEnvironmentVariable("VLAB_UI_VISUAL_REVIEW")=="1";
        var ids=string.IsNullOrWhiteSpace(requested)?allIds:requested.Split(',').Select(s=>s.Trim().ToUpperInvariant()).Distinct().ToArray();
        if(ids.Length==0||ids.Any(id=>!allIds.Contains(id)))
        {File.WriteAllText(Path.Combine(folder,"ABORT.txt"),"VLAB_UI_IDS contains an invalid experiment ID");SecondTestGameView.Restore();EditorApplication.Exit(2);yield break;}
        selection=string.Join(",",ids);
        string duration=Environment.GetEnvironmentVariable("VLAB_UI_TIMEOUT_SECONDS");
        if(!string.IsNullOrWhiteSpace(duration)&&(!float.TryParse(duration,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out lessonTimeout)||!float.IsFinite(lessonTimeout)||lessonTimeout<30||lessonTimeout>600))
        {File.WriteAllText(Path.Combine(folder,"ABORT.txt"),"VLAB_UI_TIMEOUT_SECONDS must be finite between 30 and 600 seconds");SecondTestGameView.Restore();EditorApplication.Exit(2);yield break;}
        try
        {
            BenchDisplayVisibilityChecks.Run((name,okay)=>scopedRows.Add((okay?"PASS ":"FAIL ")+"Bench displays and line hints: "+name));
            SecondTestWorldChecks.StoredRetrieval((name,okay)=>scopedRows.Add((okay?"PASS ":"FAIL ")+"Isolated storage retrieval: "+name));
            SecondTestWorldChecks.FrictionRetry((name,okay)=>scopedRows.Add((okay?"PASS ":"FAIL ")+"Isolated friction recovery: "+name));
        }
        catch(Exception ex){scopedRows.Add("FAIL Isolated storage retrieval exception: "+ex);}
        trace.AddRange(scopedRows.Select(row=>"SCOPED API REGRESSION (not UI journey) "+row));
        results.AddRange(ids.Select(id=>new LessonResult{id=id}));Write(profile);
        foreach(var result in results)
        {
            current=result;string id=current.id;current.status="RUNNING";
            yield return Guard(Lesson(id),ex=>{current.status="BLOCKED";current.blocker=ex.Message;trace.Add(id+" BLOCKED "+ex);Capture(id+"-blocker");});
            Write(profile);
            bool recovered=true;
            yield return Guard(Home(),ex=>{recovered=false;trace.Add(id+" UI RECOVERY BLOCKED "+ex.Message);});
            if(!recovered)
            {
                trace.Add(id+" Explicit isolated-app restart to test remaining IDs; no experiment state advanced by recovery.");
                if(App)Destroy(App.gameObject);yield return null;
                yield return SceneManager.LoadSceneAsync("VLAB_Boot");yield return new WaitForSecondsRealtime(1);
            }
        }
        current=null;Write(profile);Application.logMessageReceived-=Log;
        if(touch!=null&&touch.added)InputSystem.RemoveDevice(touch);if(previous!=null&&previous.added)previous.MakeCurrent();
        SessionState.SetBool("VLAB.SecondTestUI",false);SessionState.SetString("VLAB.TestProfile","");
        bool complete=results.Count==ids.Length&&results.All(r=>(r.status=="UI JOURNEY RECORDED"||Environment.GetEnvironmentVariable("VLAB_UI_ENTRY_ONLY")=="1"&&r.status=="UI ENTRY + INVENTORY ONLY")&&r.worldFailures.Length==0)&&errors.Count==0&&!scopedRows.Any(row=>row.StartsWith("FAIL ",StringComparison.Ordinal))&&!presentationRows.Any(row=>row.StartsWith("FAIL ",StringComparison.Ordinal));
        Debug.Log("VLAB SECOND TEST UI: "+results.Count+" attempted, "+results.Count(r=>r.recorded)+" recorded, evidence="+folder);
        SecondTestGameView.Restore();EditorApplication.Exit(complete?0:1);
    }
    void Write(string profile)
    {
        File.WriteAllText(Path.Combine(folder,"matrix.json"),JsonUtility.ToJson(new Report{profile=profile,selection=selection,lessonTimeout=lessonTimeout,screenWidth=Screen.width,screenHeight=Screen.height,visualReview=visualReview,safeArea=Screen.safeArea.ToString(),lessons=results.ToArray(),runtimeErrors=errors.ToArray(),scopedRegressions=scopedRows.ToArray()},true));
        var lines=new List<string>{"# Actual queued touchscreen UI journeys","Editor synthetic input only; no physical Android/VR claim. NOT RUN fields are not passes.","","| ID | State | Entry | Assembly | Run | Saved/reopen | Blocker |","|---|---|---|---|---|---|---|"};
        lines.Insert(2,"Selection: "+selection+". Per-lesson limit: "+lessonTimeout+" seconds. Scoped world regressions call fixture/math helpers separately; they are not UI journey actions.");
        lines.Insert(3,"Observed resolution: "+Screen.width+" × "+Screen.height+"; safe area: "+Screen.safeArea+". Requested 1600×900; Editor Game View may override the request.");
        lines.AddRange(results.Select(r=>"| "+r.id+" | "+r.status+" | "+r.entered+" | "+r.assembled+" | "+r.started+" | "+r.recorded+"/"+r.reopened+" | "+r.blocker.Replace('|','/')+" |"));
        lines.Add("");lines.Add("## Scoped world regressions (separate from UI journey)");
        lines.AddRange(scopedRows);
        lines.AddRange(results.Select(r=>r.id+": entry checks="+r.entryWorldChecks+", assembled checks="+r.assembledWorldChecks+", failures="+r.worldFailures.Length+" "+string.Join("; ",r.worldFailures)));
        File.WriteAllLines(Path.Combine(folder,"matrix.md"),lines);File.WriteAllLines(Path.Combine(folder,"touch-trace.txt"),trace);
    }
    void Capture(string name)
    {
        if(!App||!App.View)return;
        var camera=App.View;var canvas=App.GetComponentInChildren<Canvas>();if(!canvas)return;
        var mode=canvas.renderMode;var oldCamera=canvas.worldCamera;var target=camera.targetTexture;float distance=canvas.planeDistance;
        int width=Mathf.Max(1,Screen.width),height=Mathf.Max(1,Screen.height);var rt=new RenderTexture(width,height,24);var active=RenderTexture.active;Texture2D texture=null;
        try
        {
            camera.targetTexture=rt;if(mode!=RenderMode.WorldSpace){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=.2f;}
            var layout=canvas.GetComponent<PhoneUiViewport>();if(layout)layout.Refresh();Canvas.ForceUpdateCanvases();
            LabPanoramaCapture.RenderMono(camera,rt);RenderTexture.active=rt;texture=new Texture2D(width,height,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,width,height),0,0);texture.Apply();
            string path=Path.Combine(folder,name+".png");File.WriteAllBytes(path,texture.EncodeToPNG());if(current!=null)current.screenshot=path;
        }
        finally
        {
            RenderTexture.active=active;camera.targetTexture=target;canvas.renderMode=mode;canvas.worldCamera=oldCamera;canvas.planeDistance=distance;
            var layout=canvas.GetComponent<PhoneUiViewport>();if(layout)layout.Refresh();Canvas.ForceUpdateCanvases();if(texture)Destroy(texture);Destroy(rt);
        }
    }
}
[InitializeOnLoad] public static class VlabSecondTestSmoke
{
    static VlabSecondTestSmoke(){EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("VLAB.SecondTestUI",false))new GameObject("Isolated second test UI journeys",typeof(SecondTestUiProbe));};}
    public static void Run()
    {
        if(!SecondTestGameView.Configure(out var sizing)){Debug.LogError(sizing);EditorApplication.Exit(2);return;}Debug.Log(sizing);
        SessionState.SetBool("NguaManVLAB.ProductSmoke",false);SessionState.SetBool("VLAB.FollowupRun",false);SessionState.SetBool("VLAB.SecondTestUI",true);
        SessionState.SetString("VLAB.TestProfile",Path.Combine(Application.persistentDataPath,"QA","SecondTestUI-"+Guid.NewGuid().ToString("N")));
        EditorSceneManager.OpenScene("Assets/VLABProduct/Scenes/VLAB_Boot.unity");EditorApplication.EnterPlaymode();
    }
}
#endif
