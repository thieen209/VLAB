#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
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
            DrawingPersistence(Path.Combine(folder,"drawing"),check);
        }
        static void DrawingPersistence(string folder,Action<string,bool> check)
        {
            var profile=new LocalProfile();string now=DateTime.UtcNow.ToString("O");
            var note=new NotebookPage{id="ink-page",title="Sơ đồ con lắc",body="Ghi chú vẫn còn.\nă â đ ê ô ơ ư",experimentId="P01",createdAtUtc=now,updatedAtUtc=now};profile.notebookPages.Add(note);
            string legacy=Regex.Replace(JsonUtility.ToJson(profile),",\"inkStrokes\"\\s*:\\s*\\[\\]","");Raw(folder,legacy);
            var store=new ProfileStore(folder);var loaded=store.Load();
            check("A schema-2 page without ink loads all legacy typed notes unchanged",!loaded.writeProtected&&loaded.profile.notebookPages[0].body==note.body&&loaded.profile.notebookPages[0].inkStrokes!=null&&loaded.profile.notebookPages[0].inkStrokes.Count==0);
            var drawing=new VlabWhiteboard.Drawing(note.inkStrokes);drawing.Add(new Vector2(.1f,.2f),true);drawing.Add(new Vector2(.8f,.7f),false);drawing.Add(new Vector2(.3f,.4f),true);
            check("Ink and typed notes save together through the existing atomic store",store.Save(profile,out _));
            var reopened=new ProfileStore(folder).Load();string saved=JsonUtility.ToJson(note);
            check("Fresh profile reload restores each normalized stroke and typed note exactly",!reopened.writeProtected&&JsonUtility.ToJson(reopened.profile.notebookPages[0])==saved&&!ReferenceEquals(reopened.profile.notebookPages[0].inkStrokes,note.inkStrokes));
            string primary=Path.Combine(folder,"profile.json"),temp=Path.Combine(folder,"profile.json.tmp");string committed=File.ReadAllText(primary);
            drawing.Add(new Vector2(.6f,.3f),true);string draft=JsonUtility.ToJson(note);Directory.CreateDirectory(temp);
            try{check("Failed ink save retains draft strokes and the previously committed file",!store.Save(profile,out _)&&JsonUtility.ToJson(note)==draft&&File.ReadAllText(primary)==committed);}
            finally{Directory.Delete(temp,false);}
            check("Retry after an ink-save failure commits the retained draft",store.Save(profile,out _)&&JsonUtility.ToJson(new ProfileStore(folder).Load().profile.notebookPages[0])==draft);
            check("Undo changes the persisted page draft without changing its text",drawing.Undo()&&note.inkStrokes.Count==2&&note.body==loaded.profile.notebookPages[0].body&&store.Save(profile,out _));
            check("Undone ink stays removed after a fresh reload",new ProfileStore(folder).Load().profile.notebookPages[0].inkStrokes.Count==2);
            check("Progress reset preserves typed notes and drawings together",store.Reset(profile,false,out var progress,out _)&&JsonUtility.ToJson(progress.notebookPages[0])==JsonUtility.ToJson(note));
            var invalids=new Action<NotebookPage>[]
            {
                n=>n.inkStrokes=null,n=>n.inkStrokes.Add(null),n=>n.inkStrokes[0].points=null,n=>n.inkStrokes[0].points.Clear(),
                n=>n.inkStrokes[0].points[0]=new Vector2(float.NaN,0),n=>n.inkStrokes[0].points[0]=new Vector2(0,float.PositiveInfinity),
                n=>n.inkStrokes[0].points[0]=new Vector2(-.1f,0),n=>n.inkStrokes[0].points[0]=new Vector2(0,1.1f),
                n=>n.inkStrokes[0].points=Enumerable.Repeat(Vector2.zero,NotebookPage.InkPointLimit+1).ToList(),
                n=>n.inkStrokes=Enumerable.Range(0,NotebookPage.InkStrokeLimit+1).Select(_=>new NotebookStroke{points=new List<Vector2>{Vector2.zero}}).ToList()
            };
            committed=File.ReadAllText(primary);
            for(int i=0;i<invalids.Length;i++)
            {
                var invalid=JsonUtility.FromJson<LocalProfile>(JsonUtility.ToJson(progress));invalids[i](invalid.notebookPages[0]);
                check("Malformed or oversized notebook ink is rejected before disk changes: "+i,!store.Save(invalid,out _)&&File.ReadAllText(primary)==committed);
            }
            string nullFolder=Path.Combine(folder,"null-ink");Raw(nullFolder,legacy.Replace("\"body\":","\"inkStrokes\":null,\"body\":"));
            string original=File.ReadAllText(Path.Combine(nullFolder,"profile.json"));var nullStore=new ProfileStore(nullFolder);
            check("Explicit null ink is protected instead of silently erasing drawings",nullStore.Load().writeProtected&&!nullStore.Save(new LocalProfile(),out _)&&File.ReadAllText(Path.Combine(nullFolder,"profile.json"))==original);
            var malformed=JsonUtility.FromJson<LocalProfile>(JsonUtility.ToJson(progress));malformed.notebookPages[0].inkStrokes[0].points[0]=new Vector2(2,0);string malformedFolder=Path.Combine(folder,"bad-coordinate");Raw(malformedFolder,JsonUtility.ToJson(malformed));
            check("Out-of-range persisted ink cannot enter the renderer",new ProfileStore(malformedFolder).Load().writeProtected);
        }
    }
}
#endif
