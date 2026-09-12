using System;
using UnityEngine;

namespace VLAB.ChemistryLab
{
    /// <summary>Unity adapter shared by the XR console, mouse, phone UI and BLE bridge.</summary>
    public sealed class TitrationLessonController : MonoBehaviour
    {
        [SerializeField] private GameObject burette;
        [SerializeField] private Renderer buretteLiquidRenderer;
        [SerializeField] private GameObject flask;
        [SerializeField] private Renderer flaskLiquidRenderer;
        [SerializeField] private float dropDoseMl = 0.01f;
        [SerializeField] private float fastDoseMl = 0.10f;

        private static readonly Color ClearLiquid = new Color(0.96f, 0.96f, 0.92f, 0.78f);
        private static readonly Color EndpointPink = new Color(1f, 0.64f, 0.76f, 0.88f);
        private static readonly Color OvershotPink = new Color(0.92f, 0.12f, 0.46f, 0.95f);
        private MaterialPropertyBlock liquidProperties;

        public event Action StateChanged;
        public TitrationExperiment Experiment { get; private set; }
        public bool LastActionAccepted { get; private set; }
        public string StatusMessage { get; private set; }
        public Color LiquidColor => Experiment.EndpointState == TitrationEndpointState.Endpoint
            ? EndpointPink : Experiment.EndpointState == TitrationEndpointState.Overshot ? OvershotPink : ClearLiquid;

        private void Awake()
        {
            EnsureExperiment();
            Publish("Đeo kính, găng tay và áo choàng để bắt đầu.", true);
        }

        public void Configure(GameObject buretteObject, Renderer buretteRenderer,
            GameObject flaskObject, Renderer liquidRenderer)
        {
            EnsureExperiment();
            burette = buretteObject;
            buretteLiquidRenderer = buretteRenderer;
            flask = flaskObject;
            flaskLiquidRenderer = liquidRenderer;
            RefreshVisuals();
        }

        public void PerformSafety() => Complete(TitrationStep.WearSafetyEquipment, "PPE đạt. Tráng và nạp burette bằng NaOH.");
        public void PrepareBurette() => Complete(TitrationStep.RinseAndFillBurette, "Burette sẵn sàng. Pipette 10,00 mL mẫu.");
        public void AddSample() => Complete(TitrationStep.PipetteDilutedVinegar, "Đã lấy mẫu. Thêm 2–3 giọt phenolphthalein.");
        public void AddIndicator() => Complete(TitrationStep.AddIndicator, "Chuẩn độ đến màu hồng nhạt bền.");
        public void DoseDrop() => Dose(dropDoseMl);
        public void DoseFast() => Dose(fastDoseMl);
        public void DoseCoarse() => Dose(1.00f);
        public void DoseMeasured(float volumeMl) => Dose(volumeMl, true);
        public void CompleteMeasuredStep(TitrationStep step) => Complete(step, "Đã hoàn thành bằng dụng cụ.", true);

        public void RecordResult()
        {
            EnsureExperiment();
            var physical = GetComponentInChildren<VLAB.ChemistryLab.Interaction.HandsOnTitration>();
            if (physical != null && physical.RequiresReset)
            {
                Publish("Đã mất mẫu: RESET và thực hiện lại lượt này trước khi ghi kết quả.", false);
                return;
            }
            LastActionAccepted = Experiment.RecordCurrentTitre();
            if (LastActionAccepted)
                Publish(Experiment.IsComplete
                    ? $"Hoàn thành. Trung bình {Experiment.MeanTitreMl:F2} mL; {Experiment.MassVolumePercent:F2}% m/V."
                    : $"Đã ghi lần {Experiment.Observations.Count}. Chuẩn bị lượt tiếp theo.", true);
            else
                Publish(Experiment.EndpointState == TitrationEndpointState.Overshot
                    ? "Quá điểm cuối (hồng đậm). Reset và làm lại lượt này."
                    : "Chưa ở điểm cuối hồng nhạt; chưa thể ghi.", false);
        }

        public void ResetTrial()
        {
            EnsureExperiment();
            Experiment.ResetTrial();
            Publish(Experiment.CurrentStep == TitrationStep.WearSafetyEquipment
                ? "Đeo PPE để bắt đầu." : "Đã làm lại lượt hiện tại. Tráng và nạp burette.", true);
        }

