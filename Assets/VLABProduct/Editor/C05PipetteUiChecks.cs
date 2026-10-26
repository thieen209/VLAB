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
    IEnumerator ManualPipetteJourney(bool currentStation=false)
    {
        if(!currentStation)
        {
        C05ManualPipetteChecks.Run();Check("C05 SDK manual pipette and interrupted draft checks",true);
        C05ManualWashChecks.Run();Check("C05 manual wash regression after explicit pipette preparation",true);
        yield return Tap("Subject C");yield return Wait("browser");yield return Tap("Browser lesson C05");yield return Tap("Browser start");yield return Wait("readiness");yield return Tap("Readiness enter");yield return Wait("activity");
        }
        var rig=App.Apparatus;for(int i=0;i<40&&!rig.Ready;i++){rig.Select(rig.NextInstallableItem());Check("C05 apparatus installation fixture",rig.InstallSelected());}
        rows.Add("Only apparatus installation is a direct fixture. Pipette and wash transfers below use queued phone input.");
        App.ShowActivity();Call("FocusStation");yield return new WaitForSecondsRealtime(.4f);
        var hide=App.GetComponentsInChildren<UnityEngine.UI.Button>().FirstOrDefault(b=>b.GetComponentInChildren<TMPro.TMP_Text>()?.text=="Ẩn UI"||b.GetComponentInChildren<TMPro.TMP_Text>()?.text=="Hide UI");if(hide)yield return Tap(hide.name);
        var item=rig.items.Single(i=>i.assetId=="pipette");var bulbItem=rig.items.Single(i=>i.assetId=="bulb");var pipette=item.GetComponentInChildren<VlabFlask>();var bulb=bulbItem.GetComponentInChildren<VlabPipetteBulb>();
        var stock=rig.items.Single(i=>i.assetId=="stock").GetComponentInChildren<VlabReagentBottle>();var waste=rig.items.Single(i=>i.assetId=="waste").GetComponentInChildren<VlabBeaker>();var flask=rig.items.Single(i=>i.assetId=="vol50").GetComponentInChildren<VlabVolumetricFlask>();
        var driver=(ChemistryDriver)rig.Driver;var camera=App.View.transform.rotation;int finger=0;Vector2 pointer=default;
        Vector2 ScreenTarget(Vector3 world)
        {var point=App.View.WorldToScreenPoint(new Vector3(world.x,rig.Worktop.y+.18f,world.z));Check("C05 pipette target is on screen",point.z>0&&point.x>=0&&point.x<=Screen.width&&point.y>=0&&point.y<=Screen.height);return point;}
        IEnumerator Hold(EquipmentHandle selected)
        {
            pointer=ModelPoint(selected);finger=++serial;InputSystem.QueueStateEvent(touch,new TouchState{touchId=finger,phase=UnityEngine.InputSystem.TouchPhase.Began,position=pointer});yield return null;yield return new WaitForSecondsRealtime(.15f);
            Check("C05 pipette and bulb share exclusive gesture ownership",item.Owner==EquipmentGrabOwner.Flagship&&bulbItem.Owner==EquipmentGrabOwner.Flagship);
        }
        IEnumerator MoveHeld(Vector3 world,Func<bool> done,string label)
        {
            var from=pointer;pointer=ScreenTarget(world);
            for(int i=1;i<=12;i++){InputSystem.QueueStateEvent(touch,new TouchState{touchId=finger,phase=UnityEngine.InputSystem.TouchPhase.Moved,position=Vector2.Lerp(from,pointer,i/12f)});yield return new WaitForSecondsRealtime(.03f);}
            float until=Time.realtimeSinceStartup+12;
            while(!done()&&Time.realtimeSinceStartup<until)yield return null;
            if(!done())rows.Add("Pipette state: "+driver.SaveState()+" tip="+item.GetComponentsInChildren<Transform>().First(t=>t.name.EndsWith("_TipOutlet_SOCKET",StringComparison.Ordinal)).position+" pointer="+pointer);
            Check(label,done());Check("C05 pipette gesture retains camera orientation",Quaternion.Angle(camera,App.View.transform.rotation)<.1f);
        }
        IEnumerator Release()
        {
            InputSystem.QueueStateEvent(touch,new TouchState{touchId=finger,phase=UnityEngine.InputSystem.TouchPhase.Ended,position=pointer});yield return null;yield return new WaitForSecondsRealtime(.2f);
            Check("C05 release closes valve and releases both instruments",bulb.Control==0&&item.Owner==EquipmentGrabOwner.None&&bulbItem.Owner==EquipmentGrabOwner.None);
            Check("C05 release parks the actual loaded/empty pipette in its clamp",driver.ValidatePipetteParking(out _));
        }
        yield return Hold(item);yield return MoveHeld(stock.transform.position,()=>pipette.VolumeMl>=4,"C05 phone pointer aspirates a partial rinse");yield return Release();
        double held=pipette.VolumeMl,source=stock.Liquid.VolumeMl;yield return new WaitForSecondsRealtime(.3f);
        Check("C05 released partial pipette keeps its actual contents",held>0&&held<10&&pipette.VolumeMl==held&&stock.Liquid.VolumeMl==source&&App.Session.Operation==0);
        var saved=JsonUtility.FromJson<ChemistryDriver.Draft>(App.Session.Checkpoint().detailJson);
        Check("C05 phone partial load checkpoint matches accepted liquid",Math.Abs(saved.dilution.pipette-held)<1e-8&&saved.samples.Count==0);yield return Capture("C05-pipette-partial-held");
        yield return Hold(bulbItem);yield return MoveHeld(stock.transform.position,()=>pipette.VolumeMl>=10-1e-8,"C05 regrab completes finite 10 mL rinse aspiration");
        yield return MoveHeld(waste.transform.position,()=>App.Session.Operation==1,"C05 actual waste delivery completes rinse preparation");yield return Capture("C05-pipette-rinsed");
        yield return MoveHeld(stock.transform.position,()=>pipette.VolumeMl>=10-1e-8,"C05 phone pointer draws finite aliquot");
        yield return MoveHeld(flask.transform.position,()=>App.Session.Operation==2,"C05 actual flask delivery completes aliquot preparation");yield return Release();
        Check("C05 direct pipette sequence conserves 120 mL stock",Math.Abs(stock.Liquid.VolumeMl-100)<1e-8&&Math.Abs(waste.VolumeMl-10)<1e-8&&Math.Abs(flask.Liquid.VolumeMl-10)<1e-8&&pipette.VolumeMl==0);
        Check("C05 direct sequence records no assisted transfer or finished sample",string.IsNullOrEmpty(App.Session.Assistance)&&!App.Profile.attempts.Any(a=>a.experimentId=="C05"&&!a.partial));yield return Capture("C05-pipette-aliquot-delivered");
        var washItem=rig.items.Single(i=>i.assetId=="wash");var wash=washItem.GetComponentInChildren<VlabWashBottle>();pointer=ModelPoint(washItem);finger=++serial;
        InputSystem.QueueStateEvent(touch,new TouchState{touchId=finger,phase=UnityEngine.InputSystem.TouchPhase.Began,position=pointer});yield return null;yield return new WaitForSecondsRealtime(.15f);
        Check("C05 manual pipette unlocks direct wash gesture",washItem.Owner==EquipmentGrabOwner.Flagship);
        var neck=flask.GetComponentsInChildren<Transform>().Single(t=>t.name.EndsWith("_NeckSocket_SOCKET",StringComparison.Ordinal));var waterTarget=(Vector2)App.View.WorldToScreenPoint(new Vector3(neck.position.x,wash.Outlet.y,neck.position.z));
        InputSystem.QueueStateEvent(touch,new TouchState{touchId=finger,phase=UnityEngine.InputSystem.TouchPhase.Moved,position=waterTarget});float waterUntil=Time.realtimeSinceStartup+35;
        while(driver.CanManualWash&&Time.realtimeSinceStartup<waterUntil)yield return null;
        InputSystem.QueueStateEvent(touch,new TouchState{touchId=finger,phase=UnityEngine.InputSystem.TouchPhase.Ended,position=waterTarget});yield return null;yield return new WaitForSecondsRealtime(.2f);
        Check("C05 phone wash reaches actual calibration band",driver.Controls("vol50").Any(c=>c.id=="invert"&&c.enabled)&&!wash.Stream.Flowing);
        Check("C05 phone dilution preserves solute and finite water",Math.Abs(driver.Observe().Get("solute")-.010)<1e-10&&Math.Abs(flask.Liquid.VolumeMl+wash.RemainingMl-260)<1e-7);
        yield return Capture("C05-pipette-and-wash-complete");
        var show=App.GetComponentsInChildren<UnityEngine.UI.Button>().FirstOrDefault(b=>b.GetComponentInChildren<TMPro.TMP_Text>()?.text=="Hiện UI"||b.GetComponentInChildren<TMPro.TMP_Text>()?.text=="Show UI");if(show)yield return Tap(show.name);
    }
}
#endif
