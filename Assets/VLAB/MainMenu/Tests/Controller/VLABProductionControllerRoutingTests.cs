using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using VLAB.Core.Input;
using VLAB.ChemistryLab;
using VLAB.ChemistryLab.Input;

namespace VLAB.MainMenu.Tests
{
    public class VLABProductionControllerRoutingTests
    {
        private GameObject root;
        private Camera view;
        private VLabControllerReplayProvider replay;
        private InputManager input;
        private VLabGazeInputModule module;
        private VLABProductionPointerProbe probe;
        private TestProvider provider;
        private RenderTexture uiTarget;

        private sealed class TestProvider : IVLABInputProvider, IVLabRayProvider
        {
            public VLABInputState State;
            public Ray Ray;
            public bool Connected = true;
            public string ProviderName => "Controller test";
            public bool InteractionPressed => State.PrimaryPressed;
            public bool ResetPressed => false;
            public VLABInputState ReadState() => Connected ? State : default;
            public bool TryGetRay(Camera camera, out Ray ray) { ray = Ray; return Connected; }
        }

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("ControllerRoutingTest");
            replay = root.AddComponent<VLabControllerReplayProvider>();
            input = root.AddComponent<InputManager>();
            input.enabled = false; // Tests advance samples explicitly.
            var cameraObject = new GameObject("ControllerTestCamera");
            cameraObject.transform.SetParent(root.transform, false);
            view = cameraObject.AddComponent<Camera>();
            view.enabled = false;
            view.transform.position = new Vector3(100, 100, 100);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (view != null) view.targetTexture = null;
            if (uiTarget != null) RenderTexture.ReleaseTemporary(uiTarget);
            Object.Destroy(root);
            yield return null;
        }

        [Test]
        public void HeadYawDoesNotRotateControllerRay_ExplicitCalibrationRebases()
        {
            replay.Submit(default, Quaternion.identity, 100);
            Assert.That(replay.TryGetRay(view, out var first), Is.True);
            view.transform.rotation = Quaternion.Euler(0, 65, 0);
            Assert.That(replay.TryGetRay(view, out var second), Is.True);
            Assert.That(Vector3.Angle(first.direction, second.direction), Is.LessThan(.01f));
            replay.Calibrate();
            replay.TryGetRay(view, out var calibrated);
            Assert.That(Vector3.Angle(view.transform.forward, calibrated.direction), Is.LessThan(.01f));
        }

