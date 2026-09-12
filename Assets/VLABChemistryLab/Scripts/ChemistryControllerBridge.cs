using UnityEngine;

namespace VLAB.ChemistryLab
{
    /// <summary>Hardware-neutral UnitySendMessage target for an optional BLE/ESP32 plugin.</summary>
    public sealed class ChemistryControllerBridge : MonoBehaviour
    {
        [SerializeField] private TitrationLessonController controller;
        public void Configure(TitrationLessonController target) => controller = target;

        public void ReceiveCommand(string command)
        {
            if (!isActiveAndEnabled || controller == null || string.IsNullOrWhiteSpace(command)) return;
            switch (command.Trim().ToLowerInvariant())
            {
                case "safety": case "ppe": controller.PerformSafety(); break;
                case "prepare": case "setup": controller.PrepareBurette(); break;
                case "sample": controller.AddSample(); break;
                case "indicator": controller.AddIndicator(); break;
                case "dose_drop": case "titrate": controller.DoseDrop(); break;
                case "dose_fast": controller.DoseFast(); break;
                case "record": controller.RecordResult(); break;
                case "reset": controller.ResetTrial(); break;
                case "clear": controller.ClearResults(); break;
            }
        }
    }
}
