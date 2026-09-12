using UnityEngine;
using VLAB.ChemistryLab.Interaction;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace VLAB.ChemistryLab
{
    [RequireComponent(typeof(Collider))]
    public sealed class ChemistryLabControlButton : MonoBehaviour, ILabCommandTarget
    {
        // Builder relies on indices 0..8. Keep this order synchronized with ChemistryLabBuilder.
        public enum Command { PPE, Prepare, Sample, Indicator, DoseFast, DoseDrop, Record, Reset, Clear }
        [SerializeField] private TitrationLessonController controller;
        [SerializeField] private Command command;
        private XRSimpleInteractable interactable;
        private LabButtonFeedback feedback;

        public void Configure(TitrationLessonController target, Command newCommand)
        {
            controller = target;
            command = newCommand;
        }

        private void Awake()
        {
            interactable = GetComponent<XRSimpleInteractable>();
            if (interactable == null) interactable = gameObject.AddComponent<XRSimpleInteractable>();
            interactable.selectEntered.AddListener(OnSelected);
            feedback = GetComponent<LabButtonFeedback>() ?? gameObject.AddComponent<LabButtonFeedback>();
        }
        private void OnDestroy() { if (interactable != null) interactable.selectEntered.RemoveListener(OnSelected); }
        private void OnSelected(SelectEnterEventArgs _) => TryActivate();
        public void TryActivate() { if (feedback != null && feedback.TryPress()) Execute(); }

        public void Execute()
        {
            if (controller == null) return;
            switch (command)
            {
                case Command.PPE: controller.PerformSafety(); break;
                case Command.Prepare: controller.PrepareBurette(); break;
                case Command.Sample: controller.AddSample(); break;
                case Command.Indicator: controller.AddIndicator(); break;
                case Command.DoseFast: controller.DoseFast(); break;
                case Command.DoseDrop: controller.DoseDrop(); break;
                case Command.Record: controller.RecordResult(); break;
                case Command.Reset: controller.ResetTrial(); break;
                case Command.Clear: controller.ClearResults(); break;
            }
        }
    }
}