        [Test]
        public void TimeoutAcceptsRestartedSequence_InvalidPacketsDoNotReviveConnection()
        {
            Assert.That(replay.Submit(default, Quaternion.identity, 100), Is.True);
            typeof(VLabControllerReplayProvider).GetField("receivedAt", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(replay, Time.unscaledTime - 10);
            Assert.That(replay.Connected, Is.False);
            Assert.That(replay.Submit(default, new Quaternion(float.NaN, 0, 0, 1), 1), Is.False);
            Assert.That(replay.Connected, Is.False);
            Assert.That(replay.Submit(default, Quaternion.identity, 1), Is.True);
            Assert.That(replay.Submit(default, Quaternion.identity, 1), Is.False);
        }

        private void CreatePointer(bool armWithNeutralSample = true)
        {
            var eventsObject = new GameObject("ControllerTestEvents");
            eventsObject.transform.SetParent(root.transform, false);
            var events = eventsObject.AddComponent<EventSystem>();
            events.enabled = false;
            module = eventsObject.AddComponent<VLabGazeInputModule>();
            module.Input = input;
            module.ViewCamera = view;
            input.enabled = true;
            var canvasObject = new GameObject("ControllerTestCanvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(root.transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = view;
            canvas.transform.position = view.transform.position + view.transform.forward * 2;
            canvas.transform.rotation = view.transform.rotation;
            canvas.transform.localScale = Vector3.one * .002f;
            ((RectTransform)canvas.transform).sizeDelta = new Vector2(800, 500);
            var target = new GameObject("PointerTarget", typeof(RectTransform), typeof(Image));
            target.transform.SetParent(canvas.transform, false);
            target.GetComponent<RectTransform>().sizeDelta = new Vector2(700, 400);
            probe = target.AddComponent<VLABProductionPointerProbe>();
            provider = new TestProvider { Ray = new Ray(view.transform.position, view.transform.forward) };
            input.SetProvider(provider);
            Canvas.ForceUpdateCanvases();
            // GraphicRaycaster rejects depth == -1 until Unity has processed the Canvas.
            // This synchronous fixture uses its own disabled camera, so render it explicitly.
            uiTarget = RenderTexture.GetTemporary(800, 600, 24);
            view.targetTexture = uiTarget;
            view.Render();
            Assert.That(target.GetComponent<Image>().depth, Is.GreaterThanOrEqualTo(0), "Fixture Canvas must be rendered before pointer input.");
            var hit = VLabPointerUi.Raycast(provider.Ray, new PointerEventData(events), new List<RaycastResult>());
            Assert.That(hit.gameObject, Is.SameAs(target), "The controller must hit the actual fixture UI before interaction assertions.");
            if (armWithNeutralSample) Step(false);
        }

        private void Step(bool pressed)
        {
            provider.State.PrimaryPressed = pressed;
            input.RefreshInput();
            module.Process();
        }

        [Test]
        public void NewlyActivatedModuleRequiresReleaseBeforeFirstClick()
        {
            CreatePointer(false);
            Step(true);
            Assert.That(probe.Down, Is.Zero, "A held button must not create a press when a module is activated.");
            Step(false);
            Assert.That(probe.Clicks, Is.Zero);
            Step(true); Step(false);
            Assert.That(probe.Down, Is.EqualTo(1));
            Assert.That(probe.Clicks, Is.EqualTo(1));
        }

        [Test]
        public void DisconnectAndProviderSwitchCancelPendingClick()
        {
            CreatePointer();
            Step(true);
            Assert.That(probe.Down, Is.EqualTo(1));
            provider.Connected = false;
            Step(false);
            Assert.That(probe.Clicks, Is.Zero);
            Assert.That(probe.Up, Is.EqualTo(1));
            provider.Connected = true;
            Step(false);
            Step(true);
            provider = new TestProvider { Ray = provider.Ray };
            input.SetProvider(provider);
            Step(false);
            Assert.That(probe.Clicks, Is.Zero);
            Step(true);
            Step(false);
            Assert.That(probe.Clicks, Is.EqualTo(1));
        }

        [Test]
        public void PausedUiReceivesRawClickScrollAndDrag_WithoutClickAfterDrag()
        {
            CreatePointer();
            input.BlockExperimentInput = true;
            Step(true);
            Assert.That(input.CurrentState.PrimaryPressed, Is.False);
            Step(false);
            Assert.That(probe.Clicks, Is.EqualTo(1));
            provider.State.ScrollDelta = 1;
            Step(false);
            Assert.That(probe.Scroll.y, Is.EqualTo(1));
            provider.State.ScrollDelta = 0;
            Step(true);
            provider.Ray = new Ray(view.transform.position, new Vector3(.3f, 0, 2).normalized);
            Step(true);
            Assert.That(probe.BeginDrag, Is.EqualTo(1));
            Assert.That(probe.Drag, Is.GreaterThan(0));
            Step(false);
            Assert.That(probe.EndDrag, Is.EqualTo(1));
            Assert.That(probe.Clicks, Is.EqualTo(1));
        }

        [Test]
        public void ControllerDragChangesActualSlider_AndDeactivationCancelsClick()
        {
            CreatePointer();
            var slider = probe.gameObject.AddComponent<Slider>();
            var handle = new GameObject("SliderHandle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(slider.transform, false);
            handle.GetComponent<Image>().raycastTarget = false;
            var rect = handle.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(40, 100);
            slider.handleRect = rect;
            slider.value = .5f;
            Canvas.ForceUpdateCanvases();
            Step(true);
            provider.Ray = new Ray(view.transform.position, new Vector3(.4f, 0, 2).normalized);
            Step(true);
            Assert.That(slider.value, Is.GreaterThan(.6f));
            module.DeactivateModule();
            Assert.That(probe.EndDrag, Is.EqualTo(1));
            Step(false);
            Assert.That(probe.Clicks, Is.Zero);
        }

        [Test]
        public void ChemistryUseButtonFiresOncePerPressAndResetsForNewProvider()
        {
            var navigator = view.gameObject.AddComponent<DesktopLabNavigator>();
            navigator.enabled = false;
            navigator.SharedInput = input;
            provider = new TestProvider { State = new VLABInputState { SecondaryPressed = true } };
            input.SetProvider(provider);
            input.RefreshInput();
            var read = typeof(DesktopLabNavigator).GetMethod("ReadSharedControllerFrame", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(read, Is.Not.Null);
            Assert.That(((VLabDesktopInputFrame)read.Invoke(navigator, null)).UsePressed, Is.True);
            Assert.That(((VLabDesktopInputFrame)read.Invoke(navigator, null)).UsePressed, Is.False);
            provider.State.SecondaryPressed = false;
            input.RefreshInput();
            read.Invoke(navigator, null);
            provider.State.SecondaryPressed = true;
            input.RefreshInput();
            Assert.That(((VLabDesktopInputFrame)read.Invoke(navigator, null)).UsePressed, Is.True);
            input.SetProvider(new TestProvider { State = provider.State });
            input.RefreshInput();
            Assert.That(((VLabDesktopInputFrame)read.Invoke(navigator, null)).UsePressed, Is.True);
        }
    }

    public sealed class VLABProductionPointerProbe : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
        IPointerClickHandler, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
    {
        public int Down, Up, Clicks, BeginDrag, Drag, EndDrag;
        public Vector2 Scroll;
        public void OnPointerDown(PointerEventData data) => Down++;
        public void OnPointerUp(PointerEventData data) => Up++;
        public void OnPointerClick(PointerEventData data) => Clicks++;
        public void OnInitializePotentialDrag(PointerEventData data) { }
        public void OnBeginDrag(PointerEventData data) => BeginDrag++;
        public void OnDrag(PointerEventData data) => Drag++;
        public void OnEndDrag(PointerEventData data) => EndDrag++;
        public void OnScroll(PointerEventData data) => Scroll = data.scrollDelta;
    }
}
