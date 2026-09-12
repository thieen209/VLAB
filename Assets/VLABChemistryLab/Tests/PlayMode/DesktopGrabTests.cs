using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VLAB.ChemistryLab.Input;
using VLAB.ChemistryLab.Interaction;

namespace VLAB.ChemistryLab.Tests.PlayMode
{
    public class DesktopGrabTests
    {
        private Mouse mouse;
        private Keyboard keyboard;
        [SetUp] public void Setup() { mouse = InputSystem.AddDevice<Mouse>(); keyboard = InputSystem.AddDevice<Keyboard>(); }
        [TearDown] public void Cleanup() { InputSystem.RemoveDevice(mouse); InputSystem.RemoveDevice(keyboard); }
        [UnityTest]
        public IEnumerator MouseClick_GrabsMovesUsesAndReleasesBottle()
        {
            yield return SceneManager.LoadSceneAsync("ChemistryLab");
            yield return null;
            var navigator = Object.FindAnyObjectByType<DesktopLabNavigator>();
            Assert.That(GameObject.Find("Simulated Left Hand"), Is.Null);
            Assert.That(GameObject.Find("Simulated Right Hand"), Is.Null);
            var camera = navigator.GetComponent<Camera>();
            var grabber = navigator.GetComponent<DesktopLabGrabber>();
            var bottle = GameObject.Find("HandsOn_NaOH").GetComponent<LabLiquidVessel>();
            // Move the test target in reach and into a clear screen area; do not bypass the mouse pick path.
            bottle.GetComponent<Rigidbody>().useGravity = false;
            bottle.transform.position = camera.transform.position + camera.transform.forward * 1.25f + camera.transform.right * .3f;
            var panel = Object.FindAnyObjectByType<DesktopTitrationInterface>();
            if (panel.IsPanelVisible) panel.TogglePanel();
            var clickAt = camera.WorldToScreenPoint(bottle.transform.position + Vector3.up * .07f);
            Physics.SyncTransforms();
            InputSystem.QueueStateEvent(mouse, new MouseState { position = clickAt }.WithButton(MouseButton.Left));
            InputSystem.Update(); navigator.SendMessage("Update");
            Assert.That(grabber.IsHolding, Is.True, "A mouse click must select the bottle through the real navigator path.");
            Assert.That(bottle.IsHeld, Is.True, "Desktop must share XRI ownership with the chemistry/recovery components.");
            InputSystem.QueueStateEvent(mouse, new MouseState { position = clickAt }); InputSystem.Update();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F)); InputSystem.Update(); navigator.SendMessage("Update");
            Assert.That(bottle.IsOpen, Is.True);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
            Vector3 before = bottle.transform.position;
            float fov = camera.fieldOfView;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = clickAt + new Vector3(80, 0, 0), scroll = new Vector2(0, 1) });
            InputSystem.Update(); navigator.SendMessage("Update");
            yield return new WaitForSeconds(.2f);
            Assert.That(grabber.IsHolding, Is.True, "The active Desktop manager must retain selection across frames.");
            Assert.That(Vector3.Distance(bottle.transform.position, before), Is.GreaterThan(.02f));
            Assert.That(camera.fieldOfView, Is.EqualTo(fov), "Held-object depth must not change FOV.");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape)); InputSystem.Update(); navigator.SendMessage("Update");
            Assert.That(bottle.IsHeld, Is.False);
            Assert.That(grabber.IsHolding, Is.False);
        }
        [UnityTest]
        public IEnumerator ReagentShelfBottlesAndPipette_AreGrabbable()
        {
            yield return SceneManager.LoadSceneAsync("ChemistryLab"); yield return null;
            var navigator = Object.FindAnyObjectByType<DesktopLabNavigator>();
            var grabber = navigator.GetComponent<DesktopLabGrabber>();
            foreach (string name in new[] { "NaOH_0.100M", "DilutedVinegar", "Phenolphthalein", "ReagentPipette_10mL" })
            {
                var vessel = GameObject.Find(name).GetComponent<LabLiquidVessel>();
                Assert.That(vessel, Is.Not.Null, name + " must no longer be a decorative bottle.");
                vessel.transform.position = navigator.transform.position + navigator.transform.forward;
                var panel = Object.FindAnyObjectByType<DesktopTitrationInterface>();
                if (panel.IsPanelVisible) panel.TogglePanel();
                InputSystem.QueueStateEvent(mouse, new MouseState { position = navigator.GetComponent<Camera>().WorldToScreenPoint(vessel.transform.position) });
                InputSystem.Update();
                Physics.SyncTransforms();
                Assert.That(grabber.TryGrab(vessel.GetComponentInChildren<Collider>()), Is.True, name);
                Assert.That(vessel.IsHeld, Is.True);
                float deadline = Time.time + 1f;
                do { yield return new WaitForFixedUpdate(); }
                while (Vector3.Dot(vessel.transform.up, Vector3.up) <= .95f && Time.time < deadline);
                Assert.That(Vector3.Dot(vessel.transform.up, Vector3.up), Is.GreaterThan(.95f),
                    $"{name}: held={grabber.IsHolding}, body={vessel.transform.position}, rotation={vessel.transform.eulerAngles}, hand={navigator.transform.Find("Desktop physical hand").eulerAngles}, angular={vessel.GetComponent<Rigidbody>().angularVelocity}, nearby={string.Join(",", System.Array.ConvertAll(Physics.OverlapSphere(vessel.transform.position, .35f), c => c.transform.root.name + "/" + c.name))}");
                grabber.Release();
                Assert.That(vessel.IsHeld, Is.False);
                // Selection detachment finishes later in the player loop. Remove each finished
                // fixture so its colliders cannot obstruct the next independent pick test.
                vessel.gameObject.SetActive(false);
            }
        }
        [UnityTest]
        public IEnumerator MouseTap_HoldDrainsAndReleaseOrFocusLossStops()
        {
            yield return SceneManager.LoadSceneAsync("ChemistryLab"); yield return null;
            var navigator = Object.FindAnyObjectByType<DesktopLabNavigator>();
            var camera = navigator.GetComponent<Camera>();
            var tap = Object.FindAnyObjectByType<LabBuretteTap>();
            var burette = GameObject.Find("Burette_50mL_NaOH").GetComponent<LabLiquidVessel>();
            var source = GameObject.Find("HandsOn_NaOH").GetComponent<LabLiquidVessel>();
            var waste = GameObject.Find("HandsOn_Waste").GetComponent<LabLiquidVessel>();
            foreach (var body in source.Station.GetComponentsInChildren<Rigidbody>()) { body.useGravity = false; body.isKinematic = true; }
            Object.FindAnyObjectByType<TitrationLessonController>().PerformSafety();
            source.transform.position = navigator.transform.position + navigator.transform.forward;
            var grabber = navigator.GetComponent<DesktopLabGrabber>();
            Assert.That(grabber.TryGrab(source.GetComponentInChildren<Collider>()), Is.True);
            source.Use(); grabber.Release();
            Assert.That(source.Station.Transfer(source, burette, 5), Is.EqualTo(5));
            source.transform.position = new Vector3(-2, 1.2f, 0);
            GameObject.Find("ErlenmeyerFlask_250mL").transform.position = new Vector3(-1.5f, 1.2f, 0);
            // Separate the pick target from the fluid outlet so the test exercises real input, not aim precision.
            tap.transform.position = camera.transform.position + camera.transform.forward * 1.2f;
            var outlet = burette.transform.Find("Outlet");
            waste.transform.rotation = Quaternion.identity;
            waste.transform.position += outlet.position - Vector3.up * .1f - waste.Mouth.position;
            var panel = Object.FindAnyObjectByType<DesktopTitrationInterface>();
            if (panel.IsPanelVisible) panel.TogglePanel();
            Physics.SyncTransforms();
            var clickAt = camera.WorldToScreenPoint(tap.GetComponent<Collider>().bounds.center);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = clickAt }.WithButton(MouseButton.Left));
            InputSystem.Update(); navigator.SendMessage("Update");
            Assert.That(tap.IsDesktopHeld, Is.True);
            yield return new WaitForSeconds(.45f);
            Assert.That(burette.Liquid.VolumeMl, Is.LessThan(4.8));
            Assert.That(source.Station.RequiresReset, Is.False, "The rinse must enter the waste vessel.");
            InputSystem.QueueStateEvent(mouse, new MouseState { position = clickAt });
            InputSystem.Update(); navigator.SendMessage("Update");
            double remaining = burette.Liquid.VolumeMl;
            yield return new WaitForSeconds(.2f);
            Assert.That(tap.IsDesktopHeld, Is.False);
            Assert.That(burette.Liquid.VolumeMl, Is.EqualTo(remaining));
            InputSystem.QueueStateEvent(mouse, new MouseState { position = clickAt }.WithButton(MouseButton.Left));
            InputSystem.Update(); navigator.SendMessage("Update");
            navigator.SendMessage("OnApplicationFocus", false);
            Assert.That(tap.IsDesktopHeld, Is.False);
            Assert.That(burette.Liquid.VolumeMl + waste.Liquid.VolumeMl, Is.EqualTo(5).Within(.001));
        }
        [UnityTest]
        public IEnumerator HeldBottle_PushedBelowBenchAndReleased_StaysAboveSurface()
        {
            yield return SceneManager.LoadSceneAsync("ChemistryLab"); yield return null;
            var vessel = GameObject.Find("HandsOn_NaOH").GetComponent<LabLiquidVessel>();
            var body = vessel.GetComponent<Rigidbody>();
            var grab = vessel.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            var table = GameObject.Find("MainTitrationBench").transform.Find("Top").GetComponent<Collider>();
            float surface = table.bounds.max.y;
            var handObject = new GameObject("Table collision test hand");
            var manager = handObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>();
            handObject.AddComponent<SphereCollider>().isTrigger = true;
            var hand = handObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRDirectInteractor>();
            hand.interactionManager = manager;
            hand.selectInput.inputSourceMode = UnityEngine.XR.Interaction.Toolkit.Inputs.Readers.XRInputButtonReader.InputSourceMode.ManualValue;
            hand.selectInput.manualPerformed = true;
            hand.selectInput.manualValue = 1;
            hand.keepSelectedTargetValid = true;
            grab.interactionManager = manager;
            body.position = new Vector3(-1.25f, surface + .35f, .63f);
            hand.transform.SetPositionAndRotation(body.position, body.rotation);
            Physics.SyncTransforms();
            try
            {
                manager.SelectEnter((UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor)hand, grab);
                yield return new WaitForSeconds(.2f);
                hand.transform.position = new Vector3(-1.25f, surface - .25f, .63f);
                for (int i = 0; i < 40; i++)
                {
                    yield return new WaitForFixedUpdate();
                    Assert.That(grab.isSelected, Is.True);
                    Assert.That(body.position.y, Is.GreaterThanOrEqualTo(surface - .015f), "Held bottle crossed the tabletop.");
                }
                hand.selectInput.manualPerformed = false;
                manager.SelectExit((UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor)hand, grab);
                yield return new WaitForSeconds(.4f);
                Assert.That(body.position.y, Is.GreaterThanOrEqualTo(surface - .015f), "Released bottle fell through the tabletop.");
            }
            finally { Object.Destroy(handObject); }
        }
        [UnityTest]
        public IEnumerator DisablingDesktop_ReleasesAndRestoresXrManager()
        {
            yield return SceneManager.LoadSceneAsync("ChemistryLab"); yield return null;
            var navigator = Object.FindAnyObjectByType<DesktopLabNavigator>();
            var grabber = navigator.GetComponent<DesktopLabGrabber>();
            var bottle = GameObject.Find("HandsOn_NaOH").GetComponent<LabLiquidVessel>();
            var grab = bottle.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            var original = grab.interactionManager;
            bottle.transform.position = navigator.transform.position + navigator.transform.forward;
            Assert.That(grabber.TryGrab(bottle.GetComponentInChildren<Collider>()), Is.True);
            Assert.That(bottle.GetComponent<LabGrabRecovery>().RecoverToSpawn(), Is.False);
            grabber.enabled = false;
            Assert.That(grab.isSelected, Is.False);
            Assert.That(grab.interactionManager, Is.SameAs(original));
        }
    }
}
