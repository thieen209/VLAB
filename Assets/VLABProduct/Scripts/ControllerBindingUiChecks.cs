#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Vlab.Product;

public static class ControllerBindingUiChecks
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static Button Find(VlabApp app,params string[] names)=>app.GetComponentsInChildren<Button>().Single(button=>button.isActiveAndEnabled&&(names.Contains(button.name)||names.Contains(button.GetComponentInChildren<TextMeshProUGUI>()?.text)));
    static IEnumerator Press(VlabApp app,params string[] names){yield return new WaitForSecondsRealtime(.28f);Find(app,names).onClick.Invoke();yield return new WaitForSecondsRealtime(.28f);}
    static GameSettings Draft(VlabApp app)=>(GameSettings)typeof(VlabApp).GetField("draft",Private).GetValue(app);
    static void Call(VlabApp app,string method,params object[] args)=>typeof(VlabApp).GetMethod(method,Private).Invoke(app,args);
    public static IEnumerator Run(VlabApp app,Action<string> capture,Action<string,bool> check)
    {
        string original=JsonUtility.ToJson(app.Profile),locale=app.Profile.locale,attempts=string.Join("\n",app.Profile.attempts.Select(record=>JsonUtility.ToJson(record)));var settings=app.Profile.settings.Copy();
        yield return Press(app,"Home utility 0");yield return Press(app,"Điều khiển","Controls");yield return Press(app,"Controls button mapping");
        var ui=(VlabUi)typeof(VlabApp).GetField("ui",Private).GetValue(app);bool largeBefore=ui.LargeText;
        foreach(bool vi in new[]{true,false})foreach(bool large in new[]{false,true})
        {
            typeof(VlabApp).GetField("draftLocale",Private).SetValue(app,vi?"vi":"en");ui.Vietnamese=vi;ui.LargeText=large;Call(app,"ShowControllerBindings");yield return new WaitForSecondsRealtime(.28f);
            Fit(app,check,"mapping "+vi+"/"+large);
            check("Actual transparent controller illustration is loaded",app.GetComponentsInChildren<RawImage>().Any(image=>image.name=="Controller hand photograph"&&image.texture));
            foreach(int option in new[]{4,5,2,8,10,12}){var target=Find(app,"Controller physical "+option);check("Large physical button target available "+option,target.interactable&&target.GetComponent<RectTransform>().rect.height>=90);}
            if(vi&&!large)capture("73-gan-nut-tay-cam-VI");
            yield return Press(app,"Controller physical 8");Fit(app,check,"action picker "+vi+"/"+large);
            foreach(ControllerAction action in Enum.GetValues(typeof(ControllerAction)))check("Each configurable action is present: "+action,Find(app,"Controller assign "+action).interactable);
            yield return Press(app,"Controller diagram back");
            for(int chapter=0;chapter<3;chapter++)
            {
                Call(app,"ShowControllerHelp",chapter);yield return new WaitForSecondsRealtime(.28f);Fit(app,check,"controller help "+chapter+"/"+vi+"/"+large);
                var scroll=app.GetComponentsInChildren<VlabScrollContent>().Single();scroll.Refresh();
                check("Long controller instructions remain reachable through VR page controls",scroll.Up&&scroll.Down&&scroll.Body.rectTransform.rect.height>=scroll.Scroll.viewport.rect.height);
                if(vi&&!large&&chapter==2)capture("74-huong-dan-hieu-chinh-tay-cam-VI");
            }
        }
        ui.LargeText=largeBefore;ui.Vietnamese=locale!="en";typeof(VlabApp).GetField("draftLocale",Private).SetValue(app,locale);Call(app,"ShowControllerBindings");yield return new WaitForSecondsRealtime(.28f);
        yield return Press(app,"Controller hand right");yield return Press(app,"Controller angle side");
        check("Side angle exposes only trigger and grip",app.GetComponentsInChildren<Button>().Count(button=>button.name.StartsWith("Controller physical "))==2&&Find(app,"Controller physical 11").interactable&&Find(app,"Controller physical 9").interactable);
        capture("77-tay-cam-goc-nghieng-VI");yield return Press(app,"Controller hotspot 11");check("Direct image hotspot opens the function picker",Find(app,"Controller assign Select").interactable);yield return Press(app,"Controller diagram back");
        var state=app.GetComponentInChildren<ControllerDiagramState>();check("Returning from picker preserves hand and view",state.RightHand&&state.SideView);
        yield return Press(app,"Controller hand left");yield return Press(app,"Controller angle front");yield return Press(app,"Controller physical 8");yield return Press(app,"Controller assign Select");check("Mapping edits remain a conflict-free draft",!ControllerBindings.Equal(Draft(app),settings)&&JsonUtility.ToJson(app.Profile)==original);Draft(app).Validate();
        Call(app,"ControllerBack");yield return new WaitForSecondsRealtime(.28f);Call(app,"ControllerBack");yield return new WaitForSecondsRealtime(.28f);
        check("Controller Back requests a decision for unapplied button changes",app.GetComponentsInChildren<TextMeshProUGUI>().Any(text=>text.text=="Thay đổi chưa áp dụng"||text.text=="Unapplied changes"));
        yield return Press(app,"Ở lại","Stay");yield return Press(app,"Hủy","Cancel");check("Cancelling button edits preserves every stored learner field",JsonUtility.ToJson(app.Profile)==original);
        yield return Press(app,"Home utility 0");yield return Press(app,"Điều khiển","Controls");yield return Press(app,"Controls button mapping");yield return Press(app,"Controller physical 8");yield return Press(app,"Controller assign Select");var expected=Draft(app).Copy();
        yield return Press(app,"Quay lại cài đặt","Back to Settings");yield return Press(app,"Settings apply");
        var reopened=new ProfileStore(SessionState.GetString("VLAB.TestProfile","")).Load();
        check("Applied controller bindings reopen from the actual local profile",ControllerBindings.Equal(app.Profile.settings,expected)&&!reopened.writeProtected&&ControllerBindings.Equal(reopened.profile.settings,expected)&&reopened.profile.attempts.Count==app.Profile.attempts.Count);
        yield return Press(app,"Home utility 0");typeof(VlabApp).GetField("draft",Private).SetValue(app,settings);yield return Press(app,"Settings apply");
        check("Controller mapping review restores original preferences and all scientific records",JsonUtility.ToJson(app.Profile.settings)==JsonUtility.ToJson(settings)&&app.Profile.locale==locale&&string.Join("\n",app.Profile.attempts.Select(record=>JsonUtility.ToJson(record)))==attempts);
    }
    static void Fit(VlabApp app,Action<string,bool> check,string scope)
    {
        foreach(var text in app.GetComponentsInChildren<TextMeshProUGUI>().Where(text=>text.isActiveAndEnabled))
        {text.ForceMeshUpdate();check("Native controller text fits "+scope+" / "+text.name+" / "+text.text,!text.isTextOverflowing&&text.GetPreferredValues(text.text,text.rectTransform.rect.width,Mathf.Infinity).y<=text.rectTransform.rect.height+.75f);}
    }
}
#endif
