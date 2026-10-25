#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using Vlab.Product;

public sealed partial class WorkbenchUpgradeProbe
{
    IEnumerator PracticeLayout()
    {
        foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(2340,1080),new Vector2Int(2048,1536)})
        {
            Check("Configure actual Game View "+size,SecondTestGameView.Configure(out _,size.x,size.y));yield return new WaitForSecondsRealtime(.6f);Canvas.ForceUpdateCanvases();
            var summary=App.GetComponentsInChildren<RectTransform>().Single(x=>x.name=="Practice summary");var corners=new Vector3[4];summary.GetWorldCorners(corners);
            var rect=Rect.MinMaxRect(corners.Min(x=>x.x),corners.Min(x=>x.y),corners.Max(x=>x.x),corners.Max(x=>x.y));
            Check("Practice summary within display and clear of center "+size,rect.xMin>=0&&rect.yMin>=0&&rect.xMax<=Screen.width&&rect.yMax<=Screen.height&&!rect.Contains(new Vector2(Screen.width*.5f,Screen.height*.5f)));
            Check("Practice summary text fits "+size,!summary.GetComponentsInChildren<TextMeshProUGUI>().Any(x=>x.isTextOverflowing||x.isTextTruncated));yield return Capture("practice-layout-"+size.x+"x"+size.y);
        }
        SecondTestGameView.Configure(out _);yield return new WaitForSecondsRealtime(.4f);
        App.Input.SetSimulator(true);yield return new WaitForSecondsRealtime(.7f);
        Check("Practice summary rendered in VR world canvas",App.GetComponentsInChildren<RectTransform>().Single(x=>x.name=="Practice summary").GetComponentInParent<Canvas>().renderMode==RenderMode.WorldSpace);
        yield return Capture("practice-layout-vr-editor");App.Input.SetSimulator(false);yield return new WaitForSecondsRealtime(.7f);rows.Add("VR fixture checks layout only, not native head tracking or VR input.");
    }
    IEnumerator PracticeExtraGasPoint()
    {
        string trial=App.Session.TrialId;int count=JsonUtility.FromJson<BiologyLessonState>(App.Apparatus.Driver.SaveState()).gas.Count;
        yield return new WaitForSecondsRealtime(10.2f);
        yield return Tap("Station tools");yield return Tap("Dụng cụ");
        for(int n=0;n<5&&!Find("Equipment item syringe");n++)yield return Tap(Find("Equipment next")?"Equipment next":"Equipment previous");
        yield return Tap("Equipment item syringe");yield return Tap("Instrument control gasRead");yield return Tap("Close equipment controls");yield return new WaitForSecondsRealtime(1.2f);
        var state=JsonUtility.FromJson<BiologyLessonState>(App.Profile.attempts.Single(x=>x.attemptId==trial).detailJson);
        Check("An additional measured gas point updates the same saved trial",state.gas.Count==count+1&&App.Profile.attempts.Count(x=>x.attemptId==trial)==1);
    }
    IEnumerator PracticeMotor()
    {
        var source=UnityEngine.Object.FindObjectsByType<AudioSource>().Single(x=>x.name=="Running laboratory motor");
        Check("Motor sound follows running mechanism",source.isPlaying&&source.volume>0&&source.spatialBlend>0);
        yield return Tap("‹ Quay lại");yield return Wait("pause");yield return new WaitForSecondsRealtime(.3f);
        Check("Pause stops mechanism sound",!source.isPlaying&&source.volume==0);
        yield return Tap("Tiếp tục");yield return Wait("activity");yield return new WaitForSecondsRealtime(.3f);
        Check("Resume restores mechanism sound",source.isPlaying&&source.volume>0);
    }
    static bool SameObservation(AttemptRecord a,AttemptRecord b)=>a.attemptId==b.attemptId&&a.experimentId==b.experimentId&&a.partial==b.partial&&a.savedAtUtc==b.savedAtUtc&&(a.detailJson??"")== (b.detailJson??"")&&a.readings.Count==b.readings.Count&&a.readings.Zip(b.readings,(x,y)=>x.key==y.key&&x.unit==y.unit&&(x.labelVi??"")== (y.labelVi??"")&&(x.labelEn??"")== (y.labelEn??"")&&Math.Abs(x.value-y.value)<=1e-12*Math.Max(1,Math.Abs(x.value))).All(x=>x)&&a.parameterValues.Length==b.parameterValues.Length&&a.parameterValues.Zip(b.parameterValues,(x,y)=>Math.Abs(x-y)<=1e-12*Math.Max(1,Math.Abs(x))).All(x=>x);
    IEnumerator PracticeRestored()
    {
        yield return Wait("home");
        var expected=JsonUtility.FromJson<LocalProfile>(File.ReadAllText(Environment.GetEnvironmentVariable("VLAB_PRACTICE_EXPECTED")));
        Check("Fresh process reloads every observed trial",App.Profile.attempts.Count==expected.attempts.Count);
        foreach(var record in expected.attempts)Check(record.experimentId+" cold restart retains values units identity and details",SameObservation(record,App.Profile.attempts.Single(x=>x.attemptId==record.attemptId)));
        NotebookPersistenceChecks.Run(Check);
        var currentStore=Read<ProfileStore>("store");var currentProfile=App.Profile;var test=JsonUtility.FromJson<LocalProfile>(JsonUtility.ToJson(currentProfile));
        string folder=Path.Combine(Path.GetTempPath(),"VlabPracticePersistence-"+Guid.NewGuid().ToString("N"));var isolated=new ProfileStore(folder);
        Check("Persistence fault fixture saves baseline",isolated.Save(test,out _));
        var zero=test.attempts.First(x=>x.readings.Any(r=>r.value==0));Check("Real measured zero reloads",isolated.Load().profile.attempts.Single(x=>x.attemptId==zero.attemptId).readings.Any(r=>r.value==0));
        var snapshot=JsonUtility.FromJson<AttemptRecord>(JsonUtility.ToJson(test.attempts.Last()));snapshot.partial=true;
        string before=File.ReadAllText(Path.Combine(folder,"profile.json"));string fault=Path.Combine(folder,"profile.json.tmp");Directory.CreateDirectory(fault);
        typeof(VlabApp).GetProperty("Profile").SetValue(App,test);typeof(VlabApp).GetField("store",Private).SetValue(App,isolated);
        try
        {
            Check("Failed snapshot reports failure",!(bool)Call("CommitPracticeRecord",snapshot));
            Check("Failed save retains previous disk bytes",File.ReadAllText(Path.Combine(folder,"profile.json"))==before);
            Check("Failed replacement rolls back in-memory trial",!test.attempts.Single(x=>x.attemptId==snapshot.attemptId).partial);
        }
        finally{Directory.Delete(fault,false);}
        Check("Retry saves same trial exactly once",(bool)Call("CommitPracticeRecord",snapshot)&&test.attempts.Count(x=>x.attemptId==snapshot.attemptId)==1);
        Check("Partial status survives reopen",isolated.Load().profile.attempts.Single(x=>x.attemptId==snapshot.attemptId).partial);
        typeof(VlabApp).GetProperty("Profile").SetValue(App,currentProfile);typeof(VlabApp).GetField("store",Private).SetValue(App,currentStore);typeof(VlabApp).GetField("writeProtected",Private).SetValue(App,false);Call("CloseModal");App.ShowHome();
        yield return new WaitForSecondsRealtime(.4f);yield return Tap("Open journal");yield return Wait("notebook");yield return Tap("Journal mode records");yield return Capture("practice-cold-restart-notebook");
        Check("Cold notebook actually rendered",App.GetComponentsInChildren<TextMeshProUGUI>().Any(t=>t.text.Contains("lượt")||t.text.Contains("Đã lưu")));
        foreach(var lesson in LessonSpec.All.Keys)
        {
            yield return Tap("Journal subject "+lesson[0]);
            while(!Find("Journal lesson "+lesson))yield return Tap("Journal index next");
            yield return Tap("Journal lesson "+lesson);yield return Tap("Journal tab 0");
            var record=currentProfile.attempts.Where(x=>x.experimentId==lesson).OrderByDescending(x=>x.savedAtUtc,StringComparer.Ordinal).First();
            Check(lesson+" restored latest trial is complete",!record.partial);
            foreach(var reading in ObservationCards.PresentationReadings(record.readings,lesson).Take(2))
            {
                var binding=App.GetComponentsInChildren<ObservationCardBinding>().Single(x=>x.Key==reading.key);
                Check(lesson+" restored notebook displays "+reading.key,binding.Value.text==ObservationCards.FormatValue(reading,true)&&binding.Unit.text==ObservationCards.DisplayUnit(reading));
            }
            rows.Add("PASS NOTEBOOK "+lesson+" actual touch selection and restored reading values/units");
        }
        File.WriteAllLines("Evidence/WorkbenchUpgrade/practice-reload-results.txt",rows);
    }
}
#endif
