#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Vlab.Product;

// Actual settings callbacks and native geometry. Aesthetic acceptance and
// physical phone-viewer legibility remain separate checks.
public static class SettingsPresentationChecks
{
    public static IEnumerator Run(VlabApp app,Action<string> capture,Action<string,bool> check)
    {
        string profile=JsonUtility.ToJson(app.Profile);bool vi=app.Profile.locale!="en";
        check("Settings presentation starts at Home",app.Route=="home");
        yield return Press(app,"Home utility 0");
        string[] labels=vi?new[]{"Chung","Âm thanh","Đồ họa","Điều khiển","Giao diện","Tương tác"}:new[]{"General","Audio","Graphics","Controls","Style","Interaction"};
        for(int category=0;category<labels.Length;category++)
        {
            yield return Press(app,category==5?"Settings interaction tab":labels[category]);Canvas.ForceUpdateCanvases();
            var panel=app.GetComponentsInChildren<RectTransform>().Single(rect=>rect.gameObject.activeInHierarchy&&rect.name=="Settings");
            var title=panel.GetComponentsInChildren<TextMeshProUGUI>().Single(text=>text.name=="Settings section title");
            check("Settings category has a readable independent content heading: "+labels[category],title.text==labels[category]&&title.fontSize>=32);
            check("Settings Back is in the header, away from footer decisions",Find(app,"Settings back").GetComponent<RectTransform>().anchoredPosition.y==-28);
            var footer=panel.GetComponentsInChildren<Button>().Where(button=>button.GetComponent<RectTransform>().anchoredPosition.y<=-680).ToArray();
            check("Settings footer has one primary Apply and at most two quiet alternatives",footer.Length<=3&&footer.Single(button=>button.name=="Settings apply").GetComponent<Image>().color.a>.9f&&footer.Where(button=>button.name!="Settings apply").All(button=>button.GetComponent<Image>().color.a==0));
            foreach(var text in panel.GetComponentsInChildren<TextMeshProUGUI>())
            {text.ForceMeshUpdate(true,true);check("Native settings text fits: "+text.name+" / "+labels[category],!text.isTextOverflowing&&text.GetPreferredValues(text.text,text.rectTransform.rect.width,Mathf.Infinity).y<=text.rectTransform.rect.height+.75f);}
            foreach(var track in panel.GetComponentsInChildren<RectTransform>().Where(rect=>rect.name=="Settings switch track"))
            {
                var button=track.GetComponentInParent<Button>();var hit=button.GetComponent<RectTransform>();
                check("Visible switch uses the full72px row as its hit target",hit.rect.height>=72&&hit.rect.width>=1000&&button.targetGraphic==track.GetComponent<Image>()&&button.GetComponent<Image>().raycastTarget);
                var knob=track.GetComponentsInChildren<RectTransform>().Single(rect=>rect.name=="Settings switch knob");check("Switch thumb remains inside its track",knob.anchoredPosition.x>=0&&knob.anchoredPosition.x+knob.rect.width<=track.rect.width);
            }
            int expectedTracks=category==1||category==4?1:category==5?2:0;
            check("Settings value controls are grouped with continuous visible tracks",panel.GetComponentsInChildren<RectTransform>().Count(rect=>rect.name=="Settings value track")==expectedTracks);
            if(category==0||category==1||category==3||category==4||category==5){yield return null;yield return null;capture("70-cai-dat-"+new[]{"chung","am-thanh","do-hoa","dieu-khien","giao-dien","tuong-tac"}[category]+"-"+(vi?"VI":"EN"));}
        }
        yield return Press(app,vi?"Hủy":"Cancel");
        check("Settings presentation review leaves every saved preference, note and reading unchanged",app.Route=="home"&&JsonUtility.ToJson(app.Profile)==profile);
    }
    static Button Find(VlabApp app,string name)=>app.GetComponentsInChildren<Button>().Single(button=>button.isActiveAndEnabled&&(button.name==name||button.GetComponentInChildren<TextMeshProUGUI>()?.text==name));
    static IEnumerator Press(VlabApp app,string name)
    {yield return new WaitForSecondsRealtime(.28f);var button=Find(app,name);if(!button.interactable)throw new InvalidOperationException("Settings target disabled: "+name);button.onClick.Invoke();yield return new WaitForSecondsRealtime(.28f);}
}
#endif
