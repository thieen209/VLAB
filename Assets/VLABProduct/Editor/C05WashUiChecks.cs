#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Vlab.Product;
public sealed partial class WorkbenchUpgradeProbe
{
    IEnumerator ManualWashJourney()
    {
        ChemistryIntegrationChecks.Run();Check("C01-C08 driver regression",true);
        C05ManualWashChecks.Run();Check("C05 manual volume, mark, restore and actual concentration",true);
        yield return Tap("Subject C");yield return Wait("browser");yield return Tap("Browser lesson C05");yield return Tap("Browser start");yield return Wait("readiness");yield return Tap("Readiness enter");yield return Wait("activity");
        var rig=App.Apparatus;for(int i=0;i<40&&!rig.Ready;i++){rig.Select(rig.NextInstallableItem());Check("C05 fixture installation",rig.InstallSelected());}
        Check("C05 fixture rinse",rig.PrepareNext(out _));Check("C05 fixture transfers10mL aliquot",rig.PrepareNext(out _));
        rows.Add("Installation/rinse/aliquot above are explicit fixtures; only wash gesture below uses queued phone input.");
        App.ShowActivity();yield return new WaitForSecondsRealtime(.4f);
        var focus=App.GetComponentsInChildren<UnityEngine.UI.Button>().FirstOrDefault(b=>b.GetComponentInChildren<TMPro.TMP_Text>()?.text=="Nhìn về bàn");if(focus)yield return Tap(focus.name);
        var hide=App.GetComponentsInChildren<UnityEngine.UI.Button>().FirstOrDefault(b=>b.GetComponentInChildren<TMPro.TMP_Text>()?.text=="Ẩn UI");if(hide)yield return Tap(hide.name);
        var item=rig.items.Single(i=>i.assetId=="wash");var wash=item.GetComponent<VlabWashBottle>();var flask=rig.items.Single(i=>i.assetId=="vol50").GetComponent<VlabVolumetricFlask>();
        double before=flask.Liquid.VolumeMl,water=wash.RemainingMl;var camera=App.View.transform.rotation;
        var start=ModelPoint(item);int finger=++serial;
        InputSystem.QueueStateEvent(touch,new TouchState{touchId=finger,phase=UnityEngine.InputSystem.TouchPhase.Began,position=start});yield return null;yield return new WaitForSecondsRealtime(.15f);
        Check("C05 phone hold acquires actual wash model",item.Owner==EquipmentGrabOwner.Flagship);
        var neck=flask.GetComponentsInChildren<Transform>().Single(t=>t.name.EndsWith("_NeckSocket_SOCKET",StringComparison.Ordinal));
        var goal=App.View.WorldToScreenPoint(new Vector3(neck.position.x,wash.Outlet.y,neck.position.z));
        Check("C05 phone target is physically on screen",goal.z>0&&goal.x>=0&&goal.x<=Screen.width&&goal.y>=0&&goal.y<=Screen.height);
        for(int i=1;i<=12;i++){InputSystem.QueueStateEvent(touch,new TouchState{touchId=finger,phase=UnityEngine.InputSystem.TouchPhase.Moved,position=Vector2.Lerp(start,goal,i/12f)});yield return new WaitForSecondsRealtime(.03f);}
        yield return new WaitForSecondsRealtime(.7f);
        Check("C05 actual pointer gesture transfers water",flask.Liquid.VolumeMl>before);
        Check("C05 gesture conserves water",Math.Abs(water-wash.RemainingMl-(flask.Liquid.VolumeMl-before))<1e-7);
        Check("C05 holding tool does not rotate camera",Quaternion.Angle(camera,App.View.transform.rotation)<.1f);
        yield return Capture("C05-manual-water-held");
        InputSystem.QueueStateEvent(touch,new TouchState{touchId=finger,phase=UnityEngine.InputSystem.TouchPhase.Ended,position=goal});yield return null;yield return new WaitForSecondsRealtime(.2f);
        Check("C05 release stops real stream and ownership",!wash.Stream.Flowing&&item.Owner==EquipmentGrabOwner.None);
        Check("C05 partial fill creates no measurement",App.Session.Phase==ExperimentPhase.Preparing&&!App.Profile.attempts.Any(a=>a.experimentId=="C05"));
        yield return Capture("C05-manual-water-released");
    }
}
#endif
