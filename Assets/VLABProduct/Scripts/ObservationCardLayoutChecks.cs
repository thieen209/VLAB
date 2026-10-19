#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using Vlab.Product;

// Native TMP template geometry only; this does not certify whole-room or headset visuals.
public static class ObservationCardLayoutChecks
{
    struct Template {public string name;public float width,height;public int size;public Template(string n,float w,float h,int s){name=n;width=w;height=h;size=s;}}
    public static void Run(IEnumerable<AttemptRecord> records,Action<string,bool> check)
    {
        var saved=records.ToArray();check("Card layout input covers original47 saved attempts and32 lesson IDs",saved.Length>=47&&saved.Select(record=>record.experimentId).Distinct().Count()==32);
        var originals=saved.Select(record=>JsonUtility.ToJson(record)).ToArray();var receipt=new List<string>{"Native TMP geometry for saved-reading cards only; no whole-screen, Android or physical headset claim.","Attempts="+saved.Length+" distinctLessons="+saved.Select(record=>record.experimentId).Distinct().Count()};
        var outcomes=new List<KeyValuePair<string,bool>>();var templates=new[]{new Template("instrument",920,420,4),new Template("results",1360,400,4),new Template("book",1036,242,2)};
        double started=Time.realtimeSinceStartupAsDouble;int totalCards=0,totalFields=0;var owner=new GameObject("Temporary observation-card layout checks"){hideFlags=HideFlags.HideAndDontSave};VlabUi ui=null;bool previousLarge=false;
        try
        {
            ui=new VlabUi(owner.transform);previousLarge=ui.LargeText;ui.canvas.enabled=false;var host=ui.Rect(ui.surface,"Card geometry template",0,0,1600,1000);
            foreach(bool vi in new[]{true,false})foreach(bool large in new[]{false,true})foreach(var template in templates)
            {
                ui.Vietnamese=vi;ui.LargeText=large;bool passed=true;int cards=0,fields=0;
                foreach(var record in saved)for(int page=0;page<ObservationCards.PageCount(record.readings,template.size);page++)
                {
                    ObservationCards.Render(ui,host,record.readings,vi,page,0,0,template.width,template.height,template.size);Canvas.ForceUpdateCanvases();
                    var bindings=host.GetComponentsInChildren<ObservationCardBinding>();var expected=record.readings.Skip(page*template.size).Take(template.size).ToArray();
                    if(!bindings.Select(binding=>binding.Key).SequenceEqual(expected.Select(reading=>reading.key))){passed=false;receipt.Add("FAIL missing/reordered readings: "+record.experimentId+" "+template.name+" page="+page);}
                    foreach(var binding in bindings)
                    {
                        cards++;CheckField(binding.Label,26,"label");CheckField(binding.Value,48,"value");CheckField(binding.Unit,26,"unit");
                        void CheckField(TextMeshProUGUI text,float minimum,string role)
                        {
                            fields++;text.ForceMeshUpdate(true,true);float expectedSize=minimum*(large?1.12f:1);bool okay=!text.isTextOverflowing&&text.fontSize>=expectedSize-.01f;
                            if(okay)return;passed=false;var rect=text.rectTransform.rect;Vector2 needed=text.GetPreferredValues(text.text,rect.width,Mathf.Infinity);
                            receipt.Add("FAIL "+record.experimentId+" "+template.name+" "+(vi?"VI":"EN")+" large="+large+" key="+binding.Key+" field="+role+" font="+text.fontSize.ToString("0.##",CultureInfo.InvariantCulture)+" expectedMin="+expectedSize.ToString("0.##",CultureInfo.InvariantCulture)+" rect="+rect.width+"x"+rect.height+" preferred="+needed.x.ToString("0.##",CultureInfo.InvariantCulture)+"x"+needed.y.ToString("0.##",CultureInfo.InvariantCulture)+" overflow="+text.isTextOverflowing+" text="+text.text.Replace("\n"," "));
                        }
                    }
                    foreach(Transform child in host.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
                }
                totalCards+=cards;totalFields+=fields;string scope=template.name+" "+(vi?"VI":"EN")+" large="+large+" cards="+cards+" fields="+fields;receipt.Add((passed?"PASS ":"FAIL ")+scope);outcomes.Add(new KeyValuePair<string,bool>("Native card template fits: "+scope,passed));
            }
        }
        finally
        {
            if(ui!=null)ui.LargeText=previousLarge;UnityEngine.Object.DestroyImmediate(owner);Directory.CreateDirectory("Evidence/Runtime");receipt.Add("Total cards="+totalCards+" fields="+totalFields+" elapsedSeconds="+(Time.realtimeSinceStartupAsDouble-started).ToString("0.00",CultureInfo.InvariantCulture));File.WriteAllLines("Evidence/Runtime/observation-card-layout.txt",receipt);
        }
        check("Native card geometry leaves every saved record unchanged",originals.SequenceEqual(saved.Select(record=>JsonUtility.ToJson(record))));foreach(var outcome in outcomes)check(outcome.Key,outcome.Value);
    }
}
#endif
