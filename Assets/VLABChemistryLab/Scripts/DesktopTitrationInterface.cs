using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VLAB.ChemistryLab
{
    /// <summary>Responsive dashboard: drag its header, resize from lower-right, and it stays inside the screen.</summary>
    public sealed class DesktopTitrationInterface : MonoBehaviour
    {
        [SerializeField] private TitrationLessonController controller;
        [SerializeField] private ChemistryLabLessonHub lessonHub;
        private GUIStyle titleStyle, bodyStyle, compactBodyStyle, hintStyle, compactHintStyle, buttonStyle, tabStyle, smallStyle;
        private Rect panel = new Rect(24f, 24f, 480f, 650f);
        private Vector2 scroll, resizeStart, sizeStart, pendingSize;
        private bool isPanelVisible = true, resizing, hasPendingSize;
        private const int WindowId = 48193;
        private bool showSettings;
        private Vector2 settingsScroll;
        private static readonly string[] QualityLabels = { "NHẸ", "CÂN BẰNG", "CAO", "RẤT CAO" };

        public void Configure(TitrationLessonController target) => controller = target;
        public void ConfigureHub(ChemistryLabLessonHub target) => lessonHub = target;
        public bool IsPanelVisible => isPanelVisible;
        public void TogglePanel() => isPanelVisible = !isPanelVisible;
        public void OpenSettings() { isPanelVisible = true; showSettings = true; }
        public bool IsPointerOverScrollableUi
        {
            get
            {
                if (Mouse.current == null)
                    return false;
                Vector2 pointer = Mouse.current.position.ReadValue();
                pointer.y = Screen.height - pointer.y;
                if (!isPanelVisible) return new Rect(16f, 16f, 142f, 38f).Contains(pointer);
                return panel.Contains(pointer);
            }
        }

        private void Awake()
        {
            if (controller == null) controller = GetComponent<TitrationLessonController>();
            if (lessonHub == null) lessonHub = GetComponent<ChemistryLabLessonHub>();
        }

        private void OnGUI()
        {
            if (controller == null || controller.Experiment == null) return;
            CreateStyles();
            if (!isPanelVisible)
            {
                if (GUI.Button(new Rect(16f, 16f, 142f, 38f), "MỞ BẢNG LAB", smallStyle)) isPanelVisible = true;
                return;
            }
            ClampPanelToScreen();
            Rect updatedPanel = GUI.Window(WindowId, panel, DrawWindow, GUIContent.none, GUIStyle.none);
            if (hasPendingSize)
            {
                updatedPanel.size = pendingSize;
                hasPendingSize = false;
            }
            panel = updatedPanel;
        }

        private void DrawWindow(int _)
        {
            Color old = GUI.color;
            Fill(new Rect(0f, 0f, panel.width, panel.height), new Color(.035f, .055f, .085f, .98f));
            Fill(new Rect(0f, 0f, panel.width, 48f), new Color(.065f, .12f, .18f));
            Fill(new Rect(0f, 0f, 4f, 48f), new Color(.2f, .83f, .74f));
            GUI.color = old;
            GUI.Label(new Rect(16f, 10f, panel.width - 214f, 30f), showSettings ? "THIẾT LẬP" : "VLAB", titleStyle);
            if (GUI.Button(new Rect(panel.width - 186f, 10f, 100f, 27f), showSettings ? "VỀ BÀI LAB" : "THIẾT LẬP", smallStyle)) showSettings = !showSettings;
            if (GUI.Button(new Rect(panel.width - 78f, 10f, 60f, 27f), "ẨN", smallStyle)) { isPanelVisible = false; return; }
            GUI.DragWindow(new Rect(0f, 0f, panel.width - 194f, 48f));
            if (showSettings) { DrawSettings(); DrawResizeHandle(); return; }

            float tabWidth = (panel.width - 32f) / 3f;
            DrawTab(new Rect(16f, 58f, tabWidth - 4f, 30f), "CHUẨN ĐỘ", 0);
            DrawTab(new Rect(20f + tabWidth, 58f, tabWidth - 4f, 30f), "PIN DANIELL", 1);
            DrawTab(new Rect(24f + tabWidth * 2f, 58f, tabWidth - 4f, 30f), "ĐIỆN PHÂN", 2);

            bool titration = lessonHub == null || lessonHub.ActiveLesson == ChemistryLabLessonHub.Lesson.Titration;
            float progress = titration ? (int)controller.Experiment.CurrentStep / 6f :
                (lessonHub.ActiveConfigurable == null || lessonHub.ActiveConfigurable.Definition == null ? 0f :
                lessonHub.ActiveConfigurable.CompletedSteps / (float)Mathf.Max(1, lessonHub.ActiveConfigurable.Definition.procedureSteps.Count));
            Fill(new Rect(16f, 96f, panel.width - 32f, 4f), new Color(.12f, .19f, .24f));
            Fill(new Rect(16f, 96f, (panel.width - 32f) * Mathf.Clamp01(progress), 4f), new Color(.2f, .83f, .74f));
            Rect viewport = new Rect(12f, 108f, panel.width - 24f, panel.height - 134f);
            if (titration)
            {
                DrawButton(new Rect(16f, 108f, panel.width - 32f, 30f), "RESET LƯỢNG CHẤT — GIỮ KẾT QUẢ", new Color(.12f, .43f, .48f), controller.ResetChemicalAmounts);
                viewport.y += 38f;
                viewport.height -= 38f;
            }
            bool compact = viewport.height < 360f;
            float contentWidth = Mathf.Max(1f, viewport.width - 18f);
            float contentHeight = titration ? (compact ? 286f : Mathf.Max(122f, bodyStyle.CalcHeight(new GUIContent(StatusText()), Mathf.Max(1f, contentWidth - 32f))) + 480f) : (compact ? 360f : 600f);
            Rect content = new Rect(0f, 0f, contentWidth, contentHeight);
            scroll = GUI.BeginScrollView(viewport, scroll, content);
            if (titration) DrawTitration(content.width, compact); else DrawConfigurable(content.width, lessonHub.ActiveConfigurable, compact);
            GUI.EndScrollView();
            DrawResizeHandle();
        }

        private void DrawTab(Rect rect, string label, int index)
        {
            bool selected = lessonHub == null ? index == 0 : (int)lessonHub.ActiveLesson == index;
            Color old = GUI.backgroundColor;
            GUI.backgroundColor = selected ? new Color(.12f, .56f, .80f) : new Color(.11f, .16f, .22f);
            if (GUI.Button(rect, label, tabStyle) && lessonHub != null) { lessonHub.Select(index); scroll = Vector2.zero; }
            GUI.backgroundColor = old;
        }

        private void DrawSettings()
        {
            float width = panel.width - 52f;
            settingsScroll = GUI.BeginScrollView(new Rect(16, 62, panel.width - 32, panel.height - 88), settingsScroll, new Rect(0, 0, width, 440));
            var settings = LabPreferences.Current;
            GUI.Label(new Rect(0, 0, width, 44), "Thiết lập được lưu tự động trên máy này. Không thay đổi bài đang làm.", bodyStyle);
            GUI.Label(new Rect(0, 54, width, 26), "CHẤT LƯỢNG HÌNH ẢNH", bodyStyle);
            GUI.changed = false;
            for (int i = 0; i < QualityLabels.Length; i++)
            {
                int choice = i;
                float half = (width - 6) / 2;
                DrawButton(new Rect((i % 2) * (half + 6), 84 + (i / 2) * 36, half, 32),
                    (settings.quality == i ? "• " : "") + QualityLabels[i],
                    settings.quality == i ? new Color(.10f, .47f, .55f) : new Color(.12f, .19f, .24f),
                    () => { settings.quality = choice; LabPreferences.Apply(settings); });
            }
            GUI.Label(new Rect(0, 166, width, 24), "Độ nhạy chuột: " + settings.lookSensitivity.ToString("F1"), bodyStyle);
            settings.lookSensitivity = GUI.HorizontalSlider(new Rect(0, 198, width, 20), settings.lookSensitivity, .2f, 5f);
            GUI.Label(new Rect(0, 228, width, 24), "Âm lượng: " + Mathf.RoundToInt(settings.volume * 100) + "%", bodyStyle);
            settings.volume = GUI.HorizontalSlider(new Rect(0, 260, width, 20), settings.volume, 0, 1);
            settings.pourGuide = GUI.Toggle(new Rect(0, 298, width, 28), settings.pourGuide, "  Hiện đường căn rót và điểm rơi");
            if (GUI.changed) LabPreferences.Apply(settings);
            DrawButton(new Rect(0, 346, width, 38), "KHÔI PHỤC THIẾT LẬP", new Color(.12f, .43f, .48f), () => LabPreferences.Apply(new LabUserSettings()));
            GUI.Label(new Rect(0, 396, width, 40), "Máy yếu: chọn NHẸ. VR thật vẫn cần kiểm thử riêng.", hintStyle);
            GUI.EndScrollView();
        }

        private void DrawTitration(float width, bool compact)
        {
            float x = 4f, y = 4f;
            if (compact)
            {
                GUI.Label(new Rect(x, y, width - 8f, 64f), CompactStatusText(), compactBodyStyle); y += 70f;
                float compactHalf = (width - 14f) * .5f;
                DrawButton(new Rect(x, y, compactHalf, 30f), "1 PPE", new Color(.10f, .39f, .68f), controller.PerformSafety);
                DrawButton(new Rect(x + compactHalf + 6f, y, compactHalf, 30f), "2 NẠP BURETTE", new Color(.10f, .39f, .68f), controller.PrepareBurette); y += 34f;
                DrawButton(new Rect(x, y, compactHalf, 30f), "3 LẤY MẪU", new Color(.10f, .39f, .68f), controller.AddSample);
                DrawButton(new Rect(x + compactHalf + 6f, y, compactHalf, 30f), "4 CHỈ THỊ", new Color(.70f, .14f, .41f), controller.AddIndicator); y += 36f;
                DrawButton(new Rect(x, y, compactHalf, 30f), "+1.00 mL", new Color(.10f, .47f, .30f), controller.DoseCoarse);
                DrawButton(new Rect(x + compactHalf + 6f, y, compactHalf, 30f), "+0.10 mL", new Color(.10f, .55f, .36f), controller.DoseFast); y += 34f;
                DrawButton(new Rect(x, y, compactHalf, 30f), "+0.01 mL", new Color(.16f, .62f, .42f), controller.DoseDrop);
                DrawButton(new Rect(x + compactHalf + 6f, y, compactHalf, 30f), "GHI", new Color(.80f, .42f, .06f), controller.RecordResult); y += 34f;
                DrawButton(new Rect(x, y, compactHalf, 30f), "LÀM LẠI", new Color(.62f, .22f, .14f), controller.ResetTrial);
                DrawButton(new Rect(x + compactHalf + 6f, y, compactHalf, 30f), "XÓA", new Color(.36f, .20f, .52f), controller.ClearResults); y += 34f;
                GUI.Label(new Rect(x, y, width - 8f, 30f), "Mục tiêu 8.33 mL • 3 lượt đồng quy", compactHintStyle);
                return;
            }
            string status = StatusText();
            float statusHeight = Mathf.Max(122f, bodyStyle.CalcHeight(new GUIContent(status), width - 32f));
            Fill(new Rect(x, y, width - 8f, statusHeight + 20f), new Color(.065f, .105f, .15f));
            GUI.Label(new Rect(x + 12f, y + 10f, width - 32f, statusHeight), status, bodyStyle); y += statusHeight + 30f;
            DrawButton(x, ref y, width - 8f, "1  MANG PPE", new Color(.10f, .39f, .68f), controller.PerformSafety);
            DrawButton(x, ref y, width - 8f, "2  RỬA & NẠP BURETTE", new Color(.10f, .39f, .68f), controller.PrepareBurette);
            DrawButton(x, ref y, width - 8f, "3  LẤY 10.00 mL MẪU", new Color(.10f, .39f, .68f), controller.AddSample);
            DrawButton(x, ref y, width - 8f, "4  THÊM CHỈ THỊ", new Color(.70f, .14f, .41f), controller.AddIndicator);
            float half = (width - 14f) * .5f;
            DrawButton(new Rect(x, y, half, 40f), "+1.00 mL", new Color(.10f, .47f, .30f), controller.DoseCoarse);
            DrawButton(new Rect(x + half + 6f, y, half, 40f), "+0.10 mL", new Color(.10f, .55f, .36f), controller.DoseFast); y += 46f;
            DrawButton(new Rect(x, y, half, 40f), "+0.01 mL", new Color(.16f, .62f, .42f), controller.DoseDrop);
            DrawButton(new Rect(x + half + 6f, y, half, 40f), "GHI KẾT QUẢ", new Color(.80f, .42f, .06f), controller.RecordResult); y += 46f;
            DrawButton(new Rect(x, y, half, 40f), "LÀM LẠI", new Color(.62f, .22f, .14f), controller.ResetTrial);
            DrawButton(new Rect(x + half + 6f, y, half, 40f), "XÓA KẾT QUẢ", new Color(.36f, .20f, .52f), controller.ClearResults); y += 50f;
            GUI.Label(new Rect(x, y, width - 8f, 56f), "WASD: di chuyển · Chuột phải: nhìn · Click: cầm/thả\nF: mở nắp/pipette · R: nghiêng · Giữ vòi: rót\nChuẩn độ bằng dụng cụ hoặc nút hướng dẫn; RESET để đổi cách làm.", hintStyle);
        }

        private void DrawConfigurable(float width, ConfigurableExperimentController experiment, bool compact)
        {
            if (experiment == null || experiment.Definition == null) { GUI.Label(new Rect(4f, 4f, width - 8f, 50f), "Bài thí nghiệm chưa được cấu hình.", bodyStyle); return; }
            ChemistryExperimentDefinition d = experiment.Definition;
            float x = 4f, y = 4f;
            float headerHeight = compact ? 56f : 76f;
            GUI.Label(new Rect(x, y, width - 8f, headerHeight), d.learningObjective + "\nAn toàn: " + d.safetyNote, compact ? compactBodyStyle : bodyStyle); y += headerHeight + 10f;
            GUI.Label(new Rect(x, y, width - 8f, compact ? 38f : 54f), "YÊU CẦU: " + string.Join(" • ", d.requirements), compact ? compactHintStyle : hintStyle); y += compact ? 46f : Mathf.Max(72f, 22f + d.requirements.Count * 19f);
            GUI.Label(new Rect(x, y, width - 8f, compact ? 36f : 48f), "BƯỚC " + (experiment.CompletedSteps + 1) + ": " + experiment.CurrentInstruction(), compact ? compactBodyStyle : bodyStyle); y += compact ? 42f : 58f;
            float actionHeight = compact ? 30f : 40f;
            DrawButton(new Rect(x, y, width - 8f, actionHeight), "THỰC HIỆN BƯỚC TIẾP", new Color(.10f, .48f, .72f), experiment.Advance); y += actionHeight + 6f;
            DrawButton(new Rect(x, y, width - 8f, actionHeight), "GHI NHẬN KẾT QUẢ", new Color(.80f, .42f, .06f), experiment.RecordResult); y += actionHeight + 6f;
            DrawButton(new Rect(x, y, width - 8f, actionHeight), "LÀM LẠI BÀI", new Color(.62f, .22f, .14f), experiment.ResetExperiment); y += actionHeight + 6f;
            GUI.Label(new Rect(x, y + 4f, width - 8f, 66f), experiment.StatusMessage, hintStyle);
        }

        private void DrawResizeHandle()
        {
            Rect handle = new Rect(panel.width - 22f, panel.height - 22f, 18f, 18f);
            GUI.Label(handle, "◢", smallStyle);
            Event evt = Event.current;
            if (evt.type == EventType.MouseDown && handle.Contains(evt.mousePosition)) { resizing = true; resizeStart = evt.mousePosition; sizeStart = panel.size; evt.Use(); }
            if (resizing && evt.type == EventType.MouseDrag)
            {
                Vector2 delta = evt.mousePosition - resizeStart;
                pendingSize.x = Mathf.Clamp(sizeStart.x + delta.x, 320f, Mathf.Max(320f, Screen.width - panel.x - 8f));
                pendingSize.y = Mathf.Clamp(sizeStart.y + delta.y, 330f, Mathf.Max(330f, Screen.height - panel.y - 8f));
                hasPendingSize = true;
                evt.Use();
            }
            if (resizing && evt.type == EventType.MouseUp) { resizing = false; evt.Use(); }
        }

        private void ClampPanelToScreen()
        {
            panel.width = Mathf.Min(panel.width, Mathf.Max(280f, Screen.width - 16f));
            panel.height = Mathf.Min(panel.height, Mathf.Max(260f, Screen.height - 112f));
            panel.x = Mathf.Clamp(panel.x, 8f - panel.width + 72f, Screen.width - 72f);
            panel.y = Mathf.Clamp(panel.y, 8f, Mathf.Max(8f, Screen.height - 88f - panel.height));
        }

        private string LessonTitle() => lessonHub == null || lessonHub.ActiveLesson == ChemistryLabLessonHub.Lesson.Titration ? "VLAB | CHUẨN ĐỘ" : lessonHub.ActiveConfigurable?.Definition?.displayName?.ToUpperInvariant() ?? "VLAB | THÍ NGHIỆM";
        private string StatusText()
        {
            TitrationExperiment e = controller.Experiment;
            string complete = e.IsComplete ? "\n\nKẾT QUẢ: " + e.MassVolumePercent.ToString("F2") + "% m/V CH3COOH" : string.Empty;
            return "BƯỚC: " + TitrationLessonController.StepInstruction(e.CurrentStep).ToUpperInvariant() + "\nNaOH đã thêm: " + e.DeliveredVolumeMl.ToString("F2") + " mL\nLượt hợp lệ: " + e.Observations.Count + "/3\n\n" + controller.StatusMessage + "\n\n" + e.RubricMessage + complete;
        }
        private string CompactStatusText()
        {
            TitrationExperiment e = controller.Experiment;
            return "BƯỚC: " + TitrationLessonController.StepInstruction(e.CurrentStep).ToUpperInvariant() + "\n" +
                "NaOH: " + e.DeliveredVolumeMl.ToString("F2") + " mL  |  Lượt: " + e.Observations.Count + "/3\n" + controller.StatusMessage;
        }
        private void DrawButton(float x, ref float y, float width, string label, Color color, Action action) { DrawButton(new Rect(x, y, width, 40f), label, color, action); y += 46f; }
        private static void Fill(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }
        private void DrawButton(Rect rect, string label, Color color, Action action)
        {
            Color old = GUI.backgroundColor;
            GUI.backgroundColor = rect.Contains(Event.current.mousePosition) ? Color.Lerp(color, Color.white, .18f) : color;
            if (GUI.Button(rect, label, buttonStyle)) { action(); LabUiAudio.PlayClick(); }
            GUI.backgroundColor = old;
        }
        private void CreateStyles()
        {
            if (titleStyle != null) return;
            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 19, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
            bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, wordWrap = true, normal = { textColor = new Color(.90f, .96f, 1f) } };
            compactBodyStyle = new GUIStyle(bodyStyle) { fontSize = 13 };
            hintStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, wordWrap = true, fontStyle = FontStyle.Italic, normal = { textColor = new Color(.68f, .84f, .94f) } };
            compactHintStyle = new GUIStyle(hintStyle) { fontSize = 10 };
            buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 14, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter,
                normal = { background = Texture2D.whiteTexture, textColor = Color.white },
                hover = { background = Texture2D.whiteTexture, textColor = Color.white },
                active = { background = Texture2D.whiteTexture, textColor = Color.white } };
            tabStyle = new GUIStyle(buttonStyle) { fontSize = 11 };
            smallStyle = new GUIStyle(buttonStyle) { fontSize = 12,
                normal = { textColor = new Color(.04f, .09f, .14f) },
                hover = { textColor = new Color(.04f, .09f, .14f) },
                active = { textColor = new Color(.04f, .09f, .14f) } };
        }
    }
}
