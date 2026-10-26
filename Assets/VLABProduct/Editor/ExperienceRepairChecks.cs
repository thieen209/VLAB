#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Vlab.Product;

[InitializeOnLoad]
public static class ExperienceRepairBuildGate
{
    static ExperienceRepairBuildGate()
    {
        EditorApplication.playModeStateChanged+=state=>{
            if(state!=PlayModeStateChange.EnteredEditMode||!SessionState.GetBool("VLAB.BuildExperience",false))return;
            SessionState.SetBool("VLAB.BuildExperience",false);
            EditorApplication.delayCall+=PracticeRepairBuild.Run;
        };
    }
}

public sealed partial class WorkbenchUpgradeProbe
{
    IEnumerator ExperienceRepairChecks()
    {
        yield return Wait("home");if(Find("Skip quick tour"))yield return Tap("Skip quick tour");
        bool guideOnly=Environment.GetEnvironmentVariable("VLAB_GUIDANCE_ONLY")=="1";
        if(!guideOnly){
        GuidanceTextChecks.Run();Check("Guidance covers32 authored lessons and preserves checkpoints",true);
        LiquidPourTransferChecks.Run();Check("Conserved liquid transfer core and tilted stock-bottle limit",true);
        yield return FormulaSheetChecks.Run(App,name=>ScreenCapture.CaptureScreenshot("Evidence/WorkbenchUpgrade/"+name+".png"),Check);
        yield return ExploreNavigationChecks();}
        yield return Capture("experience-home");
        ExperienceNavigationChecks.Run(Check);Patch1SettingsChecks.Run();Check("Settings migration and validation",true);
        Check("High-power warning explains fine focus in Vietnamese",LessonGuide.ExplainFailure("At 40× use the fine-focus knob; coarse movement can hit the slide.",true).Contains("chỉnh tinh"));
        var clamp=typeof(GuideCue).GetMethod("ClampLabelAxis",BindingFlags.Static|BindingFlags.NonPublic);
        Check("Oversized hint stays at viewport centre",Mathf.Abs((float)clamp.Invoke(null,new object[]{.9f,.1f,.7f,1f})-.4f)<.001f);
        yield return Tap("Subject P");yield return Wait("browser");yield return Tap("Browser lesson P01");yield return Tap("Browser start");yield return Wait("readiness");yield return Tap("Readiness enter");yield return Wait("activity");
        var rig=App.Apparatus;for(int n=0;n<50&&!rig.Ready;n++){rig.Select(rig.NextInstallableItem());Check("P01 setup fixture support order",rig.InstallSelected());}
        Check("P01 setup ready for camera checks",rig.Ready);yield return null;
        bool valid=rig.ValidateAssembly(out var geometry);Check("P01 native assembly geometry: "+geometry,valid);
        StepFocusChecks.Run(App,Check);Call("ClearGuideCue");yield return Capture("P01-arm-clearance");
        var item=rig.items.First(x=>x.installed);Call("ShowEquipmentCue",item,true);yield return null;
        var cue=Read<GuideCue>("worldGuideCue");Check("Hint preserves model and science state",cue&&cue.AssertPresentationOnly(App.Session,out _));
        yield return Capture("experience-P01-guidance");
        Call("ShowGuidance");yield return new WaitForSecondsRealtime(.4f);
        var guidePanel=App.GetComponentsInChildren<RectTransform>().Single(r=>r.name=="Guidance content").GetComponent<VlabScrollContent>();
        var guideTask=(GuideTask)Call("CurrentGuide");
        Check("Guide shows actual P01 interaction, authored explanation and conditional next task",guidePanel.Scroll.viewport.rect.height>0&&guidePanel.Body.text.Contains((string)Call("FlagshipInstruction"))&&guidePanel.Body.text.Contains((string)Call("GuidanceNext",guideTask))&&guidePanel.Body.text.Contains((string)Call("GuidanceExplanation",guideTask)));
        Check("Phone guidance reuses one scroll viewport",!App.GetComponentsInChildren<RectTransform>().Any(r=>r.name=="Phone dialog scroll viewport"&&r.gameObject.activeInHierarchy));
        yield return Capture("guidance-what-next");yield return Tap("Guidance close");Check("Phone guidance Back closes actual dialog",!Read<RectTransform>("modal"));
        if(guideOnly)yield break;
        App.ShowHome();yield return null;
        var oldScene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();var fixture=UnityEngine.SceneManagement.SceneManager.CreateScene("Experience science fixture");UnityEngine.SceneManagement.SceneManager.SetActiveScene(fixture);yield return UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(oldScene);
        ChemistryIntegrationChecks.Run();Check("Chemistry SDK checks including gradual C04 and pause",true);
        rows.Add(BiologyIntegrationChecks.Run());Check("Biology SDK checks including direct microscope and supported release",true);
        VlabAudioChecks.Run();Check("Audio generated samples finite and unclipped",true);
        rows.Add("SDK fixtures are not all-32 UI journeys or physical Android, Cardboard, or BLE acceptance.");
    }
}
#endif
