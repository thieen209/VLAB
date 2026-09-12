using System;
using UnityEngine;
using UnityEngine.XR;

namespace VLAB.ChemistryLab
{
    /// <summary>Hides a lost controller and suspends its interactors without moving it to world origin.</summary>
    public sealed class XRTrackedControllerGuard : MonoBehaviour
    {
        [SerializeField] private XRNode node = XRNode.LeftHand;

        private Renderer[] ownedRenderers;
        private Behaviour[] ownedInteractors;
        private bool? lastTracked;

        public void Configure(XRNode controllerNode) => node = controllerNode;
        private void OnEnable() { lastTracked = null; }

        private void Awake()
        {
            ownedRenderers = GetComponentsInChildren<Renderer>(true);
            ownedInteractors = Array.FindAll(
                GetComponentsInChildren<Behaviour>(true),
                behaviour => behaviour != this &&
                    (behaviour.GetType().Name.IndexOf("Interactor", StringComparison.Ordinal) >= 0 ||
                     behaviour.GetType().Name.IndexOf("Controller", StringComparison.Ordinal) >= 0));
        }

        private void Update()
        {
            InputDevice device = InputDevices.GetDeviceAtXRNode(node);
            bool tracked = device.isValid &&
                device.TryGetFeatureValue(CommonUsages.isTracked, out bool isTracked) && isTracked;
            var inputController = node == XRNode.LeftHand
                ? UnityEngine.InputSystem.XR.XRController.leftHand
                : UnityEngine.InputSystem.XR.XRController.rightHand;
            if (inputController != null) tracked = inputController.isTracked.isPressed;
            if (lastTracked == tracked)
                return;
            lastTracked = tracked;
            SetTrackingState(tracked);
        }

        public void SetTrackingState(bool tracked)
        {
            if (ownedRenderers != null)
                foreach (Renderer renderer in ownedRenderers)
                    if (renderer != null) renderer.enabled = tracked;
            if (ownedInteractors != null)
                foreach (Behaviour interactor in ownedInteractors)
                    if (interactor != null) interactor.enabled = tracked;
        }
    }
}
