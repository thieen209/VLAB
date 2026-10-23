#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Vlab.Product;

[InitializeOnLoad]
public static class FinalPassHudChecks
{
    static FinalPassHudChecks(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("VLAB.HudQa",false)){SessionState.SetBool("VLAB.HudQa",false);new GameObject("HUD checks",typeof(FinalPassHudProbe));}};}
    public static void Baseline(){SessionState.SetBool("VLAB.HudBaseline",true);Run();}
    public static void Layout(){SessionState.SetBool("VLAB.HudLayoutOnly",true);Run();}
    public static void GearLighting(){SessionState.SetBool("VLAB.GearLighting",true);Run();}
    public static void NotebookExit(){SessionState.SetBool("VLAB.NotebookExit",true);Run();}
    public static void Home(){SessionState.SetBool("VLAB.HudHomeOnly",true);Run();}
    public static void FlagshipLayouts(){SessionState.SetBool("VLAB.HudFourLayouts",true);Layout();}
    public static void Run(){Directory.CreateDirectory("Evidence/FinalPass/Hud");SecondTestGameView.Configure(out _);SessionState.SetString("VLAB.TestProfile",Path.Combine(Application.persistentDataPath,"QA","Hud-"+Guid.NewGuid().ToString("N")));SessionState.SetBool("VLAB.HudQa",true);EditorSceneManager.OpenScene("Assets/VLABProduct/Scenes/VLAB_Boot.unity");EditorApplication.EnterPlaymode();}
}
public sealed class FinalPassHudProbe:MonoBehaviour
{
    readonly List<string> rows=new List<string>();int failures;bool baseline,layoutOnly,fourLayouts,homeOnly,gearLighting,notebookExit;
    VlabApp App=>VlabApp.Instance;
    Button Find(string name)=>App.GetComponentsInChildren<Button>().FirstOrDefault(x=>x.name==name&&x.IsInteractable());
    void Check(string name,bool pass){rows.Add((pass?"PASS ":"FAIL ")+name);if(!pass)failures++;}
    IEnumerator Press(string name){yield return new WaitForSecondsRealtime(.4f);var b=Find(name);if(!b)throw new Exception("Missing "+name+" on "+App.Route);b.onClick.Invoke();yield return new WaitForSecondsRealtime(.4f);}
    IEnumerator Capture(string name){if(layoutOnly&&!name.EndsWith("-phone",StringComparison.Ordinal))yield break;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot("Evidence/FinalPass/Hud/"+(baseline?"before-":"after-")+name+".png");yield return null;}
    IEnumerator Start()
    {
        Application.runInBackground=true;DontDestroyOnLoad(gameObject);baseline=SessionState.GetBool("VLAB.HudBaseline",false);SessionState.SetBool("VLAB.HudBaseline",false);
        layoutOnly=SessionState.GetBool("VLAB.HudLayoutOnly",false);SessionState.SetBool("VLAB.HudLayoutOnly",false);
        fourLayouts=SessionState.GetBool("VLAB.HudFourLayouts",false);SessionState.SetBool("VLAB.HudFourLayouts",false);
        homeOnly=SessionState.GetBool("VLAB.HudHomeOnly",false);SessionState.SetBool("VLAB.HudHomeOnly",false);
        gearLighting=SessionState.GetBool("VLAB.GearLighting",false);SessionState.SetBool("VLAB.GearLighting",false);
        notebookExit=SessionState.GetBool("VLAB.NotebookExit",false);SessionState.SetBool("VLAB.NotebookExit",false);
        var stack=new Stack<IEnumerator>();stack.Push(Run());
        while(stack.Count>0){object value=null;bool next=false;try{next=stack.Peek().MoveNext();if(next)value=stack.Peek().Current;}catch(Exception e){rows.Add("FAIL "+e);failures++;break;}if(!next){stack.Pop();continue;}if(value is IEnumerator nested)stack.Push(nested);else yield return value;}
        File.WriteAllLines("Evidence/FinalPass/Hud/"+(notebookExit?"notebook-exit":gearLighting?"gear-lighting":homeOnly?"home-checks":layoutOnly?"layout-checks":baseline?"baseline":"checks")+".txt",new[]{"Editor UI callbacks and raycast/layout checks; no Android touch or physical tracking claim."}.Concat(rows));SecondTestGameView.Restore();EditorApplication.Exit(failures==0?0:1);
    }
    IEnumerator Run()
    {
        for(int theme=0;theme<3;theme++)
        {
            var palette=UiTheme.Palette(theme);
            Check("Theme "+theme+" primary text contrast",UiTheme.Contrast(palette.text,palette.panel)>=4.5f);
            Check("Theme "+theme+" secondary text contrast",UiTheme.Contrast(palette.secondary,palette.panel)>=4.5f);
        }
        foreach(string hex in new[]{"3B82F6","8B5CF6"}){var color=VlabUi.Hex(hex);Check("Accent caption contrast "+hex,UiTheme.Contrast(UiTheme.On(color),color)>=4.5f);}
        float deadline=Time.realtimeSinceStartup+60;while((!App||App.Route!="home")&&Time.realtimeSinceStartup<deadline)yield return null;
        if(!App||App.Route!="home")throw new Exception("Home timeout");App.Profile.settings.gazeAutoSelect=false;
        if(notebookExit)
        {
            void Call(string method,params object[] args)=>typeof(VlabApp).GetMethod(method,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(App,args);
            TMP_InputField Body()=>App.GetComponentsInChildren<TMP_InputField>(true).Single(f=>f.name=="Journal note body");
            string folder=SessionState.GetString("VLAB.TestProfile","");string blocked=Path.Combine(folder,"profile.json.tmp");
            yield return Press("Open journal");yield return Press("Journal mode notes");yield return Press("Journal note new");
            foreach(var field in App.GetComponentsInChildren<TMP_InputField>().ToArray())
            {
                field.ActivateInputField();yield return null;yield return null;
                field.text="Bản nháp giữ khi đóng bàn phím";
                field.OnCancel(new UnityEngine.EventSystems.BaseEventData(UnityEngine.EventSystems.EventSystem.current));
                Check("Cancel retains draft in "+field.name,field.text=="Bản nháp giữ khi đóng bàn phím");
            }
            Body().text="Ghi chép cần giữ — tiếng Việt";string id=App.Profile.notebookPages.Last().id;
            Call("ControllerBack");yield return null;
            Check("Back saves actual notebook field before leaving",App.Route=="home"&&new ProfileStore(folder).Load().profile.notebookPages.Any(p=>p.id==id&&p.body=="Ghi chép cần giữ — tiếng Việt"));
            yield return Press("Open journal");Body().text="Bản nháp khi ổ lưu không ghi được";
            if(File.Exists(blocked)||Directory.Exists(blocked))throw new Exception("Temporary path already occupied");Directory.CreateDirectory(blocked);
            try
            {
                Call("ControllerBack");yield return null;
                Check("Back on save failure retains notebook and draft",App.Route=="notebook"&&Body().text=="Bản nháp khi ổ lưu không ghi được"&&Find("Close dialog"));
                yield return Press("Close dialog");
                App.Input.UseMockController=true;
                App.Input.Controller.Submit(new ControllerSignal{sequence=1,receivedAt=Time.realtimeSinceStartupAsDouble,orientation=Quaternion.identity,menu=true});
                yield return new WaitForSecondsRealtime(.1f);
                Check("Controller Menu also refuses to discard unsaved draft",App.Route=="notebook"&&Find("Close dialog")&&Body().text=="Bản nháp khi ổ lưu không ghi được");
                App.Input.Controller.Submit(new ControllerSignal{sequence=2,receivedAt=Time.realtimeSinceStartupAsDouble,orientation=Quaternion.identity});yield return null;
                yield return Press("Close dialog");
            }
            finally{App.Input.UseMockController=false;Directory.Delete(blocked);}
            Call("ControllerBack");yield return null;
            Check("Retry Back persists recovered draft",App.Route=="home"&&new ProfileStore(folder).Load().profile.notebookPages.Any(p=>p.id==id&&p.body=="Bản nháp khi ổ lưu không ghi được"));
            yield break;
        }
        if(!baseline){yield return Press("VR mode");Check("Home round VR label updates on entry",Find("VR mode").GetComponentInChildren<TextMeshProUGUI>().text=="Thoát VR");yield return Press("VR mode");Check("Home round VR label updates on exit",Find("VR mode").GetComponentInChildren<TextMeshProUGUI>().text=="VR");}
        if(homeOnly)
        {
            var viewport=App.GetComponentInChildren<PhoneUiViewport>();
            foreach(var size in new[]{new Vector2Int(2340,1080),new Vector2Int(1280,720)})
            {
                if(!SecondTestGameView.Configure(out var diagnostic,size.x,size.y))throw new Exception(diagnostic);
                yield return new WaitForSecondsRealtime(.6f);
                float inset=size.x>2000?140:40;var safe=new Rect(inset,0,size.x-inset,size.y);viewport.SetViewportOverride(size,safe);Canvas.ForceUpdateCanvases();
                Check("Home centred despite left notch "+size,Mathf.Abs(viewport.ContentPixels.center.x-size.x*.5f)<.5f);
                Check("Home retains safe bounds "+size,viewport.ContentPixels.xMin>=safe.xMin-.5f&&viewport.ContentPixels.xMax<=safe.xMax+.5f);
                var controls=new[]{Find("Home utility 0"),Find("Home utility 1"),Find("Home utility 2"),Find("Open journal"),Find("VR mode")};
                var rects=controls.Select(b=>(RectTransform)b.transform).ToArray();
                Check("Five same-size controls share bottom row "+size,rects.All(r=>r.sizeDelta==new Vector2(120,120)&&Mathf.Abs(r.anchoredPosition.y+770)<.1f));
                Check("Five controls evenly spaced and centred "+size,Mathf.Abs((rects[0].anchoredPosition.x+rects[4].anchoredPosition.x+120)*.5f-800)<.1f&&Enumerable.Range(1,4).All(i=>Mathf.Abs(rects[i].anchoredPosition.x-rects[i-1].anchoredPosition.x-152)<.1f));
                yield return Capture("home-centred-"+size.x);
                viewport.ClearViewportOverride();
            }
            var constrained=PhoneUiViewport.Fit(new Vector2(1600,1000),new Rect(140,0,1460,1000));
            Check("Constrained notch still respected",constrained.xMin>=139.9f&&constrained.xMax<=1600.1f);
            yield break;
        }
        foreach(string id in gearLighting?new[]{"M01"}:layoutOnly&&!fourLayouts?new[]{"P01"}:new[]{"P01","C04","B01","M01"})
        {
            App.ShowHome();yield return Press("Subject "+id[0]);yield return Press("Browser lesson "+id);yield return Press("Browser start");yield return Press("Readiness enter");
            deadline=Time.realtimeSinceStartup+60;while(App.Route!="activity"&&Time.realtimeSinceStartup<deadline)yield return null;
            if(App.Route!="activity"||App.Session.spec.id!=id)throw new Exception("Activity timeout "+id);
            yield return new WaitForSecondsRealtime(1);yield return Capture(id+"-phone");
            Check(id+" prepared station loaded",App.Apparatus&&App.Apparatus.Ready);
            if(gearLighting)
            {
                var shade=UnityEngine.Object.FindAnyObjectByType<LabShade>();
                var painted=App.Apparatus.GetComponentsInChildren<Renderer>().SelectMany(r=>r.sharedMaterials).Distinct().Where(m=>m.name=="GearTeal"||m.name=="MotorTeal"||m.name=="MotorDark").ToArray();
                Check("Coated surfaces use nonmetallic paint",painted.Length==3&&painted.All(m=>m.GetFloat("_Metallic")==0&&Mathf.Approximately(m.GetFloat("_Smoothness"),.35f)));
                Check("Bronze bore keeps metallic response",App.Apparatus.GetComponentsInChildren<Renderer>().SelectMany(r=>r.sharedMaterials).Any(m=>m.name=="BoreBronze"&&m.GetFloat("_Metallic")>.7f));
                var position=App.View.transform.position;var rotation=App.View.transform.rotation;
                var lamps=UnityEngine.Object.FindObjectsByType<Light>().Where(l=>l.name=="Ceiling illumination").ToArray();
                Check("Ceiling intensity preserved",lamps.Length>0&&lamps.All(l=>l.intensity==1));
                yield return Capture("M01-painted-open");
                shade.Toggle();yield return new WaitForSecondsRealtime(2);
                Check("Shade closes without disabling room lamps",shade.lowered&&shade.daylight.All(l=>l.intensity==0)&&lamps.All(l=>l.enabled));
                yield return Capture("M01-painted-closed");
                shade.Toggle();yield return new WaitForSecondsRealtime(2);
                Check("Open shade restores daylight",!shade.lowered&&shade.daylight.Select((l,i)=>Mathf.Approximately(l.intensity,shade.daylightOpenIntensities[i])).All(v=>v));
                Check("Lighting leaves camera pose unchanged",Vector3.Distance(position,App.View.transform.position)<.001f&&Quaternion.Angle(rotation,App.View.transform.rotation)<.01f);
                yield break;
            }
            if(layoutOnly)
            {
                bool hints=App.Profile.settings.hints;App.Profile.settings.hints=false;
                yield return new WaitForSecondsRealtime(.4f);
                Check(id+" disabling hints removes step instructions",App.GetComponentsInChildren<TextMeshProUGUI>().Any(t=>t.text=="Chuẩn bị theo quy trình. Mở Hướng dẫn khi cần."));
                App.Profile.settings.hints=hints;yield return new WaitForSecondsRealtime(.4f);
                yield return Press("Thu gọn bảng");Check(id+" compact action remains accessible",Find("Compact current action")!=null);
                yield return Press("Hiện bảng");Check(id+" expanded panel restored",!Find("Compact current action"));
            }
            if(baseline)continue;
            if(!layoutOnly)
            {
            var shade=UnityEngine.Object.FindAnyObjectByType<LabShade>();
            Check(id+" authored blind and daylight references",shade&&shade.bottomBar&&shade.daylight.Length>0&&shade.daylight.Length==shade.daylightOpenIntensities.Length);
            var fixture=shade.GetComponentsInChildren<LabFixture>().Single(f=>f.action==LabFixture.Action.Shade);
            var indoor=UnityEngine.Object.FindObjectsByType<Light>().Where(l=>l.name=="Ceiling illumination").ToArray();var indoorValues=indoor.Select(l=>l.intensity).ToArray();
            yield return Press("HUD visibility");yield return Capture(id+"-daylight-open");var viewpoint=App.View.transform.position;var viewRotation=App.View.transform.rotation;
            fixture.Activate();yield return new WaitForSecondsRealtime(2);
            Check(id+" closed shade removes runtime daylight",shade.lowered&&shade.daylight.All(l=>l.intensity==0));
            Check(id+" indoor lamps remain lit",indoor.Length>0&&indoor.Select((l,i)=>l.enabled&&Mathf.Approximately(l.intensity,indoorValues[i])&&l.intensity>0).All(v=>v));
            for(int tier=0;tier<4;tier++){RoomLighting.Apply(tier);Check(id+" quality "+tier+" preserves closed daylight",shade.daylight.All(l=>l.intensity==0));}
            RoomLighting.Apply(App.Profile.settings.GraphicsTier);yield return null;yield return Capture(id+"-daylight-closed");
            Check(id+" lighting captures keep camera pose",Vector3.Distance(viewpoint,App.View.transform.position)<.001f&&Quaternion.Angle(viewRotation,App.View.transform.rotation)<.01f);
            for(int repeat=0;repeat<10;repeat++){fixture.Activate();shade.Step(10);}
            fixture.Activate();yield return new WaitForSecondsRealtime(2);
            Check(id+" repeated shade toggles restore exact daylight",!shade.lowered&&shade.daylight.Select((l,i)=>Mathf.Approximately(l.intensity,shade.daylightOpenIntensities[i])).All(v=>v));
            RoomLighting.Apply(App.Profile.settings.GraphicsTier);yield return Press("HUD visibility");
            Check(id+" VR entry absent outside Home",!Find("VR mode"));
            yield return Press("Station tools");
            Check(id+" tools sheet retains manual guidance and notebook",Find("Hướng dẫn")&&Find("Sổ tay"));
            Check(id+" tools sheet has no redundant VR entry",!Find("VR mode"));
            yield return Press("Room lighting");
            Check(id+" lighting panel exposes usable controls",Find("Room shade").IsInteractable()&&Find("Room lamp").IsInteractable());
            yield return Press("Room shade");Check(id+" panel closes actual shade",shade.lowered);
            fixture.Activate();yield return new WaitForSecondsRealtime(.2f);
            Check(id+" direct shade reversal updates panel action",!shade.lowered&&Find("Room shade").GetComponentInChildren<TextMeshProUGUI>().text=="Đóng rèm");
            yield return Press("Room lamp");Check(id+" ceiling switch independent from shade",indoor.All(l=>!l.enabled)&&!shade.lowered);
            yield return Press("Room lamp");Check(id+" ceiling switch restores indoor light",indoor.All(l=>l.enabled));
            Canvas.ForceUpdateCanvases();Check(id+" lighting panel text fits",!App.GetComponentsInChildren<TextMeshProUGUI>().Any(t=>t.isTextOverflowing||t.isTextTruncated));
            yield return Capture(id+"-lighting-panel");yield return Press("Lighting back");
            yield return Press("Station return");
            }
            if(id=="P01")foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(2340,1080),new Vector2Int(2048,1536)})
            {
                if(!SecondTestGameView.Configure(out var diagnostic,size.x,size.y))throw new Exception(diagnostic);
                yield return new WaitForSecondsRealtime(.6f);Canvas.ForceUpdateCanvases();
                Check("Real Game View dimensions "+size,Screen.width==size.x&&Screen.height==size.y);
                var buttons=App.GetComponentsInChildren<Button>();bool inside=true,overlap=false;var rects=new List<Rect>();
                foreach(var button in buttons){var corners=new Vector3[4];((RectTransform)button.transform).GetWorldCorners(corners);var r=Rect.MinMaxRect(corners.Min(v=>v.x),corners.Min(v=>v.y),corners.Max(v=>v.x),corners.Max(v=>v.y));inside&=r.xMin>=-.5f&&r.yMin>=-.5f&&r.xMax<=Screen.width+.5f&&r.yMax<=Screen.height+.5f;overlap|=rects.Any(other=>other.Overlaps(r));rects.Add(r);}
                Check("All HUD touch bounds inside "+size,inside);Check("HUD buttons do not overlap "+size,!overlap);
                yield return Capture("P01-"+size.x+"x"+size.y);
            }
            SecondTestGameView.Configure(out _);yield return new WaitForSecondsRealtime(.4f);
            foreach(bool world in new[]{false,true,false})
            {
                App.Input.SetSimulator(world);yield return new WaitForSecondsRealtime(.7f);
                Check(id+" mode has restore and VR exit controls "+world,Find("HUD visibility")&&!Find("VR mode")&&(world?Find("VR exit")!=null:Find("VR exit")==null));
                yield return Press("HUD visibility");
                Check(id+" hidden HUD has no active contents "+world,!App.GetComponentsInChildren<RectTransform>().Any(x=>x.name=="Activity HUD contents"));
                Check(id+" hidden HUD still has restore and VR exit "+world,Find("HUD visibility")&&!Find("VR mode")&&(world?Find("VR exit")!=null:Find("VR exit")==null));
                var hidden=App.GetComponentsInChildren<RectTransform>(true).First(x=>x.name=="Activity HUD contents");Check(id+" hidden graphics cannot intercept input "+world,!hidden.GetComponentsInChildren<Graphic>(true).Any(g=>g.isActiveAndEnabled&&g.raycastTarget));
                yield return Capture(id+(world?"-world-hidden":"-phone-hidden"));
                yield return Press("HUD visibility");
                Check(id+" restore reactivates contents "+world,App.GetComponentsInChildren<RectTransform>().Any(x=>x.name=="Activity HUD contents"));
                Canvas.ForceUpdateCanvases();var overflow=App.GetComponentsInChildren<TextMeshProUGUI>().Where(t=>t.isTextOverflowing||t.isTextTruncated).Select(t=>t.text).ToArray();
                Check(id+" text fits "+world+": "+string.Join(" | ",overflow),overflow.Length==0);
                if(world){yield return Press("VR exit");Check(id+" exit callback restores phone without entry button",!App.Input.SimulatorMode&&!App.Input.VrMode&&!Find("VR mode")&&!Find("VR exit"));}
            }
        }
    }
}
#endif
