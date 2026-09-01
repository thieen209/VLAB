using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using VLAB.Core.Input;

namespace VLAB.PhysicsLab.Interaction
{
    public sealed class InteractionRaycaster : MonoBehaviour
    {
        [SerializeField] private InputManager inputManager;
        [SerializeField] private DesktopPlayerRig playerRig;
        [SerializeField] private GrabController grabController;
        [SerializeField] private Camera viewCamera;
        [SerializeField, Min(0.5f)] private float maximumDistance = 4f;
        [SerializeField] private LayerMask interactionMask = ~0;

        private IInteractable hovered;
        private Vector3 hoveredPoint;

        public IInteractable Hovered => hovered;

        public void Configure(InputManager input, DesktopPlayerRig rig, GrabController grab, Camera camera)
        {
            inputManager = input;
            playerRig = rig;
            grabController = grab;
            viewCamera = camera;
        }

        private void OnEnable()
        {
            if (inputManager != null)
            {
                inputManager.InteractionPressed += Interact;
                inputManager.InteractionReleased += EndInteraction;
            }
        }

        private void OnDisable()
        {
            if (inputManager != null)
            {
                inputManager.InteractionPressed -= Interact;
                inputManager.InteractionReleased -= EndInteraction;
            }
            SetHovered(null, Vector3.zero);
        }

        private void Update()
        {
            RefreshHover();
        }

        private void RefreshHover()
        {
            var cursorLocked = playerRig != null && playerRig.IsCursorLocked;
            if (viewCamera == null || (!cursorLocked && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()))
            {
                SetHovered(null, Vector3.zero);
                return;
            }

            var pointer = Mouse.current;
            var screenPoint = cursorLocked
                ? new Vector2(Screen.width * 0.5f, Screen.height * 0.5f)
                : pointer?.position.ReadValue() ?? Vector2.zero;
            var ray = viewCamera.ScreenPointToRay(screenPoint);
            if (!Physics.Raycast(ray, out var hit, maximumDistance, interactionMask, QueryTriggerInteraction.Ignore))
            {
                SetHovered(null, Vector3.zero);
                return;
            }

            var grabbable = hit.collider.GetComponentInParent<LabGrabbable>();
            if (grabbable != null && grabbable.CanGrab)
            {
                SetHovered(grabbable, hit.point);
                return;
            }
            SetHovered(hit.collider.GetComponentInParent<Common.LabInteractable>(), hit.point);
        }

        private void Interact()
        {
            if (hovered == null || !hovered.CanInteract)
            {
                return;
            }
            if (hovered is IGrabbable grabbable && grabController != null && grabController.TryGrab(grabbable, hoveredPoint))
            {
                return;
            }
            hovered.BeginInteraction();
            if (hovered is IActivatable activatable)
            {
                activatable.Activate();
            }
        }

        private void EndInteraction()
        {
            if (grabController != null && grabController.IsHolding)
            {
                grabController.Release();
                return;
            }
            hovered?.EndInteraction();
        }

        private void SetHovered(IInteractable next, Vector3 point)
        {
            if (!ReferenceEquals(hovered, next))
            {
                hovered?.SetHighlighted(false);
                hovered = next;
                hovered?.SetHighlighted(true);
            }
            hoveredPoint = point;
        }
    }
}
