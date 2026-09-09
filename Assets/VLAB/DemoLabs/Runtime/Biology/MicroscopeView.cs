using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VLAB.DemoLabs
{
    public sealed class MicroscopeView : MonoBehaviour, IPointerClickHandler
    {
        public BiologyExperiment Experiment;
        public Camera SpecimenCamera;
        public Renderer SpecimenPlane;
        public RawImage ScopeImage, MonitorImage;
        public GameObject ScopePanel;
        public CanvasGroup ScopeFade;
        public TMP_Text Status, Prompt, Annotation;
        public Button ExitButton, ObjectiveButton;
        public VLabDialDrag CoarseDial, FineDial, LightDial;
        public Transform EyepieceView;
        public RectTransform OpticalRim;
        public RectTransform[] IdentificationMarkers, IdentificationLines, IdentificationLabels;
        public bool Inspecting { get; private set; }
        public RenderTexture Texture { get; private set; }
        private Material specimenMaterial;
        private Vector3 savedPosition;
        private Quaternion savedRotation;
        private float transition;
        private bool transitioning;
        private float lastFocus = -1, lastLight = -1;
        private int lastObjective = -1;
        private bool lastActive;
        private int lastIdentified = -1;
        private float displayedBlur = .12f, renderedBlur = -1;
        private int lastUiState = -1;
        private Vector2 lastPanelSize;
        private void FitOpticalField()
        {
            var size = ((RectTransform)ScopePanel.transform).rect.size;
            if (size == lastPanelSize) return;
            lastPanelSize = size;
            var diameter = Mathf.Max(80, Mathf.Min(550, size.y - 70, size.x * .56f));
            OpticalRim.localScale = Vector3.one * (diameter / 550f);
            OpticalRim.anchoredPosition = new Vector2(0, 18);
        }
        private void Start()
        {
            Texture = new RenderTexture(768, 768, 16, RenderTextureFormat.ARGB32) { name = "VLAB microscope live view", antiAliasing = 1 };
            Texture.Create(); SpecimenCamera.targetTexture = Texture;
            ScopeImage.texture = Texture; MonitorImage.texture = Texture;
            specimenMaterial = SpecimenPlane.material;
            ExitButton.onClick.AddListener(Exit);
            ObjectiveButton.onClick.AddListener(() => Experiment.Nosepiece.Activate());
            ScopePanel.SetActive(false); SpecimenCamera.enabled = false;
        }
        private void OnDestroy()
        {
            if (Texture != null) { Texture.Release(); Destroy(Texture); }
            if (specimenMaterial != null) Destroy(specimenMaterial);
        }
        public void Enter()
        {
            if (Inspecting) return;
            Inspecting = true; transitioning = true; transition = 0;
            savedPosition = Experiment.Driver.ViewCamera.transform.position;
            savedRotation = Experiment.Driver.ViewCamera.transform.rotation;
            Experiment.Driver.ReturnHeld(); Experiment.Driver.ViewLocked = true;
            ScopePanel.SetActive(true); ScopeFade.alpha = 0;
            Canvas.ForceUpdateCanvases(); FitOpticalField();
        }
        public void Exit()
        {
            if (!Inspecting) return;
            Inspecting = false; transitioning = false;
            ScopePanel.SetActive(false); Experiment.Driver.ViewLocked = false;
            Experiment.Driver.ViewCamera.transform.SetPositionAndRotation(savedPosition, savedRotation);
        }
        private void Update()
        {
            if (specimenMaterial == null) return;
            var model = Experiment.Model;
            var active = model.Mounted && model.Clips && model.Objective != 0;
            specimenMaterial.SetFloat("_Prepared", active ? 1 : 0);
            var targetBlur = Mathf.Clamp(model.FocusError * 1.4f, 0, .12f);
            displayedBlur = Mathf.MoveTowards(displayedBlur, targetBlur, Time.unscaledDeltaTime * .6f);
            specimenMaterial.SetFloat("_Blur", displayedBlur);
            specimenMaterial.SetFloat("_Brightness", model.Light);
            specimenMaterial.SetFloat("_Annotated", model.Identified >= 3 ? 1 : 0);
            SpecimenCamera.orthographicSize = model.Objective == 40 ? .25f : 1;
            // Render on optical changes; stable specimens do not need an extra camera every frame.
            if (renderedBlur != displayedBlur || lastFocus != model.Focus || lastLight != model.Light || lastObjective != model.Objective || lastActive != active || lastIdentified != model.Identified)
            {
                SpecimenCamera.Render(); lastFocus = model.Focus; lastLight = model.Light; lastObjective = model.Objective;
                lastActive = active; lastIdentified = model.Identified;
                renderedBlur = displayedBlur;
            }
            if (!Inspecting) return;
            FitOpticalField();
            if (transitioning)
            {
                transition = Mathf.Min(1, transition + Time.unscaledDeltaTime * 2.1f);
                var t = Mathf.SmoothStep(0, 1, transition);
                Experiment.Driver.ViewCamera.transform.SetPositionAndRotation(Vector3.Lerp(savedPosition, EyepieceView.position, t), Quaternion.Slerp(savedRotation, EyepieceView.rotation, t));
                ScopeFade.alpha = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.35f, 1, transition));
                transitioning = transition < 1;
            }
            if (Mathf.Abs(displayedBlur - targetBlur) < .002f) model.Observe();
            var uiState = model.Objective + (model.Sharp ? 100 : 0) + (model.Observed10 ? 200 : 0) + (model.Observed40 ? 400 : 0) + model.Identified * 1000 + (model.Light < .2f ? 10000 : 0);
            if (lastUiState == uiState) return;
            lastUiState = uiState;
            Status.text = $"VẬT KÍNH {model.Objective}×  ·  THỊ KÍNH 10×  ·  TỔNG {model.Objective * 10}×\n" + (model.Light < .2f ? "Tăng ánh sáng để nhìn rõ tiêu bản" : model.Sharp ? "Ảnh rõ nét" : "Xoay ốc lấy nét để làm rõ tiêu bản");
            Prompt.text = !model.Observed10 ? "Ở 10×: dùng ốc sơ cấp rồi vi cấp để thấy rõ thành tế bào."
                : !model.Observed40 ? "Chuyển sang 40×, dùng ốc vi cấp để lấy nét lại."
                : model.Identified == 0 ? "Chọn trực tiếp NHÂN TẾ BÀO trong ảnh."
                : model.Identified == 1 ? "Chọn THÀNH TẾ BÀO — đường bao mỗi tế bào."
                : model.Identified == 2 ? "Chọn vùng TẾ BÀO CHẤT — lớp nhuộm quanh không bào."
                : "Đã nhận biết đủ ba cấu trúc.";
            Annotation.text = model.Identified >= 3 ? "Tím đậm: nhân  ·  Đường bao: thành tế bào\nVùng nhuộm sát thành: tế bào chất  ·  Vùng nhạt ở giữa: không bào" : "Tiêu bản minh họa có tăng tương phản nhân; không phải ảnh chụp mẫu thật.";
        }
        public void ResetAnnotations()
        {
            lastUiState = -1;
            foreach (var item in IdentificationMarkers) item.gameObject.SetActive(false);
            foreach (var item in IdentificationLines) item.gameObject.SetActive(false);
            foreach (var item in IdentificationLabels) item.gameObject.SetActive(false);
        }
        public void ShowIdentification(int index, Vector2 specimenUv)
        {
            if (index < 0 || index >= IdentificationMarkers.Length) return;
            var viewUv = (specimenUv - Vector2.one * .5f) / (Experiment.Model.Objective == 40 ? .25f : 1) + Vector2.one * .5f;
            var position = Vector2.Scale(viewUv - Vector2.one * .5f, ScopeImage.rectTransform.rect.size);
            IdentificationMarkers[index].anchoredPosition = position;
            var labelPosition = IdentificationLabels[index].anchoredPosition;
            var delta = labelPosition - position;
            var line = IdentificationLines[index]; line.anchoredPosition = (position + labelPosition) * .5f;
            line.sizeDelta = new Vector2(delta.magnitude, 1.5f); line.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            IdentificationMarkers[index].gameObject.SetActive(true); line.gameObject.SetActive(true); IdentificationLabels[index].gameObject.SetActive(true);
        }
        public Vector2 ViewToSpecimen(Vector2 viewUv) => (viewUv - Vector2.one * .5f) * (Experiment.Model.Objective == 40 ? .25f : 1) + Vector2.one * .5f;
        public void OnPointerClick(PointerEventData eventData)
        {
            if (!Inspecting || !RectTransformUtility.ScreenPointToLocalPointInRectangle(ScopeImage.rectTransform, eventData.position, eventData.pressEventCamera, out var point)) return;
            var rect = ScopeImage.rectTransform.rect;
            var uv = new Vector2((point.x - rect.xMin) / rect.width, (point.y - rect.yMin) / rect.height);
            if (Vector2.Distance(uv, Vector2.one * .5f) > .49f) return;
            Experiment.IdentifyAt(ViewToSpecimen(uv));
        }
    }
}
