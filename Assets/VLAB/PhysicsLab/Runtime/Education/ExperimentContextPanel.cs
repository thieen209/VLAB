using TMPro;
using UnityEngine;

namespace VLAB.PhysicsLab.Education
{
    public sealed class ExperimentContextPanel : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI titleLabel;
        [SerializeField] private TextMeshProUGUI cueLabel;
        [SerializeField] private TextMeshProUGUI hintLabel;
        [SerializeField] private TextMeshProUGUI measurementLabel;
        [SerializeField] private TextMeshProUGUI settingsSummaryLabel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject helpPanel;
        [SerializeField] private GameObject resultsButton;
        [SerializeField] private GameObject resultsPanel;
        [SerializeField] private TextMeshProUGUI resultsSummaryLabel;

        private ExperimentPhysicalController controller;

        public void Configure(TextMeshProUGUI title, TextMeshProUGUI cue, TextMeshProUGUI hint, TextMeshProUGUI measurement, TextMeshProUGUI settingsSummary, GameObject settings, GameObject help, GameObject resultButton, GameObject results, TextMeshProUGUI resultsSummary)
        {
            titleLabel = title;
            cueLabel = cue;
            hintLabel = hint;
            measurementLabel = measurement;
            settingsSummaryLabel = settingsSummary;
            settingsPanel = settings;
            helpPanel = help;
            resultsButton = resultButton;
            resultsPanel = results;
            resultsSummaryLabel = resultsSummary;
            DisableRaycasts(titleLabel, cueLabel, hintLabel, measurementLabel, settingsSummaryLabel, resultsSummaryLabel);
        }

        public void Bind(ExperimentPhysicalController target)
        {
            if (controller != null)
            {
                controller.Changed -= Refresh;
            }
            controller = target;
            if (controller != null)
            {
                controller.Changed += Refresh;
            }
            Refresh();
        }

        private void OnDestroy()
        {
            if (controller != null)
            {
                controller.Changed -= Refresh;
            }
        }

        public void Refresh()
        {
            if (controller == null)
            {
                return;
            }
            Set(titleLabel, controller.Content?.Title ?? controller.ExperimentId);
            Set(cueLabel, PhysicsLabPreferences.GuidanceEnabled ? controller.CurrentCue : string.Empty);
            Set(hintLabel, PhysicsLabPreferences.GuidanceLevel > 0 && controller.ShowSubtleHint ? "Gợi ý: " + controller.HintText : string.Empty);
            Set(measurementLabel, controller.MeasurementText);
            if (resultsButton != null) resultsButton.SetActive(controller.HasEnoughTrials);
            Set(resultsSummaryLabel, controller.ResultSheetText);
            UpdateSettingsSummary();
        }

        public void ToggleSettings() { if (settingsPanel != null) settingsPanel.SetActive(!settingsPanel.activeSelf); }
        public void ToggleHelp() { if (helpPanel != null) helpPanel.SetActive(!helpPanel.activeSelf); }
        public void ToggleResults() { if (resultsPanel != null && controller != null && controller.HasEnoughTrials) resultsPanel.SetActive(!resultsPanel.activeSelf); }
        public void CycleGuidanceLevel() { PhysicsLabPreferences.GuidanceLevel = (PhysicsLabPreferences.GuidanceLevel + 1) % 3; UpdateSettingsSummary(); }
        public void ToggleActionGuidance() { PhysicsLabPreferences.GuidanceEnabled = !PhysicsLabPreferences.GuidanceEnabled; UpdateSettingsSummary(); }
        public void ToggleInteractionOutlines() { PhysicsLabPreferences.InteractionOutlines = !PhysicsLabPreferences.InteractionOutlines; UpdateSettingsSummary(); }
        public void ToggleAutoReturn() { PhysicsLabPreferences.AutoReturnTools = !PhysicsLabPreferences.AutoReturnTools; UpdateSettingsSummary(); }
        public void IncreaseMouseSensitivity() { PhysicsLabPreferences.MouseSensitivity += 0.01f; UpdateSettingsSummary(); }
        public void DecreaseMouseSensitivity() { PhysicsLabPreferences.MouseSensitivity -= 0.01f; UpdateSettingsSummary(); }

        private void UpdateSettingsSummary()
        {
            Set(settingsSummaryLabel,
                $"Mức hướng dẫn: {PhysicsLabPreferences.GuidanceLevel}\n" +
                $"Cue thao tác: {(PhysicsLabPreferences.GuidanceEnabled ? "Bật" : "Tắt")}\n" +
                $"Viền tương tác: {(PhysicsLabPreferences.InteractionOutlines ? "Bật" : "Tắt")}\n" +
                $"Tự trả dụng cụ: {(PhysicsLabPreferences.AutoReturnTools ? "Bật" : "Tắt")}\n" +
                $"Độ nhạy chuột: {PhysicsLabPreferences.MouseSensitivity:0.00}");
        }

        private static void Set(TextMeshProUGUI label, string value) { if (label != null) label.text = value; }

        private static void DisableRaycasts(params TextMeshProUGUI[] labels)
        {
            foreach (var label in labels)
            {
                if (label != null) label.raycastTarget = false;
            }
        }
    }
}
