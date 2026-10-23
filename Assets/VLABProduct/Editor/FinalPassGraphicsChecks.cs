#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using TMPro;
using Vlab.Product;

[InitializeOnLoad]
public static class FinalPassGraphicsChecks
{
    static FinalPassGraphicsChecks(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("VLAB.GraphicsChecks",false)){SessionState.SetBool("VLAB.GraphicsChecks",false);new GameObject("Graphics checks",typeof(FinalPassGraphicsProbe));}};}
    public static void Run()
    {
        Directory.CreateDirectory("Evidence/FinalPass");SecondTestGameView.Configure(out _);
        SessionState.SetString("VLAB.TestProfile",Path.Combine(Application.persistentDataPath,"QA","Graphics-"+Guid.NewGuid().ToString("N")));
        SessionState.SetBool("VLAB.GraphicsChecks",true);EditorSceneManager.OpenScene("Assets/VLABProduct/Scenes/VLAB_Boot.unity");EditorApplication.EnterPlaymode();
    }
}
public sealed class FinalPassGraphicsProbe:MonoBehaviour
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    [Serializable] sealed class Measurement
    {
        public string lesson,scope="Unity Editor Windows; no Android, stereoscopic or phone sensor claim",device,gpu,graphicsApi,unity;
        public int width,height,graphics;public float renderScale;public Vector3 eye,euler;public double durationSeconds,meanMs,p50Ms,p95Ms,fps,cpuMs,gpuMs;public double[] frameIntervalsMs;
    }
    [Serializable] sealed class Envelope{public int format=1;public string payload,sha256;}
    VlabApp App=>VlabApp.Instance;readonly List<string> rows=new List<string>();int failures;
    object Call(string name,params object[] args)=>typeof(VlabApp).GetMethod(name,Flags).Invoke(App,args);
    void Check(string name,bool ok){rows.Add((ok?"PASS ":"FAIL ")+name);if(!ok)failures++;Debug.Log(rows[rows.Count-1]);}
    IEnumerator Press(string name)
    {
        yield return new WaitForSecondsRealtime(.35f);
        var button=App.GetComponentsInChildren<Button>().FirstOrDefault(b=>b.name==name&&b.interactable);if(!button)throw new Exception("Missing UI control "+name+" at "+App.Route);
        button.onClick.Invoke();yield return null;
    }
    IEnumerator Tap(string name)
    {
        yield return new WaitForSecondsRealtime(.5f);
        var button=App.GetComponentsInChildren<Button>().FirstOrDefault(b=>b.name==name&&b.interactable);if(!button)throw new Exception("Missing touch control "+name);
        var scroll=button.GetComponentInParent<ScrollRect>();
        if(scroll)
        {
            var corners=new Vector3[4];((RectTransform)button.transform).GetWorldCorners(corners);float middle=(corners[0].y+corners[2].y)*.5f;scroll.viewport.GetWorldCorners(corners);
            if(middle<corners[0].y+12||middle>corners[2].y-12){scroll.verticalNormalizedPosition=middle<corners[0].y?.0f:1;yield return null;}
        }
        var point=RectTransformUtility.WorldToScreenPoint(null,((RectTransform)button.transform).TransformPoint(((RectTransform)button.transform).rect.center));
        var screen=InputSystem.AddDevice<Touchscreen>();
        InputSystem.QueueStateEvent(screen,new TouchState{touchId=941,phase=UnityEngine.InputSystem.TouchPhase.Began,position=point});yield return new WaitForSecondsRealtime(.12f);
        InputSystem.QueueStateEvent(screen,new TouchState{touchId=941,phase=UnityEngine.InputSystem.TouchPhase.Ended,position=point});yield return new WaitForSecondsRealtime(.5f);InputSystem.RemoveDevice(screen);
    }
    IEnumerator SelectTier(int tier,string returnTo)
    {
        var session=App.Session;int operation=session?.Operation??0;
        int previous=App.Profile.settings.GraphicsTier;Call("OpenSettings",returnTo);yield return Tap("Đồ họa");yield return Tap("Graphics tier "+tier);
        Check("Tier "+tier+" queued touch changes draft only",((GameSettings)typeof(VlabApp).GetField("draft",Flags).GetValue(App)).GraphicsTier==tier&&App.Profile.settings.GraphicsTier==previous);
        yield return Tap("Settings apply");Check("Tier "+tier+" applies through real UI",App.Profile.settings.GraphicsTier==tier&&App.Route==returnTo);
        var pipeline=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        Check("Tier "+tier+" real render configuration",pipeline&&Mathf.Approximately(pipeline.renderScale,tier==0?.75f:tier==1?.85f:1)&&pipeline.mainLightShadowmapResolution==(tier<2?512:tier==2?1024:2048)&&pipeline.msaaSampleCount==(tier<2?2:4)&&pipeline.shadowCascadeCount==(tier<2?1:tier==2?2:4));
        var loaded=new ProfileStore(SessionState.GetString("VLAB.TestProfile","")).Load();
        Check("Tier "+tier+" retained in reloaded offline profile",!loaded.writeProtected&&loaded.profile.settings.GraphicsTier==tier);
        if(App.Route=="activity")
        {
            Check("Changing graphics preserves live experiment and step",ReferenceEquals(session,App.Session)&&App.Session.Operation==operation);
            var sun=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).FirstOrDefault(l=>l.type==LightType.Directional);
            Check(App.Session.spec.id+" tier "+tier+" shadows and exposure",sun&&Mathf.Approximately(sun.intensity,1.05f)&&sun.shadows==(tier==0?LightShadows.None:tier==1?LightShadows.Hard:LightShadows.Soft)&&QualitySettings.anisotropicFiltering==(tier>=2?AnisotropicFiltering.ForceEnable:AnisotropicFiltering.Disable));
        }
        var same=pipeline;Call("ApplySettings");Check("Unrelated apply reuses pipeline at tier "+tier,ReferenceEquals(same,UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline));
    }
    void Core()
    {
        var samples=new FrameSamples();samples.AddSeconds(double.NaN);samples.AddSeconds(0);samples.AddSeconds(-1);samples.Summarize();Check("Frame interval rejects invalid values without fabricated FPS",samples.Count==0&&samples.Fps==0);
        foreach(double value in new[]{.01,.02,.03,.04,.05})samples.AddSeconds(value);samples.Summarize();
        Check("Observed interval mean, median, p95 and FPS use correct units",samples.Count==5&&Math.Abs(samples.MeanMs-30)<.0001&&samples.MedianMs==30&&samples.P95Ms==50&&Math.Abs(samples.Fps-1000.0/30)<.0001);
        for(int i=0;i<700;i++)samples.AddSeconds(.02);samples.Summarize();Check("Frame window is bounded to latest 600 samples",samples.Count==600&&samples.CopyMilliseconds().All(x=>x==20)&&samples.P95Ms==20);
        samples.Clear();Check("Clearing sample window removes stale pause/loading timing",samples.Count==0&&samples.MeanMs==0);
        Check("Hardware suggestion handles unknown/low/high without promising Ultra",VlabGraphics.Recommend(0,0)==1&&VlabGraphics.Recommend(2048,1024)==0&&VlabGraphics.Recommend(12000,4096)==2);
        var profile=JsonUtility.FromJson<LocalProfile>(JsonUtility.ToJson(App.Profile));string now=DateTime.UtcNow.ToString("O");
        profile.notebookPages.Add(new NotebookPage{id="graphics-fixture",experimentId="P01",title="Ghi chú kiểm thử lưu",body="Đơn vị: T = 2π√(l/g).",createdAtUtc=now,updatedAtUtc=now});
        string folder=Path.Combine(Application.persistentDataPath,"QA","GraphicsPersistence-"+Guid.NewGuid().ToString("N"));var store=new ProfileStore(folder);
        profile.settings.SetGraphicsTier(3);profile.settings.showPerformance=true;
        Check("Ultra stores legacy-compatible graphics ID",profile.settings.graphics==2&&profile.settings.ultraGraphics);
        Check("Ultra and diagnostics save",store.Save(profile,out _));var loaded=new ProfileStore(folder).Load();
        Check("Ultra roundtrip preserves notebook and other profile data",!loaded.writeProtected&&loaded.profile.settings.GraphicsTier==3&&loaded.profile.settings.showPerformance&&JsonUtility.ToJson(loaded.profile.notebookPages.Last())==JsonUtility.ToJson(profile.notebookPages.Last())&&loaded.profile.attempts.Count==profile.attempts.Count&&loaded.profile.locale==profile.locale);
        string payload=JsonUtility.ToJson(profile);payload=Regex.Replace(payload,"\"(?:ultraGraphics|showPerformance)\":(?:true|false),?","").Replace(",}","}");
        // An older app verifies the raw envelope, ignores unknown fields and writes legacy graphics=2.
        File.WriteAllText(Path.Combine(folder,"profile.json"),JsonUtility.ToJson(new Envelope{payload=payload,sha256=ProfileStore.Hash(payload)}));
        loaded=new ProfileStore(folder).Load();Check("Legacy High profile loads with diagnostics off, no schema migration or notes loss",!loaded.writeProtected&&loaded.profile.settings.GraphicsTier==2&&!loaded.profile.settings.showPerformance&&loaded.profile.notebookPages.Last().body==profile.notebookPages.Last().body);
        profile.settings.graphics=1;bool rejected=false;try{profile.Validate();}catch(InvalidDataException){rejected=true;}Check("Contradictory Ultra settings rejected at save boundary",rejected);
        foreach(int invalid in new[]{-1,4}){rejected=false;try{profile.settings.SetGraphicsTier(invalid);}catch(ArgumentOutOfRangeException){rejected=true;}Check("Invalid tier "+invalid+" rejected",rejected);}
    }
    IEnumerator LayoutCases()
    {
        var ui=(VlabUi)typeof(VlabApp).GetField("ui",Flags).GetValue(App);var canvas=ui.canvas;
        foreach(string locale in new[]{"vi","en"})
        {
            App.Profile.locale=locale;App.Profile.settings.largeText=true;Call("ApplySettings");Call("OpenSettings","activity");yield return Press(locale=="vi"?"Đồ họa":"Graphics");
            foreach(var size in new[]{new Vector2Int(1600,900),new Vector2Int(2340,1080),new Vector2Int(2400,1080),new Vector2Int(1080,2340)})
            {
                var safe=new Rect(72,24,size.x-96,size.y-48);var target=new RenderTexture(size.x,size.y,24);target.Create();var mode=canvas.renderMode;var camera=canvas.worldCamera;var distance=canvas.planeDistance;var previous=App.View.targetTexture;
                try
                {
                    App.View.targetTexture=target;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=App.View;canvas.planeDistance=.2f;ui.PhoneViewport.SetViewportOverride(size,safe);ui.RefreshPhoneLayout();
                    yield return null;yield return null;yield return null;Canvas.ForceUpdateCanvases();
                    var box=App.GetComponentsInChildren<RectTransform>().First(r=>r.name=="Settings");var corners=new Vector3[4];box.GetWorldCorners(corners);
                    Check(locale+" "+size+" settings inside safe area",corners.All(c=>{var pixel=App.View.WorldToScreenPoint(c);return pixel.x>=safe.xMin-.5f&&pixel.x<=safe.xMax+.5f&&pixel.y>=safe.yMin-.5f&&pixel.y<=safe.yMax+.5f;}));
                    var centre=App.View.WorldToScreenPoint(box.TransformPoint(box.rect.center));Check(locale+" "+size+" panel is centered and bounded on tall screens",Vector2.Distance(centre,safe.center)<1&&box.rect.height<=780.1f);
                    var tiers=App.GetComponentsInChildren<Button>().Where(b=>b.name.StartsWith("Graphics tier ")).ToArray();
                    Check(locale+" "+size+" four equal tier targets",tiers.Length==4&&tiers.Select(b=>((RectTransform)b.transform).rect.size).Distinct().Count()==1);
                    Check(locale+" "+size+" selected underline stays inside card",tiers.All(b=>b.GetComponentsInChildren<RectTransform>().Where(r=>r.name=="Section rule").All(r=>r.anchoredPosition.x>=0&&r.anchoredPosition.x+r.rect.width<=((RectTransform)b.transform).rect.width)));
                    var labels=box.GetComponentsInChildren<TextMeshProUGUI>();foreach(var text in labels)text.ForceMeshUpdate(true,true);
                    var overflow=labels.Where(t=>t.isTextOverflowing||t.isTextTruncated).Select(t=>t.text).ToArray();Check(locale+" "+size+" large-text labels readable without TMP overflow: "+string.Join(" | ",overflow),overflow.Length==0);
                    var scroll=box.GetComponentInChildren<ScrollRect>();
                    foreach(var button in tiers)
                    {
                        bool reachable=false;
                        foreach(float position in new[]{1f,.5f,0f})
                        {scroll.verticalNormalizedPosition=position;Canvas.ForceUpdateCanvases();var point=button.transform.TransformPoint(((RectTransform)button.transform).rect.center);var local=scroll.viewport.InverseTransformPoint(point);if(scroll.viewport.rect.Contains(local))reachable=true;}
                        Check(locale+" "+size+" scroll can expose "+button.name,reachable);
                    }
                    scroll.verticalNormalizedPosition=1;Canvas.ForceUpdateCanvases();yield return new WaitForEndOfFrame();LabPanoramaCapture.RenderMono(App.View,target);
                    var active=RenderTexture.active;RenderTexture.active=target;var image=new Texture2D(size.x,size.y,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,size.x,size.y),0,0);image.Apply();File.WriteAllBytes("Evidence/FinalPass/graphics-settings-"+locale+"-"+size.x+"x"+size.y+"-safe.png",image.EncodeToPNG());Destroy(image);RenderTexture.active=active;
                }
                finally{App.View.targetTexture=previous;canvas.renderMode=mode;canvas.worldCamera=camera;canvas.planeDistance=distance;ui.PhoneViewport.ClearViewportOverride();target.Release();Destroy(target);}
                yield return null;yield return null;
            }
            Call("ReturnTo","activity");
        }
    }
    IEnumerator OverlayNavigation()
    {
        App.Profile.locale="vi";App.Profile.settings.showPerformance=true;Call("ApplySettings");
        var ui=(VlabUi)typeof(VlabApp).GetField("ui",Flags).GetValue(App);var panel=App.GetComponentsInChildren<RectTransform>(true).Single(r=>r.name=="Performance overlay");
        Func<RectTransform,Rect> bounds=rect=>{var corners=new Vector3[4];rect.GetWorldCorners(corners);var points=corners.Select(p=>ui.surface.InverseTransformPoint(p)).ToArray();return Rect.MinMaxRect(points.Min(p=>p.x),points.Min(p=>p.y),points.Max(p=>p.x),points.Max(p=>p.y));};
        foreach(bool world in new[]{false,true})
        {
            App.Input.SetSimulator(world);App.ShowHome();yield return new WaitForSecondsRealtime(.7f);
            Check((world?"World":"Phone")+" home counter does not cover any interactive target",panel.gameObject.activeSelf&&App.GetComponentsInChildren<Button>().All(b=>!bounds(panel).Overlaps(bounds((RectTransform)b.transform))));
            yield return Press("Subject P");yield return new WaitForSecondsRealtime(.6f);Check("Browser navigation remains unobscured by diagnostics",!panel.gameObject.activeSelf);
            yield return Press("Browser lesson P01");yield return Press("Browser start");yield return Press("Readiness enter");double until=Time.realtimeSinceStartupAsDouble+50;while(App.Route!="activity"&&Time.realtimeSinceStartupAsDouble<until)yield return null;yield return new WaitForSecondsRealtime(.7f);
            Check((world?"World":"Phone")+" lab counter does not cover VR/experiment controls",panel.gameObject.activeSelf&&App.GetComponentsInChildren<Button>().All(b=>!bounds(panel).Overlaps(bounds((RectTransform)b.transform))));
            Call("ShowStationTools");yield return new WaitForSecondsRealtime(.6f);Check("Instrument UI temporarily hides counter without disabling sampling",!panel.gameObject.activeSelf&&App.Performance.enabled);
            Call("CloseModal");yield return new WaitForSecondsRealtime(.6f);Check("Closing instrument UI restores counter",panel.gameObject.activeSelf);
        }
        App.Input.SetSimulator(false);
    }
    IEnumerator Enter(string id)
    {
        App.ShowHome();yield return Press("Subject "+id[0]);yield return Press("Browser lesson "+id);yield return Press("Browser start");yield return Press("Readiness enter");
        double until=Time.realtimeSinceStartupAsDouble+50;while(App.Route!="activity"&&Time.realtimeSinceStartupAsDouble<until)yield return null;
        Check(id+" entered real station",App.Route=="activity"&&App.Session?.spec.id==id);yield return new WaitForSecondsRealtime(2);
    }
    IEnumerator Measure(string id,string prefix)
    {
        App.Performance.ResetSamples();double start=Time.realtimeSinceStartupAsDouble;yield return new WaitForSecondsRealtime(10);
        App.Performance.Samples.Summarize();var samples=App.Performance.Samples;var pipeline=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        var data=new Measurement{lesson=id,device=SystemInfo.deviceModel,gpu=SystemInfo.graphicsDeviceName,graphicsApi=SystemInfo.graphicsDeviceType.ToString(),unity=Application.unityVersion,width=Screen.width,height=Screen.height,graphics=App.Profile.settings.GraphicsTier,renderScale=pipeline?pipeline.renderScale:0,eye=App.View.transform.position,euler=App.View.transform.eulerAngles,durationSeconds=Time.realtimeSinceStartupAsDouble-start,meanMs=samples.MeanMs,p50Ms=samples.MedianMs,p95Ms=samples.P95Ms,fps=samples.Fps,cpuMs=double.IsFinite(App.Performance.CpuMs)?App.Performance.CpuMs:-1,gpuMs=double.IsFinite(App.Performance.GpuMs)?App.Performance.GpuMs:-1,frameIntervalsMs=samples.CopyMilliseconds()};
        File.WriteAllText("Evidence/FinalPass/"+prefix+"-"+id+".json",JsonUtility.ToJson(data,true));
        Check(id+" real frame samples captured (not hardware FPS acceptance)",samples.Count>=30&&samples.MeanMs>0&&samples.P95Ms>=samples.MedianMs);
        yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot("Evidence/FinalPass/"+prefix+"-"+id+".png");yield return null;
    }
    IEnumerator Start()
    {
        Application.runInBackground=true;DontDestroyOnLoad(gameObject);var stack=new Stack<IEnumerator>();stack.Push(Run());
        while(stack.Count>0){object value=null;bool next=false;Exception error=null;try{next=stack.Peek().MoveNext();if(next)value=stack.Peek().Current;}catch(Exception e){error=e;}if(error!=null){rows.Add("BLOCKER "+error);failures++;break;}if(!next){stack.Pop();continue;}if(value is IEnumerator nested)stack.Push(nested);else yield return value;}
        File.WriteAllLines("Evidence/FinalPass/graphics-checks.txt",rows);SecondTestGameView.Restore();EditorApplication.Exit(failures==0?0:1);
    }
    IEnumerator Run()
    {
        double until=Time.realtimeSinceStartupAsDouble+50;while((!App||App.Route!="home")&&Time.realtimeSinceStartupAsDouble<until)yield return null;
        if(!App||App.Route!="home")throw new Exception("Home startup timed out");
        App.Profile.settings.gazeAutoSelect=false;
        if(Environment.GetEnvironmentVariable("VLAB_GRAPHICS_BASELINE")=="1")
        {App.Profile.settings.showPerformance=true;Call("ApplySettings");foreach(string id in new[]{"P01","C01","B01","M01"}){yield return Enter(id);yield return Measure(id,"graphics-baseline");}yield break;}
        Core();
        if(Environment.GetEnvironmentVariable("VLAB_GRAPHICS_LAYOUT_ONLY")=="1"){yield return Enter("P01");yield return LayoutCases();yield return OverlayNavigation();yield break;}
        foreach(int tier in new[]{0,1,2,3})yield return SelectTier(tier,"home");
        Call("OpenSettings","home");yield return Tap("Đồ họa");yield return Tap("Graphics recommendation");
        Check("Suggested tier requires explicit Apply",((GameSettings)typeof(VlabApp).GetField("draft",Flags).GetValue(App)).GraphicsTier==VlabGraphics.RecommendedTier&&App.Profile.settings.GraphicsTier==3);
        yield return Tap("Settings apply");Check("User can apply the hardware suggestion",App.Profile.settings.GraphicsTier==VlabGraphics.RecommendedTier);
        Call("OpenSettings","home");yield return Tap("Đồ họa");yield return Tap("Graphics performance toggle");yield return Tap("Settings apply");
        Check("Diagnostics enabled through real queued touch",App.Profile.settings.showPerformance&&App.Performance.enabled);
        foreach(string id in new[]{"P01","C01","B01","M01"})
        {yield return Enter(id);foreach(int tier in new[]{0,3}){yield return SelectTier(tier,"activity");yield return new WaitForSecondsRealtime(2);yield return Measure(id,"graphics-tier"+tier);}}
        var label=App.GetComponentsInChildren<TextMeshProUGUI>().First(t=>t.transform.parent.name=="Performance overlay");label.ForceMeshUpdate(true,true);
        Check("Metrics show measured app interval and native CPU/GPU without text overflow",!label.isTextTruncated&&!label.isTextOverflowing&&label.text.Contains("CPU")&&label.text.Contains("GPU")&&!label.text.Contains("motion-to-photon"));
        Check("Diagnostic overlay never blocks UI/model raycasts",label.transform.parent.GetComponentsInChildren<UnityEngine.UI.Graphic>().All(g=>!g.raycastTarget));
        int chosen=App.Profile.settings.GraphicsTier;
        for(int i=0;i<10;i++)
        {
            App.Input.SetSimulator(true);yield return new WaitForSecondsRealtime(.2f);Check("Stereo simulation "+i+" keeps quality and metrics plane",App.Profile.settings.GraphicsTier==chosen&&label.GetComponentInParent<Canvas>().renderMode==RenderMode.WorldSpace);
            App.Input.SetSimulator(false);yield return new WaitForSecondsRealtime(.2f);Check("Phone return "+i+" keeps quality and one diagnostics overlay",App.Profile.settings.GraphicsTier==chosen&&App.GetComponentsInChildren<RectTransform>(true).Count(r=>r.name=="Performance overlay")==1);
        }
        App.Performance.SendMessage("OnApplicationPause",true);Check("Pause callback resets measured window and native timing",App.Performance.Samples.Count==0&&double.IsNaN(App.Performance.CpuMs)&&double.IsNaN(App.Performance.GpuMs));
        App.Performance.SendMessage("OnApplicationPause",false);
        Call("OpenSettings","activity");yield return Tap("Đồ họa");yield return Tap("Graphics performance toggle");yield return Tap("Settings apply");Check("Diagnostics off really hides overlay",!App.Performance.enabled&&!label.gameObject.activeInHierarchy);
        yield return LayoutCases();
    }
}
#endif
