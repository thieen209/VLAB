using System;
using UnityEngine;

namespace VLAB.ChemistryLab
{
    /// <summary>Runs a sequence from a ChemistryExperimentDefinition and is shared by desktop and XR controls.</summary>
    public sealed class ConfigurableExperimentController : MonoBehaviour
    {
        [SerializeField] private ChemistryExperimentDefinition definition;
        private int completedSteps;

        public event Action StateChanged;
        public ChemistryExperimentDefinition Definition => definition;
        public int CompletedSteps => completedSteps;
        public bool IsComplete => definition != null && completedSteps >= definition.procedureSteps.Count;
        public string StatusMessage { get; private set; }

        public void Configure(ChemistryExperimentDefinition target)
        {
            definition = target;
            ResetExperiment();
        }

        private void Awake()
        {
            if (definition != null) ResetExperiment();
        }

        public void Advance()
        {
            if (definition == null || definition.procedureSteps.Count == 0) return;
            if (IsComplete)
            {
                StatusMessage = "Đã hoàn tất các thao tác. Hãy ghi nhận kết quả.";
                StateChanged?.Invoke();
                return;
            }
            completedSteps++;
            StatusMessage = IsComplete
                ? definition.expectedObservation
                : "Đã hoàn tất. Bước tiếp theo: " + definition.procedureSteps[completedSteps];
            StateChanged?.Invoke();
        }

        public void RecordResult()
        {
            if (definition == null) return;
            StatusMessage = IsComplete
                ? $"KẾT QUẢ: {definition.resultLabel} {definition.targetResult}. {definition.expectedObservation}"
                : "Chưa đủ thao tác để ghi kết quả.";
            StateChanged?.Invoke();
        }

        public void ResetExperiment()
        {
            completedSteps = 0;
            StatusMessage = definition == null || definition.procedureSteps.Count == 0
                ? "Chưa có cấu hình bài thí nghiệm."
                : "Sẵn sàng. Bước đầu tiên: " + definition.procedureSteps[0];
            StateChanged?.Invoke();
        }

        public string CurrentInstruction()
        {
            if (definition == null) return "Chưa chọn bài thí nghiệm.";
            return IsComplete ? "Ghi nhận kết quả" : definition.procedureSteps[completedSteps];
        }
    }
}
