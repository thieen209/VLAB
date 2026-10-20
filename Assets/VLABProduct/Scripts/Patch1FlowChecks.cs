#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Vlab.Product;

// Isolated native fixture only; does not open or write a learner Profile.
[InitializeOnLoad] public static class Patch1FlowChecks
{
    const string Pending="Vlab.Patch1FlowProbe";
    static Patch1FlowChecks()
    {
        EditorApplication.playModeStateChanged+=state=>
        {if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Pending,false)){SessionState.SetBool(Pending,false);new GameObject("Patch1 flow probe",typeof(Patch1FlowProbe));}};
    }
    public static string[] Verify()
    {
        var rows=new List<string>();
        void Check(bool value,string name){if(!value)throw new Exception(name);rows.Add("PASS "+name);}
        var fixture=new GameObject("Patch1 synthetic interaction fixture");
        var foreign=new GameObject("Foreign equipment ownership fixture");
        GameObject cueTarget=null;
        try
        {
            var session=new ExperimentSession("P01");var rig=fixture.AddComponent<ApparatusRig>();rig.Build(session,new Vector3(5,.8f,0));
            var stand=rig.items.Single(x=>x.assetId=="stand");var clamp=rig.items.Single(x=>x.assetId=="clamp");var ruler=rig.items.Single(x=>x.assetId=="ruler");
            int operation=session.Operation;var phase=session.Phase;double elapsed=session.Elapsed;
            rig.Select(stand);var ray=new Ray(stand.transform.position+new Vector3(0,1,-1),Vector3.forward);
            Check(rig.TakeSelected(ray,out var why),"Explicit Take accepts loose supplied stand: "+why);
            rig.UpdateGrab(new ActionFrame{pointer=ray,released=true});
            Check(rig.IsCarrying&&rig.Selected==stand&&!stand.installed,"Phone button release retains explicitly taken item without auto-install");
            rig.Select(ruler);
            Check(!rig.IsCarrying&&Vector3.Distance(stand.transform.position,stand.trayPosition)<.0001f,"Selecting another item returns old held item to supported tray");
            var owned=rig.Selected;rig.Select(foreign.AddComponent<EquipmentHandle>());
            Check(rig.Selected==owned,"Foreign equipment cannot replace this rig's selection");
            rig.Select(clamp);
            Check(!rig.CanInstallSelected(out why)&&why.Contains("supporting")&&!rig.InstallSelected(),"Unfitted support blocks clamp placement with actionable reason");
            Check(new LessonGuide(session).Current(rig,true).key=="stand","Optional install guidance identifies missing support rather than blocked selection");
            rig.Select(stand);Check(rig.InstallSelected(),"Explicit placement fits source stand");
            Check(!rig.IsCarrying&&rig.Selected==null&&stand.installed,"Successful placement clears take state");
            rig.Select(stand);Check(!rig.TakeSelected(ray,out why)&&stand.installed,"Take cannot silently detach an installed instrument");
            rig.Select(ruler);
            var missed=new Ray(ruler.trayPosition+new Vector3(0,2,-2),Vector3.right);
            rig.BeginGrab(ruler,missed);rig.UpdateGrab(new ActionFrame{pointer=missed,released=true});
            Check(!rig.IsCarrying&&rig.Selected==null&&Vector3.Distance(ruler.transform.position,ruler.trayPosition)<.0001f,"Missed legacy drag returns to tray rather than leaving floating geometry");
            rig.Select(ruler);Check(rig.TakeSelected(ray,out why),"A returned loose instrument can be taken again: "+why);rig.CancelGrab();
            Check(!rig.IsCarrying&&rig.Selected==null&&Vector3.Distance(ruler.transform.position,ruler.trayPosition)<.0001f,"Return/cancel always clears held state and restores tray");
            rig.SetCoachingEnabled(false);
            Check(!rig.CoachingEnabled&&!fixture.GetComponentsInChildren<LineRenderer>(true).Any(x=>x.name.StartsWith("Snap target",StringComparison.Ordinal)&&x.enabled),"Guidance off immediately disables automatic snap arrows");
            rig.SetCoachingEnabled(true);
            Check(rig.CoachingEnabled,"Automatic assistance can be re-enabled without doing a student action");
            cueTarget=new GameObject("Patch1 optional help target",typeof(RectTransform));
            var cue=GuideCue.AttachTo((RectTransform)cueTarget.transform,true);cue.Stop();
            Check(cue.IsStopped&&!cue.gameObject.activeSelf,"Closing optional help hides its presentation immediately");
            Check(session.Operation==operation&&session.Phase==phase&&session.Elapsed==elapsed&&session.Recorded==null,"Take/place/return/coaching do not advance preparation, run time or fabricate observations");
            Check(LessonGuide.ValidateMetadata().Length==0&&LessonSpec.All.Count==32,"All32 existing guide/authority IDs retain complete preparation metadata");
            rows.Add("LIMIT: synthetic native interaction APIs only; actual phone UI/input, all32 authority journeys, profile snapshots and headset must be checked separately.");
            return rows.ToArray();
        }
        finally{if(cueTarget)UnityEngine.Object.DestroyImmediate(cueTarget);UnityEngine.Object.DestroyImmediate(foreign);UnityEngine.Object.DestroyImmediate(fixture);}
    }
    public static void Run()
    {
        if(Application.isPlaying){RunNow();return;}
        SessionState.SetBool(Pending,true);EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects,NewSceneMode.Single);EditorApplication.EnterPlaymode();
    }
    public static void RunNow()
    {
        try{var rows=Verify();Directory.CreateDirectory("Evidence/Patch1");File.WriteAllLines("Evidence/Patch1/flow-checks.txt",rows);Debug.Log("PATCH1 FLOW PASS "+(rows.Length-1));if(Application.isBatchMode)EditorApplication.Exit(0);}
        catch(Exception ex){Directory.CreateDirectory("Evidence/Patch1");File.WriteAllText("Evidence/Patch1/flow-checks.txt","FAIL "+ex);Debug.LogException(ex);if(Application.isBatchMode)EditorApplication.Exit(1);else throw;}
    }
}
public sealed class Patch1FlowProbe:MonoBehaviour
{System.Collections.IEnumerator Start(){yield return null;Patch1FlowChecks.RunNow();}}
#endif
