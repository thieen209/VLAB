using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using VLAB.Core.Input;

namespace VLAB.DemoLabs
{
    // Physical-device reads stay at this boundary; experiments only receive semantic actions.
    public sealed class VLabDemoInputProvider : MonoBehaviour, IVLABInputProvider
    {
        public string ProviderName => "VLAB desktop / device simulator";
        // Preserve taps whose press and release both arrive before the next rendered frame.
        private static bool Pressed(ButtonControl button) => button != null && (button.wasPressedThisFrame || button.isPressed);
        private InputAction selectAction;
        private Vector2 pressPointer;
        private bool hasPress;
        private bool previousEventMerging;
        public Vector2 PressPointer => hasPress ? pressPointer : Pointer;
        private void OnEnable()
        {
            // Mouse event merging can replace a press position with a later held-motion position.
            previousEventMerging = InputSystem.settings.disableRedundantEventsMerging;
            InputSystem.settings.disableRedundantEventsMerging = true;
            PrimeFrameTracking(); InputSystem.onDeviceChange += DeviceChanged;
            if (selectAction == null)
            {
                selectAction = new InputAction("Demo selection", InputActionType.Button);
                selectAction.AddBinding("<Mouse>/leftButton");
                selectAction.AddBinding("<Keyboard>/e");
                selectAction.AddBinding("<Touchscreen>/primaryTouch/press");
                selectAction.performed += CapturePress;
            }
            hasPress = false; selectAction.Enable();
        }
        private void OnDisable()
        {
            InputSystem.onDeviceChange -= DeviceChanged; selectAction?.Disable();
            InputSystem.settings.disableRedundantEventsMerging = previousEventMerging;
        }
        private void OnDestroy() => selectAction?.Dispose();
        private void CapturePress(InputAction.CallbackContext context)
        {
            // Capture before later motion/release events overwrite the pointer in the same frame.
            pressPointer = context.control.device is Touchscreen touch ? touch.primaryTouch.position.ReadValue()
                : context.control.device is Mouse mouse ? mouse.position.ReadValue() : Pointer;
            hasPress = true;
        }
        private void DeviceChanged(InputDevice device, InputDeviceChange change) => PrimeFrameTracking();
        private void PrimeFrameTracking()
        {
            // Input System starts recording inter-frame transitions after the first flag query.
            _ = Mouse.current?.leftButton.wasPressedThisFrame;
            _ = Keyboard.current?.eKey.wasPressedThisFrame; _ = Keyboard.current?.rKey.wasPressedThisFrame;
            _ = Keyboard.current?.qKey.wasPressedThisFrame; _ = Keyboard.current?.escapeKey.wasPressedThisFrame;
            _ = Touchscreen.current?.primaryTouch.press.wasPressedThisFrame;
        }
        public bool InteractionPressed => Pressed(Mouse.current?.leftButton) || Pressed(Keyboard.current?.eKey);
        public bool ResetPressed => Pressed(Keyboard.current?.rKey);
        public Vector2 Pointer => Touchscreen.current != null && (Mouse.current == null || !Mouse.current.enabled || Pressed(Touchscreen.current.primaryTouch.press) || Touchscreen.current.primaryTouch.press.wasReleasedThisFrame)
            ? Touchscreen.current.primaryTouch.position.ReadValue()
            : Mouse.current?.position.ReadValue() ?? new Vector2(Screen.width / 2f, Screen.height / 2f);
        public VLABInputState ReadState()
        {
            var k = Keyboard.current;
            var m = Mouse.current;
            return new VLABInputState
            {
                Move = k == null ? Vector2.zero : new Vector2((k.dKey.isPressed ? 1 : 0) - (k.aKey.isPressed ? 1 : 0), (k.wKey.isPressed ? 1 : 0) - (k.sKey.isPressed ? 1 : 0)),
                Look = m?.delta.ReadValue() ?? Vector2.zero,
                ScrollDelta = (m?.scroll.ReadValue().y ?? 0) / 120f,
                PrimaryPressed = InteractionPressed || Pressed(Touchscreen.current?.primaryTouch.press),
                SecondaryPressed = m?.rightButton.isPressed == true,
                DropPressed = Pressed(k?.qKey),
                PausePressed = Pressed(k?.escapeKey),
                SprintPressed = k?.leftShiftKey.isPressed == true
            };
        }
    }
}
