#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using Vlab.Product;

public sealed partial class WorkbenchUpgradeProbe
{
    IEnumerator PracticeEnter(string id)
    {
        if(App.Route!="home")App.ShowHome();
        yield return new WaitForSecondsRealtime(.4f);
        yield return Tap("Subject "+id[0]);yield return Wait("browser");
        yield return Tap("Browser lesson "+id);yield return Tap("Browser start");
        yield return Wait("readiness");yield return Tap("Readiness enter");yield return Wait("activity");
    }
    IEnumerator PracticeInstall()
    {
        for(int n=0;n<40&&!App.Apparatus.Ready;n++)
        {
            var item=App.Apparatus.NextInstallableItem();Check("Available next instrument",item);
            if(Find("Equipment next required"))yield return Tap("Equipment next required");
            else{if(Find("Close equipment controls"))yield return Tap("Close equipment controls");yield return Tap("Compact current action");}
            yield return Tap("Equipment place selected");
            Check("Installed through actual touch "+item.assetId,item.installed);
        }
        if(Find("Close equipment controls"))yield return Tap("Close equipment controls");
        Check("Full kit installed",App.Apparatus.Ready);
    }
    IEnumerator PracticeChecks()
    {
        if(Environment.GetEnvironmentVariable("VLAB_PRACTICE_RELOAD")=="1"){yield return PracticeRestored();yield return PracticeCase("P01");yield break;}
        yield return Wait("home");if(Find("Skip quick tour"))yield return Tap("Skip quick tour");
        VlabAudioChecks.Run();Check("Audio sample checks",true);
        string requested=Environment.GetEnvironmentVariable("VLAB_PRACTICE_IDS")??"P01,P02,M02,B03,B04,C04";
        foreach(string id in requested.Split(','))
        {
            var stack=new Stack<IEnumerator>();stack.Push(PracticeCase(id));bool failed=false;
            while(stack.Count>0)
            {
                object value=null;bool next=false;
                try{next=stack.Peek().MoveNext();if(next)value=stack.Peek().Current;}
                catch(Exception e){rows.Add("FAIL LESSON "+id+" "+e);failures++;failed=true;break;}
                if(!next){stack.Pop();continue;}if(value is IEnumerator nested&&!(value is CustomYieldInstruction))stack.Push(nested);else yield return value;
            }
            if(failed)yield return Capture("practice-"+id+"-failure");
            File.WriteAllLines("Evidence/WorkbenchUpgrade/practice-results.txt",rows);
        }
        File.WriteAllText("Evidence/WorkbenchUpgrade/practice-profile-path.txt",Read<ProfileStore>("store").Folder);
        File.WriteAllText("Evidence/WorkbenchUpgrade/practice-expected.json",JsonUtility.ToJson(App.Profile));
    }
    IEnumerator PracticeCase(string id)
    {
        yield return PracticeEnter(id);if(id=="P01"){yield return PracticeRecovery();yield return PracticeLayout();}yield return PracticeInstall();
        if(id=="P03"){App.Profile.settings.hints=false;Call("RefreshActivity");rows.Add("P03 hints disabled fixture; subsequent practice uses touch controls");}
        if(id=="B03"||id=="C03")
        {
            yield return Tap("Station tools");yield return Tap("Thông số");string key=id=="B03"?"duration":"time";
            while(App.Session.parameters[key]>(id=="B03"?60:20))yield return Tap("Parameter decrease "+key);
            yield return Tap("Variables close");
        }
        float deadline=Time.realtimeSinceStartup+600;int actions=0;string previous="";int repeated=0;
        while(Time.realtimeSinceStartup<deadline)
        {
            if(App.Session.Phase==ExperimentPhase.Paused){rows.Add(id+" resumed through UI after pause");if(Find("Close equipment controls"))yield return Tap("Close equipment controls");yield return Tap("Compact current action");continue;}
            var task=(GuideTask)Call("CurrentGuide");string step=App.Session.Phase+":"+App.Session.Operation+":"+task.kind+":"+task.key+":"+task.control;
            if(step!=previous){rows.Add(id+" STATE "+step);previous=step;repeated=0;}
            bool complete=App.Session.Phase==ExperimentPhase.Running&&(App.Apparatus.Driver!=null?App.Apparatus.Driver.CanRecord(out _):App.Session.Pendulum!=null&&App.Session.Pendulum.Cycles>=App.Session.parameters["cycles"]);
            if(complete)break;
            if(Find("Close equipment controls")&&(task.kind!="instrument"||!Find("Instrument control "+task.control)))yield return Tap("Close equipment controls");
            if(App.Session.Phase==ExperimentPhase.Ready){yield return Tap("Compact current action");continue;}
            if(task.kind=="prepare")
            {
                yield return Tap("Compact current action");
                if(Find("Equipment preparation"))yield return Tap("Equipment preparation");
            }
            else if(task.kind=="instrument"&&(task.control=="cell"||task.control.EndsWith(":",StringComparison.Ordinal)))yield return PracticeImages(id);
            else if(task.kind=="instrument")
            {
                if(!Find("Instrument control "+task.control))yield return Tap("Compact current action");yield return Tap("Instrument control "+task.control,.08f,.02f);
                if(task.control.StartsWith("weighInitial:")||task.control=="gasRead"||task.control=="dose:water")
                {
                    var partial=App.Profile.attempts.SingleOrDefault(x=>x.attemptId==App.Session.TrialId);
                    Check(id+" intermediate real reading retained",partial!=null&&partial.readings.Count>0);
                    if(task.control=="dose:water")Check("Valid zero control is retained",partial.readings.Any(x=>x.value==0));
                }
            }
            else if(task.kind=="image")yield return PracticeImages(id);
            else if(task.kind=="variables"&&id=="C01")yield return PracticeTitrate();
            else if(task.kind=="observe")yield return new WaitForSecondsRealtime(.5f);
            else throw new Exception("Unimplemented real-input test step "+step);
            if(task.kind!="observe"&&++repeated>90)throw new Exception("Repeated interaction without progress: "+step);
            if(++actions%30==0)File.WriteAllLines("Evidence/WorkbenchUpgrade/practice-results.txt",rows);
        }
        Check(id+" becomes recordable from native interactions",Time.realtimeSinceStartup<deadline);
        if(Find("Close equipment controls"))yield return Tap("Close equipment controls");
        yield return new WaitForSecondsRealtime(1.3f);
        var saved=App.Profile.attempts.SingleOrDefault(x=>x.attemptId==App.Session.TrialId);
        Check(id+" valid reading saved before leaving or finishing",saved!=null&&!saved.partial&&saved.readings.Count>0);
        Check(id+" compact instruction visible",App.GetComponentsInChildren<TextMeshProUGUI>().Any(x=>x.transform.parent.name=="Practice summary"&&!string.IsNullOrEmpty(x.text)));
        yield return Capture("practice-"+id+"-observed");if(id=="M01")yield return PracticeMotor();if(id=="B04")yield return PracticeExtraGasPoint();
        yield return Tap("Compact current action");yield return Wait("results");
        var record=App.Profile.attempts.Single(x=>x.attemptId==App.Session.TrialId);Check(id+" finishing replaces same trial, no duplicate",!record.partial);
        var store=Read<ProfileStore>("store");var loaded=new ProfileStore(store.Folder).Load();
        Check(id+" disk values and units match",SameObservation(loaded.profile.attempts.Single(x=>x.attemptId==record.attemptId),record));
        yield return Tap("Open result journal");yield return Wait("notebook");yield return Capture("practice-"+id+"-notebook");
        Check(id+" journal shows reading cards",App.GetComponentsInChildren<TextMeshProUGUI>().Any(t=>t.text.Contains(ObservationCards.FormatValue(record.readings[0],true))));
        rows.Add("PASS LESSON "+id+" full UI preparation, practice, measured result, persisted notebook");App.Profile.settings.hints=true;
    }
}
#endif
