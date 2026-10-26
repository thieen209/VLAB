#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Vlab.Product;

public sealed partial class WorkbenchUpgradeProbe
{
    IEnumerator TouchEquipment(EquipmentHandle item)
    {
        var point=ModelPoint(item);int finger=++serial;
        InputSystem.QueueStateEvent(touch,new TouchState{touchId=finger,phase=UnityEngine.InputSystem.TouchPhase.Began,position=point});
        yield return null;yield return new WaitForSecondsRealtime(.12f);
        InputSystem.QueueStateEvent(touch,new TouchState{touchId=finger,phase=UnityEngine.InputSystem.TouchPhase.Ended,position=point});
        yield return new WaitForSecondsRealtime(.4f);
    }
    IEnumerator EquipmentPlacementChecks()
    {
        yield return Wait("home");if(Find("Skip quick tour"))yield return Tap("Skip quick tour");
        foreach(string id in new[]{"P02","C01","B07","M02"})
        {
            Check("Viewport for "+id,SecondTestGameView.Configure(out _,id=="P02"?1280:2340,id=="P02"?720:1080));
            App.ShowHome();yield return null;yield return Tap("Subject "+id[0]);yield return Wait("browser");
            yield return Tap("Browser lesson "+id);yield return Tap("Browser start");yield return Wait("readiness");yield return Tap("Readiness enter");yield return Wait("activity");
            var rig=App.Apparatus;var item=rig.NextInstallableItem();
            yield return TouchEquipment(item);
            Check(id+" touching loose model opens placement controls",Read<EquipmentHandle>("workbenchItem")==item&&Find("Equipment place selected"));
            Check(id+" tap keeps model held after release",rig.HasLatchedCarry&&item.Owner==EquipmentGrabOwner.Legacy);
            Check(id+" explicit put-down label",Find("Equipment place selected").GetComponentInChildren<TextMeshProUGUI>().text=="Đặt xuống");
            yield return Capture(id+"-holding-clear-actions");
            yield return Tap("Equipment take selected");
            Check(id+" return restores tray and releases ownership",!rig.IsCarrying&&item.Owner==EquipmentGrabOwner.None&&!item.installed&&(item.stowed||Vector3.Distance(item.transform.position,item.trayPosition)<.001f));
            yield return Tap("Equipment take selected");
            yield return Tap("Equipment place selected");
            Check(id+" put down fits and releases without completing procedure",item.installed&&item.Owner==EquipmentGrabOwner.None&&!rig.IsCarrying&&App.Session.Operation==0);
            yield return Tap("Close equipment controls");
            if(id=="C01")
            {
                var clamp=rig.items.First(x=>rig.InstallSupport(x)==item);
                // Developer setup fixture removes the support, then recovery uses real UI taps.
                Check("Fixture can remove unused stand",rig.ReturnToTray(item));
                Call("ChooseWorkbenchItem",clamp,true);yield return new WaitForSecondsRealtime(.4f);
                Check("Unsupported placement is disabled",!Find("Equipment place selected")&&rig.HasLatchedCarry);
                Check("Missing support is explained",Read<TextMeshProUGUI>("toolState").text.Contains(rig.EquipmentLabel(item,true)));
                yield return Tap("Equipment take selected");Check("Wrong-order pickup can be returned",!rig.IsCarrying&&clamp.Owner==EquipmentGrabOwner.None&&!clamp.installed);
                App.Profile.settings.hints=false;Call("ApplySettings");Call("ChooseWorkbenchItem",item,true);yield return new WaitForSecondsRealtime(.4f);
                Check("Hint-off still shows essential placement target",Read<GuideCue>("worldGuideCue")&&!Read<GuideCue>("worldGuideCue").IsStopped);
                yield return new WaitForSecondsRealtime(13);
                Check("Held placement target does not time out",Read<GuideCue>("worldGuideCue")&&!Read<GuideCue>("worldGuideCue").IsStopped);
                yield return Tap("Close equipment controls");Check("Close safely returns held item",!rig.IsCarrying&&item.Owner==EquipmentGrabOwner.None);
                App.Profile.settings.hints=true;Call("ApplySettings");
                // Installed apparatus fixture isolates inspect/return; no measurement is fabricated.
                for(int n=0;n<40&&!rig.Ready;n++){rig.Select(rig.NextInstallableItem());Check("Inspection fixture fits apparatus",rig.InstallSelected());}
                var vessel=rig.items.First(rig.CanLiftForInspection);var rest=vessel.transform.position;var rotation=vessel.transform.rotation;
                Call("SelectWorkbenchTool",vessel,"");yield return new WaitForSecondsRealtime(.4f);yield return Tap("Lift equipment");
                Check("Inspection lifts the actual vessel",rig.InspectingInstalled&&Vector3.Distance(rest,vessel.transform.position)>.05f);
                yield return Capture("C01-inspection-put-down");vessel.transform.position=rest+Vector3.up*.2f;yield return Tap("Return inspected equipment");
                Check("Inspection put-down settles onto supported bench",!rig.IsCarrying&&vessel.Owner==EquipmentGrabOwner.None&&Vector2.Distance(new Vector2(rest.x,rest.z),new Vector2(vessel.transform.position.x,vessel.transform.position.z))<.001f);
                rest=vessel.transform.position;
                yield return Tap("Lift equipment");Call("ControllerBack");yield return null;
                Check("Back puts down before leaving activity",App.Route=="activity"&&!rig.IsCarrying&&Vector3.Distance(rest,vessel.transform.position)<.001f);
            }
        }
        rows.Add("Editor touchscreen input on four lab setups; not physical Android/BLE/headset acceptance.");
    }
}
#endif
