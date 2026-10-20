#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Vlab.Product;
using UnityEngine;

// Writes isolated evidence files only; never touches the user's application profile.
public static class SubjectRecordVerifier
{
    static string folder,privacyHash,termsHash;static LocalProfile profile;
    static readonly List<string> checks=new List<string>();
    static readonly HashSet<string> verifiedIds=new HashSet<string>();
    public static void Begin()
    {
        // Opt-in Editor harness context; default retains the historical project-Evidence path.
        // The explicit token selects a NEW QA sibling, never the real Profile folder.
        string qaContext=Environment.GetEnvironmentVariable("VLAB_QA_PROFILE_ROOT");
        if(!string.IsNullOrEmpty(qaContext)&&qaContext!="persistent")throw new InvalidOperationException("Unknown QA profile context.");
        folder=qaContext=="persistent"?Path.Combine(Application.persistentDataPath,"QA","Subjects-"+Guid.NewGuid().ToString("N")):Path.GetFullPath("Evidence/Subjects/OfflineProfile-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));
        profile=new LocalProfile{locale="vi",safetyAcknowledged=true};
        string Hash(string id)=>ProfileStore.Hash(Resources.Load<TextAsset>("Legal/"+id+"_vi").text+"\n"+Resources.Load<TextAsset>("Legal/"+id+"_en").text);
        privacyHash=Hash("privacy");termsHash=Hash("terms");
        profile.Accept("privacy",privacyHash,true);profile.Accept("terms",termsHash,true);checks.Clear();verifiedIds.Clear();
    }
    public static void Verify(ExperimentSession session)
    {
        if(profile==null)Begin();var record=session.Record("Isolated full-journey persistence test.");
        VerifyReadingIsolation(session,record);
        profile.attempts.Add(record);profile.checkpoint=session.Checkpoint();
        var store=new ProfileStore(folder);
        if(!store.Save(profile,out var error))throw new Exception("Offline save failed: "+error);
        var loaded=new ProfileStore(folder).Load();var restored=loaded.profile.attempts.Last();
        if(loaded.writeProtected||loaded.recovered||loaded.profile.attempts.Count!=profile.attempts.Count||restored.attemptId!=record.attemptId||restored.detailJson!=record.detailJson||restored.model!=record.model||string.IsNullOrEmpty(restored.modelNotesVi)||restored.modelNotesVi!=record.modelNotesVi||string.IsNullOrEmpty(restored.modelNotesEn)||restored.modelNotesEn!=record.modelNotesEn||restored.readings.Count!=record.readings.Count||!restored.parameterValues.SequenceEqual(record.parameterValues))throw new Exception("Offline attempt was not restored exactly: "+record.experimentId);
        for(int i=0;i<record.readings.Count;i++)
        {
            var a=record.readings[i];var b=restored.readings[i];
            // JSON decimal conversion may differ by a few double-precision ULPs;
            // require agreement far below every displayed/instrument resolution.
            if(a.key!=b.key||Math.Abs(a.value-b.value)>1e-12*Math.Max(1,Math.Abs(a.value))||a.unit!=b.unit||(a.labelVi??"")!=(b.labelVi??"")||(a.labelEn??"")!=(b.labelEn??""))throw new Exception("Offline reading changed: "+record.experimentId+" "+a.key+" "+a.value.ToString("G17")+" -> "+b.value.ToString("G17"));
        }
        var safe=ExperimentSession.Restore(loaded.profile.checkpoint);
        if(safe.Phase!=ExperimentPhase.Preparing||safe.ApparatusReady||safe.Elapsed!=0||safe.Pendulum!=null)throw new Exception("Checkpoint resumed an unsafe live process.");
        if(loaded.profile.NextSetup(privacyHash,termsHash)!="home")throw new Exception("Onboarding repeated after saved acceptance.");
        profile=loaded.profile;verifiedIds.Add(record.experimentId);checks.Add("PASS "+record.experimentId+" file save/reopen: parameters, labelled readings, detailed observations, retained notebook, safe checkpoint, onboarding skipped.");
        checks.Add("PASS "+record.experimentId+" observation snapshot isolation: bidirectional mutation preserves the other reading's value, unit, key and bilingual labels.");
        Directory.CreateDirectory("Evidence/Subjects");File.WriteAllLines("Evidence/Subjects/offline-records.txt",new[]{"Isolated local profile: "+folder}.Concat(checks));
    }
    static void VerifyReadingIsolation(ExperimentSession session,AttemptRecord record)
    {
        if(record.readings.Count==0)throw new Exception("No observations available for record isolation: "+record.experimentId);
        var live=session.Recorded.readings[0];var saved=record.readings[0];
        double value=saved.value;string key=saved.key,unit=saved.unit,vi=saved.labelVi,en=saved.labelEn;
        bool Original(Reading reading)=>reading.value==value&&reading.key==key&&reading.unit==unit&&reading.labelVi==vi&&reading.labelEn==en;
        void Restore(Reading reading){reading.value=value;reading.key=key;reading.unit=unit;reading.labelVi=vi;reading.labelEn=en;}
        void Mutate(Reading reading){reading.value=value+123.456;reading.key="isolation-test";reading.unit="test-unit";reading.labelVi="nhãn thử";reading.labelEn="test label";}
        try
        {
            Mutate(live);
            if(!Original(saved))throw new Exception("Saved record aliases the live observation: "+record.experimentId);
        }
        finally{Restore(live);}
        try
        {
            Mutate(saved);
            if(!Original(live))throw new Exception("Saved observation mutates the live measurement: "+record.experimentId);
        }
        finally{Restore(saved);}
    }
    public static void Complete()
    {
        var missing=LessonSpec.All.Keys.Where(id=>!verifiedIds.Contains(id)).ToArray();
        if(missing.Length>0)throw new Exception("Missing completed and reopened records: "+string.Join(", ",missing));
        checks.Add("PASS all 32 distinct experiment records coexist in the same reopened offline profile; "+profile.attempts.Count+" attempts including control cases.");
        File.WriteAllLines("Evidence/Subjects/offline-records.txt",new[]{"Isolated local profile: "+folder}.Concat(checks));
    }
}
#endif
