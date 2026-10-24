#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Vlab.Product;

public sealed partial class WorkbenchUpgradeProbe
{
    IEnumerator ScrollSetting(string name)
    {
        var button=Find(name);var scroll=button.GetComponentInParent<ScrollRect>();
        for(int attempt=0;attempt<4;attempt++)
        {
            var corners=new Vector3[4];scroll.viewport.GetWorldCorners(corners);var point=RectTransformUtility.WorldToScreenPoint(null,button.transform.position);
            if(point.y>corners[0].y+50&&point.y<corners[2].y-50)yield break;
            Vector2 from=new Vector2(corners[0].x+(corners[2].x-corners[0].x)*.65f,corners[0].y+(corners[2].y-corners[0].y)*.25f),to=from+Vector2.up*(corners[2].y-corners[0].y)*.6f;
            int id=++serial;UnityEngine.InputSystem.InputSystem.QueueStateEvent(touch,new UnityEngine.InputSystem.LowLevel.TouchState{touchId=id,phase=UnityEngine.InputSystem.TouchPhase.Began,position=from});yield return null;
            for(int n=1;n<=10;n++){UnityEngine.InputSystem.InputSystem.QueueStateEvent(touch,new UnityEngine.InputSystem.LowLevel.TouchState{touchId=id,phase=UnityEngine.InputSystem.TouchPhase.Moved,position=Vector2.Lerp(from,to,n/10f)});yield return new WaitForSecondsRealtime(.04f);}
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(touch,new UnityEngine.InputSystem.LowLevel.TouchState{touchId=id,phase=UnityEngine.InputSystem.TouchPhase.Ended,position=to});yield return new WaitForSecondsRealtime(.35f);
        }
    }
    IEnumerator DesignChecks()
    {
        var ui=Read<VlabUi>("ui");
        foreach(string room in new[]{"P","C","B","M"})
        {
            App.ShowHome();yield return new WaitForSecondsRealtime(.25f);yield return Tap("Subject "+room);yield return Wait("browser");
            foreach(string id in LessonSpec.All.Keys.Where(id=>id.StartsWith(room)).OrderBy(id=>id))
            {
                yield return Tap("Browser lesson "+id);
                var photo=App.GetComponentsInChildren<RawImage>().FirstOrDefault(x=>x.name=="Lesson photograph "+id);
                Check(id+" browser shows its actual apparatus photograph",photo&&photo.texture&&photo.texture==Resources.Load<Texture2D>("UI/Lessons/"+id));
                var layout=App.GetComponentInChildren<PhoneBrowserLayout>();Check(id+" browse stays inside safe viewport",layout&&layout.AssertPhoneLayout(out _));
            }
            yield return Tap("Browser lesson "+room+"01");yield return Capture("design-"+room+"-browser");
        }
        foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(2340,1080),new Vector2Int(2048,1536),new Vector2Int(1080,2340)})
        {
            SecondTestGameView.Configure(out _,size.x,size.y);yield return new WaitForSecondsRealtime(.5f);App.ShowBrowser();yield return new WaitForSecondsRealtime(.4f);
            var layout=App.GetComponentInChildren<PhoneBrowserLayout>();Check("Browser reflows at "+size,layout.AssertPhoneLayout(out _));yield return Capture("design-browser-"+size.x+"x"+size.y);
            yield return Tap("Browser start");yield return Wait("readiness");yield return Tap("Readiness back");yield return Wait("browser");
        }
        SecondTestGameView.Configure(out _,1600,900);yield return new WaitForSecondsRealtime(.4f);
        ui.PhoneViewport.SetViewportOverride(new Vector2(1600,900),new Rect(70,20,1510,860));App.ShowBrowser();yield return new WaitForSecondsRealtime(.4f);
        Check("Asymmetric notch browser safe",App.GetComponentInChildren<PhoneBrowserLayout>().AssertPhoneLayout(out _));ui.PhoneViewport.ClearViewportOverride();
        App.ShowHome();yield return new WaitForSecondsRealtime(.3f);yield return Capture("design-home");
        Call("OpenSettings","home");yield return new WaitForSecondsRealtime(.3f);yield return Tap("Hint level");Check("Detailed hint selection changes draft",Read<GameSettings>("draft").hintLevel==1);yield return Tap("Settings apply");
        Check("Hint level saved and applied",App.Profile.settings.hintLevel==1);var store=Read<ProfileStore>("store");Check("Hint level survives reopen",new ProfileStore(store.Folder).Load().profile.settings.hintLevel==1);
        Call("OpenSettings","home");typeof(VlabApp).GetField("settingsTab",Private).SetValue(App,2);Call("ShowSettings");yield return new WaitForSecondsRealtime(.3f);yield return Capture("design-settings-graphics");
        yield return ScrollSetting("VR clarity");yield return Capture("design-settings-vr-clarity");yield return Tap("VR clarity");Check("VR clarity draft independent from graphics",Read<GameSettings>("draft").vrResolution==1&&Read<GameSettings>("draft").GraphicsTier==App.Profile.settings.GraphicsTier);yield return Tap("Settings apply");
        Check("VR clarity saved",new ProfileStore(store.Folder).Load().profile.settings.vrResolution==1);
        var pipeline=Read<UniversalRenderPipelineAsset>("runtimePipeline");App.Input.SetSimulator(true);yield return new WaitForSecondsRealtime(.6f);
        Check("VR economy applies real render scale",Mathf.Abs(pipeline.renderScale-.85f)<.001f);yield return Capture("design-world-home");
        App.Profile.settings.vrResolution=2;Call("ApplySettings");Check("VR clear applies native scale",Mathf.Abs(pipeline.renderScale-1)<.001f);
        App.Input.SetSimulator(false);yield return new WaitForSecondsRealtime(.6f);Check("Phone restores tier resolution",Mathf.Abs(pipeline.renderScale-VlabGraphics.Resolution(App.Profile.settings.GraphicsTier,false,2))<.001f);
        foreach(int tier in new[]{0,1,2,3})foreach(int resolution in new[]{0,1,2})Check("Resolution range "+tier+"/"+resolution,VlabGraphics.Resolution(tier,true,resolution)>=.85f&&VlabGraphics.Resolution(tier,true,resolution)<=1);
        yield return UiMotionChecks.Run(Check);
        for(int theme=0;theme<5;theme++)
        {
            Call("OpenSettings","home");typeof(VlabApp).GetField("settingsTab",Private).SetValue(App,4);Call("ShowSettings");yield return new WaitForSecondsRealtime(.3f);yield return Tap("Appearance theme "+theme);yield return Tap("Settings apply");
            Check("Theme "+theme+" saved and applied",App.Profile.settings.uiTheme==theme&&UiTheme.Index==theme&&new ProfileStore(store.Folder).Load().profile.settings.uiTheme==theme);
            Check("Theme "+theme+" text contrast",UiTheme.Contrast(UiTheme.Current.text,UiTheme.Current.panel)>=4.5f&&UiTheme.Contrast(UiTheme.Current.secondary,UiTheme.Current.panel)>=4.5f);
        }
        Check("New profiles default to Dark",new GameSettings().uiTheme==0);App.Profile.settings.uiTheme=0;Call("ApplySettings");App.ShowHome();yield return new WaitForSecondsRealtime(.3f);
        var circles=App.GetComponentsInChildren<FrostedMenuDisc>();Check("Five circular blurred utilities",circles.Length==5&&circles.All(c=>c.GetComponent<RawImage>().enabled&&c.GetComponent<RawImage>().material.shader.isSupported&&c.GetComponent<RawImage>().texture==App.Preview.CurrentTexture));
        Check("No rectangular utility plates",App.GetComponentsInChildren<Button>().Where(b=>b.name.StartsWith("Home utility")||b.name=="Open journal").All(b=>b.GetComponent<Image>().color.a==0));yield return Capture("design-home-final");
        var invalid=App.Profile.settings.Copy();invalid.vrResolution=3;bool rejected=false;try{invalid.Validate();}catch(System.IO.InvalidDataException){rejected=true;}Check("Corrupt resolution rejected",rejected);
        foreach(string locale in new[]{"en","vi"})
        {
            App.Profile.locale=locale;App.Profile.settings.largeText=true;App.Profile.settings.reducedMotion=true;Call("ApplySettings");App.ShowHome();yield return new WaitForSecondsRealtime(.3f);yield return Tap("Subject C");yield return Wait("browser");
            Check(locale+" large text responsive browser",App.GetComponentInChildren<PhoneBrowserLayout>().AssertPhoneLayout(out _));yield return Capture("design-browser-"+locale+"-large");
            var title=App.GetComponentsInChildren<TMP_Text>().First(x=>x.name=="Browser selected title");Check(locale+" title is not truncated",!title.isTextTruncated);
        }
        App.Profile.settings.largeText=false;App.Profile.settings.reducedMotion=false;App.Profile.settings.hintLevel=0;App.Profile.settings.vrResolution=0;Call("ApplySettings");App.ShowHome();
    }
}
#endif
