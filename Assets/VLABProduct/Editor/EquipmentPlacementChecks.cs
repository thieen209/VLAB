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
        if(item.stowed)
        {
            rows.Add(item.assetId+" is stored off the bench; use its actual picker and pickup buttons.");
            Call("ShowEquipment");yield return Tap("Equipment item "+item.assetId);yield return Tap("Equipment take selected");yield break;
        }
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
            Check(id+" fitting is distinct from putting down",Find("Equipment place selected").GetComponentInChildren<TextMeshProUGUI>().text=="Lắp vào vị trí");
            yield return Capture(id+"-holding-clear-actions");
            yield return Tap("Close equipment controls");
            Check(id+" return restores tray and releases ownership",!rig.IsCarrying&&item.Owner==EquipmentGrabOwner.None&&!item.installed&&(item.stowed||Vector3.Distance(item.transform.position,item.trayPosition)<.001f));
            yield return TouchEquipment(item);
            yield return Tap("Equipment place selected");
            Check(id+" fit action installs and releases without completing procedure",item.installed&&item.Owner==EquipmentGrabOwner.None&&!rig.IsCarrying&&App.Session.Operation==0);
            yield return Tap("Close equipment controls");
            if(id=="C01")
            {
                var clamp=rig.items.First(x=>rig.InstallSupport(x)==item);
                // Developer setup fixture removes the support, then recovery uses real UI taps.
                Check("Fixture can remove unused stand",rig.ReturnToTray(item));
                Call("ChooseWorkbenchItem",clamp,true);yield return new WaitForSecondsRealtime(.4f);
                Check("Unsupported placement is disabled",!Find("Equipment place selected")&&rig.HasLatchedCarry);
                Check("Missing support is explained",Read<TextMeshProUGUI>("toolState").text.Contains(rig.EquipmentLabel(item,true)));
                yield return Tap("Close equipment controls");Check("Wrong-order pickup can be returned",!rig.IsCarrying&&clamp.Owner==EquipmentGrabOwner.None&&!clamp.installed);
                App.Profile.settings.hints=false;Call("ApplySettings");Call("ChooseWorkbenchItem",item,true);yield return new WaitForSecondsRealtime(.4f);
                Check("Hint-off still shows essential placement target",Read<GuideCue>("worldGuideCue")&&!Read<GuideCue>("worldGuideCue").IsStopped);
                yield return new WaitForSecondsRealtime(13);
                Check("Held placement target does not time out",Read<GuideCue>("worldGuideCue")&&!Read<GuideCue>("worldGuideCue").IsStopped);
                yield return Tap("Close equipment controls");Check("Close safely returns held item",!rig.IsCarrying&&item.Owner==EquipmentGrabOwner.None);
                App.Profile.settings.hints=true;Call("ApplySettings");
                // Installed apparatus fixture isolates inspect/return; no measurement is fabricated.
                for(int n=0;n<40&&!rig.Ready;n++){rig.Select(rig.NextInstallableItem());Check("Inspection fixture fits apparatus",rig.InstallSelected());}
                // The titration flask sits around the burette outlet; use the unobstructed waste beaker for a vertical drop.
                var vessel=rig.items.Single(item=>item.assetId=="waste"&&rig.CanLiftForInspection(item));var rest=vessel.transform.position;var rotation=vessel.transform.rotation;
                Call("SelectWorkbenchTool",vessel,"");yield return new WaitForSecondsRealtime(.4f);yield return Tap("Lift equipment");
                Check("Inspection lifts the actual vessel",rig.InspectingInstalled&&Vector3.Distance(rest,vessel.transform.position)>.05f);
                yield return Capture("C01-inspection-put-down");
                // Supply fixture pose after pointer motion, immediately before the real button release.
                yield return Tap("Return inspected equipment",beforeRelease:()=>{vessel.transform.position=rest+Vector3.up*.2f;Physics.SyncTransforms();});
                Check("Inspection put-down settles onto supported bench",!rig.IsCarrying&&vessel.Owner==EquipmentGrabOwner.None&&Vector2.Distance(new Vector2(rest.x,rest.z),new Vector2(vessel.transform.position.x,vessel.transform.position.z))<.001f);
                rest=vessel.transform.position;
                yield return Tap("Lift equipment");Call("ControllerBack");yield return null;
                Check("Back puts down before leaving activity",App.Route=="activity"&&!rig.IsCarrying&&Vector3.Distance(rest,vessel.transform.position)<.001f);
            }
        }
        yield return LoosePutDownUiCheck();
        rows.Add("Editor touchscreen input on four lab setups; not physical Android/BLE/headset acceptance.");
    }
    IEnumerator LoosePutDownUiCheck()
    {
        App.ShowHome();yield return null;yield return Tap("Subject C");yield return Wait("browser");
        yield return Tap("Browser lesson C06");yield return Tap("Browser start");yield return Wait("readiness");yield return Tap("Readiness enter");yield return Wait("activity");
        var rig=App.Apparatus;var item=rig.items.Single(i=>i.assetId=="test");
        var original=item.trayPosition;var rotation=item.trayRotation;
        rows.Add("Loose release fixture supplies a clear raised pose; only pickup/put-down/cancel buttons use queued touchscreen input. This does not claim a hand-drag journey.");
        Call("ChooseWorkbenchItem",item,false);yield return null;yield return Tap("Equipment take selected");
        Check("Loose pickup exposes an explicit put-down action",rig.CanReleaseLoose&&Find("Equipment take selected").GetComponentInChildren<TextMeshProUGUI>().text=="Đặt xuống bàn");
        item.transform.SetPositionAndRotation(original+Vector3.up*.12f,rotation);Physics.SyncTransforms();
        yield return Tap("Equipment take selected",beforeRelease:()=>{item.transform.SetPositionAndRotation(original+Vector3.up*.12f,rotation);Physics.SyncTransforms();});
        Check("Put-down button releases without mounting or preparing",!rig.IsCarrying&&!item.installed&&item.Owner==EquipmentGrabOwner.None&&App.Session.Operation==0);
        var falling=item.GetComponent<ReleasedEquipment>();Check("Put-down button uses physical settling",falling);
        for(int n=0;n<150&&falling.IsSettling;n++)yield return new WaitForFixedUpdate();
        Check("Put-down button leaves the tool on its selected support",!falling.IsSettling&&Vector2.Distance(new Vector2(item.transform.position.x,item.transform.position.z),new Vector2(original.x,original.z))<.001f);
        yield return Capture("C06-loose-put-down");
        var placed=item.transform.position;yield return Tap("Equipment take selected");yield return Tap("Close equipment controls");
        Check("Cancel after reacquiring preserves the latest resting place",!rig.IsCarrying&&item.Owner==EquipmentGrabOwner.None&&Vector3.Distance(item.transform.position,placed)<.001f);
    }
    IEnumerator PhonePlacementJourney()
    {
        foreach(bool joystick in new[]{true,false})
        {
        App.Profile.settings.showPhoneJoystick=joystick;Call("ApplySettings");
        App.ShowHome();yield return null;yield return Tap("Subject C");yield return Wait("browser");
        yield return Tap("Browser lesson C06");yield return Tap("Browser start");yield return Wait("readiness");yield return Tap("Readiness enter");yield return Wait("activity");
        var rig=App.Apparatus;var item=rig.items.Single(i=>i.assetId=="test");var rest=item.trayPosition;
        Call("ChooseWorkbenchItem",item,false);yield return Tap("Equipment take selected");
        Check("Phone placement starts with actual held equipment",rig.CanReleaseLoose);
        var heldPosition=item.transform.position;yield return new WaitForSecondsRealtime(.5f);
        Check("Held equipment stays visible while finger is lifted; joystick="+joystick,Vector3.Distance(item.transform.position,heldPosition)<.001f);
        var target=App.View.WorldToScreenPoint(rest+Vector3.up*.08f);
        Check("Phone placement target is in view",target.z>0&&target.x>0&&target.x<Screen.width&&target.y>0&&target.y<Screen.height);
        yield return Drag(ModelPoint(item),target);
        Check("Phone drag reaches the chosen bench position without pose injection",Vector2.Distance(new Vector2(item.transform.position.x,item.transform.position.z),new Vector2(rest.x,rest.z))<.06f);
        heldPosition=item.transform.position;yield return new WaitForSecondsRealtime(.5f);
        Check("Finger release preserves dragged position",Vector3.Distance(item.transform.position,heldPosition)<.001f);
        yield return Tap("Equipment take selected");
        Check("Phone put-down releases after a real drag",!rig.IsCarrying&&!item.installed&&item.Owner==EquipmentGrabOwner.None);
        var falling=item.GetComponent<ReleasedEquipment>();Check("Phone put-down invokes physical settling",falling);
        for(int n=0;n<150&&falling.IsSettling;n++)yield return new WaitForFixedUpdate();
        Check("Phone drag and release reaches support",!falling.IsSettling&&Vector2.Distance(new Vector2(item.transform.position.x,item.transform.position.z),new Vector2(rest.x,rest.z))<.06f);
        yield return Capture("C06-phone-drag-release");
        }
    }
}
#endif
