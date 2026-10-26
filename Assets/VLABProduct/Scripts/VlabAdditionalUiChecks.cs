#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Vlab.Product;
using Object=UnityEngine.Object;

// Shared UI coverage; the independent SDK journeys verify the scientific recordings.
public static class VlabAdditionalUiChecks
{
    static VlabApp app;static Action<string> capture;static Action<string,bool> report;
    static readonly List<string> lines=new List<string>();static double began;static long sequence=20000;
    static readonly Dictionary<string,string> pendulumNames=new Dictionary<string,string>{{"stand","Laboratory stand"},{"clamp","Suspension clamp"},{"pendulum","Pendulum bob"},{"string","Suspension cord"},{"ruler","Length ruler"},{"timer","Interval timer"}};
    public static IEnumerator Run(VlabApp application,Action<string> screenshot,Action<string,bool> check)
    {
        app=application;capture=screenshot;report=check;lines.Clear();began=Time.realtimeSinceStartupAsDouble;Write("IN PROGRESS");
        // Flatten our helper iterators so any failure produces a receipt before reaching the root check.
        var stack=new Stack<IEnumerator>();stack.Push(Scenario());
        while(stack.Count>0)
        {
            object yielded=null;bool more=false;Exception failure=null;
            try{more=stack.Peek().MoveNext();if(more)yielded=stack.Peek().Current;}catch(Exception error){failure=error;}
            if(failure!=null)
            {
                lines.Add(failure.ToString());Write("FAILED");report("Additional shared UI checks completed",false);
                throw new InvalidOperationException("Additional VLAB UI checks failed; see Evidence/Runtime/additional-ui.txt",failure);
            }
            if(!more){(stack.Pop() as IDisposable)?.Dispose();continue;}
            if(yielded is IEnumerator nested&&!(yielded is CustomYieldInstruction)){stack.Push(nested);continue;}
            yield return yielded;
        }
        Write("PASS");
    }
    static IEnumerator Scenario()
    {
        Verify("All integrated lesson launch controls are released",LessonSpec.All.Keys.All(VlabApp.IsLessonAvailable));
        if(app.Profile.locale!="en")
        {yield return Press("Cài đặt");yield return Press("Ngôn ngữ: Tiếng Việt");yield return Press("Apply");}
        Verify("Additional UI checks use the actual committed English locale",app.Profile.locale=="en"&&app.Route=="home");
        Verify("Guidance metadata covers all 32 native preparation sequences",LessonGuide.ValidateMetadata().Length==0);
        foreach(string id in new[]{"P","C","B","M"}.SelectMany(prefix=>Enumerable.Range(1,8).Select(n=>prefix+n.ToString("00"))))
        {
            yield return Launch(id);yield return Assemble();CheckActivityTitles(id);
            if(id=="P01")
            {yield return PrepareUntil(4);Verify("P01 stock apparatus reaches Ready through UI",app.Session.Phase==ExperimentPhase.Ready);yield return CaptureVr("13-P01-stock-VR-ready");}
            else if(id=="C01")
            {
                yield return PrepareUntil(2);yield return GuideControl("Read initial meniscus");yield return PrepareUntil(4);
                Verify("C01 manual initial meniscus unlocks remaining preparation",app.Session.Phase==ExperimentPhase.Ready);
                yield return CaptureVr("14-C01-guided-initial-meniscus");
            }
            else if(id=="C03")yield return CopperPowerNavigation();
            else if(id=="C05")
            {
                yield return PrepareUntil(2);yield return GuideControl("Add water to mark");yield return GuideControl("Stopper and invert to mix");yield return PrepareUntil(3);
                Verify("C05 guide exposes real mark and mixing controls before preparation advances",app.Session.Phase==ExperimentPhase.Ready);
            }
            else if(id=="C06")
            {
                yield return PrepareUntil(1);
                foreach(string label in new[]{"Immerse in pH 4 buffer","Rinse probe","Immerse in pH 7 buffer","Rinse probe"})yield return GuideControl(label);
                yield return PrepareUntil(2);yield return GuideControl("Immerse in sample");yield return GuideControl("Dip fresh BTB paper");yield return PrepareUntil(3);
                Verify("C06 actual buffer/rinse/sample/BTB UI sequence reaches Ready",app.Session.Phase==ExperimentPhase.Ready);
            }
            else if(id=="C08")
            {
                yield return CalorimeterVariables();yield return PrepareUntil(1);
                yield return GuideControl("Measure acid temperature");yield return GuideControl("Measure base temperature");yield return PrepareUntil(3);
                Verify("C08 real thermometer readings unlock calorimeter preparation",app.Session.Phase==ExperimentPhase.Ready);
            }
            else if(id=="B08")yield return AtlasSelection();
            else if(id=="M01")
            {
                yield return PrepareUntil(3);yield return CaptureVr("18-M01-gears-VR-ready");yield return Press("Start trial");
                yield return Press("Station tools");yield return Press("Guidance");double time=app.Session.Elapsed;
                yield return new WaitForSecondsRealtime(.4f);Verify("Guidance freezes a running mechanism's shared clock",app.Session.Phase==ExperimentPhase.Paused&&Math.Abs(app.Session.Elapsed-time)<1e-8);
                yield return Press("Back");yield return new WaitForSecondsRealtime(.15f);Verify("Closing guidance restores the originally running attempt",app.Session.Phase==ExperimentPhase.Running&&app.Session.Elapsed>time);
            }
            lines.Add("LAUNCHED AND ASSEMBLED "+id);Write("IN PROGRESS");yield return LeaveToHome();
        }
        Verify("All 32 lesson launch and equipment-picker branches checked",lines.Count(line=>line.StartsWith("LAUNCHED AND ASSEMBLED ",StringComparison.Ordinal))==32);
        yield return Press("Settings");yield return Press("Language: English");yield return Press("Áp dụng");
        Verify("Shared UI checks restore committed Vietnamese Home",app.Profile.locale=="vi"&&app.Route=="home");
    }
    public static IEnumerator RenderingOnly(VlabApp application,Action<string> screenshot,Action<string,bool> check)
    {
        app=application;capture=screenshot;report=check;lines.Clear();yield return Launch("B04");yield return Assemble();
        string locale=app.Profile.locale,profile=JsonUtility.ToJson(app.Profile);
        try{app.Profile.locale="vi";RoomBranding.SetLanguage(SceneManager.GetActiveScene(),true);app.ShowActivity();yield return CaptureVr("80-B04-learner-installed");CaptureInstalledDetails("B04");}
        finally{app.Profile.locale=locale;RoomBranding.SetLanguage(SceneManager.GetActiveScene(),locale!="en");app.ShowActivity();}
        Verify("B04 rendering diagnostic preserves every learner field",JsonUtility.ToJson(app.Profile)==profile);
    }
    public static IEnumerator LayoutOnly(VlabApp application,Action<string> screenshot,Action<string,bool> check)
    {
        app=application;capture=screenshot;report=check;lines.Clear();
        if(app.Profile.locale!="en"){yield return Press("Cài đặt");yield return Press("Ngôn ngữ: Tiếng Việt");yield return Press("Apply");}
        var layoutFailures=new System.Collections.Generic.List<string>();Action<string,bool> collect=(name,passed)=>{if(!passed)layoutFailures.Add(name);};
        foreach(string prefix in new[]{"P","C","B","M"})
        {
            yield return Launch(prefix+"01");yield return ApparatusLayoutChecks.RunCurrentRoom(prefix,collect);
            Verify(prefix+" actual learner kit uses a real worktop",!app.Apparatus.UsesSyntheticStorageSurface);
            bool loose=app.Apparatus.ValidateLooseLayout(out string looseReason);collect(prefix+" actual loose learner kit: "+looseReason,loose);System.IO.File.WriteAllLines("Evidence/Runtime/layout-findings.txt",layoutFailures);yield return CaptureVr("75-"+prefix+"-dung-cu-tren-ban");
            yield return Assemble();bool supported=app.Apparatus.ValidateInstalledSurfaceLayout(out string why);collect(prefix+" installed learner kit: "+why,supported);yield return CaptureVr("76-"+prefix+"-bo-dung-cu-da-lap");
            yield return LeaveToHome();
        }
        foreach(string id in LessonSpec.All.Keys.OrderBy(value=>value,StringComparer.Ordinal))
        {
            yield return Launch(id);yield return Assemble();
            if(id=="P02")
            {
                yield return PrepareUntil(3);var guide=new LessonGuide(app.Session).Current(app.Apparatus,true);
                Verify("P02 ready guidance locates the actual floor mat",guide.key=="target"&&guide.action.Contains("sàn"));yield return Press("Show me where");
                Verify("P02 optional locator highlights the real mat without firing or recording",app.Session.Phase==ExperimentPhase.Ready&&app.Session.Recorded==null&&app.Apparatus.GetComponentsInChildren<GuideCue>().Any(cue=>cue.name=="Guide cue · target"&&cue.AssertPresentationOnly(app.Session,out _)));
                var mat=app.Apparatus.items.Single(item=>item.assetId=="target");ApparatusRig.TryVisibleBounds(mat,out var bounds);var delta=bounds.center-app.View.transform.position;
                Verify("P02 floor mat is reachable within phone look pitch",Vector3.Angle(delta,Vector3.ProjectOnPlane(delta,Vector3.up))<=70);
                CaptureSetupDetails(id);bool blocked=Physics.Raycast(app.View.transform.position,delta.normalized,out var hit,delta.magnitude,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
                Verify("P02 floor mat learner sight line from "+app.View.transform.position+" to "+bounds.center+" first hits "+(blocked?hit.collider.name+" at "+hit.point:"nothing"),!blocked||hit.collider.GetComponentInParent<EquipmentHandle>()==mat);
            }
            string locale=app.Profile.locale,profile=JsonUtility.ToJson(app.Profile);
            try
            {
                app.Profile.locale="vi";RoomBranding.SetLanguage(SceneManager.GetActiveScene(),true);app.ShowActivity();
                yield return CaptureVr("80-"+id+"-learner-installed");CaptureInstalledDetails(id);
                if(id=="M06"||id=="M08"||id=="P02"||id=="P04"||id=="P07"||id=="P08"){yield return CaptureVr("78-"+id+"-bo-tri-thuc-te");CaptureSetupDetails(id);}
            }
            finally{app.Profile.locale=locale;RoomBranding.SetLanguage(SceneManager.GetActiveScene(),locale!="en");app.ShowActivity();}
            Verify(id+" Vietnamese installed capture preserves every learner field",JsonUtility.ToJson(app.Profile)==profile);
            yield return new WaitForSecondsRealtime(.26f);
            if(new[]{"C02","C03","B06","B07"}.Contains(id))
            {
                if(id=="B06"||id=="B07"){yield return PrepareUntil(id=="B06"?1:2);ScientificFixtureChecks.RejectFilterCapacity(app.Apparatus,Verify);}
                yield return PrepareUntil(id=="B06"?2:3);ScientificFixtureChecks.RunPrepared(app.Apparatus,Verify);
                string preparedLocale=app.Profile.locale,preparedProfile=JsonUtility.ToJson(app.Profile);
                try
                {
                    app.Profile.locale="vi";RoomBranding.SetLanguage(SceneManager.GetActiveScene(),true);app.ShowActivity();
                    yield return CaptureVr("82-"+id+"-prepared-VI");CaptureSetupDetails(id);
                }
                finally{app.Profile.locale=preparedLocale;RoomBranding.SetLanguage(SceneManager.GetActiveScene(),preparedLocale!="en");app.ShowActivity();}
                Verify(id+" prepared capture preserves every learner field",JsonUtility.ToJson(app.Profile)==preparedProfile);
                yield return new WaitForSecondsRealtime(.26f);
            }
            yield return LeaveToHome();
        }
        Verify("All32 real-room apparatus layouts: "+string.Join("; ",layoutFailures),layoutFailures.Count==0);
        yield return Press("Settings");yield return Press("Language: English");yield return Press("Áp dụng");
    }
    // Actual learner-assembled kit after isolated numeric fixtures have been destroyed.
    // These two inspection views supplement, rather than replace, the unchanged learner view.
    static void CaptureInstalledDetails(string id)
    {
        var source=app.View;var canvas=app.GetComponentInChildren<Canvas>();bool visible=canvas.enabled;
        var poses=app.Apparatus.items.ToDictionary(item=>item,item=>(item.transform.position,item.transform.rotation));
        var host=new GameObject("Temporary all32 inspection camera",typeof(Camera),typeof(UnityEngine.Rendering.Universal.UniversalAdditionalCameraData));var camera=host.GetComponent<Camera>();camera.CopyFrom(source);camera.enabled=false;camera.stereoTargetEye=StereoTargetEyeMask.None;camera.nearClipPlane=.02f;camera.ResetWorldToCameraMatrix();camera.ResetProjectionMatrix();host.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderPostProcessing=false;
        try
        {
            canvas.enabled=false;
            var renders=app.Apparatus.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&!(id=="P02"&&r.GetComponentInParent<EquipmentHandle>()?.assetId=="target")).ToArray();
            Verify(id+" inspection contains one actual installed learner rig",Object.FindObjectsByType<ApparatusRig>(FindObjectsSortMode.None).Length==1&&renders.Length>0);
            var box=renders[0].bounds;foreach(var r in renders.Skip(1))box.Encapsulate(r.bounds);
            camera.fieldOfView=80;float distance=Mathf.Max(.45f,box.size.magnitude*1.15f);int side=0;
            foreach(var direction in new[]{new Vector3(-.3f,.35f,-1),new Vector3(1,.5f,-.3f)})
            {
                var eye=box.center+direction.normalized*distance;
                if(app.Apparatus.TryReceivingFloor(out var floor)){eye.x=Mathf.Clamp(eye.x,floor.min.x+.06f,floor.max.x-.06f);eye.z=Mathf.Clamp(eye.z,floor.min.z+.06f,floor.max.z-.06f);}
                eye.y=Mathf.Min(2.35f,eye.y);camera.transform.position=eye;camera.transform.LookAt(box.center);
                LabPanoramaCapture.CaptureReview(camera,"Evidence/Runtime/81-"+id+"-inspection-"+(++side)+".png");
            }
        }
        finally{Object.DestroyImmediate(host);canvas.enabled=visible;}
        Verify(id+" all32 inspection leaves apparatus poses unchanged",poses.All(pair=>pair.Key.transform.position==pair.Value.Item1&&pair.Key.transform.rotation==pair.Value.Item2));
    }
    // Supplemental evidence views only; restore the learner camera and never move apparatus.
    static void CaptureSetupDetails(string id)
    {
        var source=app.View;var canvas=app.GetComponentInChildren<Canvas>();var position=source.transform.position;var rotation=source.transform.rotation;bool visible=canvas.enabled;
        var host=new GameObject("Temporary untracked setup evidence",typeof(Camera),typeof(UnityEngine.Rendering.Universal.UniversalAdditionalCameraData));var camera=host.GetComponent<Camera>();camera.CopyFrom(source);camera.enabled=false;camera.stereoTargetEye=StereoTargetEyeMask.None;camera.nearClipPlane=.02f;camera.ResetWorldToCameraMatrix();camera.ResetProjectionMatrix();host.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderPostProcessing=false;
        var poses=app.Apparatus.items.ToDictionary(item=>item,item=>(item.transform.position,item.transform.rotation));
        try
        {
            canvas.enabled=false;
            if(id=="P02")
            {
                app.Apparatus.TryReceivingFloor(out var floor);var launcher=app.Apparatus.items.Single(item=>item.assetId=="launcher").GetComponentInChildren<VlabLauncher>();ApparatusRig.TryVisibleBounds(app.Apparatus.items.Single(item=>item.assetId=="target"),out var mat);
                var centre=(launcher.MuzzleRay.origin+mat.center)*.5f;camera.fieldOfView=80;camera.transform.position=new Vector3(centre.x,2.65f,centre.z);camera.transform.rotation=Quaternion.LookRotation(centre-camera.transform.position,Vector3.forward);
                Verify("P02 evidence frame includes muzzle and actual landing mat",new[]{launcher.MuzzleRay.origin,mat.center}.All(point=>{var v=camera.WorldToViewportPoint(point);return v.z>0&&v.x>.02f&&v.x<.98f&&v.y>.02f&&v.y<.98f;}));LabPanoramaCapture.CaptureReview(camera,"Evidence/Runtime/79-P02-vung-do.png");
            }
            else if(id=="P04"||id=="P07"||id=="P08")
            {
                camera.fieldOfView=50;
                foreach(string key in new[]{"gateA","gateB"})
                {
                    var gate=app.Apparatus.items.Single(item=>item.assetId==key);ApparatusRig.TryVisibleBounds(gate,out var box);
                    camera.transform.position=box.center+new Vector3(-.24f,.25f,-.32f);camera.transform.LookAt(box.center);
                    LabPanoramaCapture.CaptureReview(camera,"Evidence/Runtime/79-"+id+"-seat-"+key+".png");
                }
            }
            else if(new[]{"C02","C03","B06","B07"}.Contains(id))
            {
                var keys=id=="C02"?new[]{"znCup","cuCup","zn","cu","bridge"}:id=="C03"?new[]{"bath","cathode","anode"}:new[]{"funnel","filter","receiver"};
                var renders=app.Apparatus.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&(r.GetComponentInParent<ScientificFixtureSupport>()||keys.Contains(r.GetComponentInParent<EquipmentHandle>()?.assetId))).ToArray();
                var box=renders[0].bounds;foreach(var r in renders.Skip(1))box.Encapsulate(r.bounds);
                camera.fieldOfView=50;float distance=Mathf.Max(.3f,box.size.magnitude*1.6f);int side=0;
                foreach(var direction in new[]{new Vector3(-1,.55f,-1),new Vector3(1,.7f,.6f)}){camera.transform.position=box.center+direction.normalized*distance;camera.transform.LookAt(box.center);LabPanoramaCapture.CaptureReview(camera,"Evidence/Runtime/82-"+id+"-prepared-detail-"+(++side)+".png");}
            }
            else
            {
                var keys=id=="M08"?new[]{"supports","beam","gauge","load"}:new[]{"motor","cam","guide","follower","gauge"};
                var renders=app.Apparatus.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&(r.name.StartsWith("Derived "+id)||keys.Contains(r.GetComponentInParent<EquipmentHandle>()?.assetId))).ToArray();var box=renders[0].bounds;foreach(var r in renders.Skip(1))box.Encapsulate(r.bounds);
                camera.fieldOfView=50;float distance=Mathf.Max(.45f,box.size.magnitude*1.8f);int side=0;foreach(var direction in new[]{new Vector3(-1,.7f,-1),new Vector3(1,.7f,1)}){camera.transform.position=box.center+direction.normalized*distance;camera.transform.LookAt(box.center);LabPanoramaCapture.CaptureReview(camera,"Evidence/Runtime/79-"+id+"-chi-tiet-"+(++side)+".png");}
            }
        }
        finally{UnityEngine.Object.DestroyImmediate(host);canvas.enabled=visible;}
        Verify(id+" supplemental view never changes the learner camera",source.transform.position==position&&source.transform.rotation==rotation);
        Verify(id+" supplemental camera leaves source apparatus poses unchanged",poses.All(pair=>pair.Key.transform.position==pair.Value.Item1&&pair.Key.transform.rotation==pair.Value.Item2));
    }
    public static IEnumerator BrandingOnly(VlabApp application,Action<string> screenshot,Action<string,bool> check)
    {
        app=application;capture=screenshot;report=check;lines.Clear();
        if(app.Profile.locale!="en"){yield return Press("Cài đặt");yield return Press("Ngôn ngữ: Tiếng Việt");yield return Press("Apply");}
        foreach(string id in new[]{"P01","C01","B01","M01"})
        {
            yield return Launch(id);var session=app.Session;int records=app.Profile.attempts.Count;
            RoomBrandingChecks.Run(check,SceneManager.GetActiveScene(),false);yield return CaptureVr("71-"+id[0]+"-logo-trang-EN");
            yield return Press("‹ Back");yield return Press("Settings");yield return Press("Language: English");yield return Press("Áp dụng");yield return Press("Tiếp tục");
            var mark=SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<RoomBrandingSurface>()).Single(value=>value.IsMain);
            Verify(id+" actual Settings language change updates live room lettering",mark.Vietnamese&&mark.LaboratoryTitle.text==RoomBranding.LaboratoryName(SceneManager.GetActiveScene(),true)&&ReferenceEquals(session,app.Session)&&app.Profile.attempts.Count==records);
            yield return CaptureVr("72-"+id[0]+"-logo-trang-VI");
            yield return Press("‹ Quay lại");yield return Press("Cài đặt");yield return Press("Ngôn ngữ: Tiếng Việt");yield return Press("Apply");yield return Press("Resume");
            Verify(id+" live room lettering returns to English after Apply",!mark.Vietnamese&&mark.LaboratoryTitle.text==RoomBranding.LaboratoryName(SceneManager.GetActiveScene(),false));
            yield return LeaveToHome();
        }
        yield return Press("Settings");yield return Press("Language: English");yield return Press("Áp dụng");
    }
    public static IEnumerator GuidanceOnly(VlabApp application,Action<string> screenshot,Action<string,bool> check)
    {
        app=application;capture=screenshot;report=check;lines.Clear();
        if(app.Profile.locale!="en"){yield return Press("Cài đặt");yield return Press("Ngôn ngữ: Tiếng Việt");yield return Press("Apply");}
        yield return Launch("P01");Verify("P01 optional installation guidance localizes stock equipment instead of raw asset IDs",new LessonGuide(app.Session).Current(app.Apparatus,true).action.StartsWith("Giá đỡ thí nghiệm:")&&new LessonGuide(app.Session).Current(app.Apparatus,false).action.StartsWith("Laboratory stand:"));bool originalMotion=app.Profile.settings.reducedMotion;app.Profile.settings.reducedMotion=true;yield return Press("Show me where");
        var reduced=app.Apparatus.GetComponentsInChildren<GuideCue>();Verify("Reduced-motion installation cue is present",reduced.Length>0);
        foreach(var cue in reduced)Verify("Reduced-motion installation cue stays static and preserves apparatus",cue.AssertPresentationOnly(app.Session,out _));
        yield return Press("Done");app.Profile.settings.reducedMotion=originalMotion;
        yield return Assemble();yield return PrepareUntil(4);
        yield return Press("Station tools");yield return Press("Guidance");CheckGuidanceReadability();yield return Press("Back");yield return Press("Show me where");
        var cues=app.GetComponentsInChildren<GuideCue>().Concat(app.Apparatus.GetComponentsInChildren<GuideCue>()).ToArray();Verify("P01 instruction exposes presentation cues without starting the pendulum",cues.Length>=2&&app.Session.Phase==ExperimentPhase.Ready);
        foreach(var cue in cues)Verify("Guide cue preserves actual apparatus and lesson: "+cue.name,cue.AssertPresentationOnly(app.Session,out _));
        yield return CaptureVr("26-P01-animated-instruction");yield return LeaveToHome();
        yield return Launch("C01");yield return Assemble();yield return PrepareUntil(2);yield return Press("Show me where");
        cues=app.GetComponentsInChildren<GuideCue>();Verify("C01 guide animates the actual meniscus control",cues.Any(cue=>cue.GetComponentInParent<Button>()?.GetComponentInChildren<TextMeshProUGUI>()?.text=="Read initial meniscus"));
        foreach(var cue in cues)Verify("Meniscus cue does not execute the measurement",cue.AssertPresentationOnly(app.Session,out _));
        yield return CaptureVr("27-C01-animated-meniscus-guide");yield return CaptureVietnameseGuide();yield return Press("Read initial meniscus");yield return Press("Return to station");
        Verify("Closing the C01 guide restores one readable station panel",app.GetComponentsInChildren<RectTransform>().Count(value=>value.name=="Current laboratory action")==1&&!app.GetComponentsInChildren<RectTransform>().Any(value=>value.name=="C01 guided sidepanel"));
        Debug.Log("C01 restored phase="+app.Session.Phase+" operation="+app.Session.Operation+" visible buttons="+string.Join(" | ",Buttons().Select(Label)));
        yield return CaptureVr("65-C01-station-after-guide");
        yield return PrepareUntil(4);yield return LeaveToHome();
        yield return Press("Settings");yield return Press("Language: English");yield return Press("Áp dụng");
    }
    static IEnumerator CaptureVietnameseGuide()
    {
        string locale=app.Profile.locale,profile=JsonUtility.ToJson(app.Profile);
        const System.Reflection.BindingFlags flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        var current=typeof(VlabApp).GetMethod("CurrentGuide",flags);var show=typeof(VlabApp).GetMethod("ShowGuideTarget",flags);
        try
        {
            app.Profile.locale="vi";show.Invoke(app,new[]{current.Invoke(app,null)});yield return new WaitForSecondsRealtime(.3f);
            Verify("Vietnamese guide has no underlying station text or buttons",!app.GetComponentsInChildren<RectTransform>().Any(value=>value.name=="Current laboratory action")&&Find("Đọc mặt khum đầu"));
            foreach(var text in app.GetComponentsInChildren<TextMeshProUGUI>()){text.ForceMeshUpdate(true,true);Verify("Vietnamese guide text fits: "+text.text,!text.isTextOverflowing);}
            yield return CaptureVr("64-C01-huong-dan-VI");
        }
        finally{app.Profile.locale=locale;show.Invoke(app,new[]{current.Invoke(app,null)});}
        Verify("Language-only guide capture preserves every learner field",JsonUtility.ToJson(app.Profile)==profile);
        yield return new WaitForSecondsRealtime(.26f); // Let the recreated modal finish its click debounce before the next learner action.
    }
    static void CheckGuidanceReadability()
    {
        var content=app.GetComponentsInChildren<RectTransform>().Single(rect=>rect.name=="Guidance content").GetComponent<VlabScrollContent>();
        var body=content.Body;string saved=body.text;float size=body.fontSize;
        try{
        foreach(bool large in new[]{false,true})foreach(bool vi in new[]{false,true})foreach(string id in LessonSpec.All.Keys)
        {
            var session=new ExperimentSession(id);var guide=new LessonGuide(session);
            for(int step=0;step<session.spec.operationsEn.Length;step++)
            {
                var task=guide.Preparation(app.Apparatus,vi,step);
                body.fontSize=26*(large?1.12f:1);body.text=task.action+"\n\n"+task.success+"\n\n"+guide.Next(app.Apparatus,task,vi,step)+"\n\n"+guide.Explanation(task,vi);content.Refresh();
                Verify(id+" guidance remains readable through native scrolling "+step+"/"+vi+"/"+large,content.Scroll.viewport.rect.height>0&&body.GetPreferredValues(body.text,body.rectTransform.rect.width,Mathf.Infinity).y<=body.rectTransform.rect.height+.75f);
            }
        }}finally{body.text=saved;body.fontSize=size;content.Refresh();}
    }
    static IEnumerator Launch(string id)
    {
        Verify("Home is the launch origin for "+id,app.Route=="home");
        yield return PressName("Subject "+id.Substring(0,1));yield return WaitRoute("browser");
        var visible=Buttons().Where(button=>button.name.StartsWith("Browser lesson ",StringComparison.Ordinal)).ToArray();
        Verify(id+" browser exposes all eight distinct subject lessons without pagination",visible.Length==8&&visible.Select(button=>button.name).Distinct().Count()==8&&visible.All(button=>button.name.StartsWith("Browser lesson "+id.Substring(0,1),StringComparison.Ordinal)));
        yield return PressName("Browser lesson "+id);
        int savedBeforeStudy=app.Profile.attempts.Count;
        yield return PressName("Browser study");yield return WaitRoute("notebook");
        Verify(id+" opens its own study content before a new measured attempt",Buttons().Any(button=>button.name=="Journal study chapter 0")&&app.GetComponentsInChildren<TextMeshProUGUI>().Any(text=>text.name=="Journal lesson title"&&text.text==LessonGuide.ShortTitle(id,false)));
        yield return Press("‹ Back");yield return WaitRoute("browser");
        Verify(id+" returns from studying to the same lesson without fabricating measurements",app.Profile.attempts.Count==savedBeforeStudy&&app.GetComponentsInChildren<TextMeshProUGUI>().Any(text=>text.name=="Browser selected title"&&text.text==LessonGuide.ShortTitle(id,false)));
        foreach(var text in Buttons().Where(button=>button.name.StartsWith("Browser lesson ",StringComparison.Ordinal)).Select(button=>button.GetComponentInChildren<TextMeshProUGUI>())){text.ForceMeshUpdate(true,true);Verify(id+" all visible lesson titles and counts fit",!text.isTextOverflowing);}
        foreach(string name in new[]{"Browser selected title","Browser selected goal"}){var text=app.GetComponentsInChildren<TextMeshProUGUI>().Single(value=>value.isActiveAndEnabled&&value.name==name);text.ForceMeshUpdate(true,true);Verify(id+" selected browser detail fits: "+name,!text.isTextOverflowing);}
        yield return Press("Start experiment");yield return WaitRoute("readiness");
        string room=id[0]=='P'?"Physics":id[0]=='C'?"Chemistry":id[0]=='B'?"Biology":"Engineering";
        Verify(id+" browser/readiness background follows the selected lesson",app.Preview.CurrentRoom==room&&app.Preview.CurrentTexture&&app.Preview.CurrentTexture.name=="Panorama_"+room);
        if(id.EndsWith("01",StringComparison.Ordinal))capture("48-lesson-background-"+id);
        Verify(id+" readiness has explicit Back and Help",Buttons().Any(button=>button.name=="Readiness back"&&button.IsInteractable())&&Buttons().Any(button=>button.name=="Readiness help"&&button.IsInteractable()));
        if(id=="C01")
        {
            yield return PressName("Readiness help");Verify("Readiness Help keeps the selected Chemistry room background instead of a stale Physics session",app.Route=="help"&&app.Preview.CurrentRoom=="Chemistry");
            yield return Press("‹ Back");yield return WaitRoute("readiness");
        }
        yield return Press("Enter station");yield return WaitRoute("activity");yield return new WaitForSecondsRealtime(.12f);
        Verify(id+" correct native session and subject room",app.Session?.spec.id==id&&SceneManager.GetActiveScene().name==(id[0]=='P'?"Physics":id[0]=='C'?"Chemistry":id[0]=='B'?"Biology":"Engineering"));
        Verify(id+" active apparatus remains visible without a wallpaper overlay",app.Preview.CurrentRoom==null&&!app.Preview.CurrentTexture&&app.Preview.Alpha<.001f);
        Verify(id+" delivered kit is instantiated",app.Apparatus.items.Count==(id=="P01"?6:app.Apparatus.Driver.Kit.Length)&&app.Apparatus.items.All(item=>item.GetComponentsInChildren<Renderer>(true).Length>0||item.GetComponent<VlabChips>() is VlabChips chips&&chips.Count>0&&chips.SharedMesh));
    }
    static IEnumerator Assemble()
    {
        string id=app.Session.spec.id;yield return Press("Open equipment");yield return EquipmentFirstPage();int page=0;
        for(int i=0;i<app.Apparatus.items.Count;i++)
        {
            while(page<i/6){yield return Press("Next");page++;}
            var item=app.Apparatus.items[i];string name=Name(item);
            yield return PressName("Equipment item "+item.assetId);Verify(id+" UI selects "+item.assetId,app.Apparatus.Selected==item);
            yield return Press("Place at installation anchor");Verify(id+" UI installs "+item.assetId,item.installed);
        }
        yield return Press("Done");Verify(id+" equipment gate satisfied through picker",app.Apparatus.Ready);
    }
    static IEnumerator EquipmentFirstPage()
    {
        // Page state intentionally survives reopening; reach the first page through visible controls.
        int pages=Math.Max(1,(app.Apparatus.items.Count+5)/6);
        for(int i=1;i<pages;i++)yield return Press("Previous");
    }
    static IEnumerator PrepareUntil(int operation)
    {
        int guard=0;
        while(app.Session.Operation<operation&&guard++<12)
        {int before=app.Session.Operation;yield return Press("Perform current action");Verify(app.Session.spec.id+" preparation step "+before+" advances through UI",app.Session.Operation==before+1);}
        Verify("Requested preparation boundary reached",app.Session.Operation==operation);
    }
    static IEnumerator GuideControl(string label)
    {
        int before=app.Session.Operation;yield return Press("Show me where");
        Verify(app.Session.spec.id+" guide reveals enabled "+label,Find(label)&&Find(label).interactable);
        if(app.Session.spec.id=="C01")
        {
            var panel=app.GetComponentsInChildren<RectTransform>().Single(value=>value.name=="C01 guided sidepanel");
            Verify("C01 guidance leaves the centre and right apparatus visible",panel.rect.width<=520&&panel.anchoredPosition.x+panel.rect.width<800&&panel.parent.GetComponent<UnityEngine.UI.Image>().color.a==0);
            yield return CaptureVr("64-C01-guidage-dung-cu-khong-bi-che");
        }
        Verify("Show me where does not execute preparation",app.Session.Operation==before);
        yield return Press(label);yield return Press("Return to station");
    }
    static void CheckActivityTitles(string id)
    {
        Canvas.ForceUpdateCanvases();var title=app.GetComponentsInChildren<TextMeshProUGUI>().FirstOrDefault(text=>text.isActiveAndEnabled&&text.text==LessonGuide.ShortTitle(id,false));
        Verify(id+" actual English compact title visible",title&&!title.isTextOverflowing);
        foreach(bool vi in new[]{false,true})
        {
            Vector2 preferred=title.GetPreferredValues(LessonGuide.ShortTitle(id,vi),title.rectTransform.rect.width,Mathf.Infinity);
            Verify(id+" "+(vi?"VI":"EN")+" short title fits its actual TMP template",preferred.y<=title.rectTransform.rect.height+1);
        }
    }
    static IEnumerator CalorimeterVariables()
    {
        yield return Press("Station tools");yield return Press("Variables");
        int pages=(app.Session.spec.parameters.Length+3)/4;for(int i=1;i<pages;i++)yield return Press("Previous");
        for(int page=0;page<pages;page++)
        {
            var expected=app.Session.spec.parameters.Skip(page*4).Take(4).ToArray();
            Verify("C08 variable page "+(page+1)+" displays its actual parameter labels",expected.All(parameter=>ActiveTexts().Any(text=>text.text.StartsWith(parameter.en+"   ",StringComparison.Ordinal))));
            Verify("C08 variable page "+(page+1)+" has at most four parameter rows",app.Session.spec.parameters.Count(parameter=>ActiveTexts().Any(text=>text.text.StartsWith(parameter.en+"   ",StringComparison.Ordinal)))<=4);
            if(page+1<pages)yield return Press("Next");
        }
        yield return CaptureVr("15-C08-paged-variables-VR");yield return Press("Back");
    }
    static IEnumerator CopperPowerNavigation()
    {
        yield return PrepareUntil(3);yield return Press("Start trial");yield return new WaitForSecondsRealtime(.2f);
        yield return OpenInstalled("supply");yield return Press("Switch off");
        var supply=app.Apparatus.items.First(item=>item.assetId=="supply").GetComponentInChildren<VlabPowerSupply>();Verify("C03 real power switch is off",!supply.On);
        yield return Press("Other equipment");yield return SelectInstalled("balance");yield return Press("Return to station");
        Verify("C03 manual Off survives live equipment/instrument navigation",!supply.On&&app.Session.Phase==ExperimentPhase.Running);
    }
    static IEnumerator OpenInstalled(string key)
    {
        yield return Press("Station tools");yield return Press("Equipment");yield return SelectInstalled(key);
    }
    static IEnumerator SelectInstalled(string key)
    {
        yield return EquipmentFirstPage();var item=app.Apparatus.items.First(value=>value.assetId==key);int index=app.Apparatus.items.IndexOf(item);
        for(int page=0;page<index/6;page++)yield return Press("Next");
        yield return Press("Fitted · "+Name(item));
    }
    static IEnumerator AtlasSelection()
    {
        yield return PrepareUntil(3);yield return Press("Start trial");app.Input.SetSimulator(true);yield return new WaitForSecondsRealtime(.4f);
        app.Input.UseMockController=true;Submit(Quaternion.identity,false);app.Input.Controller.Calibrate(Quaternion.identity);BlankPointer();
        yield return OpenInstalled("microscope");yield return Press("Next");yield return Press("Left half");
        var inspection=Object.FindObjectsByType<InstrumentInspection>(FindObjectsSortMode.None).Single(value=>value.isActiveAndEnabled);
        var image=inspection.GetComponent<RawImage>();Verify("B08 actual left crop is displayed",Mathf.Abs(image.uvRect.x)<1e-6&&Mathf.Abs(image.uvRect.width-.5f)<1e-6);
        var atlas=app.Apparatus.items.First(item=>item.assetId=="specimen").GetComponentInChildren<VlabMitosisAtlas>();
        Vector2 sourceUv=Vector2.zero;int expectedId=0;string expectedStage=null;
        for(int y=5;y<95&&expectedId==0;y++)for(int x=3;x<48;x++)
        {var uv=new Vector2(x/100f,y/100f);if(atlas.TryResolve(uv,out int id,out string stage,out bool scorable)&&scorable){sourceUv=uv;expectedId=id;expectedStage=stage;break;}}
        Verify("B08 source metadata supplies a scorable fixture inside left crop",expectedId>0);
        Rect rect=image.rectTransform.rect;var local=new Vector3(rect.xMin+(sourceUv.x-image.uvRect.x)/image.uvRect.width*rect.width,rect.yMin+(sourceUv.y-image.uvRect.y)/image.uvRect.height*rect.height,0);
        yield return RayClick(image.rectTransform.TransformPoint(local));
        var state=JsonUtility.FromJson<BiologyLessonState>(app.Apparatus.Driver.SaveState());
        Verify("B08 actual product ray maps crop UV to the source global ID",state.selectedCell==expectedId);
        Verify("Selecting image does not automatically classify a cell",state.cells.Count==0);
        Verify("B08 actual selected-cell marker is visible",Object.FindObjectsByType<InstrumentInspection>(FindObjectsSortMode.None).Single(value=>value.isActiveAndEnabled).GetComponentsInChildren<RectTransform>().Any(value=>value.name=="Selected image location"&&value.gameObject.activeInHierarchy));
        capture("16-B08-cropped-atlas-selected-cell");
        if(!Find(expectedStage))yield return Press("Next");yield return Press(expectedStage);
        state=JsonUtility.FromJson<BiologyLessonState>(app.Apparatus.Driver.SaveState());
        Verify("B08 explicit source-fixture stage button retains one distinct global ID",state.cells.Count==1&&state.cells[0].id==expectedId&&state.cells[0].answer==expectedStage);
        for(int page=0;!Find("Whole atlas")&&page<4;page++)yield return Press("Previous");yield return Press("Whole atlas");
        Verify("B08 selection marker survives changing crop",Object.FindObjectsByType<InstrumentInspection>(FindObjectsSortMode.None).Single(value=>value.isActiveAndEnabled).GetComponentsInChildren<RectTransform>().Any(value=>value.name=="Selected image location"&&value.gameObject.activeInHierarchy));
        capture("17-B08-global-ID-and-manual-classification");app.Input.UseMockController=false;app.Input.SetSimulator(false);yield return new WaitForSecondsRealtime(.3f);yield return Press("Return to station");
    }
    static IEnumerator RayClick(Vector3 world)
    {
        yield return new WaitForSecondsRealtime(.24f);Quaternion orientation=Quaternion.LookRotation((world-app.View.transform.position).normalized);
        Submit(orientation,false);yield return null;yield return null;Submit(orientation,true);yield return null;yield return null;Submit(orientation,false);yield return null;yield return null;BlankPointer();yield return new WaitForSecondsRealtime(.24f);
    }
    static void Submit(Quaternion orientation,bool pressed)=>app.Input.Controller.Submit(new ControllerSignal{sequence=sequence++,receivedAt=Time.realtimeSinceStartupAsDouble,orientation=orientation,select=pressed});
    static void BlankPointer()=>Submit(Quaternion.LookRotation(Vector3.up,Vector3.forward),false);
    static IEnumerator CaptureVr(string name)
    {
        bool already=app.Input.SimulatorMode;if(!already)app.Input.SetSimulator(true);yield return new WaitForSecondsRealtime(.35f);
        string materialState=app.Route=="activity"&&app.Session.spec.id=="B04"?RoomMaterialState():null;
        if(app.Route=="activity"&&!ModalScope()&&(app.Session.spec.id=="P02"||app.Session.spec.id=="P08"))
        {
            string id=app.Session.spec.id;var panels=Buttons().Select(button=>button.GetComponent<RectTransform>()).Concat(app.GetComponentsInChildren<RectTransform>().Where(rect=>rect.name=="Current laboratory action")).ToArray();
            var renderers=app.Apparatus.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&(id=="P02"?r.GetComponentInParent<EquipmentHandle>()?.assetId=="launcher":r.name.EndsWith("_AngleScale_VIS",StringComparison.Ordinal)||r.name.EndsWith("_AnglePointer_VIS",StringComparison.Ordinal))).ToArray();
            Verify(id+" original critical instrument faces exist in the VR view",renderers.Length>0);
            foreach(var renderer in renderers)
            {
                var b=renderer.bounds;var points=from x in new[]{b.min.x,b.max.x} from y in new[]{b.min.y,b.max.y} from z in new[]{b.min.z,b.max.z} select new Vector3(x,y,z);
                Verify(id+" critical face is visible outside activity UI in actual VR: "+renderer.name,points.All(point=>{var v=app.View.WorldToViewportPoint(point);var screen=app.View.WorldToScreenPoint(point);return v.z>0&&v.x>0&&v.x<1&&v.y>0&&v.y<1&&!panels.Any(panel=>RectTransformUtility.RectangleContainsScreenPoint(panel,screen,app.View));}));
            }
        }
        if(app.Route=="activity"&&!ModalScope())foreach(var button in Buttons())
        {
            var rect=button.GetComponent<RectTransform>();var corners=new Vector3[4];rect.GetWorldCorners(corners);Vector3 centre=rect.TransformPoint(rect.rect.center);
            foreach(var corner in corners)
            {
                var point=Vector3.Lerp(corner,centre,.1f);var delta=point-app.View.transform.position;
                Verify("Activity control is not hidden behind apparatus/cabinets: "+button.name,!Physics.Raycast(new Ray(app.View.transform.position,delta.normalized),delta.magnitude-.015f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore));
            }
        }
        if(name.StartsWith("80-",StringComparison.Ordinal))yield return CaptureAutomaticView(name);else capture(name);
        if(materialState!=null&&name.StartsWith("80-B04",StringComparison.Ordinal))
        {
            yield return CaptureAutomaticView("83-B04-automatic-user-view");Verify("B04 normal and requested captures preserve original room material/texture references and colors",RoomMaterialState()==materialState);
        }
        if(!already)app.Input.SetSimulator(false);yield return new WaitForSecondsRealtime(.26f);
    }
    static string RoomMaterialState()=>string.Join("\n",SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<Renderer>(true)).Where(r=>!r.GetComponentInParent<EquipmentHandle>()).SelectMany(r=>r.sharedMaterials.Select(m=>r.name+"|"+(m?m.GetEntityId().ToString()+"|"+m.shader.name+"|"+(m.HasProperty("_BaseColor")?m.GetColor("_BaseColor").ToString():m.HasProperty("_Color")?m.GetColor("_Color").ToString():"")+"|"+(m.HasProperty("_BaseMap")&&m.GetTexture("_BaseMap")?m.GetTexture("_BaseMap").GetEntityId().ToString():"none"):"null"))).OrderBy(value=>value,StringComparer.Ordinal));
    static IEnumerator CaptureAutomaticView(string name)
    {
        var camera=app.View;var oldTarget=camera.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(1600,1000,24);Texture2D texture=null;
        string state="Automatic enabled learner camera; no Camera.Render/SubmitRenderRequest call. stereoEnabled="+camera.stereoEnabled+"; targetEye="+camera.stereoTargetEye+"; postProcessing="+camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderPostProcessing;
        try
        {
            Verify(app.Session.spec.id+" diagnostic uses the actual enabled learner camera",camera.enabled);rt.Create();camera.targetTexture=rt;
            yield return null;yield return null;yield return null;
            RenderTexture.active=rt;texture=new Texture2D(1600,1000,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1600,1000),0,0);texture.Apply();
            var pixels=texture.GetPixels32();Verify(app.Session.spec.id+" automatic frame contains rendered scene detail",pixels.Any(pixel=>pixel.r>40)&&pixels.Any(pixel=>pixel.r<20));
            if(name!=null)File.WriteAllBytes("Evidence/Runtime/"+name+".png",texture.EncodeToPNG());if(name=="83-B04-automatic-user-view")File.WriteAllText("Evidence/Runtime/83-B04-render-state.txt",state+"\n"+RoomMaterialState());
        }
        finally{camera.targetTexture=oldTarget;RenderTexture.active=active;if(texture)Object.DestroyImmediate(texture);rt.Release();Object.DestroyImmediate(rt);}
    }
    static IEnumerator LeaveToHome()
    {
        for(int i=0;i<4&&ModalScope();i++)
        {
            if(Find("Return to station"))yield return Press("Return to station");else if(Find("Done"))yield return Press("Done");else if(Find("Back"))yield return Press("Back");else throw new InvalidOperationException("Unknown modal while leaving "+app.Session.spec.id);
        }
        yield return Press("‹ Back");yield return WaitRoute("pause");yield return Press("Leave laboratory");yield return Press("Confirm");yield return WaitRoute("browser");yield return Press("‹ Home");yield return WaitRoute("home");
    }
    static IEnumerator WaitRoute(string route)
    {double until=Time.realtimeSinceStartupAsDouble+30;while(app.Route!=route&&Time.realtimeSinceStartupAsDouble<until)yield return null;Verify("Route reached "+route,app.Route==route);yield return new WaitForSecondsRealtime(.26f);}
    static IEnumerator Press(string label)=>PressButton(()=>Find(label),label);
    static IEnumerator PressName(string name)=>PressButton(()=>Buttons().FirstOrDefault(button=>button.name==name),name);
    static IEnumerator PressButton(Func<Button> lookup,string description)
    {
        yield return null;var button=lookup();
        Verify("UI target available: "+description,button&&button.interactable);button.onClick.Invoke();yield return new WaitForSecondsRealtime(.24f);
    }
    static Transform ModalScope()=>app.GetComponentsInChildren<Transform>().LastOrDefault(value=>value.name=="Modal input blocker"&&value.gameObject.activeInHierarchy);
    static IEnumerable<Button> Buttons(){var scope=ModalScope();return app.GetComponentsInChildren<Button>().Where(button=>button.isActiveAndEnabled&&(!scope||button.transform.IsChildOf(scope)));}
    static IEnumerable<TextMeshProUGUI> ActiveTexts(){var scope=ModalScope();return app.GetComponentsInChildren<TextMeshProUGUI>().Where(text=>text.isActiveAndEnabled&&(!scope||text.transform.IsChildOf(scope)));}
    static string Label(Button button)=>button.GetComponentInChildren<TextMeshProUGUI>()?.text??"";
    static Button Find(string label)=>Buttons().LastOrDefault(button=>Label(button)==label);
    static string Name(EquipmentHandle item)=>app.Apparatus.EquipmentLabel(item,false)??pendulumNames[item.assetId];
    static void Verify(string name,bool okay){lines.Add((okay?"PASS ":"FAIL ")+name);report(name,okay);if(!okay)throw new InvalidOperationException(name);}
    static void Write(string status)
    {Directory.CreateDirectory("Evidence/Runtime");File.WriteAllLines("Evidence/Runtime/additional-ui.txt",new[]{status,"Actual UI callbacks, product ray/image handlers and Editor world-space renders; no physical phone/headset proof.","This covers launch, assembly and selected shared UI branches. Full scientific recordings are covered by the independent SDK journeys.","Elapsed seconds: "+(Time.realtimeSinceStartupAsDouble-began).ToString("0.0",System.Globalization.CultureInfo.InvariantCulture)}.Concat(lines));}
}
#endif
