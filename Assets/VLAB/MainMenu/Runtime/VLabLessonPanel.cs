using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;
using VLAB.DemoLabs;

namespace VLAB.MainMenu
{
    /// <summary>Reuses the guided lesson's live controls in a fixed, compact bench panel.</summary>
    public sealed class VLabLessonPanel : MonoBehaviour
    {
        private VLabHud hud;
        private GameObject task, feedback;
        public static void Configure(VLabHud hud, VLabExperimentStation station, Camera view)
        {
            var canvas=new GameObject("VLAB Guided Lesson",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster),typeof(TrackedDeviceGraphicRaycaster)).GetComponent<Canvas>();
            canvas.transform.SetParent(hud.transform.parent,false);
            canvas.gameObject.layer=LayerMask.NameToLayer("UI");
            canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=view;
            ((RectTransform)canvas.transform).sizeDelta=new Vector2(680,720);
            canvas.transform.SetPositionAndRotation(station.transform.position+new Vector3(-1.50f,.86f,.05f),Quaternion.Euler(0,-12,0));
            canvas.transform.localScale=Vector3.one*.00165f;
            var layout=canvas.gameObject.AddComponent<VLabLessonPanel>();layout.hud=hud;
            layout.task=hud.transform.Find("TaskBar").gameObject;
            layout.feedback=hud.transform.Find("FeedbackBar").gameObject;
            Place(layout.task.transform,canvas.transform,0,190,680,340);
            Place(layout.feedback.transform,canvas.transform,0,-170,680,380);
            Place(hud.Modal.transform,canvas.transform,0,0,680,720);
            foreach(var child in new[]{"Brand","Discipline"})layout.task.transform.Find(child).gameObject.SetActive(false);
            Place(hud.StepText.transform,layout.task.transform,0,100,610,95);hud.StepText.fontSize=27;
            Place(hud.HintText.transform,layout.task.transform,0,-10,610,110);hud.HintText.fontSize=25;
            Place(hud.HomeButton.transform,layout.task.transform,-158,-122,294,58);
            Place(hud.ResetButton.transform,layout.task.transform,158,-122,294,58);
            Place(hud.FeedbackText.transform,layout.feedback.transform,0,95,610,135);hud.FeedbackText.fontSize=25;
            Place(hud.ContextText.transform,layout.feedback.transform,0,-38,610,105);hud.ContextText.fontSize=23;
            Place(hud.CheckButton.transform,layout.feedback.transform,0,-144,610,62);
            var card=hud.Modal.transform.Find("Card");Place(card,hud.Modal.transform,0,0,680,720);
            Place(card.Find("Accent"),card,-337,0,5,720);
            Place(card.Find("Eyebrow"),card,0,314,606,40);
            Place(hud.ModalTitle.transform,card,0,232,606,110);hud.ModalTitle.fontSize=31;
            Place(hud.ModalBody.transform,card,0,-21,606,360);hud.ModalBody.fontSize=24;
            hud.ModalBody.enableAutoSizing=true;hud.ModalBody.fontSizeMin=21;hud.ModalBody.fontSizeMax=24;
            card.Find("Controls").gameObject.SetActive(false);
            Place(hud.ModalButton.transform,card,0,-294,606,70);
            foreach(var button in canvas.GetComponentsInChildren<Button>(true))
            {
                VLABUI.StyleButton(button);
                foreach(var label in button.GetComponentsInChildren<TMP_Text>(true))
                {Place(label.transform,button.transform,0,0,((RectTransform)button.transform).sizeDelta.x-20,54);label.fontSize=23;}
            }
            foreach(var image in new[]{layout.task.GetComponent<Image>(),layout.feedback.GetComponent<Image>(),card.GetComponent<Image>()})image.color=VLABUI.Surface;
            var ui=new VLABUI(Resources.Load<VLABMenuAssets>("VLABMenuAssets").font);
            ui.Ribbon(layout.task.transform,"THỰC HÀNH CÓ HƯỚNG DẪN",0,.965f,1,.12f);
            VLabCollapsiblePanel.Configure(canvas);
        }
        private static void Place(Transform item,Transform parent,float x,float y,float width,float height)
        {
            var rect=(RectTransform)item;rect.SetParent(parent,false);
            rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.pivot=new Vector2(.5f,.5f);
            rect.anchoredPosition=new Vector2(x,y);rect.sizeDelta=new Vector2(width,height);rect.localScale=Vector3.one;
        }
        private void LateUpdate()
        {
            if(hud==null)return;
            task.SetActive(!hud.ModalOpen);feedback.SetActive(!hud.ModalOpen);
        }
    }
}
