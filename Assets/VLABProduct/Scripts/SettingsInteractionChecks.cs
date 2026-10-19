#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

namespace Vlab.Product
{
    // Native settings/persistence plus the exact shared runtime gaze-timer path.
    // Synthetic UI callbacks/timing do not establish physical headset behaviour.
    public static class SettingsInteractionChecks
    {
        [Serializable] sealed class Envelope {public int format=1;public string payload,sha256;}
        static void Verify(Action<string,bool> check,string message,bool passed)
        {check(message,passed);if(!passed)throw new InvalidOperationException(message);}
        static string Attempts(LocalProfile p)=>string.Join("\n",p.attempts.Select(x=>JsonUtility.ToJson(x)));
        static string Legal(LocalProfile p)=>string.Join("\n",p.legal.Select(x=>JsonUtility.ToJson(x)));
        static bool Same(float a,float b)=>Mathf.Abs(a-b)<.0001f;
        static void Write(string folder,string payload)
        {Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,"profile.json"),JsonUtility.ToJson(new Envelope{payload=payload,sha256=ProfileStore.Hash(payload)}));}
        public static void Core(LocalProfile original,Action<string,bool> check)
        {
            Verify(check,"New profiles use a softer adjustable80percent panel default",Same(new GameSettings().uiOpacity,.8f));
            string folder=Path.Combine(Path.GetTempPath(),"VlabInteraction-"+Guid.NewGuid().ToString("N"));
            var profile=JsonUtility.FromJson<LocalProfile>(JsonUtility.ToJson(original));string attempts=Attempts(profile),legal=Legal(profile);
            profile.settings.gazeAutoSelect=false;profile.settings.gazeDwellSeconds=1.7f;profile.settings.mouseLookSensitivity=1.6f;
            string payload=JsonUtility.ToJson(profile);
            foreach(string key in new[]{"gazeAutoSelect","gazeDwellSeconds","mouseLookSensitivity"})payload=Regex.Replace(payload,"\\\""+key+"\\\"\\s*:\\s*(?:true|false|[-+0-9.eE]+),?","").Replace(",}","}");
            try
            {
                Write(folder,payload);var legacy=new ProfileStore(folder).Load();
                Verify(check,"Missing interaction preferences receive defaults without schema change",!legacy.writeProtected&&!legacy.recovered&&legacy.profile.schemaVersion==2&&legacy.profile.settings.gazeAutoSelect&&Same(legacy.profile.settings.gazeDwellSeconds,1.2f)&&Same(legacy.profile.settings.mouseLookSensitivity,1));
                Verify(check,"Interaction migration retains all "+original.attempts.Count+" scientific records and consent",Attempts(legacy.profile)==attempts&&Legal(legacy.profile)==legal&&legacy.profile.locale==original.locale&&legacy.profile.safetyAcknowledged==original.safetyAcknowledged&&legacy.profile.notebookPages.Count==original.notebookPages.Count);
                var store=new ProfileStore(folder);Verify(check,"Customized interaction preferences save atomically",store.Save(profile,out _));var reopened=new ProfileStore(folder).Load();
                Verify(check,"Disabled auto-gaze and custom timing/sensitivity round trip",!reopened.writeProtected&&!reopened.profile.settings.gazeAutoSelect&&Same(reopened.profile.settings.gazeDwellSeconds,1.7f)&&Same(reopened.profile.settings.mouseLookSensitivity,1.6f)&&Attempts(reopened.profile)==attempts);
                profile.settings.uiOpacity=1;Verify(check,"An explicitly opaque existing choice still saves",store.Save(profile,out _));
                Verify(check,"New opacity default preserves explicit existing100percent choice",Same(new ProfileStore(folder).Load().profile.settings.uiOpacity,1));
                foreach(float duration in new[]{.6f,2f})foreach(float sensitivity in new[]{.5f,2f}){var settings=new GameSettings{gazeDwellSeconds=duration,mouseLookSensitivity=sensitivity};settings.Validate();}
                Verify(check,"Interaction boundary settings remain valid",true);
                foreach(float value in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity,0f,.59f,2.01f})
                {var settings=new GameSettings{gazeDwellSeconds=value};bool rejected=false;try{settings.Validate();}catch(InvalidDataException){rejected=true;}Verify(check,"Invalid dwell value is rejected: "+value,rejected);}
                foreach(float value in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity,0f,.49f,2.01f})
                {var settings=new GameSettings{mouseLookSensitivity=value};bool rejected=false;try{settings.Validate();}catch(InvalidDataException){rejected=true;}Verify(check,"Invalid mouse sensitivity is rejected: "+value,rejected);}
                string invalid=Regex.Replace(JsonUtility.ToJson(profile),"\"gazeDwellSeconds\"\\s*:\\s*[-+0-9.eE]+","\"gazeDwellSeconds\":0");string invalidFolder=Path.Combine(folder,"invalid");Write(invalidFolder,invalid);
                Verify(check,"Explicit invalid saved dwell is protected rather than migrated",new ProfileStore(invalidFolder).Load().writeProtected);
                float time=0;bool consumed=false;
                Verify(check,"Real gaze timer waits before minimum configured duration",!VlabApp.AdvanceGazeTimer(ref time,ref consumed,true,false,.6f,.59f));
                Verify(check,"Real gaze timer selects at configured duration",VlabApp.AdvanceGazeTimer(ref time,ref consumed,true,false,.6f,.02f));
                Verify(check,"Dwell never repeats on a stationary consumed target",!VlabApp.AdvanceGazeTimer(ref time,ref consumed,true,false,.6f,2));
                time=0;consumed=false;Verify(check,"Maximum dwell does not use hardcoded1.2s",!VlabApp.AdvanceGazeTimer(ref time,ref consumed,true,false,2,1.9f)&&VlabApp.AdvanceGazeTimer(ref time,ref consumed,true,false,2,.11f));
                Verify(check,"Disabled auto-gaze clears timing and never auto-selects",!VlabApp.AdvanceGazeTimer(ref time,ref consumed,false,false,.6f,10)&&time==0);
                Verify(check,"Manual trigger still selects with auto-gaze disabled",VlabApp.AdvanceGazeTimer(ref time,ref consumed,false,true,.6f,0));
            }
            finally {if(Directory.Exists(folder))Directory.Delete(folder,true);}
        }
        static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static GameSettings Draft(VlabApp app)=>(GameSettings)typeof(VlabApp).GetField("draft",Private).GetValue(app);
        static Button Find(VlabApp app,params string[] names)=>app.GetComponentsInChildren<Button>().FirstOrDefault(b=>b.isActiveAndEnabled&&b.interactable&&(names.Contains(b.name)||names.Contains(b.GetComponentInChildren<TextMeshProUGUI>()?.text)))??throw new InvalidOperationException("Settings button missing: "+string.Join(" / ",names));
        static IEnumerator Press(VlabApp app,params string[] names)
        {yield return new WaitForSecondsRealtime(.28f);Find(app,names).onClick.Invoke();yield return new WaitForSecondsRealtime(.28f);}
        static IEnumerator Open(VlabApp app)
        {yield return Press(app,"Home utility 0");yield return Press(app,"Settings interaction tab");}
        static void Layout(VlabApp app,Action<string,bool> check,string category)
        {
            Canvas.ForceUpdateCanvases();var box=app.GetComponentsInChildren<RectTransform>().Single(rect=>rect.gameObject.activeInHierarchy&&rect.name=="Settings");
            string locale=(string)typeof(VlabApp).GetField("draftLocale",Private).GetValue(app);
            string[] labels=locale=="en"?new[]{"General","Audio","Graphics","Controls","Appearance","Interaction"}:new[]{"Chung","Âm thanh","Đồ họa","Điều khiển","Giao diện","Tương tác"};
            var tabs=labels.Select(label=>Find(app,label)).Select(button=>button.GetComponent<RectTransform>()).ToArray();
            var rail=box.GetComponentsInChildren<RectTransform>().Single(rect=>rect.name=="Settings category rail");
            Verify(check,category+": six equal72px hit areas form one connected top rail",tabs.Length==6&&Mathf.Abs(rail.anchoredPosition.y+104)<.1f&&tabs.All(tab=>tab.parent==rail&&Mathf.Abs(tab.anchoredPosition.y)<.1f&&Same(tab.rect.height,72)&&Same(tab.rect.width,rail.rect.width/6))&&tabs.Zip(tabs.Skip(1),(a,b)=>Mathf.Abs(a.anchoredPosition.x+a.rect.width-b.anchoredPosition.x)<.1f).All(ok=>ok));
            Verify(check,category+": category hit areas have no separate box outlines or seams",tabs.All(tab=>tab.GetComponent<Image>().color.a==0&&!tab.GetComponent<Outline>()&&!tab.GetComponent<Shadow>())&&rail.GetComponentsInChildren<RectTransform>().Count(rect=>rect.name=="Settings selected category")==1);
            foreach(var tab in tabs)
            {
                var caption=tab.GetComponentInChildren<TextMeshProUGUI>();float originalSize=caption.fontSize;caption.fontSize=Mathf.Max(originalSize,22*1.12f);caption.ForceMeshUpdate();float height=caption.GetPreferredValues(caption.text,caption.rectTransform.rect.width,float.PositiveInfinity).y;
                bool fits=!caption.isTextOverflowing&&height<=caption.rectTransform.rect.height+.75f;caption.fontSize=originalSize;caption.ForceMeshUpdate();Verify(check,category+": category label also fits large text "+caption.text,fits);
            }
            var actions=box.GetComponentsInChildren<Button>().Where(button=>button.isActiveAndEnabled).Select(button=>button.GetComponent<RectTransform>()).ToArray();
            var rects=actions.Select(rect=>{var corners=new Vector3[4];rect.GetWorldCorners(corners);var a=box.InverseTransformPoint(corners[0]);var b=box.InverseTransformPoint(corners[2]);return Rect.MinMaxRect(a.x,-b.y,b.x,-a.y);}).ToArray();
            string invalid=string.Join("; ",rects.Select((rect,index)=>new{rect,index}).Where(value=>value.rect.height<63.9f||value.rect.xMin<-.1f||value.rect.yMin<-.1f||value.rect.xMax>box.rect.width+.1f||value.rect.yMax>box.rect.height+.1f).Select(value=>actions[value.index].name+" "+value.rect));
            Verify(check,category+": every action fits the panel with a64px minimum target "+invalid,actions.All(rect=>rect.rect.height>=64)&&string.IsNullOrEmpty(invalid));
            bool overlap=false;for(int i=0;i<rects.Length;i++)for(int j=i+1;j<rects.Length;j++)overlap|=Mathf.Min(rects[i].xMax,rects[j].xMax)-Mathf.Max(rects[i].xMin,rects[j].xMin)>.1f&&Mathf.Min(rects[i].yMax,rects[j].yMax)-Mathf.Max(rects[i].yMin,rects[j].yMin)>.1f;
            Verify(check,category+": top categories, content actions and footer do not overlap",!overlap);
            foreach(var text in box.GetComponentsInChildren<TextMeshProUGUI>().Where(text=>text.isActiveAndEnabled))
            {text.ForceMeshUpdate();float height=text.GetPreferredValues(text.text,text.rectTransform.rect.width,float.PositiveInfinity).y;Verify(check,category+": text fits "+text.name,!text.isTextOverflowing&&height<=text.rectTransform.rect.height+.75f);}
        }
        public static IEnumerator Run(VlabApp app,Action<string,bool> check,Action<string> capture=null)
        {
            Verify(check,"Interaction journey begins at Home",app.Route=="home");var original=app.Profile.settings.Copy();string originalJson=JsonUtility.ToJson(original),attempts=Attempts(app.Profile),legal=Legal(app.Profile),checkpoint=app.Profile.checkpoint==null?"":JsonUtility.ToJson(app.Profile.checkpoint);
            var simulator=(XRDeviceSimulator)typeof(VlabInput).GetField("simulatorDriver",Private).GetValue(app.Input);
            Vector2 originalSimulator=simulator?new Vector2(simulator.mouseXRotateSensitivity,simulator.mouseYRotateSensitivity):Vector2.zero;
            Verify(check,"Active PC VR simulator exposes its actual mouse-look sensitivities",!app.Input.SimulatorMode||simulator);
            string locale=app.Profile.locale;yield return Press(app,"Home utility 0");
            var language=app.GetComponentsInChildren<Button>().Single(button=>button.GetComponentInChildren<TextMeshProUGUI>()?.text==(locale=="vi"?"Ngôn ngữ: Tiếng Việt":"Language: English"));
            language.onClick.Invoke();yield return new WaitForSecondsRealtime(.28f);
            Verify(check,"Language draft changes its visible value without committing",app.Profile.locale==locale&&app.GetComponentsInChildren<TextMeshProUGUI>().Any(text=>text.text==(locale=="vi"?"Language: English":"Ngôn ngữ: Tiếng Việt")));
            string[] alternateCategories=locale=="vi"?new[]{"General","Audio","Graphics","Controls","Appearance","Interaction"}:new[]{"Chung","Âm thanh","Đồ họa","Điều khiển","Giao diện","Tương tác"};
            foreach(string categoryName in alternateCategories){yield return Press(app,categoryName);Layout(app,check,"Draft language "+categoryName);}
            Verify(check,"All alternate-language settings layouts preserve committed locale",app.Profile.locale==locale);
            yield return Press(app,"Hủy","Cancel");
            yield return Open(app);string[] categories=locale=="en"?new[]{"General","Audio","Graphics","Controls","Appearance","Interaction"}:new[]{"Chung","Âm thanh","Đồ họa","Điều khiển","Giao diện","Tương tác"};
            for(int i=0;i<categories.Length;i++)
            {
                yield return Press(app,categories[i]);Layout(app,check,categories[i]);if(i==0)capture?.Invoke("51-cai-dat-chung");if(i==4)capture?.Invoke("52-cai-dat-giao-dien");
                if(i==3){var buttons=app.GetComponentsInChildren<Button>().Where(button=>button.isActiveAndEnabled).ToArray();Verify(check,"QR control is enabled only on its supported Android host",buttons.Single(button=>button.name=="Controls viewer QR").interactable==app.Input.CanScanViewer);Verify(check,"PC simulation action is offered only in the Editor",buttons.Any(button=>button.name=="Controls PC simulator")==Application.isEditor);Verify(check,"Phone display choice is always available",buttons.Single(button=>button.name=="Controls phone").interactable);Verify(check,"Controller pairing opens only on Android",buttons.Single(button=>button.name=="Controls pair controller").interactable==app.Input.CanPairController);}
            }
            capture?.Invoke("50-cai-dat-tuong-tac");
            yield return Press(app,"Interaction gaze toggle");yield return Press(app,"Interaction dwell minus");yield return Press(app,"Interaction mouse minus");
            Verify(check,"Interaction edits remain independent of committed settings",JsonUtility.ToJson(app.Profile.settings)==originalJson&&Same(app.Input.MouseLookSensitivity,original.mouseLookSensitivity));
            yield return Press(app,"Quay lại","Back");Verify(check,"Dirty Interaction Back offers an explicit decision",app.GetComponentsInChildren<TextMeshProUGUI>().Any(t=>t.text=="Thay đổi chưa áp dụng"||t.text=="Unapplied changes"));yield return Press(app,"Ở lại","Stay");
            yield return Press(app,"Hủy","Cancel");Verify(check,"Interaction Cancel preserves original preferences",app.Route=="home"&&JsonUtility.ToJson(app.Profile.settings)==originalJson);
            yield return Open(app);yield return Press(app,"Interaction gaze toggle");yield return Press(app,original.gazeDwellSeconds>=1.9f?"Interaction dwell minus":"Interaction dwell plus");yield return Press(app,original.mouseLookSensitivity>=1.9f?"Interaction mouse minus":"Interaction mouse plus");var expected=Draft(app).Copy();
            yield return Press(app,"Áp dụng","Apply");Verify(check,"Interaction Apply commits preferences and mouse runtime multiplier",app.Route=="home"&&JsonUtility.ToJson(app.Profile.settings)==JsonUtility.ToJson(expected)&&Same(app.Input.MouseLookSensitivity,expected.mouseLookSensitivity));
            if(simulator)
            {
                var scaled=originalSimulator*(expected.mouseLookSensitivity/original.mouseLookSensitivity);
                Verify(check,"Mouse preference scales actual PC VR simulator pitch/yaw",Same(simulator.mouseXRotateSensitivity,scaled.x)&&Same(simulator.mouseYRotateSensitivity,scaled.y));
                app.Input.MouseLookSensitivity=expected.mouseLookSensitivity;
                Verify(check,"Reapplying simulator mouse preference does not compound",Same(simulator.mouseXRotateSensitivity,scaled.x)&&Same(simulator.mouseYRotateSensitivity,scaled.y));
            }
            string folder=SessionState.GetString("VLAB.TestProfile","");Verify(check,"Interaction journey uses an isolated profile",!string.IsNullOrEmpty(folder));var reopened=new ProfileStore(folder).Load();Verify(check,"Actual Apply reopens with custom preferences and unchanged attempts",!reopened.writeProtected&&JsonUtility.ToJson(reopened.profile.settings)==JsonUtility.ToJson(expected)&&Attempts(reopened.profile)==attempts&&Legal(reopened.profile)==legal);
            yield return Open(app);yield return Press(app,"Mặc định mục này","Category defaults");var defaults=new GameSettings();var category=Draft(app);
            Verify(check,"Interaction category defaults affect only their own fields",category.gazeAutoSelect&&Same(category.gazeDwellSeconds,defaults.gazeDwellSeconds)&&Same(category.mouseLookSensitivity,defaults.mouseLookSensitivity)&&category.volume==expected.volume&&category.uiTheme==expected.uiTheme&&category.graphics==expected.graphics&&category.largeText==expected.largeText);
            yield return Press(app,"Áp dụng","Apply");
            // Restore through the real Apply path; this fixture does not change the user's chosen preferences.
            yield return Press(app,"Home utility 0");typeof(VlabApp).GetField("draft",Private).SetValue(app,original.Copy());yield return Press(app,"Áp dụng","Apply");
            Verify(check,"Interaction checks restore preferences and preserve consent/records/checkpoint",app.Route=="home"&&JsonUtility.ToJson(app.Profile.settings)==originalJson&&Attempts(app.Profile)==attempts&&Legal(app.Profile)==legal&&(app.Profile.checkpoint==null?"":JsonUtility.ToJson(app.Profile.checkpoint))==checkpoint);
            if(simulator)Verify(check,"Restored preference restores simulator's original pitch/yaw",Same(simulator.mouseXRotateSensitivity,originalSimulator.x)&&Same(simulator.mouseYRotateSensitivity,originalSimulator.y));
            yield return Press(app,"Home utility 0");yield return Press(app,"Giao diện","Appearance");yield return Press(app,"Mặc định mục này","Category defaults");
            Verify(check,"Appearance category defaults select80percent without committing",Same(Draft(app).uiOpacity,.8f)&&JsonUtility.ToJson(app.Profile.settings)==originalJson);
            yield return Press(app,"Hủy","Cancel");Verify(check,"Cancelling appearance defaults preserves the user's opacity",app.Route=="home"&&JsonUtility.ToJson(app.Profile.settings)==originalJson);
        }
    }
}
#endif
