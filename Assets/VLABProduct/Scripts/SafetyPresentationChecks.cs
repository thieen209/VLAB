#if UNITY_EDITOR
using System;
using System.Linq;
using System.Collections;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vlab.Product
{
    public static class SafetyPresentationChecks
    {
        public static IEnumerator Capture(VlabApp app,Action<string> capture,Action<string,bool> check)
        {
            string before=JsonUtility.ToJson(app.Profile);
            typeof(VlabApp).GetMethod("ShowSafety",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(app,null);
            yield return new WaitForSecondsRealtime(.3f);
            check("Actual safety screen contains all three illustrated instructions",app.Route=="safety"&&app.GetComponentsInChildren<RectTransform>().Count(rect=>rect.name.StartsWith("Safety instruction ",StringComparison.Ordinal))==3);
            capture("51-an-toan");app.ShowHome();yield return new WaitForSecondsRealtime(.3f);
            check("Reviewing illustrated safety preserves consent, records and settings",app.Route=="home"&&JsonUtility.ToJson(app.Profile)==before);
        }
        public static void Run(Action<string,bool> check)
        {
            foreach(bool vi in new[]{true,false})foreach(bool large in new[]{false,true})
            {
                var owner=new GameObject("Isolated safety layout check");
                try
                {
                    var ui=new VlabUi(owner.transform){LargeText=large};var page=ui.Page("Safety check");var box=ui.Panel(page,"Safety",370,190,860,620,VlabUi.PanelColor);
                    SafetyPresentation.Instructions(ui,box,vi);Canvas.ForceUpdateCanvases();
                    var rows=box.GetComponentsInChildren<RectTransform>().Where(rect=>rect.name.StartsWith("Safety instruction ",StringComparison.Ordinal)).ToArray();
                    check("Three illustrated safety instructions "+vi+"/"+large,rows.Length==3&&rows.All(row=>row.GetComponentsInChildren<Image>().Length>=3));
                    check("Safety illustrations do not intercept input "+vi+"/"+large,rows.All(row=>row.GetComponentsInChildren<Image>().All(image=>!image.raycastTarget)));
                    foreach(var text in box.GetComponentsInChildren<TextMeshProUGUI>()){text.ForceMeshUpdate(true,true);check("Safety text fits "+vi+"/"+large+" "+text.text,!text.isTextOverflowing);}
                }
                finally{UnityEngine.Object.DestroyImmediate(owner);}
            }
        }
    }
}
#endif
