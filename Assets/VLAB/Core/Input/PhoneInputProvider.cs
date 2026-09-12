using UnityEngine;
using UnityEngine.InputSystem;

namespace VLAB.Core.Input
{
    /// <summary>Viewer trigger and standard gamepad fallback; does not impersonate a BLE protocol.</summary>
    public sealed class PhoneInputProvider : MonoBehaviour, IVLABInputProvider
    {
        private bool armed;
        private static int touchFrame=-1;
        private static bool acceptedTouch;
        public static bool ViewerTouchHeld
        {
            get
            {
                var touch=Touchscreen.current?.primaryTouch;
                if(touch==null)return false;
#if UNITY_ANDROID && !UNITY_EDITOR
                if (!VLabHeadPose.PhoneViewer)
                { acceptedTouch = false; touchFrame = -1; return touch.press.isPressed; }
                if(touchFrame!=Time.frameCount)
                {
                    touchFrame=Time.frameCount;
                    if(touch.press.wasPressedThisFrame)acceptedTouch=Google.XR.Cardboard.Api.IsTriggerPressed;
                    if(!touch.press.isPressed)acceptedTouch=false;
                }
                return touch.press.isPressed&&acceptedTouch;
#else
                return touch.press.isPressed;
#endif
            }
        }
        public static bool ViewerTouchPressed => ViewerTouchHeld && Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
        private void Update() { if (!RawPressed) armed = true; }
        private bool RawPressed => ViewerTouchHeld || Gamepad.current?.buttonSouth.isPressed == true;
        public string ProviderName => "Phone viewer / gamepad";
        public bool InteractionPressed => armed && RawPressed;
        public bool ResetPressed => Gamepad.current?.startButton.wasPressedThisFrame == true;
        public VLABInputState ReadState() => new VLABInputState
        {
            Move = Gamepad.current?.leftStick.ReadValue() ?? Vector2.zero,
            Look = Gamepad.current?.rightStick.ReadValue() ?? Vector2.zero,
            PrimaryPressed = InteractionPressed,
            SecondaryPressed = Gamepad.current?.rightShoulder.isPressed == true,
            DropPressed = Gamepad.current?.buttonEast.isPressed == true,
            PausePressed = Gamepad.current?.selectButton.isPressed == true || Keyboard.current?.escapeKey.isPressed == true,
            LeftGrabPressed = Gamepad.current?.leftTrigger.isPressed == true,
            RightGrabPressed = Gamepad.current?.rightTrigger.isPressed == true
        };
    }
}
