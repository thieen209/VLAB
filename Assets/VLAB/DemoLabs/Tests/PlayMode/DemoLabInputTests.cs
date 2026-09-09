using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace VLAB.DemoLabs.Tests
{
    // Use Unity's isolated input runtime: native input updates are unavailable in batch mode.
    public sealed class DemoLabInputTests : InputTestFixture
    {
        [Test]
        public void FastDragRetainsPickupPositionBeforeBatchedMotionAndRelease()
        {
            var previousMerging = InputSystem.settings.disableRedundantEventsMerging;
            var mouse = InputSystem.AddDevice<Mouse>();
            var host = new GameObject("DragBoundaryTest");
            try
            {
                var provider = host.AddComponent<VLabDemoInputProvider>();
                var start = new Vector2(200, 300); var end = new Vector2(700, 500);
                InputSystem.QueueStateEvent(mouse, new MouseState { position = start, buttons = 1 });
                InputSystem.QueueStateEvent(mouse, new MouseState { position = end, buttons = 1 });
                InputSystem.QueueStateEvent(mouse, new MouseState { position = end });
                InputSystem.Update();
                Assert.That(provider.ReadState().PrimaryPressed, Is.True);
                Assert.That(provider.PressPointer, Is.EqualTo(start), "Pick up at press, not at the drop target");
                Assert.That(provider.Pointer, Is.EqualTo(end));
            }
            finally { Object.DestroyImmediate(host); }
            Assert.That(InputSystem.settings.disableRedundantEventsMerging, Is.EqualTo(previousMerging), "Restore other scenes' input settings");
        }

        [Test]
        public void ShortDeviceTapsSurviveOneInputUpdate()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var mouse = InputSystem.AddDevice<Mouse>();
            var host = new GameObject("TapBoundaryTest");
            try
            {
                var provider = host.AddComponent<VLabDemoInputProvider>();
                _ = provider.ReadState(); _ = provider.ResetPressed;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.R));
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.QueueStateEvent(mouse, new MouseState { buttons = 1 });
                InputSystem.QueueStateEvent(mouse, new MouseState());
                InputSystem.Update();
                Assert.That(provider.ResetPressed, Is.True, "Fast reset tap");
                Assert.That(provider.ReadState().PrimaryPressed, Is.True, "Fast selection tap");
                InputSystem.Update();
                Assert.That(provider.ResetPressed, Is.False);
                Assert.That(provider.ReadState().PrimaryPressed, Is.False);
            }
            finally { Object.DestroyImmediate(host); }
        }

        [Test]
        public void SimulatorTouchKeepsItsPositionAfterFingerRelease()
        {
            var mouse = InputSystem.AddDevice<Mouse>(); InputSystem.DisableDevice(mouse);
            var touch = InputSystem.AddDevice<Touchscreen>();
            var host = new GameObject("TouchBoundaryTest");
            try
            {
                var provider = host.AddComponent<VLabDemoInputProvider>();
                var position = new Vector2(480, 320);
                InputSystem.QueueStateEvent(touch, new TouchState { touchId = 1, phase = UnityEngine.InputSystem.TouchPhase.Began, position = position });
                InputSystem.Update();
                Assert.That(provider.ReadState().PrimaryPressed, Is.True);
                InputSystem.QueueStateEvent(touch, new TouchState { touchId = 1, phase = UnityEngine.InputSystem.TouchPhase.Ended, position = position });
                InputSystem.Update(); InputSystem.Update();
                Assert.That(provider.ReadState().PrimaryPressed, Is.False);
                Assert.That(provider.Pointer, Is.EqualTo(position), "A held tool must not jump to the inactive mouse position");
            }
            finally { Object.DestroyImmediate(host); }
        }
    }
}
