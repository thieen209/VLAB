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

public sealed class Qa016CoreProbe:MonoBehaviour
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    static readonly string Folder=Path.GetFullPath("../work/qa015-vr-lighting/resume/core-checks-r31");
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
        Check(label+" glyphs inside hit target "+button.name,fits&&!text.isTextOverflowing&&text.textInfo.characterInfo.Take(text.textInfo.characterCount).Any(c=>c.isVisible));
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
    RaycastHit ModelHit(EquipmentHandle item,string part="")
    {
        var colliders=item.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&(part==""||c.name.Contains(part))).ToArray();
        foreach(var collider in colliders)foreach(var axis in new[]{Vector3.up,Vector3.forward,Vector3.right}){var ray=new Ray(collider.bounds.center+axis*.5f,-axis);if(collider.Raycast(ray,out var hit,1))return hit;}
        throw new Exception("No actual collider intersection: "+item.assetId+" "+part);
    }
    void WorldPress(string key,string part="")
    {var item=App.Apparatus.items.Single(x=>x.assetId==key);var hit=ModelHit(item,part);Call("BeginFlagship",item,hit,new ActionFrame{pressed=true,pointer=new Ray(hit.point+hit.normal*.5f,-hit.normal)});}
    IEnumerator FlagshipComponents(string id)
    {
        Call("ShowHome");yield return new WaitForSecondsRealtime(.4f);yield return Press("Subject "+id[0]);yield return Press("Browser lesson "+id);yield return Press("Browser start");yield return Press("Readiness enter");float limit=Time.realtimeSinceStartup+45;while(App.Route!="activity"&&Time.realtimeSinceStartup<limit)yield return null;
        if(App.Session?.spec.id!=id)throw new Exception("Flagship fixture entry failed "+id);
        if(Environment.GetEnvironmentVariable("VLAB_LIGHTING_COMPARE")=="1")yield return LightingPair(id);
        for(int n=0;n<50&&!App.Apparatus.Ready;n++){var item=App.Apparatus.NextInstallableItem();App.Apparatus.Select(item);if(!App.Apparatus.InstallSelected())throw new Exception(id+" fixture installation failed");yield return null;}
        if(id=="C04")
        {
            var body=Field<Transform>("origin");rows.Add("C04 real phone eye="+body.position+" view="+App.View.transform.position);
            foreach(var direction in new[]{Vector2.right,Vector2.left,Vector2.up,Vector2.down})
            {var step=PhoneLocomotion.Direction(App.View.transform.rotation,direction)*.1f;var moved=PhoneLocomotion.Move(body,step,out var hit);rows.Add("C04 move "+direction+" delta="+(moved-body.position)+" blocker="+(hit?hit.name+" "+hit.bounds.ToString():"none"));}
            foreach(var ground in Physics.RaycastAll(body.position,Vector3.down,3,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))rows.Add("C04 ground "+ground.collider.name+" point="+ground.point+" normal="+ground.normal);
        }
        Check(id+" complete fixture apparatus",App.Apparatus.Ready);Call("ShowEquipment");yield return new WaitForSecondsRealtime(.3f);var quick=Field<RectTransform>("modal").GetComponentInChildren<PhoneDialogLayout>();Check(id+" optional compact inventory",quick&&quick.QuickEquipment&&quick.AssertPhoneLayout(out _));Capture(id+"-final-quick-tools");Call("CloseModal");var page=Field<RectTransform>("page");page.gameObject.SetActive(false);yield return null;
        if(id=="C04")
        {
            WorldPress("water");Check("C04 wrong direct cup action does not prepare or create result",App.Session.Operation==0&&App.Profile.attempts.All(a=>a.experimentId!="C04"));
            var testCup=App.Apparatus.items.Single(x=>x.assetId=="water");ApparatusRig.TryVisibleBounds(testCup,out var testBounds);var rimRay=new Ray(testBounds.center+Vector3.up*.6f,Vector3.down);Check("C04 open cup mouth is a drop target",Call("FindSampleCup",rimRay) as EquipmentHandle==testCup);var occluder=GameObject.CreatePrimitive(PrimitiveType.Cube);occluder.transform.position=rimRay.GetPoint(.2f);occluder.transform.localScale=Vector3.one*.1f;Physics.SyncTransforms();Check("C04 refuses drop through intervening solid",Call("FindSampleCup",rimRay)==null);Destroy(occluder);yield return null;

            foreach(string key in new[]{"water","nitrate","nacl"})
            {
                WorldPress("reagent");WorldPress("dropper");var cup=App.Apparatus.items.Single(x=>x.assetId==key);ApparatusRig.TryVisibleBounds(cup,out var b);var ray=new Ray(b.center+Vector3.up*.5f,Vector3.down);
                Call("UpdateFlagshipDrag",new ActionFrame{pointer=ray,held=true});Check("C04 actual dropper moves during learner drag "+key,Vector3.Distance(Field<EquipmentHandle>("flagshipDrag").transform.position,Field<Vector3>("flagshipRest"))>.01f);Call("UpdateFlagshipDrag",new ActionFrame{pointer=ray,released=true});
                Check("C04 dropper returns to supported rest "+key,!Field<EquipmentHandle>("flagshipDrag"));
            }
            var solid=App.Apparatus.items.Single(x=>x.assetId=="solidNaCl").GetComponent<VlabPrecipitate>();Check("C04 physical precipitate visible",solid.QualitativePresent&&solid.Sediment.enabled&&solid.Sediment.bounds.size.y>.002f);rows.Add("C04 sediment bounds="+solid.Sediment.bounds+" material="+solid.Sediment.sharedMaterial.name);
            Check("C04 actual three transfers start observation",App.Session.Phase==ExperimentPhase.Running);App.Session.Advance(4);Check("C04 own controlled observations can record",App.Session.Authority.CanRecord(out _));Call("RecordMeasurement");Check("C04 recorded actual precipitate and two controls",App.Profile.attempts.Last().readings.Single(r=>r.key=="NaClSolid").value==1&&App.Profile.attempts.Last().readings.Single(r=>r.key=="waterSolid").value==0);
        }
        else if(id=="B01")
        {
            WorldPress("coverslip");Check("B01 wrong preparation preserves operation",App.Session.Operation==0);
            WorldPress("onion");WorldPress("coverslip");WorldPress("slide");Check("B01 model actions prepare and mount slide",App.Session.Operation==3);
            var micro=App.Apparatus.Driver as BiologyDriver;float before=micro.MicroscopeFocusMm;WorldPress("microscope","Fine");var last=Field<Vector3>("flagshipLast");var ray=new Ray(App.View.transform.position,last+App.View.transform.up*.03f-App.View.transform.position);Call("UpdateFlagshipDrag",new ActionFrame{pointer=ray,held=true});Check("B01 direct fine knob changes actual focus by 0.05 mm",Math.Abs(micro.MicroscopeFocusMm-before-.05f)<.001f);Call("UpdateFlagshipDrag",new ActionFrame{pointer=ray,released=true});App.Apparatus.Driver.Operate("focusFine-",out _);
            var eye=Field<Transform>("origin").position;var rotation=App.View.transform.rotation;float lens=App.View.fieldOfView;WorldPress("microscope","Eyepiece");Check("B01 real eyepiece opens observation interface",Field<RectTransform>("modal"));Check("B01 refuses unclassified result",!App.Session.Authority.CanRecord(out _));Call("CloseModal");Check("B01 inspection returns complete working camera pose",Vector3.Distance(eye,Field<Transform>("origin").position)<.001f&&Quaternion.Angle(rotation,App.View.transform.rotation)<.01f&&Math.Abs(lens-App.View.fieldOfView)<.01f);
        }
        else
        {
            foreach(var mesh in App.Apparatus.items.First(x=>x.assetId=="gears").GetComponentsInChildren<MeshRenderer>().Where(r=>r.name.EndsWith("_VIS",StringComparison.Ordinal)&&r.name.Contains("Gear"))){var hit=mesh.GetComponent<SphereCollider>();if(hit)Check("M01 gear centre selectable "+mesh.name,hit.isTrigger&&hit.bounds.Contains(mesh.bounds.center));}
            WorldPress("gears","Gear36");Check("M01 actual gear selection configures 36-tooth output",App.Session.parameters["driven"]==36);WorldPress("motor");Check("M01 physical motor starts validated transmission",App.Session.Phase==ExperimentPhase.Running);App.Session.Advance(2);WorldPress("gears","Gear24");Check("M01 controlled gearing cannot change during trial",App.Session.parameters["driver"]==12);WorldPress("motor");Check("M01 motor touch pauses",App.Session.Phase==ExperimentPhase.Paused);WorldPress("motor");Check("M01 motor touch resumes",App.Session.Phase==ExperimentPhase.Running);App.Session.Advance(2);Call("RecordMeasurement");Check("M01 own SDK-angle result recorded",App.Profile.attempts.Last().experimentId=="M01");
        }
        Call("CloseModal");yield return null;
    }

    IEnumerator AlbumFrame(string name,string caption,bool clean)
    {
        string folder=Path.GetFullPath("../work/qa015-vr-lighting/resume/ALBUM-20261021-v2");Directory.CreateDirectory(folder);var ui=Field<VlabUi>("ui");bool visible=ui.canvas.enabled;ui.canvas.enabled=!clean;Call("ClearGuideCue");yield return null;yield return new WaitForEndOfFrame();
        if(clean){var rt=new RenderTexture(1920,1080,24);rt.Create();LabPanoramaCapture.RenderMono(App.View,rt);var active=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();File.WriteAllBytes(Path.Combine(folder,name+".png"),image.EncodeToPNG());Destroy(image);RenderTexture.active=active;rt.Release();Destroy(rt);}else ScreenCapture.CaptureScreenshot(Path.Combine(folder,name+".png"));
        yield return new WaitForEndOfFrame();yield return null;ui.canvas.enabled=visible;File.AppendAllText(Path.Combine(folder,"INDEX.tsv"),name+".png\t"+caption+"\tUnity Editor6000.6.4f1/source0.1.6-r3\tHigh\t"+(App.Session?.spec.id??"Home")+"/"+(App.Session?.Phase.ToString()??"menu")+"\t"+(clean?"1920x1080 scene render; UI hidden for photo":"1600x900 normal Game View UI")+"\n");
    }
    void AlbumFit(EquipmentHandle item,bool group)
    {Bounds b;if(group){object[] a={item,new Bounds()};typeof(VlabApp).GetMethod("TryFocusBounds",Private).Invoke(App,a);b=(Bounds)a[1];if(App.Session.spec.id=="P01")foreach(var part in App.Apparatus.items)if(ApparatusRig.TryVisibleBounds(part,out var linked))b.Encapsulate(linked);}else{ApparatusRig.TryVisibleBounds(item,out b);if(App.Session.spec.id=="P01"){b.Encapsulate(App.Apparatus.Pivot);b.Expand(.08f);}}Call("FitFocusBounds",b,new Rect(.08f,.10f,.84f,.80f));}
    IEnumerator PhotoAlbum()
    {
        App.Profile.settings.graphics=2;App.Profile.settings.focusAssist=false;Call("ApplySettings");yield return new WaitForSecondsRealtime(1);yield return AlbumFrame("01-menu","Menu bốn phòng, UI thật",false);int n=2;
        foreach(string id in new[]{"P01","C04","B01","M01"})
        {
            Call("ShowHome");yield return new WaitForSecondsRealtime(.3f);yield return Press("Subject "+id[0]);yield return Press("Browser lesson "+id);yield return Press("Browser start");yield return Press("Readiness enter");float limit=Time.realtimeSinceStartup+45;while(App.Route!="activity"&&Time.realtimeSinceStartup<limit)yield return null;if(App.Session?.spec.id!=id)throw new Exception("Album entry "+id);
            var work=App.Apparatus.Worktop;Field<Transform>("origin").position=work+new Vector3(-.25f,1.15f,-2.75f);App.Input.FocusAtWorldTarget(work+new Vector3(0,.55f,.12f));App.View.fieldOfView=65;yield return new WaitForSecondsRealtime(.5f);yield return AlbumFrame((n++).ToString("00")+"-"+id+"-room","Toàn cảnh phòng "+id+", dụng cụ chuẩn bị sẵn",true);
            var item=App.Apparatus.items.Single(x=>x.assetId==(id=="P01"?"pendulum":id=="C04"?"nacl":id=="B01"?"microscope":"gears"));AlbumFit(item,true);yield return null;yield return AlbumFrame((n++).ToString("00")+"-"+id+"-setup","Bộ dụng cụ "+id+" trước thao tác; model kích thước gốc",true);
            if(id=="C04")foreach(string key in new[]{"water","nitrate","nacl"}){WorldPress("reagent");WorldPress("dropper");ApparatusRig.TryVisibleBounds(App.Apparatus.items.Single(x=>x.assetId==key),out var b);var ray=new Ray(b.center+Vector3.up*.5f,Vector3.down);Call("UpdateFlagshipDrag",new ActionFrame{pointer=ray,held=true});Call("UpdateFlagshipDrag",new ActionFrame{pointer=ray,released=true});yield return null;}
            if(id=="M01"){WorldPress("gears","Gear36");WorldPress("motor");yield return new WaitForSecondsRealtime(1);}
            if(id=="P01"){WorldPress("stand");WorldPress("pendulum");WorldPress("pendulum");var hit=ModelHit(item);App.Apparatus.BeginGrab(item,new Ray(hit.point+hit.normal*.5f,-hit.normal));var ray=new Ray(App.View.transform.position,App.Apparatus.Pivot+new Vector3(.12f,-.45f,0)-App.View.transform.position);App.Apparatus.UpdateGrab(new ActionFrame{pointer=ray,held=true});App.Apparatus.UpdateGrab(new ActionFrame{pointer=ray,released=true});yield return new WaitForSecondsRealtime(.5f);}
            if(id=="B01"){WorldPress("onion");WorldPress("coverslip");WorldPress("slide");}
            AlbumFit(item,true);yield return null;yield return AlbumFrame((n++).ToString("00")+"-"+id+"-action",id=="C04"?"NaCl có AgCl, hai đối chứng không cặn; mô phỏng định tính":id=="M01"?"Truyền động đang chạy theo góc trục SDK":id=="P01"?"Con lắc đang dao động sau kéo thả":"Lam biểu bì đã lắp; chưa hoàn thành phân loại tế bào",true);
            AlbumFit(item,false);yield return null;yield return AlbumFrame((n++).ToString("00")+"-"+id+"-detail","Cận cảnh dụng cụ/hiện tượng "+id+"; chưa chứng nhận điện thoại thật",true);object[] uiAim={item,new Bounds()};typeof(VlabApp).GetMethod("TryFocusBounds",Private).Invoke(App,uiAim);Call("FitFocusBounds",(Bounds)uiAim[1],new Rect(.44f,.08f,.52f,.72f));yield return null;yield return AlbumFrame((n++).ToString("00")+"-"+id+"-ui","UI bình thường "+id+", đúng trạng thái trong ảnh",false);
        }
        rows.Add("ALBUM actual scene/UI renders; scripted Editor state setup is not native Android touch acceptance.");
    }

    IEnumerator PreviewReactivation()
    {
        var prop=GameObject.CreatePrimitive(PrimitiveType.Cube);prop.SetActive(false);App.ShowHome();yield return new WaitForSecondsRealtime(.3f);prop.SetActive(true);App.Preview.Show("Home");yield return null;Check("Returning menu hides newly restored bench prop",prop.GetComponent<Renderer>().forceRenderingOff);App.Preview.SetVisible(false);App.Preview.Clear();for(int frame=0;frame<60&&App.Preview.ReplacesScene;frame++)yield return null;yield return null;Check("Disabled refreshed preview restores owned renderer",!prop.GetComponent<Renderer>().forceRenderingOff);Destroy(prop);App.Preview.SetVisible(true);App.ShowHome();yield return new WaitForSecondsRealtime(.4f);
    }
    IEnumerator LightingPair(string id)
    {
        yield return new WaitForSecondsRealtime(.5f);var eye=Field<Transform>("origin").position;var rotation=App.View.transform.rotation;float fov=App.View.fieldOfView;bool focus=App.Profile.settings.focusAssist;App.Profile.settings.focusAssist=false;
        App.Profile.settings.graphics=2;Call("ApplySettings");Field<Transform>("origin").position=eye;App.View.transform.rotation=rotation;App.View.fieldOfView=fov;RenderSettings.ambientSkyColor=new Color(.52f,.54f,.56f);RenderSettings.ambientEquatorColor=new Color(.37f,.39f,.41f);RenderSettings.ambientGroundColor=new Color(.23f,.24f,.25f);RenderSettings.reflectionIntensity=.40f;
        foreach(var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None)){if(light.type==LightType.Directional)light.intensity=1.05f;else if(light.name=="Ceiling illumination")light.intensity=.78f;}
        Check(id+" lighting baseline camera and FOV fixed",Vector3.Distance(eye,Field<Transform>("origin").position)<.001f&&Quaternion.Angle(rotation,App.View.transform.rotation)<.01f&&Math.Abs(fov-App.View.fieldOfView)<.01f);yield return new WaitForEndOfFrame();Capture(id+"-normal-before-high");yield return new WaitForEndOfFrame();yield return null;
        foreach(int tier in new[]{0,2}){App.Profile.settings.graphics=tier;Call("ApplySettings");Field<Transform>("origin").position=eye;App.View.transform.rotation=rotation;App.View.fieldOfView=fov;yield return new WaitForEndOfFrame();Capture(id+"-normal-after-"+tier);yield return new WaitForEndOfFrame();yield return null;}
        Check(id+" lighting comparison kept gameplay camera",Vector3.Distance(eye,Field<Transform>("origin").position)<.001f&&Quaternion.Angle(rotation,App.View.transform.rotation)<.01f);App.Profile.settings.focusAssist=focus;
    }
    IEnumerator Checks()
    {
        float deadline=Time.realtimeSinceStartup+40;while((!App||App.Route!="home")&&Time.realtimeSinceStartup<deadline)yield return null;
        if(!App||App.Route!="home")throw new Exception("Home did not initialize");App.Profile.locale="vi";App.Profile.settings.focusAssist=true;VlabAppearanceChecks.Core(Check);Check("Vietnamese recovery preserves concrete authored instruction",LessonGuide.ExplainFailure("Làm theo thứ tự: biểu bì → lamen → lam.",true)=="Làm theo thứ tự: biểu bì → lamen → lam.");
        var left=new PendulumDynamics(.5,-12,10);left.Advance(20);var right=new PendulumDynamics(.5,12,10);right.Advance(20);Check("Left and right release preserve measured period",left.MeasuredCycles==10&&right.MeasuredCycles==10&&Math.Abs(left.MeasuredPeriod-right.MeasuredPeriod)<.00001);
        if(Environment.GetEnvironmentVariable("VLAB_PHOTO_ALBUM")=="1"){yield return PhotoAlbum();yield break;}
        var ui=Field<VlabUi>("ui");
        if(Environment.GetEnvironmentVariable("VLAB_FLAGSHIP_FINAL")=="1"){PhoneInputChecks.Run();Check("Native Input System phone regression suite completed",true);SecondTestWorldChecks.Movement(Check);yield return PreviewReactivation();if(Environment.GetEnvironmentVariable("VLAB_LIGHTING_COMPARE")=="1"){yield return Press("Subject P");yield return Press("Browser lesson P01");yield return Press("Browser start");yield return Press("Readiness enter");while(App.Route!="activity")yield return null;yield return LightingPair("P01");}foreach(string id in new[]{"C04","B01","M01"})yield return FlagshipComponents(id);yield break;}
        // Native world-space UI and physics occlusion, without pretending to be an XR device.
        var nativeUiModule=App.GetComponentInChildren<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();nativeUiModule.enabled=false;
        App.Preview.Clear();yield return new WaitForSecondsRealtime(.3f);yield return null;yield return null;
        ui.SetWorld(true,App.View);Call("PlaceReadableSurface",Field<RectTransform>("page"),true);yield return null;yield return null;
        var subject=Find("Subject P");var ray=new Ray(App.View.transform.position,subject.transform.position-App.View.transform.position);
        var blocker=GameObject.CreatePrimitive(PrimitiveType.Cube);blocker.name="QA preview occlusion fixture";blocker.transform.position=ray.GetPoint(.4f);blocker.transform.localScale=Vector3.one*.12f;Physics.SyncTransforms();
        App.Preview.Show("Home");yield return new WaitForSecondsRealtime(.3f);yield return null;yield return null;
        Check("Photo preview suppresses underlying mesh",blocker.GetComponent<Renderer>().forceRenderingOff);
        object[] arguments={ray,null};Call("RaycastInterface",arguments);
        Check("Hidden room collider cannot intercept preview menu",Field<List<RaycastResult>>("uiHits").Any(hit=>hit.gameObject.GetComponentInParent<Button>()==subject));
        App.Preview.Clear();yield return new WaitForSecondsRealtime(.3f);
        Check("Leaving preview restores scene renderer",!blocker.GetComponent<Renderer>().forceRenderingOff);
        Call("RaycastInterface",arguments);Check("Visible world collider still blocks world UI",Field<List<RaycastResult>>("uiHits").Count==0);
        Destroy(blocker);ui.SetWorld(false,App.View);App.ShowHome();nativeUiModule.enabled=true;yield return new WaitForSecondsRealtime(.3f);
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
        for(int i=0;i<10&&!App.Apparatus.Ready;i++)
        {
            var item=App.Apparatus.NextInstallableItem();App.Apparatus.Select(item);if(!App.Apparatus.InstallSelected())throw new Exception("P01 fixture install failed");
            yield return null;yield return null;
            if(ApparatusRig.TryVisibleBounds(item,out var installedBounds)){var centre=App.View.WorldToViewportPoint(installedBounds.center);Check("Newly installed "+item.assetId+" stays in view",centre.z>0&&centre.x>.02f&&centre.x<.98f&&centre.y>.02f&&centre.y<.98f);}
            if(i==0)
            {
                Call("ShowEquipment");yield return new WaitForSecondsRealtime(.3f);yield return null;yield return Press("Equipment item clamp");
                var clamp=App.Apparatus.items.Single(part=>part.assetId=="clamp");ApparatusRig.TryVisibleBounds(clamp,out var bounds);var point=App.View.WorldToViewportPoint(bounds.center);
                Check("Inventory selection frames actual clamp beside left panel",point.z>0&&point.x>.58f&&point.x<.98f&&point.y>.06f&&point.y<.94f);
                yield return Aspect(2340,1080,rt=>Capture("P01-clamp-selected",rt));Call("CloseModal");
            }
        }
        var guide=new LessonGuide(App.Session);string[] targets={"stand","pendulum","pendulum","pendulum"};
        for(int i=0;i<4;i++)
        {
            Check("P01 preparation target "+i,guide.Preparation(App.Apparatus,true,i).key==targets[i]);
            if(i==2){Call("RefreshActivity");Call("ShowGuideTarget",guide.Preparation(App.Apparatus,true,i));yield return new WaitForSecondsRealtime(.5f);var cue=Field<GuideCue>("worldGuideCue");Check("Step3 points at pendulum",cue&&App.Apparatus.Selected.assetId=="pendulum");var callout=cue.GetComponentsInChildren<Canvas>().Single().GetComponent<RectTransform>();var label=Bounds(callout,App.View);foreach(var zoom in App.GetComponentsInChildren<Button>().Where(b=>b.name.StartsWith("Phone zoom")))Check("Callout clears "+zoom.name,!label.Overlaps(Bounds((RectTransform)zoom.transform)));Capture("P01-step3-guidance");yield return null;Call("ClearGuideCue");yield return null;Check("Guide dismisses",!Field<GuideCue>("worldGuideCue"));}
            if(i<3){WorldPress(i==0?"stand":"pendulum");Check("P01 direct model preparation "+i,App.Session.Operation==i+1&&string.IsNullOrEmpty(App.Session.Assistance));}
        }
        Check("No fixture measurement or notebook entry",App.Profile.attempts.Count==0);
        var ruler=App.Apparatus.items.Single(item=>item.assetId=="ruler");ApparatusRig.TryVisibleBounds(ruler,out var rulerBounds);
        var clearanceProbe=new GameObject("QA ruler clearance",typeof(BoxCollider));clearanceProbe.transform.position=rulerBounds.center;var rulerBox=clearanceProbe.GetComponent<BoxCollider>();rulerBox.size=rulerBounds.size;
        foreach(var item in App.Apparatus.items.Where(item=>item!=ruler&&item.installed))foreach(var collider in item.GetComponentsInChildren<Collider>())
            if(collider.enabled&&!collider.isTrigger)Check("P01 ruler does not penetrate "+collider.name,!Physics.ComputePenetration(rulerBox,clearanceProbe.transform.position,Quaternion.identity,collider,collider.transform.position,collider.transform.rotation,out _,out float depth)||depth<.0005f);
        Destroy(clearanceProbe);
        var rulerNative=ruler.GetComponent<VlabRuler>();var zero=(Transform)typeof(VlabRuler).GetField("zero",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(rulerNative);var end=(Transform)typeof(VlabRuler).GetField("end",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(rulerNative);
        Check("Ruler relocation preserves native 1 m scale and zero/end calibration",rulerNative.TryDistanceMetres(zero.position,end.position,out double metres)&&Math.Abs(metres-1)<1e-8);
        foreach(bool vi in new[]{true,false})foreach(bool large in new[]{false,true})
        {
            App.Profile.locale=vi?"vi":"en";ui.LargeText=large;Call("ShowEquipment");yield return null;yield return null;yield return null;
            foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(2340,1080)})
                yield return Aspect(size.x,size.y,rt=>
                {
                    var dialog=Field<RectTransform>("modal").GetComponentInChildren<PhoneDialogLayout>();Check("Inventory remains compact "+size,Bounds((RectTransform)dialog.transform,App.View).width<size.x*.60f);
                    foreach(var button in dialog.GetComponentsInChildren<Button>().Where(b=>b.name.StartsWith("Equipment item ")))
                    {Caption(button,"Fitted "+size+" vi="+vi+" large="+large);Check("Fitted state has vector check and green face",button.transform.Find("Equipment fitted check").gameObject.activeSelf&&button.GetComponent<Image>().color==VlabUi.Hex("37594C"));}
                    Capture("P01-fitted-"+(vi?"vi":"en")+(large?"-large":"")+"-"+size.x,rt);
                });
            Call("CloseModal");
        }
        ui.LargeText=false;App.Profile.locale="vi";
        Vector3 resetPosition=Vector3.zero;Quaternion resetRotation=Quaternion.identity;
        for(int repeat=0;repeat<5;repeat++){Call("FocusStation");if(repeat==0){resetPosition=App.View.transform.position;resetRotation=App.View.transform.rotation;}Check("Focus station has no drift "+repeat,Vector3.Distance(resetPosition,App.View.transform.position)<.0001f&&Quaternion.Angle(resetRotation,App.View.transform.rotation)<.01f);}
        var bob=App.Apparatus.items.Single(item=>item.assetId=="pendulum");App.Apparatus.Select(bob);
        foreach(float amount in new[]{-1000f,1000f})
        {Call("ZoomPhone",amount);ApparatusRig.TryVisibleBounds(bob,out var bounds);var point=App.View.WorldToViewportPoint(bounds.center);Check("Zoom retains selected model "+amount,point.z>0&&point.x>.05f&&point.x<.95f&&point.y>.05f&&point.y<.95f);}
        App.Apparatus.Select(null);
        for(int repeat=0;repeat<10;repeat++){Call("ShowEquipment");yield return null;yield return null;var dialog=Field<RectTransform>("modal").GetComponentInChildren<PhoneDialogLayout>();Check("Inventory reopen has stable scale "+repeat,Mathf.Abs(dialog.transform.localScale.x-1.1f)<.001f&&dialog.AssertPhoneLayout(out _));Call("CloseModal");yield return null;}
        foreach(int quality in new[]{0,2})
        {
            App.Profile.settings.graphics=quality;Call("ApplySettings");yield return null;yield return null;
            var pipeline=Field<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>("runtimePipeline");
            Check("Quality "+quality+" changes render scale",Mathf.Abs(pipeline.renderScale-(quality==0?.65f:1f))<.001f);
            Check("Quality "+quality+" changes shadows and MSAA",pipeline.shadowDistance==(quality==0?8:25)&&pipeline.msaaSampleCount==(quality==0?2:4));
            yield return Aspect(2340,1080,rt=>Capture("P01-quality-"+quality,rt));
        }
                Check("Direct P01 preparation waits for learner drag",App.Session.Phase==ExperimentPhase.Preparing&&App.Session.Operation==3);
        Call("ClearGuideCue");App.Profile.settings.hints=false;for(int mistake=0;mistake<3;mistake++)Call("ActionFailure","Install every required instrument before preparing the trial.");Check("Hints off does not summon world cue",!Field<GuideCue>("worldGuideCue"));App.Profile.settings.hints=true;for(int mistake=0;mistake<3;mistake++)Call("ActionFailure","Install every required instrument before preparing the trial.");Check("Repeated preparation failures point to current actual model",Field<GuideCue>("worldGuideCue"));Check("Failure guidance never performs preparation",App.Session.Operation==3);Call("ClearGuideCue");
        typeof(VlabApp).GetField("lastFocusedTaskTarget",Private).SetValue(App,null);Call("FocusStepTarget",new GuideTask("start","pendulum","","",""));yield return Aspect(2340,1080,rt=>Capture("P01-pivot-bob-envelope",rt));
var startRay=new Ray(App.View.transform.position,bob.transform.position-App.View.transform.position);App.Apparatus.BeginGrab(bob,startRay);
        App.Apparatus.UpdateGrab(new ActionFrame{pointer=new Ray(App.View.transform.position,bob.transform.position+Vector3.right*.1f-App.View.transform.position),held=true});
        App.Apparatus.UpdateGrab(new ActionFrame{pointer=startRay,released=true});
        Check("Direct P01 bob drag/release starts motion through apparatus authority",App.Session.Phase==ExperimentPhase.Running);
        Check("Component direct interaction creates no measurement",App.Profile.attempts.Count==0);
        var checkpoint=App.Session.Checkpoint();App.Session.Assistance="guided-preparation";checkpoint=App.Session.Checkpoint();
        Check("Assisted preparation survives checkpoint restore",ExperimentSession.Restore(checkpoint).Assistance=="guided-preparation");
        var older=new AttemptRecord{experimentId="P01",modelNotesVi="Existing notes"};Check("Historical notes retain content and unknown assistance label",JournalView.Interpretation(older,true).Contains("chưa ghi mức hỗ trợ")&&JournalView.Interpretation(older,true).EndsWith("Existing notes"));
        var assisted=new AttemptRecord{experimentId="P01",modelNotesVi="Own readings",assistance="guided-preparation"};Check("Journal explicitly labels assisted preparation",JournalView.Interpretation(assisted,true).Contains("Chuẩn bị có hỗ trợ"));
        foreach(var id in LessonSpec.All.Keys)Check("Per-station contrast policy "+id,BenchDisplayVisibility.NeedsLightSurface(id)==new[]{"C01","C02","C03","C05","C06","B06"}.Contains(id));
        var fixture=GameObject.CreatePrimitive(PrimitiveType.Cube);fixture.name="QA optical bench top";fixture.transform.position=new Vector3(100,1,0);fixture.transform.localScale=new Vector3(3,.1f,1.4f);var original=fixture.GetComponent<Renderer>().sharedMaterials;var surface=fixture.AddComponent<BenchDisplayVisibility>();
        surface.SetSurface("C01",new Vector3(100,1.055f,0));Check("Light bench owns material instance",fixture.GetComponent<Renderer>().sharedMaterial!=original[0]);Check("Off-white bench is matte and non-metal",fixture.GetComponent<Renderer>().sharedMaterial.color.r>.7f);surface.Restore();Check("Bench restore keeps original materials",fixture.GetComponent<Renderer>().sharedMaterial==original[0]);Destroy(fixture);yield return null;
        Call("ShowHome");yield return new WaitForSecondsRealtime(.4f);yield return Press("Subject C");yield return Press("Browser lesson C01");yield return Press("Browser start");yield return Press("Readiness enter");
        deadline=Time.realtimeSinceStartup+45;while(App.Route!="activity"&&Time.realtimeSinceStartup<deadline)yield return null;if(App.Session?.spec.id!="C01")throw new Exception("C01 visual fixture missing");
        for(int n=0;n<25&&!App.Apparatus.Ready;n++){var item=App.Apparatus.NextInstallableItem();App.Apparatus.Select(item);if(!App.Apparatus.InstallSelected())throw new Exception("C01 fixture install failed");yield return null;}
        Call("FocusStation");var bench=App.Apparatus.GetComponent<BenchDisplayVisibility>();var scenePage=Field<RectTransform>("page");scenePage.gameObject.SetActive(false);bench.Restore();yield return Aspect(2340,1080,rt=>Capture("C01-dark-baseline",rt));bench.SetSurface("C01",App.Apparatus.Worktop);
        foreach(int quality in new[]{0,2}){App.Profile.settings.graphics=quality;Call("ApplySettings");yield return null;var sun=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).FirstOrDefault(light=>light.type==LightType.Directional);Check("Quality light shadows "+quality,sun&&sun.shadows==(quality==0?LightShadows.None:LightShadows.Soft));Check("Quality texture filtering "+quality,QualitySettings.anisotropicFiltering==(quality==2?AnisotropicFiltering.ForceEnable:AnisotropicFiltering.Disable));yield return Aspect(2340,1080,rt=>Capture("C01-matte-quality-"+quality,rt));}scenePage.gameObject.SetActive(true);
        foreach(string id in new[]{"C04","B01","M01"})yield return FlagshipComponents(id);
    }
}
[InitializeOnLoad] public static class Qa016CoreChecks
{
    static Qa016CoreChecks(){EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("VLAB.QA016",false)){SessionState.SetBool("VLAB.QA016",false);new GameObject("QA014 bounded checks",typeof(Qa016CoreProbe));}};}
    public static void Run(){if(!SecondTestGameView.Configure(out var why))throw new Exception(why);SessionState.SetBool("VLAB.QA016",true);SessionState.SetBool("NguaManVLAB.ProductSmoke",false);SessionState.SetBool("VLAB.FollowupRun",false);SessionState.SetBool("VLAB.SecondTestUI",false);SessionState.SetString("VLAB.TestProfile",Path.Combine(Application.persistentDataPath,"QA","QA016-"+Guid.NewGuid().ToString("N")));EditorSceneManager.OpenScene("Assets/VLABProduct/Scenes/VLAB_Boot.unity");EditorApplication.EnterPlaymode();}
}
#endif
