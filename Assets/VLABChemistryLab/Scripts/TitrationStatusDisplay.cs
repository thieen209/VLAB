using System.Text;
using TMPro;
using UnityEngine;

namespace VLAB.ChemistryLab
{
    public sealed class TitrationStatusDisplay : MonoBehaviour
    {
        [SerializeField] private TitrationLessonController controller;
        [SerializeField] private TMP_Text text;

        public void Configure(TitrationLessonController target, TMP_Text targetText)
        {
            controller = target;
            text = targetText;
            Refresh();
        }
        private void OnEnable() { if (controller != null) controller.StateChanged += Refresh; Refresh(); }
        private void OnDisable() { if (controller != null) controller.StateChanged -= Refresh; }

        public void Refresh()
        {
            if (controller == null || text == null || controller.Experiment == null) return;
            TitrationExperiment experiment = controller.Experiment;
            var output = new StringBuilder("VLAB | CHUẨN ĐỘ AXIT–BAZƠ\n");
            output.AppendLine($"Bước: {TitrationLessonController.StepInstruction(experiment.CurrentStep)}");
            output.AppendLine($"NaOH: {experiment.DeliveredVolumeMl:F2} mL | Lần hợp lệ: {experiment.Observations.Count}");
            output.AppendLine(controller.StatusMessage);
            output.Append(experiment.RubricMessage);
            if (experiment.IsComplete)
                output.Append($"\nC = {experiment.AcidConcentrationMolar:F3} mol/L | {experiment.MassVolumePercent:F2}% m/V");
            text.text = output.ToString();
        }
    }
}
