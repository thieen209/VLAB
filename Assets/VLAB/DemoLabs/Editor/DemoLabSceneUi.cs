using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace VLAB.DemoLabs.Editor
{
    public static partial class DemoLabSceneBuilder
    {
        private static readonly Color uiNavy = new Color(.025f, .06f, .08f, .97f);
        private static readonly Color uiTeal = new Color(.04f, .39f, .34f, 1);
        private static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 size, Vector2 position)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            var rect = obj.GetComponent<RectTransform>(); rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = anchor; rect.sizeDelta = size; rect.anchoredPosition = position;
            return rect;
        }
        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
        private static Image Panel(string name, Transform parent, Vector2 anchor, Vector2 size, Vector2 position, Color color)
        {
            var rect = Rect(name, parent, anchor, size, position); var image = rect.gameObject.AddComponent<Image>(); image.color = color; return image;
        }
        private static TMP_Text UiText(string name, Transform parent, string content, Vector2 anchor, Vector2 size, Vector2 position, float fontSize, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft)
        {
            var rect = Rect(name, parent, anchor, size, position);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>(); text.font = font; text.text = content;
            text.fontSize = fontSize; text.color = color; text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal; text.raycastTarget = false;
            return text;
        }
        private static Button Button(string name, Transform parent, string label, Vector2 anchor, Vector2 size, Vector2 position, Color color)
        {
            var panel = Panel(name, parent, anchor, size, position, color);
            var button = panel.gameObject.AddComponent<Button>(); button.targetGraphic = panel;
            var colors = button.colors; colors.highlightedColor = new Color(.83f, 1, .95f); colors.pressedColor = new Color(.62f, .85f, .80f); button.colors = colors;
            UiText("Label", panel.transform, label, new Vector2(.5f, .5f), size - new Vector2(18, 8), Vector2.zero, 17, Color.white, TextAlignmentOptions.Center);
            return button;
        }
        private static VLabHud BuildHud(bool bio)
        {
            var canvasObject = new GameObject("LabInterface", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(VLabHud));
            canvasObject.transform.SetParent(sceneRoot, false);
            var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 800); scaler.matchWidthOrHeight = .5f;
            var hud = canvasObject.GetComponent<VLabHud>();
            var top = Panel("TaskBar", canvasObject.transform, new Vector2(.5f, 1), new Vector2(0, 117), new Vector2(0, -58.5f), uiNavy);
            top.rectTransform.anchorMin = new Vector2(0, 1); top.rectTransform.anchorMax = Vector2.one; top.raycastTarget = false;
            UiText("Brand", top.transform, "VLAB", new Vector2(0, .5f), new Vector2(96, 44), new Vector2(70, 19), 31, new Color(.54f, .84f, .76f));
            UiText("Discipline", top.transform, bio ? "SINH HỌC" : "KỸ THUẬT", new Vector2(0, .5f), new Vector2(120, 30), new Vector2(80, -20), 13, pale);
            hud.StepText = UiText("Step", top.transform, "", new Vector2(.5f, 1), new Vector2(835, 35), new Vector2(15, -26), 22, Color.white);
            hud.HintText = UiText("Hint", top.transform, "", new Vector2(.5f, 0), new Vector2(835, 54), new Vector2(15, 36), 18, pale);
            var progress = Panel("ProgressTrack", top.transform, new Vector2(.5f, 0), new Vector2(0, 3), Vector2.zero, new Color(.14f, .24f, .26f));
            progress.rectTransform.anchorMin = Vector2.zero; progress.rectTransform.anchorMax = new Vector2(1, 0); progress.raycastTarget = false;
            hud.ProgressFill = Panel("Progress", progress.transform, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero, new Color(.29f, .72f, .60f));
            Stretch(hud.ProgressFill.rectTransform); hud.ProgressFill.type = Image.Type.Filled; hud.ProgressFill.fillMethod = Image.FillMethod.Horizontal; hud.ProgressFill.raycastTarget = false;
            hud.HomeButton = Button("Home", top.transform, "Về danh sách", new Vector2(1, .5f), new Vector2(144, 40), new Vector2(-93, 15), new Color(.12f, .21f, .24f));
            hud.ResetButton = Button("Reset", top.transform, "Đặt lại thí nghiệm", new Vector2(1, .5f), new Vector2(168, 34), new Vector2(-93, -28), new Color(.12f, .21f, .24f));
            var bottom = Panel("FeedbackBar", canvasObject.transform, new Vector2(.5f, 0), new Vector2(0, 102), new Vector2(0, 51), uiNavy);
            bottom.rectTransform.anchorMin = Vector2.zero; bottom.rectTransform.anchorMax = new Vector2(1, 0); bottom.raycastTarget = false;
            hud.FeedbackText = UiText("Feedback", bottom.transform, "", new Vector2(.5f, .5f), new Vector2(990, 50), new Vector2(-100, 15), 18, pale);
            hud.ContextText = UiText("Context", bottom.transform, "", new Vector2(.5f, .5f), new Vector2(1010, 30), new Vector2(-90, -28), 16, new Color(.73f, .81f, .82f));
            hud.CheckButton = Button("Check", bottom.transform, bio ? "Kiểm tra" : "Kiểm tra mạch", new Vector2(1, .5f), new Vector2(164, 46), new Vector2(-104, 0), uiTeal);
            var shade = Panel("IntroAndResult", canvasObject.transform, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero, new Color(.015f, .035f, .045f, .72f));
            Stretch(shade.rectTransform); hud.Modal = shade.gameObject;
            var card = Panel("Card", shade.transform, new Vector2(.5f, .5f), new Vector2(820, 568), Vector2.zero, uiNavy);
            Panel("Accent", card.transform, new Vector2(0, .5f), new Vector2(5, 568), new Vector2(2, 0), uiTeal).raycastTarget = false;
            UiText("Eyebrow", card.transform, "VLAB  /  THỰC HÀNH CÓ HƯỚNG DẪN", new Vector2(.5f, 1), new Vector2(720, 30), new Vector2(0, -32), 14, new Color(.50f, .83f, .74f));
            hud.ModalTitle = UiText("Title", card.transform, "", new Vector2(.5f, 1), new Vector2(720, 91), new Vector2(0, -100), 30, Color.white);
            hud.ModalBody = UiText("Body", card.transform, "", new Vector2(.5f, .5f), new Vector2(720, 325), new Vector2(0, -35), 19, pale, TextAlignmentOptions.TopLeft);
            hud.ModalButton = Button("Continue", card.transform, "Bắt đầu", new Vector2(1, 0), new Vector2(184, 49), new Vector2(-142, 48), uiTeal);
            hud.ModalButtonLabel = hud.ModalButton.GetComponentInChildren<TMP_Text>();
            UiText("Controls", card.transform, "Chọn: chuột / E   ·   Xoay: cuộn chuột\nTrả dụng cụ: Q   ·   Đặt lại: R", new Vector2(0, 0), new Vector2(430, 49), new Vector2(264, 48), 15, pale);
            return hud;
        }
        private static MicroscopeView BuildMicroscopeView(BiologyExperiment experiment, Transform anchor)
        {
            var panel = Panel("EyepieceExperience", experiment.Hud.transform, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero, new Color(.015f, .027f, .039f, .99f));
            Stretch(panel.rectTransform);
            // Keep the task and feedback bars visible above this optically connected view.
            panel.rectTransform.offsetMin = new Vector2(0, 102); panel.rectTransform.offsetMax = new Vector2(0, -117);
            var rim = Panel("OpticalRim", panel.transform, new Vector2(.37f, .5f), new Vector2(550, 550), Vector2.zero, new Color(.15f, .23f, .25f));
            rim.sprite = circle;
            var mask = Panel("CircularField", rim.transform, new Vector2(.5f, .5f), new Vector2(526, 526), Vector2.zero, Color.white);
            mask.sprite = circle; mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var imageRect = Rect("LiveSpecimen", mask.transform, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero); Stretch(imageRect);
            var image = imageRect.gameObject.AddComponent<RawImage>();
            var viewObject = new GameObject("MicroscopeOptics", typeof(MicroscopeView)); viewObject.transform.SetParent(sceneRoot);
            var view = viewObject.GetComponent<MicroscopeView>();
            view.OpticalRim = rim.rectTransform;
            imageRect.gameObject.AddComponent<SpecimenPointer>().View = view;
            view.IdentificationMarkers = new RectTransform[3]; view.IdentificationLines = new RectTransform[3]; view.IdentificationLabels = new RectTransform[3];
            var names = new[] { "Nhân tế bào", "Thành tế bào", "Tế bào chất" };
            var labelPositions = new[] { new Vector2(-114, 171), new Vector2(118, 171), new Vector2(0, -204) };
            for (var i = 0; i < 3; i++)
            {
                var line = Panel("StructureLeader" + i, imageRect, new Vector2(.5f, .5f), new Vector2(40, 1.5f), Vector2.zero, new Color(.07f, .24f, .22f)); line.raycastTarget = false;
                var marker = Panel("StructurePoint" + i, imageRect, new Vector2(.5f, .5f), new Vector2(8, 8), Vector2.zero, new Color(.03f, .35f, .28f)); marker.sprite = circle; marker.raycastTarget = false;
                var label = Panel("StructureLabel" + i, imageRect, new Vector2(.5f, .5f), new Vector2(132, 30), labelPositions[i], uiNavy); label.raycastTarget = false;
                UiText("Text", label.transform, names[i], new Vector2(.5f, .5f), new Vector2(124, 27), Vector2.zero, 15, Color.white, TextAlignmentOptions.Center);
                view.IdentificationMarkers[i] = marker.rectTransform; view.IdentificationLines[i] = line.rectTransform; view.IdentificationLabels[i] = label.rectTransform;
                marker.gameObject.SetActive(false); line.gameObject.SetActive(false); label.gameObject.SetActive(false);
            }
            view.Experiment = experiment; view.ScopePanel = panel.gameObject; view.ScopeFade = panel.gameObject.AddComponent<CanvasGroup>(); view.ScopeImage = image;
            var materialPath = Root + "/Art/Materials/OnionSpecimen.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null) { material = new Material(Shader.Find("VLAB/DemoLabs/OnionSpecimen")); AssetDatabase.CreateAsset(material, materialPath); }
            material.mainTexture = OnionSpecimenPainter.Paint();
            EditorUtility.SetDirty(material);
            var plane = GameObject.CreatePrimitive(PrimitiveType.Quad); plane.name = "SpecimenPlane"; plane.transform.SetParent(sceneRoot);
            plane.transform.position = new Vector3(0, -40, 0); plane.transform.localScale = new Vector3(2, 2, 1); plane.layer = 31;
            Object.DestroyImmediate(plane.GetComponent<Collider>()); view.SpecimenPlane = plane.GetComponent<Renderer>(); view.SpecimenPlane.sharedMaterial = material;
            var cameraObj = new GameObject("SpecimenCamera", typeof(Camera)); cameraObj.transform.SetParent(sceneRoot); cameraObj.transform.position = new Vector3(0, -40, -4);
            var camera = cameraObj.GetComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 1; camera.nearClipPlane = .1f; camera.farClipPlane = 6;
            camera.cullingMask = 1 << 31; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black; camera.allowHDR = false; camera.allowMSAA = false;
            view.SpecimenCamera = camera;
            view.Status = UiText("OpticsStatus", panel.transform, "", new Vector2(.79f, 1), new Vector2(340, 68), new Vector2(0, -50), 18, pale);
            view.Prompt = UiText("IdentificationPrompt", panel.transform, "", new Vector2(.79f, 1), new Vector2(340, 94), new Vector2(0, -136), 21, Color.white);
            view.CoarseDial = Dial(panel.transform, "SƠ CẤP", new Vector2(.71f, .50f), experiment.CoarseKnob);
            view.FineDial = Dial(panel.transform, "VI CẤP", new Vector2(.86f, .50f), experiment.FineKnob);
            view.LightDial = Dial(panel.transform, "ÁNH SÁNG", new Vector2(.71f, .22f), experiment.LightKnob);
            view.ObjectiveButton = Button("ChangeObjective", panel.transform, "Đổi 10× / 40×", new Vector2(.86f, .25f), new Vector2(166, 45), Vector2.zero, uiTeal);
            view.ExitButton = Button("ExitEyepiece", panel.transform, "Rời thị kính", new Vector2(.86f, .14f), new Vector2(166, 39), Vector2.zero, new Color(.14f, .24f, .27f));
            view.Annotation = UiText("ScientificNote", panel.transform, "", new Vector2(.37f, 0), new Vector2(590, 36), new Vector2(0, 22), 12.5f, pale, TextAlignmentOptions.Center);
            // World monitor is a live optical preview, allowing coarse and fine knobs to affect the view before entering the eyepiece.
            Box("MicroscopeMonitor", sceneRoot, new Vector3(1.61f, 1.64f, .70f), new Vector3(.77f, .64f, .06f), navy, false);
            var screen = new GameObject("OpticsMonitor", typeof(RectTransform), typeof(Canvas)); screen.transform.SetParent(sceneRoot);
            screen.transform.position = new Vector3(1.61f, 1.64f, .659f); screen.transform.localScale = Vector3.one * .001f;
            screen.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            screen.GetComponent<RectTransform>().sizeDelta = new Vector2(600, 520);
            var preview = Rect("LivePreview", screen.transform, new Vector2(.5f, .5f), new Vector2(485, 485), Vector2.zero).gameObject.AddComponent<RawImage>(); preview.raycastTarget = false; view.MonitorImage = preview;
            WorldText("PreviewLabel", sceneRoot, "TIÊU BẢN · ẢNH TRỰC TIẾP", new Vector3(1.61f, 2.02f, .66f), .94f, .08f, 2.4f, new Color(.07f, .19f, .19f));
            panel.gameObject.SetActive(true); // Start initializes the render texture, then closes the viewing panel.
            experiment.Hud.Modal.transform.SetAsLastSibling();
            return view;
        }
        private static VLabDialDrag Dial(Transform parent, string label, Vector2 anchor, VLabRotaryControl control)
        {
            var disc = Panel("Dial_" + label, parent, anchor, new Vector2(78, 78), Vector2.zero, new Color(.25f, .36f, .38f)); disc.sprite = circle;
            var dial = disc.gameObject.AddComponent<VLabDialDrag>(); dial.Control = control;
            var indicator = Rect("NeedlePivot", disc.transform, new Vector2(.5f, .5f), new Vector2(78, 78), Vector2.zero); dial.Indicator = indicator;
            Panel("Needle", indicator, new Vector2(.5f, .5f), new Vector2(4, 22), new Vector2(0, 23), new Color(.73f, .92f, .84f)).raycastTarget = false;
            UiText("DialLabel", disc.transform, label, new Vector2(.5f, 0), new Vector2(164, 26), new Vector2(0, -22), 16, pale, TextAlignmentOptions.Center);
            UiText("DialHelp", disc.transform, "Kéo để xoay", new Vector2(.5f, 0), new Vector2(164, 20), new Vector2(0, -43), 12, pale, TextAlignmentOptions.Center);
            return dial;
        }
    }
}
