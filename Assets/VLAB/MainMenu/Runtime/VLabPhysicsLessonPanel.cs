using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VLAB.PhysicsLab.Education;

namespace VLAB.MainMenu
{
    /// <summary>Preserves live measurements and callbacks while replacing the legacy station layout.</summary>
    public sealed class VLabPhysicsLessonPanel : MonoBehaviour
    {
        private Material textMaterial;
        private void Start()
        {
            var canvas=GetComponent<Canvas>();
            canvas.worldCamera=Camera.main;
            var rect=(RectTransform)transform;rect.sizeDelta=new Vector2(680,720);
            rect.position=new Vector3(-1.6f,1.92f,-.15f);rect.rotation=Quaternion.Euler(0,-12,0);rect.localScale=Vector3.one*.0017f;
            var panel=transform.Find("ContextualLabPanel");
            Place(panel,0,0,680,720);panel.GetComponent<Image>().color=VLABUI.Surface;
            Place(panel.Find("ExperimentTitle"),0,250,606,100);
            Place(panel.Find("CurrentActionCue"),0,90,606,190);
            Place(panel.Find("AdaptiveHint"),0,-58,606,95);
            Place(panel.Find("LiveMeasurement"),0,-150,606,78);
            string[] controls={"BackToHub","ResetTrial","OpenResults","Hint","OpenSettings","OpenHelp"};
            string[] labels={"Chọn bài","Đặt lại lượt","Kết quả","Gợi ý","Cài đặt","Lý thuyết"};
            for(int i=0;i<controls.Length;i++)
            {
                var target=panel.Find(controls[i]);Place(target,(i%3-1)*210,-240-(i/3)*68,196,58);
                target.GetComponentInChildren<TMP_Text>(true).text=labels[i];
            }
            Route(panel.Find("BackToHub"),()=>FindAnyObjectByType<VLABApplicationUI>()?.SelectExperiment());
            Route(panel.Find("OpenSettings"),()=>OpenShared("settings"));
            Route(panel.Find("OpenHelp"),()=>OpenShared("help"));
            transform.Find("SettingsPanel").gameObject.SetActive(false);
            transform.Find("HelpPanel").gameObject.SetActive(false);
            var results=transform.Find("ResultsPanel");
            // Results retain the existing scrollable measured-data sheet on this same station.
            Place(results,0,0,680,720);
            foreach(RectTransform child in results)
            {
                child.anchoredPosition=Vector2.Scale(child.anchoredPosition,new Vector2(1.22f,1.52f));
                child.sizeDelta=Vector2.Scale(child.sizeDelta,new Vector2(1.2f,1.4f));
            }
            var assets=Resources.Load<VLABMenuAssets>("VLABMenuAssets");
            textMaterial=new Material(assets.activityFont.material);textMaterial.SetColor(ShaderUtilities.ID_FaceColor,Color.white);
            foreach(var label in GetComponentsInChildren<TMP_Text>(true))
            {
                label.font=assets.activityFont;label.fontSharedMaterial=textMaterial;label.color=VLABUI.White;
                label.fontSize=label.name=="ExperimentTitle"?31:label.name=="CurrentActionCue"?28:24;
                label.enableAutoSizing=false;label.raycastTarget=false;
                label.textWrappingMode=TextWrappingModes.Normal;
            }
            foreach(var button in GetComponentsInChildren<Button>(true))
            {
                VLABUI.StyleButton(button);
                foreach(var label in button.GetComponentsInChildren<TMP_Text>(true))
                {
                    label.color=VLABUI.White;label.fontSize=23;
                    var textRect=label.rectTransform;textRect.anchorMin=Vector2.zero;textRect.anchorMax=Vector2.one;textRect.offsetMin=new Vector2(8,4);textRect.offsetMax=new Vector2(-8,-4);
                }
            }
            var ui=new VLABUI(assets.font);ui.Ribbon(panel,"VẬT LÍ / THỰC HÀNH",0,.96f,.88f,.075f);
            VLabCollapsiblePanel.Configure(canvas);
        }
        private static void Place(Transform item,float x,float y,float w,float h)
        {
            var rect=(RectTransform)item;rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);
            rect.anchoredPosition=new Vector2(x,y);rect.sizeDelta=new Vector2(w,h);
        }
        private static void Route(Transform target,UnityEngine.Events.UnityAction action)
        {var button=target.GetComponent<Button>();button.onClick=new Button.ButtonClickedEvent();button.onClick.AddListener(action);}
        private static void OpenShared(string page)
        {var app=FindAnyObjectByType<VLABApplicationUI>();if(app==null)return;if(!app.IsPaused)app.TogglePause();app.Show(page);}
        private void OnDestroy(){if(textMaterial!=null)Destroy(textMaterial);}
    }
}
