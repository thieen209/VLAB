#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Vlab.Product;

// Uses the real product pages and SceneManager operation. No simulated load completion.
public static class PlanFlowChecks
{
    static Button Find(params string[] choices)
    {
        var button=UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(b=>b.gameObject.activeInHierarchy&&b.IsInteractable()&&(choices.Contains(b.name)||choices.Contains(b.GetComponentInChildren<TextMeshProUGUI>()?.text)));
        if(!button)throw new InvalidOperationException("Missing actionable product button: "+string.Join(" / ",choices));return button;
    }
    static IEnumerator Press(params string[] choices)
    {yield return new WaitForSecondsRealtime(.3f);Find(choices).onClick.Invoke();yield return new WaitForSecondsRealtime(.3f);}
    static void CheckLayout(RectTransform root,string id,bool vi,Action<string,bool> check)
    {
        Canvas.ForceUpdateCanvases();var texts=root.GetComponentsInChildren<TextMeshProUGUI>();
        var title=texts.Single(t=>t.name=="Readiness title");var goal=texts.Single(t=>t.name=="Readiness goal");
        check("Readiness uses a natural lesson title",title.text==LessonGuide.ShortTitle(id,vi));
        check("Readiness has a concise selected-lesson goal",goal.text==ReadinessView.Goal(id,vi));
        check("Readiness never exposes experiment IDs",texts.All(t=>!System.Text.RegularExpressions.Regex.IsMatch(t.text,@"\b[PCBM]\d{2}\b")));
        check("Readiness has three illustrated setup tiles and no scrolling wall",root.GetComponentsInChildren<ScrollRect>().Length==0&&Enumerable.Range(1,3).All(i=>root.Find("Readiness setup "+i)));
        foreach(var text in texts)
        {
            text.ForceMeshUpdate(true,true);float needed=text.GetPreferredValues(text.text,text.rectTransform.rect.width,float.PositiveInfinity).y;
            check("Readiness text fits: "+text.name,text.rectTransform.rect.height+.75f>=needed&&!text.isTextOverflowing);
        }
    }
    public static IEnumerator Run(VlabApp app,Action<string> capture,Action<string,bool> check)
    {
        if(!app||app.Route!="home")throw new InvalidOperationException("Plan flow checks begin at committed Home in either language.");
        bool vi=app.Profile.locale!="en";
        var catalog=JsonUtility.FromJson<BriefCatalog>(Resources.Load<TextAsset>("Catalog/experiments").text);
        check("Readiness metadata covers all selected lessons",ReadinessView.ValidateMetadata().Length==0);
        foreach(var brief in catalog.items)
        {
            var spec=LessonSpec.All[brief.id];
            check(brief.id+" has concise bilingual readiness copy",ReadinessView.Goal(brief.id,true).Length>0&&ReadinessView.Goal(brief.id,false).Length>0);
            check(brief.id+" setup uses its actual first preparation",ReadinessView.Preparation(brief.id,true)==spec.operationsVi[0]&&ReadinessView.Preparation(brief.id,false)==spec.operationsEn[0]);
        }
        var portal=UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).First(b=>b.name=="Subject P"&&b.gameObject.activeInHierarchy);
        yield return new WaitForSecondsRealtime(.3f);portal.onClick.Invoke();yield return new WaitForSecondsRealtime(.7f);
        yield return Press("Browser start","Start experiment","Bắt đầu thí nghiệm");check("Lesson-specific readiness route opens",app.Route=="readiness");
        var readiness=UnityEngine.Object.FindObjectsByType<RectTransform>(FindObjectsSortMode.None).Single(t=>t.name=="Readiness view"&&t.gameObject.activeInHierarchy);
        CheckLayout(readiness,"P01",vi,check);Find("Readiness enter");Find("Readiness back");Find("Readiness help");
        capture?.Invoke(vi?"28-ready-pendulum-VI":"28-ready-pendulum-EN");
        int attempts=app.Profile.attempts.Count;string checkpoint=JsonUtility.ToJson(app.Profile.checkpoint);
        int activations=0;string activated=null;Action<Scene,LoadSceneMode> loaded=(scene,mode)=>{activations++;activated=scene.name;};
        UnityEngine.Events.UnityAction<Scene,LoadSceneMode> handler=(scene,mode)=>loaded(scene,mode);
        SceneManager.sceneLoaded+=handler;
        try
        {
            // Starts the same native loader as Enter station. Cancel command is the real UI command;
            // direct invocation avoids claiming that a frame-exact physical tap has been tested.
            var priorSession=app.Session;
            app.StartCoroutine(app.CheckRoomLoad("Physics"));
            check("Native loading screen exposes Cancel before creating a lesson",app.Route=="loading"&&ReferenceEquals(app.Session,priorSession)&&Find("Cancel and return","Hủy và quay lại"));
            app.CancelRoomEntry();app.StartCoroutine(app.CheckRoomLoad("Chemistry"));
            check("Cancellation communicates waiting for native IO",UnityEngine.Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None).Any(t=>t.gameObject.activeInHierarchy&&(t.text.Contains("Cancelling station entry")||t.text.Contains("Đang hủy vào trạm"))));
            while(app.Route=="loading")yield return null;
            check("Cancelled native load settles once and returns safely to browser",app.Route=="browser"&&activations==1&&activated=="Physics");
            check("Duplicate load cannot replace the active native request",SceneManager.GetActiveScene().name=="Physics");
            check("Cancelled entry creates no attempt and preserves the saved checkpoint",app.Session==null&&app.Profile.attempts.Count==attempts&&JsonUtility.ToJson(app.Profile.checkpoint)==checkpoint);
        }
        finally {SceneManager.sceneLoaded-=handler;}
        app.StartCoroutine(app.CheckRoomLoad("__VLAB_INTENTIONAL_MISSING_SCENE__"));yield return null;
        check("Unavailable scene has an explicit recoverable problem page",app.Route=="load-error"&&Find("Retry","Thử lại")&&Find("Back","Quay lại"));
        capture?.Invoke("29-room-load-recovery");
        yield return Press("Retry","Thử lại");check("Retry re-enters the actual missing-scene check without a lesson",app.Route=="load-error"&&app.Session==null);
        yield return Press("Back","Quay lại");check("Load-error Back retains the selected lesson",app.Route=="browser");
        yield return Press("Browser start","Start experiment","Bắt đầu thí nghiệm");
        check("Selection is retained without exposing a lesson code",app.Route=="readiness"&&UnityEngine.Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None).Any(t=>t.gameObject.activeInHierarchy&&t.name=="Readiness title"&&t.text==LessonGuide.ShortTitle("P01",vi)));
        yield return Press("Readiness back");yield return Press("Browser home","‹ Home","‹ Trang chủ");
        check("Plan-flow checks leave the product at Home without fabricating measurements",app.Route=="home"&&app.Profile.attempts.Count==attempts);
    }
}
#endif
