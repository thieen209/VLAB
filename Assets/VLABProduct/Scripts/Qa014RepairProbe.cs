#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Vlab.Product;

public sealed class Qa014RepairProbe:MonoBehaviour
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    static readonly string Folder=Path.GetFullPath("../work/qa014-repair/evidence");
    readonly List<string> rows=new List<string>();int failures;
    VlabApp App=>VlabApp.Instance;
    void Check(string name,bool pass){rows.Add((pass?"PASS ":"FAIL ")+name);if(!pass)failures++;}
    object Call(string name,params object[] args)=>typeof(VlabApp).GetMethod(name,Private).Invoke(App,args);
    T Field<T>(string name)=>(T)typeof(VlabApp).GetField(name,Private).GetValue(App);
    Button Find(string name)=>App.GetComponentsInChildren<Button>(false).FirstOrDefault(b=>b.isActiveAndEnabled&&(b.name==name||b.GetComponentInChildren<TMP_Text>()?.text==name))??throw new Exception("Missing "+name+" route="+App.Route+" buttons="+string.Join("|",App.GetComponentsInChildren<Button>(false).Select(b=>b.name+"="+b.GetComponentInChildren<TMP_Text>()?.text)));
    IEnumerator Press(string name){Find(name).onClick.Invoke();yield return new WaitForSecondsRealtime(.8f);}
    IEnumerator Start()
    {
        DontDestroyOnLoad(gameObject);Directory.CreateDirectory(Folder);var stack=new Stack<IEnumerator>();stack.Push(Checks());
        while(stack.Count>0){object value=null;bool next=false;Exception error=null;try{next=stack.Peek().MoveNext();if(next)value=stack.Peek().Current;}catch(Exception ex){error=ex;}
            if(error!=null){rows.Add("BLOCKER "+error);failures++;break;}if(!next){stack.Pop();continue;}if(value is IEnumerator nested){stack.Push(nested);continue;}yield return value;}
        File.WriteAllLines(Path.Combine(Folder,"checks.txt"),new[]{"Editor component callbacks + native rendered geometry; isolated P01 setup fixture; not physical Android/VR or full recipes."}.Concat(rows));
        SecondTestGameView.Restore();EditorApplication.Exit(failures==0?0:1);
    }
    static Rect Bounds(RectTransform rect,Camera camera=null)
    {var corners=new Vector3[4];rect.GetWorldCorners(corners);var points=corners.Select(c=>RectTransformUtility.WorldToScreenPoint(camera,c)).ToArray();return Rect.MinMaxRect(points.Min(p=>p.x),points.Min(p=>p.y),points.Max(p=>p.x),points.Max(p=>p.y));}
    void Caption(Button button,string label)
    {
        var text=button.GetComponentInChildren<TMP_Text>();text.ForceMeshUpdate();var rect=((RectTransform)button.transform).rect;bool fits=true;
        foreach(var c in text.textInfo.characterInfo.Take(text.textInfo.characterCount).Where(c=>c.isVisible))foreach(var v in new[]{c.bottomLeft,c.topRight}){var p=button.transform.InverseTransformPoint(text.transform.TransformPoint(v));fits&=p.x>=rect.xMin-1&&p.x<=rect.xMax+1&&p.y>=rect.yMin-1&&p.y<=rect.yMax+1;}
        Check(label+" glyphs inside hit target "+button.name,fits&&!text.isTextOverflowing);
    }
    void Capture(string name,RenderTexture target=null)
    {
        if(!target){ScreenCapture.CaptureScreenshot(Path.Combine(Folder,name+".png"));return;}
        LabPanoramaCapture.RenderMono(App.View,target);var previous=RenderTexture.active;RenderTexture.active=target;var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();File.WriteAllBytes(Path.Combine(Folder,name+".png"),image.EncodeToPNG());Destroy(image);RenderTexture.active=previous;
    }
    IEnumerator Aspect(int width,int height,Action<RenderTexture> assertions)
    {
        var ui=Field<VlabUi>("ui");var canvas=ui.canvas;var mode=canvas.renderMode;var camera=canvas.worldCamera;float distance=canvas.planeDistance;var target=App.View.targetTexture;var rt=new RenderTexture(width,height,24);rt.Create();
        App.View.targetTexture=rt;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=App.View;canvas.planeDistance=.2f;ui.PhoneViewport.SetViewportOverride(new Vector2(width,height),new Rect(0,0,width,height));ui.RefreshPhoneLayout();
        yield return null;yield return null;yield return null;Canvas.ForceUpdateCanvases();assertions(rt);
        App.View.targetTexture=target;canvas.renderMode=mode;canvas.worldCamera=camera;canvas.planeDistance=distance;ui.PhoneViewport.ClearViewportOverride();Destroy(rt);yield return null;yield return null;
    }
    IEnumerator Checks()
    {
        float deadline=Time.realtimeSinceStartup+40;while((!App||App.Route!="home")&&Time.realtimeSinceStartup<deadline)yield return null;
        if(!App||App.Route!="home")throw new Exception("Home did not initialize");App.Profile.locale="vi";App.Profile.settings.focusAssist=true;
        var ui=Field<VlabUi>("ui");
        foreach(bool large in new[]{false,true})foreach(bool vi in new[]{true,false})
        {
            ui.LargeText=large;
            foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(2340,1080),new Vector2Int(2160,1080)})
            {
                var host=ui.Rect(ui.surface,"QA readiness fixture",0,0,1600,1000);var page=Field<RectTransform>("page");page.gameObject.SetActive(false);
                foreach(string id in new[]{"P02","C01","C08","M04"})
                {
                    var view=ReadinessView.Render(ui,host,new ExperimentBrief{id=id},vi,Color.cyan,false,()=>{},()=>{},()=>{});yield return null;
                    yield return Aspect(size.x,size.y,rt=>{var goal=view.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="Readiness goal");goal.ForceMeshUpdate();Check(id+" full goal "+size+" large="+large+" vi="+vi,!goal.isTextOverflowing&&goal.textInfo.characterInfo.Take(goal.textInfo.characterCount).Last(c=>!char.IsWhiteSpace(c.character)).isVisible);if(id=="P02"&&!large&&vi)Capture("P02-readiness-"+size.x,rt);});
                    Destroy(view.gameObject);yield return null;
                }
                Destroy(host.gameObject);page.gameObject.SetActive(true);yield return null;
            }
        }
        ui.LargeText=false;yield return Press("Subject P");yield return Press("Browser lesson P07");yield return Press("Browser start");yield return Press("Readiness enter");
        deadline=Time.realtimeSinceStartup+45;while(App.Route!="activity"&&Time.realtimeSinceStartup<deadline)yield return null;if(App.Route!="activity"||App.Session.spec.id!="P07")throw new Exception("P07 station selection not accepted");
        Call("ShowEquipment");yield return new WaitForSecondsRealtime(.4f);
        foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(2340,1080),new Vector2Int(2160,1080)})
        {
            yield return Aspect(size.x,size.y,rt=>{Capture("P07-before-label-check-"+size.x,rt);foreach(var b in new[]{Find("Xong"),Find("Trang trước"),Find("Trang sau")})Caption(b,"normal "+size);Capture("P07-tools-"+size.x,rt);});
            var next=Find("Trang sau");yield return Press("Trang sau");Check("Pagination changes actual page",App.GetComponentsInChildren<TMP_Text>().Any(t=>t.isActiveAndEnabled&&t.text=="2 / 2"));yield return Press("Trang trước");
            var done=Find("Xong");ExecuteEvents.Execute(done.gameObject,new PointerEventData(EventSystem.current),ExecuteEvents.pointerDownHandler);yield return new WaitForSecondsRealtime(.15f);Caption(done,"pressed");ExecuteEvents.Execute(done.gameObject,new PointerEventData(EventSystem.current),ExecuteEvents.pointerUpHandler);done.interactable=false;yield return new WaitForSecondsRealtime(.15f);Caption(done,"disabled");done.interactable=true;
        }
        yield return Press("Xong");Check("Done closes equipment sheet",!Field<RectTransform>("modal"));Call("ShowEquipment");yield return new WaitForSecondsRealtime(.3f);Check("Equipment sheet reopens",Field<RectTransform>("modal"));Call("CloseModal");Call("ShowHome");yield return new WaitForSecondsRealtime(.4f);
        yield return Press("Subject P");yield return Press("Browser lesson P01");yield return Press("Browser start");yield return Press("Readiness enter");deadline=Time.realtimeSinceStartup+45;while(App.Route!="activity"&&Time.realtimeSinceStartup<deadline)yield return null;
        // Isolated setup fixture for guide mapping, not a learner recipe completion.
        for(int i=0;i<10&&!App.Apparatus.Ready;i++){var item=App.Apparatus.NextInstallableItem();App.Apparatus.Select(item);if(!App.Apparatus.InstallSelected())throw new Exception("P01 fixture install failed");}
        var guide=new LessonGuide(App.Session);string[] targets={"stand","pendulum","pendulum","pendulum"};
        for(int i=0;i<4;i++)
        {
            Check("P01 preparation target "+i,guide.Preparation(App.Apparatus,true,i).key==targets[i]);
            if(i==2){Call("RefreshActivity");Call("ShowGuideTarget",guide.Preparation(App.Apparatus,true,i));yield return new WaitForSecondsRealtime(.5f);var cue=Field<GuideCue>("worldGuideCue");Check("Step3 points at pendulum",cue&&App.Apparatus.Selected.assetId=="pendulum");var callout=cue.GetComponentsInChildren<Canvas>().Single().GetComponent<RectTransform>();var label=Bounds(callout,App.View);foreach(var zoom in App.GetComponentsInChildren<Button>().Where(b=>b.name.StartsWith("Phone zoom")))Check("Callout clears "+zoom.name,!label.Overlaps(Bounds((RectTransform)zoom.transform)));Capture("P01-step3-guidance");yield return null;Call("ClearGuideCue");yield return null;Check("Guide dismisses",!Field<GuideCue>("worldGuideCue"));}
            if(!App.Apparatus.PrepareNext(out var why))throw new Exception("P01 preparation fixture: "+why);
        }
        Check("No fixture measurement or notebook entry",App.Profile.attempts.Count==0);
    }
}
[InitializeOnLoad] public static class Qa014RepairChecks
{
    static Qa014RepairChecks(){EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("VLAB.QA014",false)){SessionState.SetBool("VLAB.QA014",false);new GameObject("QA014 bounded checks",typeof(Qa014RepairProbe));}};}
    public static void Run(){if(!SecondTestGameView.Configure(out var why))throw new Exception(why);SessionState.SetBool("VLAB.QA014",true);SessionState.SetBool("NguaManVLAB.ProductSmoke",false);SessionState.SetBool("VLAB.FollowupRun",false);SessionState.SetBool("VLAB.SecondTestUI",false);SessionState.SetString("VLAB.TestProfile",Path.Combine(Application.persistentDataPath,"QA","QA014-"+Guid.NewGuid().ToString("N")));EditorSceneManager.OpenScene("Assets/VLABProduct/Scenes/VLAB_Boot.unity");EditorApplication.EnterPlaymode();}
}
#endif
