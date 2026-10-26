#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Vlab.Product;

public sealed partial class WorkbenchUpgradeProbe
{
    string ExploreLabel(string vi,string en)=>App.Profile.locale=="en"?en:vi;
    void ExploreNavigationState(string stage,Button button=null)
    {
        var ui=Read<VlabUi>("ui");var modal=Read<RectTransform>("modal");var page=Read<RectTransform>("page");
        var module=App.GetComponentInChildren<InputSystemUIInputModule>();
        rows.Add("EXPLORE "+stage+" route="+App.Route+" changing="+Read<bool>("changing")+" guardRemaining="+(Read<float>("inputAfter")-Time.unscaledTime).ToString("F3")+" modal="+(modal?modal.name:"none")+" pageActive="+(page&&page.gameObject.activeInHierarchy)+" canvas="+ui.canvas.renderMode+" nativeModule="+(module&&module.enabled)+" gameplay="+App.Input.Gameplay+" eye="+App.View.transform.position);
        if(!button)return;
        var rect=(RectTransform)button.transform;var point=rect.TransformPoint(rect.rect.center);var canvas=button.GetComponentInParent<Canvas>();
        Vector2 pixel=RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,point);
        var native=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=pixel},native);
        var ray=canvas.renderMode==RenderMode.ScreenSpaceOverlay?App.View.ScreenPointToRay(pixel):new Ray(App.View.transform.position,point-App.View.transform.position);
        Call("PointerHitsUi",ray);var filtered=Read<List<RaycastResult>>("uiHits");
        rows.Add("EXPLORE "+stage+" button="+button.name+" pixel="+pixel+" native="+string.Join(",",native.Select(h=>h.gameObject.name))+" app="+string.Join(",",filtered.Select(h=>h.gameObject.name))+" blockers="+string.Join(",",Physics.RaycastAll(ray,Vector3.Distance(ray.origin,point),Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance).Select(h=>h.collider.name+"@"+h.distance.ToString("F3"))));
    }
    IEnumerator ExploreSelect(string name,bool world)
    {
        yield return new WaitForSecondsRealtime(.3f);var button=Find(name);Check("Explore control exists: "+name,button);
        ExploreNavigationState("before "+name,button);
        if(!world){yield return Tap(name);yield break;}
        Canvas.ForceUpdateCanvases();var rect=(RectTransform)button.transform;var point=rect.TransformPoint(rect.rect.center);
        var ray=new Ray(App.View.transform.position,point-App.View.transform.position);Call("PointerHitsUi",ray);
        var hits=Read<List<RaycastResult>>("uiHits");
        Check("Explore world ray reaches actual "+name,hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==button);
        bool delivered=false;button.onClick.AddListener(()=>delivered=true);
        var calibration=(Quaternion)typeof(MockControllerProvider).GetField("calibration",Private).GetValue(App.Input.Controller);
        var orientation=Quaternion.Inverse(calibration)*Quaternion.LookRotation(ray.direction);
        foreach(bool pressed in new[]{false,true,false})
        {
            Check("Explore controller edge accepted",App.Input.Controller.Submit(new ControllerSignal{sequence=(long)(Time.realtimeSinceStartupAsDouble*1000000),receivedAt=Time.realtimeSinceStartupAsDouble,orientation=orientation,select=pressed}));
            yield return null;yield return null;
        }
        yield return new WaitForSecondsRealtime(.35f);Check("Explore world click delivered "+name,delivered);
    }
    IEnumerator ExploreNavigationChecks()
    {
        yield return Wait("home");if(Find("Skip quick tour"))yield return Tap("Skip quick tour");
        bool priorDwell=App.Profile.settings.gazeAutoSelect;App.Profile.settings.gazeAutoSelect=false;
        try
        {
            App.Input.UseMockController=false;App.Input.SetSimulator(false);App.ShowHome();yield return new WaitForSecondsRealtime(.4f);
            yield return Tap("Subject P");yield return Tap("Browser lesson P01");yield return Tap("Browser start");yield return Tap("Readiness enter");yield return Wait("activity");
            yield return Tap("Station tools");yield return Tap(ExploreLabel("Dụng cụ","Equipment"));
            yield return Tap("Equipment item "+App.Apparatus.items.First().assetId);yield return Tap("Close equipment controls");
            yield return Tap("Station tools");yield return Tap(ExploreLabel("Thông số","Variables"));yield return Tap("Close dialog");
            yield return Tap(ExploreLabel("‹ Quay lại","‹ Back"));yield return Wait("pause");
            yield return Tap(ExploreLabel("Rời phòng","Leave laboratory"));yield return Tap(ExploreLabel("Xác nhận","Confirm"));yield return Wait("browser");
            foreach(bool world in new[]{false,true})
            {
                App.Input.SetSimulator(world);App.Input.UseMockController=world;yield return new WaitForSecondsRealtime(.5f);
                yield return ExploreSelect("Browser explore",world);yield return Wait("explore");
                Check("Explore clears the previous equipment reference",App.Session==null&&ReferenceEquals(App.Apparatus,null));
                var stations=App.GetComponentsInChildren<Button>().Where(b=>b.IsInteractable()&&(b.name.StartsWith("Trạm ")||b.name.StartsWith("Station "))).Select(b=>b.name).ToArray();
                int authoredStations=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Count(node=>node.name.StartsWith("Teleport_"));
                Check("Explore station controls match the actual room",stations.Length==authoredStations);
                if(stations.Length==0)rows.Add("EXPLORE room has no authored Teleport_ stations; continuing pause/settings/Back journey without inventing station controls.");
                foreach(string station in stations.Take(2))yield return ExploreSelect(station,world);
                // Menu is a real input edge; subsequent Settings and Back use the visible UI.
                if(world)
                {
                    App.Input.Controller.Submit(new ControllerSignal{sequence=(long)(Time.realtimeSinceStartupAsDouble*1000000),receivedAt=Time.realtimeSinceStartupAsDouble,orientation=Quaternion.identity,menu=true});yield return null;yield return null;
                    App.Input.Controller.Submit(new ControllerSignal{sequence=(long)(Time.realtimeSinceStartupAsDouble*1000000),receivedAt=Time.realtimeSinceStartupAsDouble,orientation=Quaternion.identity});
                }
                else
                {
                    var keyboard=UnityEngine.InputSystem.Keyboard.current;bool owned=keyboard==null;if(owned)keyboard=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>();
                    UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.Escape));yield return null;yield return null;
                    UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState());yield return null;if(owned)UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard);
                }
                yield return Wait("pause");yield return ExploreSelect(ExploreLabel("Cài đặt","Settings"),world);yield return Wait("settings");
                yield return ExploreSelect("Settings back",world);yield return Wait("pause");yield return ExploreSelect(ExploreLabel("Tiếp tục","Resume"),world);yield return Wait("explore");
                ExploreNavigationState(world?"world resumed":"phone resumed");yield return Capture(world?"explore-world-before-back":"explore-phone-before-back");
                var position=App.View.transform.position;var rotation=App.View.transform.rotation;
                yield return ExploreSelect(ExploreLabel("‹ Quay lại","‹ Back"),world);yield return Wait("browser");
                Check("Explore Back preserves learner head pose "+world,Vector3.Distance(position,App.View.transform.position)<.0001f&&Quaternion.Angle(rotation,App.View.transform.rotation)<.001f);
                yield return ExploreSelect("Browser explore",world);yield return Wait("explore");
                yield return ExploreSelect(ExploreLabel("‹ Quay lại","‹ Back"),world);yield return Wait("browser");
                Check("Repeated Explore entry and Back succeeds "+world,!Read<bool>("changing")&&!Read<RectTransform>("modal"));
            }
        }
        finally{App.Input.UseMockController=false;if(App.Input.SimulatorMode)App.Input.SetSimulator(false);App.Profile.settings.gazeAutoSelect=priorDwell;}
        App.ShowHome();yield return Wait("home");
    }
}
#endif
