#if UNITY_EDITOR
using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Vlab.Product
{
    public static class NotebookPersistenceChecks
    {
        [Serializable] sealed class Envelope {public int format=1;public string payload,sha256;}
        static void Raw(string folder,string payload)
        {
            Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,"profile.json"),JsonUtility.ToJson(new Envelope{payload=payload,sha256=ProfileStore.Hash(payload)}));
        }
        public static void Run(Action<string,bool> check)
        {
            string folder=Path.Combine(Path.GetTempPath(),"VlabNotebook-"+Guid.NewGuid().ToString("N"));
            var legacy=new LocalProfile{locale="vi",safetyAcknowledged=true};legacy.Accept("privacy","privacy",true);legacy.Accept("terms","terms",true);
            legacy.schemaVersion=1;string payload=Regex.Replace(JsonUtility.ToJson(legacy),",?\"notebookPages\"\\s*:\\s*\\[\\]","");Raw(folder,payload);
            var store=new ProfileStore(folder);var loaded=store.Load();var profile=loaded.profile;
            check("Legacy profile migrates without repeating consent or clearing settings",!loaded.writeProtected&&profile.schemaVersion==2&&profile.notebookPages.Count==0&&profile.NextSetup("privacy","terms")=="home");
            string now=DateTime.UtcNow.ToString("O");profile.notebookPages.Add(new NotebookPage{id="unicode-note",title="Thí nghiệm của mình",body="Chu kỳ con lắc: 2,009 giây.\nTiếng Việt: ă â đ ê ô ơ ư.\n<b>Đây là chữ thường, không phải thẻ UI.</b>",createdAtUtc=now,updatedAtUtc=now,experimentId="P01"});
            check("Vietnamese notebook page saves atomically",store.Save(profile,out _));
            var reopened=new ProfileStore(folder).Load().profile;check("Title, Unicode, line breaks and lesson association survive reopening",reopened.notebookPages.Count==1&&JsonUtility.ToJson(reopened.notebookPages[0])==JsonUtility.ToJson(profile.notebookPages[0]));
            check("Writing does not fabricate a measured attempt",reopened.attempts.Count==0);
            string backupFailure=Path.Combine(folder,"backup-failure");Directory.CreateDirectory(Path.Combine(backupFailure,"profile.backup.json.tmp"));
            bool committed=new ProfileStore(backupFailure).Save(reopened,out var backupError);var committedLoad=new ProfileStore(backupFailure).Load();
            check("Backup failure preserves successful primary commit and current writing on reload",committed&&string.IsNullOrEmpty(backupError)&&!committedLoad.writeProtected&&committedLoad.profile.notebookPages.Count==1&&committedLoad.profile.notebookPages[0].body==reopened.notebookPages[0].body);
            check("Progress reset preserves personally written pages",store.Reset(reopened,false,out var progress,out _)&&progress.notebookPages.Count==1&&progress.notebookPages[0].body==reopened.notebookPages[0].body);
            check("Full reset clears notebook only as part of the explicit full reset",store.Reset(progress,true,out var empty,out _)&&empty.notebookPages.Count==0);
            check("Post-reset backup cannot resurrect written pages",new ProfileStore(folder).Load().profile.notebookPages.Count==0);
            profile.notebookPages[0].body=new string('x',12001);check("Oversized pages are refused without claiming save success",!new ProfileStore(Path.Combine(folder,"limits")).Save(profile,out _));profile.notebookPages[0].body="Ghi chú được giữ.";
            profile.notebookPages.Add(profile.notebookPages[0]);check("Duplicate page IDs are refused",!new ProfileStore(Path.Combine(folder,"duplicates")).Save(profile,out _));profile.notebookPages.RemoveAt(1);
            string future=Path.Combine(folder,"future");profile.schemaVersion=3;Raw(future,JsonUtility.ToJson(profile));string original=File.ReadAllText(Path.Combine(future,"profile.json"));var protectedStore=new ProfileStore(future);var protectedLoad=protectedStore.Load();
            check("Future notebook schema is preserved and write protected",protectedLoad.writeProtected&&!protectedStore.Save(new LocalProfile(),out _)&&File.ReadAllText(Path.Combine(future,"profile.json"))==original);
            profile.schemaVersion=2;string corrupt=Path.Combine(folder,"null-pages");Raw(corrupt,Regex.Replace(JsonUtility.ToJson(profile),"\"notebookPages\"\\s*:\\s*\\[[\\s\\S]*?\\](?=,\"checkpoint\")","\"notebookPages\":null"));
            check("Explicitly corrupt notebook data is not silently replaced",new ProfileStore(corrupt).Load().writeProtected);
        }
    }
}
#endif
