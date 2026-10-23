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
    public static void Run(){Directory.CreateDirectory("Evidence/FinalPass/Hud");SecondTestGameView.Configure(out _);SessionState.SetString("VLAB.TestProfile",Path.Combine(Application.persistentDataPath,"QA","Hud-"+Guid.NewGuid().ToString("N")));SessionState.SetBool("VLAB.HudQa",true);EditorSceneManager.OpenScene("Assets/VLABProduct/Scenes/VLAB_Boot.unity");EditorApplication.EnterPlaymode();}
}
public sealed class FinalPassHudProbe:MonoBehaviour
{
    readonly List<string> rows=new List<string>();int failures;bool baseline;
    VlabApp App=>VlabApp.Instance;
    Button Find(string name)=>App.GetComponentsInChildren<Button>().FirstOrDefault(x=>x.name==name&&x.IsInteractable());
    void Check(string name,bool pass){rows.Add((pass?"PASS ":"FAIL ")+name);if(!pass)failures++;}
    IEnumerator Press(string name){yield return new WaitForSecondsRealtime(.4f);var b=Find(name);if(!b)throw new Exception("Missing "+name+" on "+App.Route);b.onClick.Invoke();yield return new WaitForSecondsRealtime(.4f);}
    IEnumerator Capture(string name){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot("Evidence/FinalPass/Hud/"+(baseline?"before-":"after-")+name+".png");yield return null;}
    IEnumerator Start()
    {
        DontDestroyOnLoad(gameObject);baseline=SessionState.GetBool("VLAB.HudBaseline",false);SessionState.SetBool("VLAB.HudBaseline",false);
        var stack=new Stack<IEnumerator>();stack.Push(Run());
        while(stack.Count>0){object value=null;bool next=false;try{next=stack.Peek().MoveNext();if(next)value=stack.Peek().Current;}catch(Exception e){rows.Add("FAIL "+e);failures++;break;}if(!next){stack.Pop();continue;}if(value is IEnumerator nested)stack.Push(nested);else yield return value;}
        File.WriteAllLines("Evidence/FinalPass/Hud/"+(baseline?"baseline":"checks")+".txt",new[]{"Editor UI callbacks and raycast/layout checks; no Android touch or physical tracking claim."}.Concat(rows));SecondTestGameView.Restore();EditorApplication.Exit(failures==0?0:1);
    }
    IEnumerator Run()
    {
        float deadline=Time.realtimeSinceStartup+60;while((!App||App.Route!="home")&&Time.realtimeSinceStartup<deadline)yield return null;
        if(!App||App.Route!="home")throw new Exception("Home timeout");App.Profile.settings.gazeAutoSelect=false;
        if(!baseline){yield return Press("VR mode");Check("Home round VR label updates on entry",Find("VR mode").GetComponentInChildren<TextMeshProUGUI>().text=="Thoát VR");yield return Press("VR mode");Check("Home round VR label updates on exit",Find("VR mode").GetComponentInChildren<TextMeshProUGUI>().text=="VR");}
        foreach(string id in new[]{"P01","C04","B01","M01"})
        {
            App.ShowHome();yield return Press("Subject "+id[0]);yield return Press("Browser lesson "+id);yield return Press("Browser start");yield return Press("Readiness enter");
            deadline=Time.realtimeSinceStartup+60;while(App.Route!="activity"&&Time.realtimeSinceStartup<deadline)yield return null;
            if(App.Route!="activity"||App.Session.spec.id!=id)throw new Exception("Activity timeout "+id);
            yield return new WaitForSecondsRealtime(1);yield return Capture(id+"-phone");
            Check(id+" prepared station loaded",App.Apparatus&&App.Apparatus.Ready);
            if(baseline)continue;
            var shade=UnityEngine.Object.FindAnyObjectByType<LabShade>();
            Check(id+" authored blind and daylight references",shade&&shade.bottomBar&&shade.daylight.Length>0&&shade.daylight.Length==shade.daylightOpenIntensities.Length);
            var fixture=shade.GetComponentsInChildren<LabFixture>().Single(f=>f.action==LabFixture.Action.Shade);
            var indoor=UnityEngine.Object.FindObjectsByType<Light>().Where(l=>l.name=="Ceiling illumination").ToArray();var indoorValues=indoor.Select(l=>l.intensity).ToArray();
            yield return Press("HUD visibility");yield return Capture(id+"-daylight-open");var viewpoint=App.View.transform.position;var viewRotation=App.View.transform.rotation;
            fixture.Activate();yield return new WaitForSecondsRealtime(2);
            Check(id+" closed shade removes runtime daylight",shade.lowered&&shade.daylight.All(l=>l.intensity==0));
            Check(id+" indoor lamps remain lit",indoor.Length>0&&indoor.Select((l,i)=>l.enabled&&Mathf.Approximately(l.intensity,indoorValues[i])&&l.intensity>0).All(v=>v));
            for(int tier=0;tier<4;tier++){RoomLighting.Apply(tier);Check(id+" quality "+tier+" preserves closed daylight",shade.daylight.All(l=>l.intensity==0));}
            yield return Capture(id+"-daylight-closed");
            Check(id+" lighting captures keep camera pose",Vector3.Distance(viewpoint,App.View.transform.position)<.001f&&Quaternion.Angle(viewRotation,App.View.transform.rotation)<.01f);
            for(int repeat=0;repeat<10;repeat++){fixture.Activate();shade.Step(10);}
            fixture.Activate();yield return new WaitForSecondsRealtime(2);
            Check(id+" repeated shade toggles restore exact daylight",!shade.lowered&&shade.daylight.Select((l,i)=>Mathf.Approximately(l.intensity,shade.daylightOpenIntensities[i])).All(v=>v));
            RoomLighting.Apply(App.Profile.settings.GraphicsTier);yield return Press("HUD visibility");
            Check(id+" VR entry absent outside Home",!Find("VR mode"));
            yield return Press("Station tools");
            Check(id+" tools sheet retains manual guidance and notebook",Find("Hướng dẫn")&&Find("Sổ tay"));
            Check(id+" tools sheet has no redundant VR entry",!Find("VR mode"));
            yield return Press("Station return");
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
