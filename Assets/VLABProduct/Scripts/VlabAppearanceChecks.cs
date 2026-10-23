#if UNITY_EDITOR
using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using Vlab.Product;

// Deliberately separate persistence fixtures from actual user scientific observations.
public static class VlabAppearanceChecks
{
    [Serializable] sealed class Envelope {public int format=1;public string payload,sha256;}
    public static void Core(Action<string,bool> check)
    {
        var profile=new LocalProfile{locale="vi"};
        foreach(string id in LessonSpec.All.Keys)profile.attempts.Add(new AttemptRecord{experimentId=id,attemptId="appearance-migration-fixture",savedAtUtc="2026-10-19T00:00:00.0000000Z",model="TEST FIXTURE",modelNotesVi="Ghi chú kiểm thử",modelNotesEn="Fixture note"});
        string folder=Path.Combine(Path.GetTempPath(),"VlabAppearance-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        try
        {
            var store=new ProfileStore(folder);check("Appearance fixture saves",store.Save(profile,out _));
            string file=Path.Combine(folder,"profile.json");var envelope=JsonUtility.FromJson<Envelope>(File.ReadAllText(file));
            envelope.payload=Regex.Replace(envelope.payload,"\\\"uiOpacity\\\"\\s*:\\s*[-+0-9.eE]+,?","").Replace(",}","}");
            envelope.sha256=ProfileStore.Hash(envelope.payload);File.WriteAllText(file,JsonUtility.ToJson(envelope));
            var legacy=new ProfileStore(folder).Load();check("Legacy absent opacity migrates to one without discarding 32 records",!legacy.writeProtected&&!legacy.recovered&&legacy.profile.settings.uiOpacity==1&&legacy.profile.attempts.Count==32);
            profile.attempts[0].assistance="guided-preparation";profile.settings.uiTheme=2;profile.settings.uiOpacity=.55f;check("Appearance settings save",store.Save(profile,out _));
            var loaded=new ProfileStore(folder).Load();check("Theme, opacity and bilingual record notes round trip",loaded.profile.settings.uiTheme==2&&Math.Abs(loaded.profile.settings.uiOpacity-.55f)<.0001f&&loaded.profile.attempts[0].modelNotesEn=="Fixture note"&&loaded.profile.attempts[0].assistance=="guided-preparation");
            var copy=loaded.profile.settings.Copy();check("Draft appearance copy is independent",copy!=loaded.profile.settings&&copy.uiTheme==2&&copy.uiOpacity==loaded.profile.settings.uiOpacity);
            bool invalid=false;try{copy.uiOpacity=0;copy.Validate();}catch(InvalidDataException){invalid=true;}check("Explicit zero opacity is rejected",invalid);
            invalid=false;try{copy.uiOpacity=float.NaN;copy.Validate();}catch(InvalidDataException){invalid=true;}check("Non-finite opacity is rejected",invalid);
            foreach(int index in Enumerable.Range(0,3))
            {
                var palette=UiTheme.Palette(index);check("Theme "+index+" paragraph contrast",UiTheme.Contrast(palette.text,palette.panel)>=4.5f&&UiTheme.Contrast(palette.secondary,palette.panel)>=4.5f);
                foreach(string hex in new[]{"4C86FF","D85BEA","56BF88","F0A34D"})check("Theme "+index+" accent "+hex+" caption contrast",UiTheme.Contrast(UiTheme.On(VlabUi.Hex(hex)),VlabUi.Hex(hex))>=4.5f);
            }
            check("Journal distinguishes no attempt from an actual saved attempt",JournalView.Status(new LocalProfile(),"P01",false)=="No saved attempts"&&JournalView.Status(profile,"P01",false)=="1 saved attempt");
            check("Journal uses attached historical interpretation",JournalView.Interpretation(profile.attempts[0],true).EndsWith("Ghi chú kiểm thử")&&JournalView.Interpretation(profile.attempts[0],true).Contains("Chuẩn bị có hỗ trợ"));
        }
        finally {if(Directory.Exists(folder))Directory.Delete(folder,true);}
    }
    // Run after the normal smoke returns Home. Captures actual routes via their UI callbacks.
    // The capture callback must use the existing root screenshot routine; this does not fabricate artwork.
    public static IEnumerator CaptureThemes(VlabApp app,Action<string> capture,Action<string,bool> check,bool checkNativeSeries)
    {
        if(app.Route!="home")throw new InvalidOperationException("Appearance checks must begin at Home.");
        var canvas=app.GetComponentInChildren<Canvas>();var host=new GameObject("Isolated live-scroll fixture",typeof(RectTransform));host.transform.SetParent(canvas.transform,false);
        var fixtureUi=new VlabUi(host.transform);var fixture=fixtureUi.Scroll(fixtureUi.Page("Scroll fixture"),"Short reading",20,20,500,300).GetComponent<VlabScrollContent>();
        yield return null;check("Short live reading has no paging",!fixture.Down.interactable);float small=fixture.Body.rectTransform.rect.height;
        fixture.Body.text=string.Join("\n",Enumerable.Range(0,50).Select(i=>"Live observation "+i));yield return null;yield return null;
        check("Live readings grow their scroll extent and enable paging",fixture.Body.rectTransform.rect.height>small&&fixture.Down.interactable);
        for(int i=0;i<30;i++)fixture.Down.onClick.Invoke();check("Live reading's last paragraph is reachable",fixture.Scroll.verticalNormalizedPosition<.001f);
        fixture.Body.text="Short again";yield return null;yield return null;check("Shrinking readings clamp offset and disable paging",!fixture.Down.interactable&&Mathf.Abs(fixture.Body.rectTransform.anchoredPosition.y)<.001f);UnityEngine.Object.Destroy(host);yield return null;
        int originalTheme=app.Profile.settings.uiTheme;float originalOpacity=app.Profile.settings.uiOpacity;bool wasSimulator=app.Input.SimulatorMode;
        app.Input.SetSimulator(true);yield return new WaitForSecondsRealtime(.3f);
        for(int index=0;index<3;index++)
        {
            yield return OpenAppearance(app);yield return Press("Appearance theme "+index);
            for(int n=0;n<10;n++)yield return Press("Appearance opacity plus");
            yield return Press(app.Profile.locale=="en"?"Apply":"Áp dụng");
            check("Committed theme "+index+" recreates Home",app.Route=="home"&&app.Profile.settings.uiTheme==index&&UiTheme.Index==index);
            yield return null;yield return null;capture("21-theme-"+index+"-home-VR");
            yield return Press("Open journal");yield return Press("Journal mode records");yield return null;yield return null;capture("22-theme-"+index+"-journal-VR");
            if(index==0&&checkNativeSeries)
            {
                var data=ExperimentNotebook.BuildData("P01",app.Profile.attempts);check("Real native pendulum series has four lengths with three independent records",data.groups.Any(group=>group.fit?.eligible==true&&group.summaries.Count==4&&group.summaries.All(bin=>bin.n==3)));
                yield return Press("Journal compare");yield return null;yield return null;
                foreach(string endpoint in new[]{"0.4","1"})check("Plot X endpoint "+endpoint+" fits its actual text frame",app.GetComponentsInChildren<TMPro.TextMeshProUGUI>().Any(text=>text.text==endpoint&&!text.isTextOverflowing));
                capture("24-pendulum-native-series-VR");
                yield return Press(app.Profile.locale=="en"?"‹ Back":"‹ Quay lại");
            }
            yield return Press(app.Profile.locale=="en"?"‹ Back":"‹ Quay lại");
        }
        yield return OpenAppearance(app);for(int n=0;n<12;n++)yield return Press("Appearance opacity minus");
        yield return Press(app.Profile.locale=="en"?"Cancel":"Hủy");check("Appearance Cancel leaves committed opacity unchanged",app.Profile.settings.uiOpacity==1);
        yield return OpenAppearance(app);for(int n=0;n<12;n++)yield return Press("Appearance opacity minus");yield return Press(app.Profile.locale=="en"?"Apply":"Áp dụng");
        check("Opacity floor is 55 percent",Mathf.Abs(app.Profile.settings.uiOpacity-.55f)<.001f);
        yield return null;yield return null;capture("23-opacity-55-VR");
        yield return OpenAppearance(app);yield return Press("Appearance theme "+originalTheme);for(int n=0;n<10;n++)yield return Press("Appearance opacity plus");
        int decreases=Mathf.RoundToInt((1-originalOpacity)/.05f);for(int n=0;n<decreases;n++)yield return Press("Appearance opacity minus");yield return Press(app.Profile.locale=="en"?"Apply":"Áp dụng");
        app.Input.SetSimulator(wasSimulator);yield return new WaitForSecondsRealtime(.25f);
    }
    static IEnumerator OpenAppearance(VlabApp app){yield return Press("Home utility 0");yield return Press(app.Profile.locale=="en"?"Style":"Giao diện");}
    static IEnumerator Press(string name)
    {
        yield return new WaitForSecondsRealtime(.26f);
        var button=UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(value=>value.gameObject.activeInHierarchy&&value.interactable&&(value.name==name||value.GetComponentInChildren<TMPro.TextMeshProUGUI>()?.text==name));
        if(!button)throw new InvalidOperationException("Missing appearance UI button: "+name);button.onClick.Invoke();yield return null;
    }
}
#endif
