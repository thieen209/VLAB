#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using Vlab.Product;

public sealed partial class WorkbenchUpgradeProbe
{
    IEnumerator LearnerExperienceChecks(string scope=null)
    {
        scope=scope??Environment.GetEnvironmentVariable("VLAB_LEARNER_SCOPE")??"guide";
        if(scope=="batch"||scope=="review"||scope=="final")
        {
            var parts=scope=="final"?new[]{"dropper","trays","placement","pour"}:scope=="review"?new[]{"boot","notebook","microscope","dropper","trays","placement"}:new[]{"guide","notebook","microscope","pipette","placement"};
            foreach(string part in parts)
            {
                if(part!=parts[0]){App.Input.UseMockController=false;App.Input.SetSimulator(false);App.ShowHome();yield return null;}
                rows.Add("BEGIN learner scope "+part);yield return LearnerExperienceChecks(part);rows.Add("END learner scope "+part);
            }
            yield break;
        }
        if(scope=="guide"||scope=="boot")
        {
            float deadline=Time.realtimeSinceStartup+10;while(!App&&Time.realtimeSinceStartup<deadline)yield return null;
            Check("Startup is visible before Home",App&&App.Route=="boot");
            Check("Startup contains exact team credit",App.GetComponentsInChildren<TMPro.TMP_Text>().Any(t=>t.text=="Made by Ngựa Mán team with love"));
            yield return Capture("startup-team-credit");
        }
        yield return Wait("home");if(Find("Skip quick tour"))yield return Tap("Skip quick tour");
        if(scope=="boot")yield break;
        if(scope=="phone-placement"){yield return PhonePlacementJourney();yield break;}
        if(scope=="titration-pipette"){ChemistryIntegrationChecks.Run();Check("Chemistry SDK integration after manual C01",true);yield return ManualPipetteJourney(false,"C01");App.ShowHome();yield return Wait("home");yield return ManualPipetteJourney();yield break;}
        Action<string> capture=name=>ScreenCapture.CaptureScreenshot("Evidence/WorkbenchUpgrade/"+name+".png");
        if(scope=="trays")
        {
            yield return BenchDisplayVisibilityChecks.RunLessons(App,capture,Check);
            yield break;
        }
        if(scope=="placement")
        {
            yield return ReleasedEquipmentChecks.Run(Check);
            yield return EquipmentPlacementChecks();
            yield break;
        }
        if(scope=="pipette"){yield return ManualPipetteJourney();yield break;}
        if(scope=="dropper"){yield return ManualDropperJourney();yield break;}
        if(scope=="pour"){yield return ManualPourJourney();yield break;}
        if(scope=="microscope")
        {
            foreach(string id in new[]{"B01","B02","B08"})
            {
            App.ShowHome();yield return null;
            yield return Tap("Subject B");yield return Wait("browser");yield return Tap("Browser lesson "+id);yield return Tap("Browser start");yield return Wait("readiness");yield return Tap("Readiness enter");yield return Wait("activity");
            var rig=App.Apparatus;for(int n=0;n<40&&!rig.Ready;n++){rig.Select(rig.NextInstallableItem());Check("Eyepiece fixture installs apparatus",rig.InstallSelected());}
            rows.Add("Installation and specimen preparation below are fixture setup, not user-interaction coverage; eyepiece controls use native UI raycasts.");
            var driver=(BiologyDriver)rig.Driver;
            var microscope=rig.items.Single(i=>i.assetId=="microscope");
            if(id=="B01")
            {
            Check("Eyepiece fixture separates epidermis",rig.PrepareNext(out _));Check("Eyepiece fixture lowers cover",rig.PrepareNext(out _));
            var slide=rig.items.Single(i=>i.assetId=="slide");
            var seat=slide.GetComponentsInChildren<Transform>().Single(t=>t.name.EndsWith("_Base_SOCKET",StringComparison.Ordinal));
            var stage=microscope.GetComponentsInChildren<Transform>().Single(t=>t.name.EndsWith("_SlideCenter_SOCKET",StringComparison.Ordinal));
            slide.transform.position+=stage.position-seat.position;Check("Eyepiece fixture mounts actual slide",driver.PlaceSlideAtCurrentPose(out _));Check("Eyepiece fixture closes actual clips",driver.Operate("clips",out _));
            Check("Eyepiece fixture completes mounting",rig.PrepareNext(out _));Check("Eyepiece fixture sets 10x objective",driver.Operate("obj10",out _));Check("Eyepiece fixture completes focusing",rig.PrepareNext(out _));
            }
            else{Check(id+" fixture mounts source slide",rig.PrepareNext(out _));Check(id+" fixture sets 10x",driver.Operate("obj10",out _));}
            typeof(VlabApp).GetField("inspectionTab",Private).SetValue(App,"view");App.ShowInstrument(microscope);yield return null;yield return null;
            yield return MicroscopeEyepieceChecks.Run(App,capture,Check);
            yield return Capture(id+"-eyepiece-phone");
            }
            yield break;
        }
        if(scope=="notebook")
        {
            rows.Add(WhiteboardChecks.Run());NotebookPersistenceChecks.Run(Check);
            yield return JournalNoteChecks.Run(App,capture,Check);
            yield return NotebookVrChecks.Run(App,capture,Check);
            yield break;
        }
        var procedureBriefs=JsonUtility.FromJson<BriefCatalog>(Resources.Load<TextAsset>("Catalog/experiments").text).items;
        foreach(var spec in LessonSpec.All.Values)
        {
            var brief=procedureBriefs.Single(b=>b.id==spec.id);
            string vi=LessonGuide.FullProcedure(spec.id,true,brief),en=LessonGuide.FullProcedure(spec.id,false,brief);
            Check(spec.id+" full procedure contains every authored preparation and completion check",spec.operationsVi.All(vi.Contains)&&spec.operationsEn.All(en.Contains)&&vi.Contains("Kiểm tra:")&&en.Contains("Check:")&&vi.Contains("QUAN SÁT VÀ GHI SỐ ĐO"));
            Check(spec.id+" procedure retains research measurement and comparison steps",brief.stepsVi.Length>0&&brief.stepsVi.All(step=>!string.IsNullOrWhiteSpace(step)&&vi.Contains(step)));
            Check(spec.id+" worked example is labelled separately from measurements",vi.Contains("VÍ DỤ MINH HỌA · KHÔNG PHẢI SỐ ĐO CỦA BẠN")&&vi.Contains(brief.examplesVi));
        }
        foreach(string id in new[]{"P01","C05","B01"})
        {
            Check("Set landscape viewport for "+id,SecondTestGameView.Configure(out _,id=="P01"?1280:2340,id=="P01"?720:1080));
            App.ShowHome();yield return null;yield return Tap("Subject "+id[0]);yield return Wait("browser");yield return Tap("Browser lesson "+id);yield return Tap("Browser start");yield return Wait("readiness");
            var oldSession=App.Session;int attempts=App.Profile.attempts.Count;
            yield return Tap("Readiness help");
            var content=App.GetComponentsInChildren<RectTransform>().First(r=>r.name=="Lesson procedure content").GetComponent<VlabScrollContent>();
            Check(id+" full guide displays a scrollable procedure before entering the station",content&&content.Scroll.viewport.rect.height>0&&content.Body.text.Contains(ReadinessView.Goal(id,true)));
            Check(id+" reading the procedure does not create a session or measurement",ReferenceEquals(oldSession,App.Session)&&App.Profile.attempts.Count==attempts);
            yield return Capture(id+"-full-procedure");
            content.Scroll.verticalNormalizedPosition=0;yield return null;yield return null;
            Check(id+" long procedure reaches its final section",content.Scroll.verticalNormalizedPosition<.01f&&content.Body.text.Contains("LẶP LẠI VÀ SO SÁNH"));
            yield return Capture(id+"-full-procedure-bottom");yield return Tap("Lesson procedure close");
            Check(id+" procedure closes to readiness",App.Route=="readiness"&&!Read<RectTransform>("modal"));
            if(id=="P01")
            {
                yield return Tap("Readiness enter");yield return Wait("activity");
                var rig=App.Apparatus;for(int n=0;n<40&&!rig.Ready;n++){rig.Select(rig.NextInstallableItem());Check("Zoom fixture installs P01 apparatus",rig.InstallSelected());}
                StepFocusChecks.Run(App,Check);
                Call("FocusStation");Call("ZoomPhone",-18f);yield return Capture("P01-closer-phone-inspection");
            }
        }
        rows.Add("Guide UI tested at1280x720 and2340x1080; all32 content coverage is not all32 experiment completion or device evidence.");
    }
}
#endif
