using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using VLAB.PhysicsLab.Common;

namespace VLAB.PhysicsLab.Interaction
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(XRSimpleInteractable), typeof(LabInteractable))]
    public sealed class XrSimpleInteractableBridge : MonoBehaviour
    {
        private XRSimpleInteractable xrInteractable;
        private LabInteractable labInteractable;

        private void Awake()
        {
            xrInteractable = GetComponent<XRSimpleInteractable>();
            labInteractable = GetComponent<LabInteractable>();
        }

        private void OnEnable()
        {
            if (xrInteractable == null) xrInteractable = GetComponent<XRSimpleInteractable>();
            if (labInteractable == null) labInteractable = GetComponent<LabInteractable>();
            xrInteractable.hoverEntered.AddListener(HandleHoverEntered);
            xrInteractable.hoverExited.AddListener(HandleHoverExited);
            xrInteractable.selectEntered.AddListener(HandleSelectEntered);
            xrInteractable.selectExited.AddListener(HandleSelectExited);
        }

        private void OnDisable()
        {
            if (xrInteractable == null) return;
            xrInteractable.hoverEntered.RemoveListener(HandleHoverEntered);
            xrInteractable.hoverExited.RemoveListener(HandleHoverExited);
            xrInteractable.selectEntered.RemoveListener(HandleSelectEntered);
            xrInteractable.selectExited.RemoveListener(HandleSelectExited);
            labInteractable?.SetHighlighted(false);
            labInteractable?.EndInteraction();
        }

        private void HandleHoverEntered(HoverEnterEventArgs _) => labInteractable?.SetHighlighted(true);
        private void HandleHoverExited(HoverExitEventArgs _) => labInteractable?.SetHighlighted(false);

        private void HandleSelectEntered(SelectEnterEventArgs _)
        {
            if (labInteractable == null || !labInteractable.CanInteract) return;
            labInteractable.BeginInteraction();
            labInteractable.Activate();
        }

        private void HandleSelectExited(SelectExitEventArgs _) => labInteractable?.EndInteraction();
    }
}
