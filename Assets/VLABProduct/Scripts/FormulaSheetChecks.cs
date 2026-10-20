#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Vlab.Product;

public static class FormulaSheetChecks
{
    public static IEnumerator Run(VlabApp app,Action<string> capture,Action<string,bool> check)
    {
        var log=new List<string>();string profile=JsonUtility.ToJson(app.Profile);
        Action<string,bool> verify=(name,pass)=>{log.Add((pass?"PASS ":"FAIL ")+name);check(name,pass);if(!pass)throw new InvalidOperationException(name);};
        verify("All32 approved lessons have offline formula sheets",FormulaSheet.All.Keys.OrderBy(id=>id).SequenceEqual(LearningGuide.All.Keys.OrderBy(id=>id)));
        foreach(var item in FormulaSheet.All.Values)
        {
            string hash;using(var sha=SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(LearningGuide.Load(item.id).learnVi))).Replace("-","").ToLowerInvariant();
            verify("Formula provenance matches approved Knowledge "+item.id,hash==item.sourceSha256);
            for(int p=0;p<item.pages;p++){var texture=FormulaSheet.Image(item.id,p);verify("Actual offline equation texture "+item.id+" page "+p,texture&&texture.width==1800&&texture.height==600);}
        }
        var owner=new GameObject("Temporary formula UI checks");
        try
        {
            var ui=new VlabUi(owner.transform);ui.canvas.enabled=false;
            foreach(bool vi in new[]{true,false})foreach(bool large in new[]{false,true})foreach(var item in FormulaSheet.All.Values)
            {
                ui.Vietnamese=vi;ui.LargeText=large;var state=new JournalState{studyChapter=4};RectTransform host=null;
                Action draw=null;draw=()=>{if(host)UnityEngine.Object.DestroyImmediate(host.gameObject);host=ui.Rect(ui.surface,"Formula native template",0,0,1100,650);LearningGuide.Render(ui,host,item.id,vi,state,draw,Color.cyan);Canvas.ForceUpdateCanvases();};draw();
                for(int p=0;p<item.pages;p++)
                {
                    var image=host.GetComponentsInChildren<RawImage>().Single(raw=>raw.name=="Journal formula image");
                    verify("Formula native image keeps3:1 proportion "+item.id+" "+p,image.texture==FormulaSheet.Image(item.id,p)&&Mathf.Abs(image.rectTransform.rect.width/image.rectTransform.rect.height-3)<.001f);
                    foreach(var text in host.GetComponentsInChildren<TMP_Text>()){text.ForceMeshUpdate(true,true);verify("Formula controls fit "+item.id+" "+vi+" large="+large+" "+text.text,!text.isTextOverflowing);}
                    var next=host.GetComponentsInChildren<Button>().Single(button=>button.name=="Journal content next");verify("Only real remaining formula pages enabled "+item.id,next.interactable==(p+1<item.pages));if(next.interactable)next.onClick.Invoke();
                }
                UnityEngine.Object.DestroyImmediate(host.gameObject);
            }
        }
        finally{UnityEngine.Object.DestroyImmediate(owner);}
        const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
        var journal=(JournalState)typeof(VlabApp).GetField("journal",flags).GetValue(app);string saved=JsonUtility.ToJson(journal);
        try
        {
            foreach(string id in new[]{"P01","C08","M08"})
            {
                journal.notesMode=false;journal.subject=id.Substring(0,1);journal.experiment=id;journal.contentTab=4;journal.studyChapter=4;journal.contentPage=0;
                typeof(VlabApp).GetMethod("ShowNotebook",flags).Invoke(app,new object[]{"home"});yield return new WaitForSecondsRealtime(.3f);
                verify("Actual notebook opens formula image without changing learner data "+id,app.GetComponentsInChildren<RawImage>().Any(image=>image.name=="Journal formula image"&&image.texture==FormulaSheet.Image(id,0)));
                capture("91-cong-thuc-"+id+"-VI");
            }
        }
        finally{JsonUtility.FromJsonOverwrite(saved,journal);app.ShowHome();verify("Formula viewing preserves profile and scientific attempts",JsonUtility.ToJson(app.Profile)==profile);Directory.CreateDirectory("Evidence/Runtime");File.WriteAllLines("Evidence/Runtime/formula-sheets.txt",new[]{"Offline notation / native template geometry / actual book-route captures; not experimental measurements or physical-device certification."}.Concat(log));}
    }
}
#endif
