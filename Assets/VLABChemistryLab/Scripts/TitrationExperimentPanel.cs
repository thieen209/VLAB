using TMPro;
using UnityEngine;

namespace VLAB.ChemistryLab
{
    /// <summary>Optional uGUI/TMP phone panel; buttons call these public methods.</summary>
    public sealed class TitrationExperimentPanel : MonoBehaviour
    {
        [SerializeField] private TitrationLessonController controller;
        [SerializeField] private TMP_Text statusLabel;

        public void Configure(TitrationLessonController target, TMP_Text label)
        {
            controller = target;
            statusLabel = label;
            Refresh();
        }
        private void OnEnable() { if (controller != null) controller.StateChanged += Refresh; Refresh(); }
        private void OnDisable() { if (controller != null) controller.StateChanged -= Refresh; }
        public void Safety() => controller?.PerformSafety();
        public void Prepare() => controller?.PrepareBurette();
        public void Sample() => controller?.AddSample();
        public void Indicator() => controller?.AddIndicator();
        public void DoseDrop() => controller?.DoseDrop();
        public void DoseFast() => controller?.DoseFast();
        public void Record() => controller?.RecordResult();
        public void ResetTrial() => controller?.ResetTrial();
        public void Clear() => controller?.ClearResults();
        private void Refresh()
        {
            if (statusLabel != null && controller != null && controller.Experiment != null)
                statusLabel.text = $"{controller.StatusMessage}\nNaOH {controller.Experiment.DeliveredVolumeMl:F2} mL\n{controller.Experiment.RubricMessage}";
        }
    }
}
