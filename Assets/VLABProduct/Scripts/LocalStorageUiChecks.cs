#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Vlab.Product;

public static class LocalStorageUiChecks
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static object Get(VlabApp app,string name)=>typeof(VlabApp).GetField(name,Flags).GetValue(app);
    static void Set(VlabApp app,string name,object value)=>typeof(VlabApp).GetField(name,Flags).SetValue(app,value);
    static void Property(VlabApp app,string name,object value)=>Set(app,"<"+name+">k__BackingField",value);
    static object Call(VlabApp app,string name,params object[] args)=>typeof(VlabApp).GetMethod(name,Flags).Invoke(app,args);
    static IEnumerator Press(VlabApp app,params string[] names)
    {yield return new WaitForSecondsRealtime(.28f);app.GetComponentsInChildren<Button>().Single(b=>b.isActiveAndEnabled&&(names.Contains(b.name)||names.Contains(b.GetComponentInChildren<TextMeshProUGUI>()?.text))).onClick.Invoke();yield return new WaitForSecondsRealtime(.28f);}
    static bool CleanUi(VlabApp app)=>!app.GetComponentsInChildren<TextMeshProUGUI>().Any(t=>t.isActiveAndEnabled&&(t.text.Contains("Could not save locally")||t.text.Contains("UnauthorizedAccess")||t.text.Contains("profile.json")||t.text.Contains("Could not reset")||t.text.Contains("C:\\")||t.text.Contains("E:\\")));
    public static IEnumerator Run(VlabApp app,Action<string> capture,Action<string,bool> check)
    {
        if(app.Route!="home")throw new InvalidOperationException("Storage UI fixture starts from Home.");
        var original=app.Profile;string originalJson=JsonUtility.ToJson(original);var oldStore=Get(app,"store");var oldSelected=Get(app,"selected");var oldSession=app.Session;var oldRig=app.Apparatus;var oldStatus=app.Status;var oldSubject=Get(app,"subject");bool protectedBefore=(bool)Get(app,"writeProtected");
        string folder=Path.Combine(Path.GetTempPath(),"VlabStorageUi-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        var lines=new List<string>();Action<string,bool> verify=(name,okay)=>{lines.Add((okay?"PASS ":"FAIL ")+name);check(name,okay);if(!okay)throw new InvalidOperationException(name);};GameObject fixture=null;
        try
        {
            var profile=JsonUtility.FromJson<LocalProfile>(originalJson);profile.locale="vi";profile.settings.hints=false;Property(app,"Profile",profile);Set(app,"store",new ProfileStore(folder));Set(app,"writeProtected",false);
            var catalog=(BriefCatalog)Get(app,"catalog");Set(app,"selected",catalog.items.Single(x=>x.id=="P01"));Set(app,"subject","P");
            foreach(string blockedName in new[]{"profile.json.tmp","profile.backup.json.tmp"})
            {
                var session=new ExperimentSession("P01",401);fixture=new GameObject("Isolated storage UI original pendulum kit");var rig=fixture.AddComponent<ApparatusRig>();rig.Build(session,(Vector3)Call(app,"FindWorktop"));
                foreach(var item in rig.items.ToArray()){rig.Select(item);verify("Original pendulum item installs for storage UI: "+item.assetId,rig.InstallSelected());}
                session.SetApparatusReady(true);while(session.Phase==ExperimentPhase.Preparing)verify("Storage UI fixture prepares original operation",session.Prepare(session.Operation,out _));
                verify("Storage UI fixture starts actual pendulum",session.Start(out _));session.Advance(100);
                Property(app,"Session",session);Property(app,"Apparatus",rig);profile.checkpoint=session.Checkpoint();var checkpoint=profile.checkpoint;
                var store=(ProfileStore)Get(app,"store");verify("Isolated baseline profile commits",store.Save(profile,out _));string primary=Path.Combine(folder,"profile.json"),backup=Path.Combine(folder,"profile.backup.json");var before=File.ReadAllBytes(primary);var backupBefore=File.ReadAllBytes(backup);int oldCount=profile.attempts.Count;
                app.ShowActivity();yield return new WaitForSecondsRealtime(.3f);
                string blocked=Path.Combine(folder,blockedName);verify("Fault temp path was absent",!File.Exists(blocked)&&!Directory.Exists(blocked));Directory.CreateDirectory(blocked);
                try
                {
                    yield return Press(app,"Ghi số đo");verify("Failed reading save stays Measured with retained pending sample",session.Phase==ExperimentPhase.Measured&&Get(app,"pendingMeasurement") is AttemptRecord&&profile.attempts.Count==oldCount&&ReferenceEquals(profile.checkpoint,checkpoint));
                    verify("Failure message is localized and contains no raw storage path",app.Status.Contains("Chưa xác nhận lưu")&&CleanUi(app));
                    verify("Storage dialog hides the underlying activity text",!((RectTransform)Get(app,"page")).gameObject.activeSelf&&((RectTransform)Get(app,"modal")).gameObject.activeSelf);
                    if(blockedName=="profile.json.tmp")capture?.Invoke("84-local-save-retry-VI");
                    if(blockedName=="profile.json.tmp")verify("Prewrite failure preserves committed primary and backup bytes",before.SequenceEqual(File.ReadAllBytes(primary))&&backupBefore.SequenceEqual(File.ReadAllBytes(backup)));
                    else verify("Backup failure may already commit the same pending sample in primary",File.ReadAllText(primary).Contains(((AttemptRecord)Get(app,"pendingMeasurement")).attemptId));
                }
                finally{Directory.Delete(blocked,false);}
                string finished=JsonUtility.ToJson((AttemptRecord)Get(app,"pendingMeasurement"));var saved=(AttemptRecord)Get(app,"pendingMeasurement");Call(app,"CloseModal");
                verify("Closing storage dialog restores the activity card",((RectTransform)Get(app,"page")).gameObject.activeSelf);
                yield return Press(app,"Thử lưu số đo");var reopened=store.Load();
                verify("Actual visible Retry saves exactly one identical finished reading",app.Route=="results"&&reopened.profile.attempts.Count==oldCount+1&&reopened.profile.attempts.Count(x=>x.attemptId==saved.attemptId)==1&&JsonUtility.ToJson(reopened.profile.attempts.Single(x=>x.attemptId==saved.attemptId))==finished);
                verify("Retry does not remeasure or leave pending state",session.Phase==ExperimentPhase.Measured&&Get(app,"pendingMeasurement")==null);
                if(blockedName=="profile.json.tmp")capture?.Invoke("85-local-save-recovered-VI");
                UnityEngine.Object.DestroyImmediate(fixture);fixture=null;Property(app,"Session",null);Property(app,"Apparatus",null);app.ShowHome();
            }
            Call(app,"OpenSettings","home");yield return new WaitForSecondsRealtime(.3f);var committed=profile.settings;var draft=(GameSettings)Get(app,"draft");draft.volume=committed.volume>.5f?.25f:.8f;Set(app,"dirty",true);var oldBytes=File.ReadAllBytes(Path.Combine(folder,"profile.json"));string settingsFault=Path.Combine(folder,"profile.json.tmp");Directory.CreateDirectory(settingsFault);
            try{yield return Press(app,"Settings apply");verify("Failed settings Apply rolls back committed settings and retains draft",ReferenceEquals(profile.settings,committed)&&((GameSettings)Get(app,"draft")).volume!=profile.settings.volume&&oldBytes.SequenceEqual(File.ReadAllBytes(Path.Combine(folder,"profile.json")))&&CleanUi(app));}
            finally{Directory.Delete(settingsFault,false);}
            Call(app,"CloseModal");yield return Press(app,"Hủy","Cancel");verify("Actual Cancel leaves stored settings unchanged",ReferenceEquals(profile.settings,committed));
            string resetFault=Path.Combine(folder,"profile.backup.json.tmp");Directory.CreateDirectory(resetFault);
            try{Call(app,"ResetProfile",false);verify("Reset failure is localized and does not promise intact old records",app.Status.Contains("Chưa xác nhận đặt lại")&&app.Status.Contains("không thể bảo đảm",StringComparison.OrdinalIgnoreCase)&&CleanUi(app));verify("Reset failure fixture proves generation/primary may already change",File.Exists(Path.Combine(folder,"reset-generation.txt"))&&new ProfileStore(folder).Load().profile.attempts.Count==0);}
            finally{Directory.Delete(resetFault,false);}
            Call(app,"CloseModal");File.WriteAllText(Path.Combine(folder,"profile.json"),"invalid primary");File.WriteAllText(Path.Combine(folder,"profile.backup.json"),"invalid backup");var protectedLoad=new ProfileStore(folder).Load();verify("Actual damaged local files enter write protection",protectedLoad.writeProtected);Call(app,"ApplyStorageLoadMessage",protectedLoad);app.ShowHome();
            verify("Protected-load warning is Vietnamese and excludes diagnostics",app.Status.Contains("được bảo vệ")&&CleanUi(app));
            yield return new WaitForSecondsRealtime(.32f);
            verify("Protected-profile capture waits for visible Home entrance",app.GetComponentsInChildren<UiEntrance>().All(entrance=>entrance.GetComponent<CanvasGroup>().alpha>.999f));
            verify("Protected-profile status replaces the generic Home hint",!app.GetComponentsInChildren<TextMeshProUGUI>().Any(text=>text.isActiveAndEnabled&&(text.text=="Nhìn hoặc trỏ để chọn"||text.text=="Look or point to select")));
            capture?.Invoke("84-luu-tru-cuc-bo-VI");
        }
        finally
        {
            if(fixture)UnityEngine.Object.DestroyImmediate(fixture);Call(app,"ClearPendingMeasurement");Property(app,"Profile",original);Property(app,"Session",oldSession);Property(app,"Apparatus",oldRig);Set(app,"store",oldStore);Set(app,"selected",oldSelected);Set(app,"subject",oldSubject);Set(app,"writeProtected",protectedBefore);Set(app,"status",oldStatus);Call(app,"ApplySettings");app.ShowHome();
            verify("Storage UI fixture restores every original learner field",JsonUtility.ToJson(app.Profile)==originalJson);
            foreach(string name in new[]{"profile.json","profile.backup.json","reset-generation.txt"}){string path=Path.Combine(folder,name);if(File.Exists(path))File.Delete(path);}if(!Directory.EnumerateFileSystemEntries(folder).Any())Directory.Delete(folder,false);
            Directory.CreateDirectory("Evidence/Runtime");File.WriteAllLines("Evidence/Runtime/local-storage-ui.txt",new[]{"Native isolated prewrite/backup faults and actual UI Retry; no device/storage durability certification."}.Concat(lines));
        }
    }
}
#endif
