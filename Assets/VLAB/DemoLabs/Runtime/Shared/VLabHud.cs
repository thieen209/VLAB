using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VLAB.DemoLabs
{
    public sealed class VLabHud : MonoBehaviour
    {
        public TMP_Text StepText, HintText, ContextText, FeedbackText, ModalTitle, ModalBody;
        public Image ProgressFill;
        public GameObject Modal;
        public Button ModalButton, ResetButton, HomeButton, CheckButton;
        public TMP_Text ModalButtonLabel;
        public bool ModalOpen => Modal.activeSelf;
        private float feedbackExpires;
        private int feedbackFrame = -1;
        private void Update()
        {
            if (FeedbackText.text.Length != 0 && Time.unscaledTime >= feedbackExpires) FeedbackText.text = "";
        }
        public void Step(int number, int count, string title, string hint)
        {
            if (HintText.text != hint && feedbackFrame != Time.frameCount) FeedbackText.text = "";
            StepText.text = $"BƯỚC {number:00} / {count:00}   ·   {title}";
            HintText.text = hint;
            ProgressFill.fillAmount = number / (float)count;
            ProgressFill.rectTransform.anchorMax = new Vector2(number / (float)count, 1);
        }
        public void Context(string text) { if (ContextText.text != text) ContextText.text = text; }
        public void Feedback(string text, bool warning = false)
        {
            FeedbackText.text = text;
            feedbackExpires = Time.unscaledTime + 8f; feedbackFrame = Time.frameCount;
            FeedbackText.color = warning ? new Color(1, .75f, .39f) : new Color(.63f, .91f, .84f);
        }
        public void ShowModal(string title, string body, string action, Action callback)
        {
            ModalTitle.text = title; ModalBody.text = body; ModalButtonLabel.text = action;
            ModalButton.onClick.RemoveAllListeners();
            ModalButton.onClick.AddListener(() => { Modal.SetActive(false); callback?.Invoke(); });
            Modal.SetActive(true);
        }
        public void HideModal() => Modal.SetActive(false);
    }
}
