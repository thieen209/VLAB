using UnityEngine;

namespace VLAB.ChemistryLab.Interaction
{
    /// <summary>Maps measured physical transfers to the same lesson used by Desktop and XR.</summary>
    public sealed class HandsOnTitration : MonoBehaviour
    {
        [SerializeField] private TitrationLessonController lesson;
        [SerializeField] private TextMesh instructions;
        private LabLiquidVessel[] vessels;
        private bool rinsed;
        private double sampleMl;
        private double indicatorMl;
        private int observedTrials;
        private TitrationStep observedStep;
        public bool RequiresReset { get; private set; }
        public bool UsingTools { get; private set; }
        public bool IsRinsing => lesson != null && lesson.Experiment.CurrentStep == TitrationStep.RinseAndFillBurette;
        public string Feedback { get; private set; }
        public void Configure(TitrationLessonController controller, TextMesh display)
        {
            lesson = controller;
            instructions = display;
        }
        private void Start()
        {
            vessels = GetComponentsInChildren<LabLiquidVessel>();
            lesson.StateChanged += OnLessonChanged;
            observedTrials = lesson.Experiment.Observations.Count;
            observedStep = lesson.Experiment.CurrentStep;
            Show("Đeo PPE trên bảng lab. Rót 5 mL NaOH vào burette rồi xả vào bình thải. Desktop: click cầm, F mở nắp, giữ R rót.");
        }
        private void OnDestroy() { if (lesson != null) lesson.StateChanged -= OnLessonChanged; }
        private void OnLessonChanged()
        {
            var step = lesson.Experiment.CurrentStep;
            // Reset/record clear apparatus contents, but never teleport an object held by a student.
            bool newTrial = observedTrials != lesson.Experiment.Observations.Count;
            bool reset = step == TitrationStep.WearSafetyEquipment ||
                (step == TitrationStep.RinseAndFillBurette && observedStep != TitrationStep.WearSafetyEquipment &&
                 lesson.StatusMessage.StartsWith("Đã làm lại"));
            if (newTrial || reset) ResetApparatus();
            observedTrials = lesson.Experiment.Observations.Count;
            observedStep = step;
        }
        public void ResetApparatus()
        {
            foreach (var tap in GetComponentsInChildren<LabBuretteTap>()) tap.StopFlow();
            rinsed = RequiresReset = UsingTools = false;
            sampleMl = indicatorMl = 0;
            if (vessels == null) vessels = GetComponentsInChildren<LabLiquidVessel>();
            foreach (var vessel in vessels) vessel.ResetContents();
            Show("Dụng cụ đã làm sạch. Tráng burette với 5 mL NaOH; xả vào bình thải.");
        }
        public double Transfer(LabLiquidVessel source, LabLiquidVessel target, double amount)
        {
            if (source == null || target == null || source == target || source.Station != this || target.Station != this ||
                !source.IsOpen || !target.IsOpen || source.Liquid == null || target.Liquid == null || amount <= 0 ||
                double.IsNaN(amount) || double.IsInfinity(amount)) return 0;
            var step = lesson.Experiment.CurrentStep;
            if (RequiresReset) { Show("Đã mất mẫu hoặc trộn sai: nhấn RESET trước khi làm lại."); return 0; }
            if (step == TitrationStep.WearSafetyEquipment) { Show("Cần hoàn thành PPE trước khi dùng hóa chất."); return 0; }
            bool waste = target.Role == LabVesselRole.Waste;
            bool fill = source.Liquid.Reagent == LabReagent.NaOH && source.Role == LabVesselRole.Bottle &&
                target.Role == LabVesselRole.Burette && step == TitrationStep.RinseAndFillBurette;
            bool aspirate = source.Liquid.Reagent == LabReagent.DilutedVinegar && source.Role == LabVesselRole.Bottle &&
                target.Role == LabVesselRole.Pipette && step == TitrationStep.PipetteDilutedVinegar;
            bool sample = source.Role == LabVesselRole.Pipette && source.Liquid.Reagent == LabReagent.DilutedVinegar &&
                target.Role == LabVesselRole.Flask && step == TitrationStep.PipetteDilutedVinegar;
            bool indicator = source.Liquid.Reagent == LabReagent.Indicator && target.Role == LabVesselRole.Flask &&
                step == TitrationStep.AddIndicator;
            bool dose = source.Role == LabVesselRole.Burette && source.Liquid.Reagent == LabReagent.NaOH &&
                target.Role == LabVesselRole.Flask && step == TitrationStep.TitrateToEndpoint;
            if (!(waste || fill || aspirate || sample || indicator || dose))
            { Show("Sai hóa chất, dụng cụ hoặc thứ tự. " + TitrationLessonController.StepInstruction(step)); return 0; }
            if (sample) amount = System.Math.Min(amount, 10 - sampleMl);
            if (indicator) amount = System.Math.Min(amount, .10 - indicatorMl);
            if (fill && !rinsed) amount = System.Math.Min(amount, 5 - target.Liquid.VolumeMl);
            double before = source.Liquid.VolumeMl;
            double moved = source.Liquid.TransferTo(target.Liquid, amount);
            if (moved <= 0) return 0;
            UsingTools = true;
            if (waste)
            {
                if (source.Role == LabVesselRole.Burette && !rinsed && before >= 4.99 && source.Liquid.VolumeMl == 0)
                    rinsed = true;
                // Continuous draining may span many frames.
                if (source.Role == LabVesselRole.Burette && source.RinseLoaded && source.Liquid.VolumeMl == 0) rinsed = true;
                Show(rinsed ? "Đã tráng. Nạp burette tới 50 mL bằng NaOH." : "Đang thu gom vào bình thải.");
            }
            if (fill)
            {
                if (!rinsed && target.Liquid.VolumeMl >= 4.99) target.RinseLoaded = true;
                if (rinsed && target.Liquid.VolumeMl >= 49.99) { lesson.CompleteMeasuredStep(TitrationStep.RinseAndFillBurette); Show("Burette đủ 50 mL. Hút đúng 10 mL giấm bằng pipette."); }
                else Show(rinsed ? "Nạp NaOH tới 50 mL." : "Đủ 5 mL: đặt bình thải dưới vòi, giữ chuột trái / trigger trên khóa để xả.");
            }
            if (sample) { sampleMl += moved; if (sampleMl >= 9.999) { lesson.CompleteMeasuredStep(TitrationStep.PipetteDilutedVinegar); Show("Mẫu đủ 10 mL. Nhỏ 2 giọt chỉ thị vào bình."); } }
            if (indicator) { indicatorMl += moved; if (indicatorMl >= .0999) { lesson.CompleteMeasuredStep(TitrationStep.AddIndicator); Show("Đặt bình dưới burette. Click / trigger khóa: từng giọt; giữ: dòng chậm."); } }
            if (dose) { lesson.DoseMeasured((float)moved); Show(lesson.StatusMessage); }
            source.RefreshVisual(); target.RefreshVisual();
            return moved;
        }
        public void Spill(LabLiquidVessel source, double amount)
        {
            if (source.Liquid.Remove(amount) <= 0) return;
            if (source.Role == LabVesselRole.Flask || source.Role == LabVesselRole.Pipette || source.Role == LabVesselRole.Burette)
                RequiresReset = true;
            Show(RequiresReset ? "Đổ mất mẫu: RESET để làm lại lượt này." : "Hóa chất đổ ra ngoài. Dựng chai thẳng, đưa miệng chai gần miệng bình.");
            source.RefreshVisual();
        }
        public void Show(string message)
        {
            Feedback = message;
            if (instructions != null)
            {
                // Bound the width of runtime feedback to the physical instruction board.
                var text = new System.Text.StringBuilder("THỰC HÀNH BẰNG DỤNG CỤ\n");
                int column = 0;
                foreach (string word in message.Replace('\n', ' ').Split(' '))
                {
                    if (column + word.Length > 40) { text.Append('\n'); column = 0; }
                    text.Append(word).Append(' '); column += word.Length + 1;
                }
                instructions.text = text.ToString();
            }
        }
    }
}
