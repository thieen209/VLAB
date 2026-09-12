using UnityEngine;
using UnityEngine.InputSystem;

namespace VLAB.ChemistryLab.Input
{
    public struct VLabDesktopInputFrame
    {
        public Vector2 Move;
        public Vector2 Look;
        public float ZoomDelta;
        public bool RunHeld;
        public bool LookHeld;
        public bool CrouchHeld;
        public bool PrimaryHeld;
        public bool PrimaryPressed;
        public bool CancelPressed;
        public bool UsePressed;
        public bool TiltHeld;
        public bool PointerOverScrollableUi;
    }

    public static class VLabDesktopInputPolicy
    {
        public static Vector2 NormalizeMove(Vector2 move) =>
            move.sqrMagnitude > 1f ? move.normalized : move;

        public static float FilterZoom(VLabDesktopInputFrame frame) =>
            frame.PointerOverScrollableUi ? 0f : frame.ZoomDelta;
    }

    public interface IVLabDesktopInputSource
    {
        VLabDesktopInputFrame ReadFrame();
    }

    /// <summary>Reads the explicit Input System asset and emits presentation-neutral desktop commands.</summary>
    public sealed class DesktopInputActionSource : MonoBehaviour, IVLabDesktopInputSource
    {
        private const string ResourceName = "VLAB Desktop Input Actions";

        [SerializeField] private InputActionAsset actionAsset;
        private InputActionMap desktopMap;
        private InputAction move;
        private InputAction look;
        private InputAction zoom;
        private InputAction run;
        private InputAction lookHold;
        private InputAction crouch;
        private InputAction interact;
        private InputAction cancel;
        private InputAction use;
        private InputAction tilt;
        private bool ownsAsset;

        private void Awake()
        {
            InputActionAsset template = actionAsset != null ? actionAsset : Resources.Load<InputActionAsset>(ResourceName);
            if (template != null)
            {
                actionAsset = Instantiate(template);
                ownsAsset = true;
            }
            CacheActions();
        }

        private void OnEnable()
        {
            CacheActions();
            desktopMap?.Enable();
        }

        private void OnDisable() => desktopMap?.Disable();

        private void OnDestroy()
        {
            desktopMap?.Disable();
            if (ownsAsset && actionAsset != null) Destroy(actionAsset);
        }

        public VLabDesktopInputFrame ReadFrame()
        {
            return new VLabDesktopInputFrame
            {
                Move = move == null ? Vector2.zero : move.ReadValue<Vector2>(),
                Look = look == null ? Vector2.zero : look.ReadValue<Vector2>() * .1f,
                ZoomDelta = zoom == null ? 0f : zoom.ReadValue<float>(),
                RunHeld = run != null && run.IsPressed(),
                LookHeld = lookHold != null && lookHold.IsPressed(),
                CrouchHeld = crouch != null && crouch.IsPressed(),
                PrimaryHeld = interact != null && interact.IsPressed(),
                PrimaryPressed = interact != null && interact.WasPressedThisFrame(),
                CancelPressed = cancel != null && cancel.WasPressedThisFrame(),
                UsePressed = use != null && use.WasPressedThisFrame(),
                TiltHeld = tilt != null && tilt.IsPressed()
            };
        }

        private void CacheActions()
        {
            if (desktopMap != null || actionAsset == null)
                return;
            desktopMap = actionAsset.FindActionMap("Desktop", false);
            if (desktopMap == null)
            {
                Debug.LogError("[VLAB][P2.a] Desktop Input Action map is missing; desktop controls are disabled.", this);
                return;
            }
            move = desktopMap.FindAction("Move", false);
            look = desktopMap.FindAction("Look", false);
            zoom = desktopMap.FindAction("Zoom", false);
            run = desktopMap.FindAction("Run", false);
            lookHold = desktopMap.FindAction("LookHold", false);
            crouch = desktopMap.FindAction("Crouch", false);
            interact = desktopMap.FindAction("Interact", false);
            cancel = desktopMap.FindAction("Cancel", false);
            use = desktopMap.FindAction("Use", false);
            tilt = desktopMap.FindAction("Tilt", false);
        }
    }
}
