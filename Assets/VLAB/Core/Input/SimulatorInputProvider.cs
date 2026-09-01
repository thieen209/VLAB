using UnityEngine;
using UnityEngine.InputSystem;

namespace VLAB.Core.Input
{
    /// <summary>Keyboard adapter used only for editor and simulator development.</summary>
    public sealed class SimulatorInputProvider : MonoBehaviour, IVLABInputProvider
    {
        public string ProviderName => "Simulator / Keyboard";

        public bool InteractionPressed =>
            Keyboard.current?.eKey.isPressed == true || Mouse.current?.leftButton.isPressed == true;

        public bool ResetPressed => Keyboard.current?.rKey.wasPressedThisFrame == true;

        public VLABInputState ReadState()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return default;
            }

            var mouse = Mouse.current;
            var move = new Vector2(
                (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f),
                (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f));
            if (move.sqrMagnitude > 1f)
            {
                move.Normalize();
            }

            return new VLABInputState
            {
                Move = move,
                Look = mouse?.delta.ReadValue() ?? Vector2.zero,
                ScrollDelta = (mouse?.scroll.ReadValue().y ?? 0f) / 120f,
                PrimaryPressed = mouse?.leftButton.isPressed == true || keyboard.eKey.isPressed || keyboard.digit1Key.isPressed,
                SecondaryPressed = mouse?.rightButton.isPressed == true || keyboard.digit2Key.isPressed,
                LeftGrabPressed = mouse?.leftButton.isPressed == true,
                RightGrabPressed = mouse?.rightButton.isPressed == true,
                SprintPressed = keyboard.leftShiftKey.isPressed,
                JumpPressed = keyboard.spaceKey.wasPressedThisFrame,
                DropPressed = keyboard.qKey.wasPressedThisFrame,
                PausePressed = keyboard.escapeKey.wasPressedThisFrame,
            };
        }
    }
}
