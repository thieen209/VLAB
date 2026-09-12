using NUnit.Framework;
using UnityEngine;
using VLAB.Core.Input;

namespace VLAB.Tests
{
    public class InputLifecycleTests
    {
        private sealed class InputSource : IVLABInputProvider
        {
            public VLABInputState State;
            public string ProviderName => "Deterministic input replay";
            public bool InteractionPressed => State.PrimaryPressed;
            public bool ResetPressed => false;
            public VLABInputState ReadState() => State;
        }
        [Test]
        public void ProviderLossReleasesHeldActionsAndPausedUiRetainsRawInput()
        {
            var host = new GameObject("Input lifecycle test");
            try
            {
                var input = host.AddComponent<InputManager>();
                var source = new InputSource { State = new VLABInputState { PrimaryPressed = true, RightGrabPressed = true, Move = new Vector2(3, 4) } };
                int releases = 0, grabs = 0;
                input.InteractionReleased += () => releases++;
                input.RightGrabReleased += () => grabs++;
                input.SetProvider(source); input.RefreshInput();
                Assert.That(input.CurrentState.Move.magnitude, Is.EqualTo(1).Within(.001));
                input.BlockExperimentInput = true; input.RefreshInput();
                Assert.That(input.RawState.PrimaryPressed, Is.True);
                Assert.That(input.CurrentState.PrimaryPressed, Is.False);
                Assert.That(releases, Is.EqualTo(1)); Assert.That(grabs, Is.EqualTo(1));
                input.BlockExperimentInput = false; input.RefreshInput();
                input.SetProvider(null);
                Assert.That(releases, Is.EqualTo(2)); Assert.That(grabs, Is.EqualTo(2));
                Assert.That(input.RawState.PrimaryPressed, Is.False);
                source.State = new VLABInputState { Move = new Vector2(float.NaN, 1), Look = new Vector2(0, float.PositiveInfinity), RightTriggerValue = float.NaN };
                input.SetProvider(source); input.RefreshInput();
                Assert.That(input.CurrentState.Move, Is.EqualTo(Vector2.zero));
                Assert.That(input.CurrentState.Look, Is.EqualTo(Vector2.zero));
                Assert.That(input.CurrentState.RightTriggerValue, Is.Zero);
            }
            finally { Object.DestroyImmediate(host); }
        }
    }
}
