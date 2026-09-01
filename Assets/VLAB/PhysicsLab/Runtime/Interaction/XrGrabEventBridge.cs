using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Attachment;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace VLAB.PhysicsLab.Interaction
{
    [RequireComponent(typeof(XRGrabInteractable), typeof(LabGrabbable))]
    public sealed class XrGrabEventBridge : MonoBehaviour
    {
        private XRGrabInteractable xrGrab;
        private LabGrabbable labGrab;
        private InteractionAttachController selectedAttachController;

        [SerializeField] private bool allowsDeliberateRotation = true;

        public bool AllowsDeliberateRotation => allowsDeliberateRotation;

        public void ConfigureNativeRemoteGrab(XRGrabInteractable grab, bool allowDeliberateRotation)
        {
            xrGrab = grab != null ? grab : GetComponent<XRGrabInteractable>();
            allowsDeliberateRotation = allowDeliberateRotation;
            ApplyNativeRemoteGrabPolicy(xrGrab);
        }

        private void Awake()
        {
            xrGrab = GetComponent<XRGrabInteractable>();
            labGrab = GetComponent<LabGrabbable>();
            ApplyNativeRemoteGrabPolicy(xrGrab);
        }

        private void Update()
        {
            if (!allowsDeliberateRotation || selectedAttachController == null ||
                !selectedAttachController.manipulationInput.TryReadValue(out var input) || Mathf.Abs(input.x) < 0.08f)
            {
                return;
            }

            var rotation = Quaternion.AngleAxis(input.x * selectedAttachController.manipulationRotateSpeed * Time.unscaledDeltaTime, Vector3.up);
            transform.rotation = rotation * transform.rotation;
        }

        private void OnEnable()
        {
            if (xrGrab == null) xrGrab = GetComponent<XRGrabInteractable>();
            if (labGrab == null) labGrab = GetComponent<LabGrabbable>();
            xrGrab.hoverEntered.AddListener(HandleHoverEntered);
            xrGrab.hoverExited.AddListener(HandleHoverExited);
            xrGrab.selectEntered.AddListener(HandleEntered);
            xrGrab.selectExited.AddListener(HandleExited);
        }

        private void OnDisable()
        {
            if (xrGrab == null) return;
            xrGrab.hoverEntered.RemoveListener(HandleHoverEntered);
            xrGrab.hoverExited.RemoveListener(HandleHoverExited);
            xrGrab.selectEntered.RemoveListener(HandleEntered);
            xrGrab.selectExited.RemoveListener(HandleExited);
            labGrab?.SetHighlighted(false);
        }

        private void HandleHoverEntered(HoverEnterEventArgs _) => labGrab?.SetHighlighted(true);
        private void HandleHoverExited(HoverExitEventArgs _) => labGrab?.SetHighlighted(false);

        private void HandleEntered(SelectEnterEventArgs args)
        {
            selectedAttachController = args.interactorObject?.transform.GetComponent<InteractionAttachController>();
            labGrab?.NotifyExternalGrab(args.interactorObject?.transform);
        }

        private void HandleExited(SelectExitEventArgs args)
        {
            selectedAttachController = null;
            labGrab?.NotifyExternalRelease();
        }

        private static void ApplyNativeRemoteGrabPolicy(XRGrabInteractable grab)
        {
            if (grab == null)
            {
                return;
            }

            grab.useDynamicAttach = true;
            grab.matchAttachPosition = true;
            grab.matchAttachRotation = false;
            grab.snapToColliderVolume = false;
            grab.attachEaseInTime = 0.18f;
            grab.movementType = XRBaseInteractable.MovementType.Kinematic;
            grab.trackPosition = true;
            grab.trackRotation = false;
            grab.smoothPosition = true;
            grab.smoothPositionAmount = 12f;
            grab.tightenPosition = 0.72f;
            grab.throwOnDetach = false;
            grab.velocityDamping = 1f;
            grab.velocityScale = 0f;
            grab.angularVelocityDamping = 1f;
            grab.angularVelocityScale = 0f;
        }
    }
}