        public void ClearResults()
        {
            EnsureExperiment();
            Experiment.ClearResults();
            Publish("Đã xóa toàn bộ kết quả. Đeo PPE để bắt đầu.", true);
        }

        public void ResetChemicalAmounts()
        {
            ResetTrial();
            var physical = GetComponentInChildren<VLAB.ChemistryLab.Interaction.HandsOnTitration>();
            if (physical != null) physical.ResetApparatus();
            Publish("Đã reset lượng chất và lượt hiện tại. Giữ nguyên kết quả đã ghi; chai gốc được nạp lại, dụng cụ làm rỗng.", true);
        }

        private void Complete(TitrationStep step, string success, bool measured = false)
        {
            EnsureExperiment();
            if (!measured && RejectShortcut()) return;
            LastActionAccepted = Experiment.TryCompleteStep(step);
            Publish(LastActionAccepted ? success : $"Sai thứ tự. Bước hiện tại: {StepInstruction(Experiment.CurrentStep)}", LastActionAccepted);
        }

        private bool RejectShortcut()
        {
            var physical = GetComponentInChildren<VLAB.ChemistryLab.Interaction.HandsOnTitration>();
            if (physical == null || !physical.UsingTools) return false;
            Publish("Đang thực hành bằng dụng cụ. Tiếp tục thao tác trên bàn; RESET để quay lại hướng dẫn bằng nút.", false);
            return true;
        }

        private void Dose(float amountMl, bool measured = false)
        {
            EnsureExperiment();
            if (!measured && RejectShortcut()) return;
            LastActionAccepted = Experiment.AddTitrant(amountMl);
            string message = !LastActionAccepted
                ? $"Chưa thể thêm NaOH. Bước hiện tại: {StepInstruction(Experiment.CurrentStep)}"
                : Experiment.EndpointState == TitrationEndpointState.Overshot
                    ? $"{Experiment.DeliveredVolumeMl:F2} mL: quá chuẩn, dung dịch hồng đậm."
                    : Experiment.EndpointState == TitrationEndpointState.Endpoint
                        ? $"{Experiment.DeliveredVolumeMl:F2} mL: điểm cuối hồng nhạt. Hãy ghi kết quả."
                        : $"Đã thêm {Experiment.DeliveredVolumeMl:F2} mL NaOH; tiếp tục từng giọt gần điểm cuối.";
            Publish(message, LastActionAccepted);
        }

        private void EnsureExperiment()
        {
            if (Experiment == null) Experiment = new TitrationExperiment();
        }

        private void Publish(string message, bool accepted)
        {
            LastActionAccepted = accepted;
            StatusMessage = message;
            RefreshVisuals();
            StateChanged?.Invoke();
        }

        private void RefreshVisuals()
        {
            if (flaskLiquidRenderer != null)
            {
                if (liquidProperties == null) liquidProperties = new MaterialPropertyBlock();
                flaskLiquidRenderer.GetPropertyBlock(liquidProperties);
                liquidProperties.SetColor("_Color", LiquidColor);
                flaskLiquidRenderer.SetPropertyBlock(liquidProperties);
            }
            if (buretteLiquidRenderer != null)
            {
                float remaining = Mathf.Clamp01(1f - (float)(Experiment?.DeliveredVolumeMl ?? 0d) / 50f);
                buretteLiquidRenderer.transform.localScale = new Vector3(
                    buretteLiquidRenderer.transform.localScale.x, Mathf.Max(0.03f, remaining),
                    buretteLiquidRenderer.transform.localScale.z);
            }
        }

        public static string StepInstruction(TitrationStep step)
        {
            switch (step)
            {
                case TitrationStep.WearSafetyEquipment: return "mang PPE";
                case TitrationStep.RinseAndFillBurette: return "tráng/nạp burette";
                case TitrationStep.PipetteDilutedVinegar: return "lấy mẫu bằng pipette";
                case TitrationStep.AddIndicator: return "thêm chỉ thị";
                case TitrationStep.TitrateToEndpoint: return "chuẩn độ";
                case TitrationStep.RecordResult: return "ghi kết quả";
                default: return "hoàn thành";
            }
        }
    }
}
