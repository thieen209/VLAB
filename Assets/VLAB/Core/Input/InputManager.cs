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
        public string ActiveProviderName =>
            activeProvider?.ProviderName ?? (providerSource as IVLABInputProvider)?.ProviderName ?? "None";

        public bool HasProvider => activeProvider != null || providerSource is IVLABInputProvider;

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
            activeProvider = provider;
            providerSource = provider as MonoBehaviour;
            CurrentState = default;
            previousInteractionPressed = false;
            previousResetPressed = false;
        }

        public void RefreshInput()
        {
            if (activeProvider == null)
            {
                ResolveSerializedProvider();
            }

            if (activeProvider == null)
            {
                CurrentState = default;
                return;
            }

            var previousState = CurrentState;
            var nextState = activeProvider.ReadState();
            var interactionPressed = activeProvider.InteractionPressed;
            var resetPressed = activeProvider.ResetPressed;
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
