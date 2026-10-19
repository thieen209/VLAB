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

public static class UiMotionChecks
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static Button Find(VlabApp app,params string[] names)=>app.GetComponentsInChildren<Button>().Single(button=>button.isActiveAndEnabled&&(names.Contains(button.name)||names.Contains(button.GetComponentInChildren<TextMeshProUGUI>()?.text)));
    static IEnumerator Press(VlabApp app,params string[] names)
    {yield return new WaitForSecondsRealtime(.28f);var button=Find(app,names);if(!button.interactable)throw new InvalidOperationException("Animation settings target disabled: "+button.name);button.onClick.Invoke();yield return new WaitForSecondsRealtime(.28f);}
    static void Call(VlabApp app,string name,params object[] args)=>typeof(VlabApp).GetMethod(name,Private).Invoke(app,args);
    static GameSettings Draft(VlabApp app)=>(GameSettings)typeof(VlabApp).GetField("draft",Private).GetValue(app);
    static string AnimationCaption(bool vi,bool reduced)=>(vi?"Hiệu ứng chuyển động: ":"UI animation: ")+(vi?(reduced?"Tắt":"Bật"):(reduced?"Off":"On"));
    public static IEnumerator RunSettings(VlabApp app,Action<string,bool> check)
    {
        string folder=SessionState.GetString("VLAB.TestProfile","");
        if(string.IsNullOrEmpty(folder))throw new InvalidOperationException("Animation settings checks require an isolated test profile.");
        string original=JsonUtility.ToJson(app.Profile),locale=app.Profile.locale;var settings=app.Profile.settings.Copy();
        var ui=(VlabUi)typeof(VlabApp).GetField("ui",Private).GetValue(app);
        check("Animation settings review starts at Home",app.Route=="home");
        try
        {
            foreach(bool vi in new[]{true,false})
            {
                yield return Press(app,"Home utility 0");
                typeof(VlabApp).GetField("draftLocale",Private).SetValue(app,vi?"vi":"en");Call(app,"ShowSettings");yield return new WaitForSecondsRealtime(.28f);
                check("Animation On/Off caption inversely represents saved reduced motion "+(vi?"VI":"EN"),Find(app,"Settings UI animation").GetComponentInChildren<TextMeshProUGUI>().text==AnimationCaption(vi,settings.reducedMotion));
                yield return Press(app,"Settings UI animation");
                check("Animation switch updates only the unapplied draft "+(vi?"VI":"EN"),Draft(app).reducedMotion!=settings.reducedMotion&&JsonUtility.ToJson(app.Profile)==original&&ui.ReducedMotion==settings.reducedMotion);
                check("Animation switch caption follows the edited draft "+(vi?"VI":"EN"),Find(app,"Settings UI animation").GetComponentInChildren<TextMeshProUGUI>().text==AnimationCaption(vi,!settings.reducedMotion));
                yield return Press(app,"Hủy","Cancel");
                check("Cancel preserves all learner fields and saved animation state "+(vi?"VI":"EN"),app.Route=="home"&&JsonUtility.ToJson(app.Profile)==original&&ui.ReducedMotion==settings.reducedMotion);
            }
            yield return Press(app,"Home utility 0");yield return Press(app,"Settings UI animation");yield return Press(app,"Settings apply");
            var expected=JsonUtility.FromJson<LocalProfile>(original);expected.settings.reducedMotion=!settings.reducedMotion;
            var reopened=new ProfileStore(folder).Load();
            check("Apply changes the live UI animation state and no unrelated learner field",app.Route=="home"&&ui.ReducedMotion==!settings.reducedMotion&&JsonUtility.ToJson(app.Profile)==JsonUtility.ToJson(expected));
            check("Applied animation state reopens from native local storage",!reopened.writeProtected&&JsonUtility.ToJson(reopened.profile)==JsonUtility.ToJson(expected));
            yield return Press(app,"Home utility 0");
            check("Reopened Settings displays the applied On/Off state",Find(app,"Settings UI animation").GetComponentInChildren<TextMeshProUGUI>().text==AnimationCaption(locale!="en",!settings.reducedMotion));
            yield return Press(app,"Settings UI animation");yield return Press(app,"Settings apply");
            reopened=new ProfileStore(folder).Load();
            check("Actual toggle-back and Apply restore every preference, note, legal acceptance and scientific record",app.Route=="home"&&ui.ReducedMotion==settings.reducedMotion&&JsonUtility.ToJson(app.Profile)==original&&!reopened.writeProtected&&JsonUtility.ToJson(reopened.profile)==original);
        }
        finally
        {
            // Restore test preferences even if an assertion or missing target aborts the journey.
            if(JsonUtility.ToJson(app.Profile.settings)!=JsonUtility.ToJson(settings)||app.Profile.locale!=locale)
            {
                Call(app,"OpenSettings","home");typeof(VlabApp).GetField("draft",Private).SetValue(app,settings.Copy());typeof(VlabApp).GetField("draftLocale",Private).SetValue(app,locale);Call(app,"ApplyDraft");
            }
        }
    }
    public static IEnumerator Run(Action<string,bool> check)
    {
        var root=new GameObject("UI motion checks");float previousScale=Time.timeScale;
        try
        {
            var ui=new VlabUi(root.transform);var page=ui.Page("Motion page");
            var panel=ui.Panel(page,"Motion panel",100,100,600,400);
            int clicks=0;var button=ui.Button(panel,"Continue",40,40,250,72,()=>clicks++);
            var caption=button.GetComponentInChildren<TMPro.TextMeshProUGUI>().rectTransform;
            var position=panel.anchoredPosition;var buttonPosition=((RectTransform)button.transform).anchoredPosition;
            var group=panel.GetComponent<CanvasGroup>();
            check("Panel entrance fades presentation without blocking interaction",group&&group.alpha==0&&group.blocksRaycasts&&group.interactable);
            Time.timeScale=0;yield return new WaitForSecondsRealtime(.24f);
            check("Panel entrance completes while lesson time is paused",group.alpha==1&&panel.anchoredPosition==position);
            var rest=caption.localScale;var restPosition=caption.anchoredPosition;var feedback=button.GetComponent<UiButtonMotion>();
            feedback.OnPointerEnter(null);yield return new WaitForSecondsRealtime(.14f);
            check("Hover eases caption without moving button target",caption.localScale.x>rest.x&&((RectTransform)button.transform).anchoredPosition==buttonPosition&&button.transform.localScale==Vector3.one);
            feedback.OnPointerDown(null);yield return new WaitForSecondsRealtime(.14f);
            check("Press feedback settles at restrained caption scale",Mathf.Abs(caption.localScale.x-rest.x*.975f)<.0001f);
            button.onClick.Invoke();check("Motion leaves the original Button callback intact",clicks==1);
            ui.SetReducedMotion(true);
            check("Turning animations off immediately restores exact caption geometry",caption.localScale==rest&&caption.anchoredPosition==restPosition&&button.colors.fadeDuration==0);
            panel.gameObject.SetActive(false);panel.gameObject.SetActive(true);
            check("Disabled animations never replay entrance on reopen",group.alpha==1);
            feedback.OnPointerEnter(null);feedback.OnPointerDown(null);yield return new WaitForSecondsRealtime(.14f);
            check("Disabled animations keep hover and press stationary",caption.localScale==rest&&caption.anchoredPosition==restPosition);
            ui.SetReducedMotion(false);feedback.OnPointerUp(null);feedback.OnPointerEnter(null);yield return new WaitForSecondsRealtime(.14f);
            button.gameObject.SetActive(false);
            check("Disabling a focused control restores exact visual state",caption.localScale==rest&&caption.anchoredPosition==restPosition);
            button.gameObject.SetActive(true);button.onClick.Invoke();check("Reopened control still activates once",clicks==2);
            panel.gameObject.SetActive(false);panel.gameObject.SetActive(true);
            check("Re-enabling animations replays entrance without changing hit geometry",group.alpha==0&&panel.anchoredPosition==position);
            ui.SetReducedMotion(true);check("Disabling animations mid-entrance snaps fully visible",group.alpha==1);
        }
        finally{Time.timeScale=previousScale;UnityEngine.Object.Destroy(root);}
        yield return null;
    }
}
#endif
