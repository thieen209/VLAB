using UnityEngine;
using VLAB.ChemistryLab.Interaction;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace VLAB.ChemistryLab
{
    [RequireComponent(typeof(Collider))]
    public sealed class ConfigurableExperimentControlButton : MonoBehaviour, ILabCommandTarget
    {
        public enum Command { Advance, Record, Reset }
        [SerializeField] private ConfigurableExperimentController controller;
        [SerializeField] private Command command;
        private XRSimpleInteractable interactable;
        private LabButtonFeedback feedback;

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
            if (command == Command.Advance) controller.Advance();
            else if (command == Command.Record) controller.RecordResult();
            else controller.ResetExperiment();
        }
    }
}
