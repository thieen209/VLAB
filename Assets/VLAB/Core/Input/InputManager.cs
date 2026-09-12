using System;
using UnityEngine;

namespace VLAB.Core.Input
{
    [DefaultExecutionOrder(-200)]
    public sealed class InputManager : MonoBehaviour, IInteractionInput
    {
        [SerializeField] private MonoBehaviour providerSource;

        private IVLABInputProvider activeProvider;
        private bool previousInteractionPressed;
        private bool previousResetPressed;

        public event Action InteractionPressed;
        public event Action InteractionReleased;
        public event Action PrimaryPressed;
        public event Action PrimaryReleased;
        public event Action SecondaryPressed;
        public event Action SecondaryReleased;
        public event Action LeftGrabPressed;
        public event Action LeftGrabReleased;
        public event Action RightGrabPressed;
        public event Action RightGrabReleased;
        public event Action DropPressed;
        public event Action ResetPressed;
        public event Action PausePressed;

        public VLABInputState CurrentState { get; private set; }
        // UI must remain operable while experiment actions are suspended by a menu.
        public VLABInputState RawState { get; private set; }
        public bool BlockExperimentInput { get; set; }
        public bool TranslationLocked { get; set; }
        public string ActiveProviderName =>
            activeProvider?.ProviderName ?? (providerSource as IVLABInputProvider)?.ProviderName ?? "None";

        public bool HasProvider => activeProvider != null || providerSource is IVLABInputProvider;
        public bool HasRayProvider => activeProvider is IVLabRayProvider;
        public IVLABInputProvider Provider => activeProvider;

        public Ray PointerRay(Camera view, Vector2 screenPosition)
        {
            if (activeProvider is IVLabRayProvider provider && provider.TryGetRay(view, out var ray)) return ray;
            if(Application.platform==RuntimePlatform.Android && !VLabHeadPose.PhoneViewer && UnityEngine.InputSystem.Touchscreen.current!=null)
                screenPosition=UnityEngine.InputSystem.Touchscreen.current.primaryTouch.position.ReadValue();
            return VLabHeadPose.PhoneViewer ? new Ray(view.transform.position, view.transform.forward) : view.ScreenPointToRay(screenPosition);
        }

        private void Awake()
        {
            ResolveSerializedProvider();
        }

        private void Update()
        {
            RefreshInput();
        }

        public void SetProvider(IVLABInputProvider provider)
        {
            ReleaseCurrentInput();
            activeProvider = provider;
            providerSource = provider as MonoBehaviour;
            CurrentState = default;
            previousInteractionPressed = false;
            previousResetPressed = false;
        }

        private void OnDisable() => ReleaseCurrentInput();

        private void ReleaseCurrentInput()
        {
            var previous = CurrentState;
            CurrentState = RawState = default;
            var wasInteracting = previousInteractionPressed;
            previousInteractionPressed = previousResetPressed = false;
            if (wasInteracting) InteractionReleased?.Invoke();
            if (previous.PrimaryPressed) PrimaryReleased?.Invoke();
            if (previous.SecondaryPressed) SecondaryReleased?.Invoke();
            if (previous.LeftGrabPressed) LeftGrabReleased?.Invoke();
            if (previous.RightGrabPressed) RightGrabReleased?.Invoke();
        }

        public void RefreshInput()
        {
            if (activeProvider == null)
            {
                ResolveSerializedProvider();
            }

            if (activeProvider == null)
            {
                ReleaseCurrentInput();
                return;
            }

            var previousState = CurrentState;
            var nextState = activeProvider.ReadState();
            nextState.Move = SanitizeAxis(nextState.Move);
            nextState.Look = IsFinite(nextState.Look.x) && IsFinite(nextState.Look.y) ? nextState.Look : Vector2.zero;
            nextState.ScrollDelta = IsFinite(nextState.ScrollDelta) ? nextState.ScrollDelta : 0;
            nextState.LeftTriggerValue = SanitizeTrigger(nextState.LeftTriggerValue);
            nextState.RightTriggerValue = SanitizeTrigger(nextState.RightTriggerValue);
            RawState = nextState;
            var interactionPressed = activeProvider.InteractionPressed;
            var resetPressed = activeProvider.ResetPressed;
            if (BlockExperimentInput)
            {
                nextState = new VLABInputState { PausePressed = nextState.PausePressed };
                interactionPressed = false;
                resetPressed = false;
            }
            if (TranslationLocked) nextState.Move = Vector2.zero;
            CurrentState = nextState;

            RaiseEdge(previousInteractionPressed, interactionPressed, InteractionPressed, InteractionReleased);
            RaiseEdge(previousState.PrimaryPressed, nextState.PrimaryPressed, PrimaryPressed, PrimaryReleased);
            RaiseEdge(previousState.SecondaryPressed, nextState.SecondaryPressed, SecondaryPressed, SecondaryReleased);
            RaiseEdge(previousState.LeftGrabPressed, nextState.LeftGrabPressed, LeftGrabPressed, LeftGrabReleased);
            RaiseEdge(previousState.RightGrabPressed, nextState.RightGrabPressed, RightGrabPressed, RightGrabReleased);

            if (!previousState.DropPressed && nextState.DropPressed)
            {
                DropPressed?.Invoke();
            }
            if (!previousState.PausePressed && nextState.PausePressed)
            {
                PausePressed?.Invoke();
            }

            if (!previousResetPressed && resetPressed)
            {
                ResetPressed?.Invoke();
            }

            previousInteractionPressed = interactionPressed;
            previousResetPressed = resetPressed;
        }

        private void ResolveSerializedProvider()
        {
            if (providerSource == null)
            {
                activeProvider = null;
                return;
            }

            activeProvider = providerSource as IVLABInputProvider;
            if (activeProvider == null)
            {
                Debug.LogError($"{providerSource.GetType().Name} does not implement {nameof(IVLABInputProvider)}.", this);
            }
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static Vector2 SanitizeAxis(Vector2 value) => IsFinite(value.x) && IsFinite(value.y) ? Vector2.ClampMagnitude(value, 1) : Vector2.zero;
        private static float SanitizeTrigger(float value) => IsFinite(value) ? Mathf.Clamp01(value) : 0;

        private static void RaiseEdge(bool previous, bool current, Action pressed, Action released)
        {
            if (!previous && current)
            {
                pressed?.Invoke();
            }
            else if (previous && !current)
            {
                released?.Invoke();
            }
        }
    }
}
