using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using VLAB.ChemistryLab.Mode;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using VLAB.ChemistryLab.Interaction;

namespace VLAB.ChemistryLab.Tests.PlayMode
{
    public class HandsOnTitrationTests
    {
        private HandsOnTitration station;
        private TitrationLessonController lesson;
        private XRInteractionManager manager;
        private XRDirectInteractor hand;
        private LabLiquidVessel[] vessels;
        [UnitySetUp]
        public IEnumerator Setup()
        {
            yield return SceneManager.LoadSceneAsync("ChemistryLab");
            yield return null;
            station = Object.FindAnyObjectByType<HandsOnTitration>();
            Assert.That(station, Is.Not.Null, "Build the v10 scene before running integration tests.");
            lesson = Object.FindAnyObjectByType<TitrationLessonController>();
            vessels = station.GetComponentsInChildren<LabLiquidVessel>();
            foreach (var body in station.GetComponentsInChildren<Rigidbody>()) { body.useGravity = false; body.isKinematic = true; }
            var go = new GameObject("Hands-on test hand");
            manager = go.AddComponent<XRInteractionManager>();
            go.AddComponent<SphereCollider>().isTrigger = true;
            hand = go.AddComponent<XRDirectInteractor>(); hand.interactionManager = manager;
            yield return null;
        }
        [TearDown] public void Cleanup() { if (hand != null) Object.Destroy(hand.gameObject); }
        private LabLiquidVessel Get(LabVesselRole role, LabReagent reagent = LabReagent.None) =>
            vessels.First(v => v.Role == role && (reagent == LabReagent.None || v.Liquid.Reagent == reagent));
        private void Hold(LabLiquidVessel vessel)
        {
            hand.transform.position = vessel.transform.position;
            var grab = vessel.GetComponent<XRGrabInteractable>(); grab.interactionManager = manager;
            manager.SelectEnter((IXRSelectInteractor)hand, grab);
        }
        private void Release(LabLiquidVessel vessel) => manager.SelectExit((IXRSelectInteractor)hand, vessel.GetComponent<XRGrabInteractable>());
        private void Open(LabLiquidVessel vessel) { Hold(vessel); vessel.Use(); Release(vessel); Assert.That(vessel.IsOpen, Is.True); }
        private void Prepare()
        {
            lesson.PerformSafety();
            var naoh = Get(LabVesselRole.Bottle, LabReagent.NaOH);
            var burette = Get(LabVesselRole.Burette);
            var waste = Get(LabVesselRole.Waste);
            Open(naoh);
            Assert.That(station.Transfer(naoh, burette, 50), Is.EqualTo(5));
            lesson.PrepareBurette();
            Assert.That(lesson.LastActionAccepted, Is.False, "The console must not skip a physical rinse.");
            for (int i = 0; i < 50; i++) station.Transfer(burette, waste, .1);
            Assert.That(station.Transfer(naoh, burette, 50), Is.EqualTo(50));
            Assert.That(lesson.Experiment.CurrentStep, Is.EqualTo(TitrationStep.PipetteDilutedVinegar));
        }
        [UnityTest]
        public IEnumerator ResetChemicals_RestoresStockAndStopsTapWithoutMovingHeldBottle()
        {
            double[] initial = vessels.Select(v => v.Liquid.VolumeMl).ToArray();
            Prepare();
            var bottle = Get(LabVesselRole.Bottle, LabReagent.NaOH);
            Hold(bottle);
            var position = bottle.transform.position;
            var tap = station.GetComponentInChildren<LabBuretteTap>();
            tap.BeginDesktopHold();
            lesson.ResetChemicalAmounts();
            Assert.That(tap.IsDesktopHeld, Is.False);
            Assert.That(bottle.IsHeld, Is.True);
            Assert.That(bottle.transform.position, Is.EqualTo(position));
            Assert.That(station.UsingTools, Is.False);
            Assert.That(station.RequiresReset, Is.False);
            Assert.That(lesson.Experiment.CurrentStep, Is.EqualTo(TitrationStep.RinseAndFillBurette));
            for (int i = 0; i < vessels.Length; i++) Assert.That(vessels[i].Liquid.VolumeMl, Is.EqualTo(initial[i]));
            yield return new WaitForSeconds(.35f);
            for (int i = 0; i < vessels.Length; i++) Assert.That(vessels[i].Liquid.VolumeMl, Is.EqualTo(initial[i]), "Reset must stop any continuing flow.");
        }

        [UnityTest]
        public IEnumerator PourPreview_MatchesMouthAndRejectsObstructionWithoutConsumingLiquid()
        {
            var bottle = Get(LabVesselRole.Bottle, LabReagent.NaOH);
            var flask = Get(LabVesselRole.Flask);
            Open(bottle);
            flask.transform.position = new Vector3(0, 2, -2);
            bottle.PreviewPour(out var oldOrigin, out _);
            bottle.transform.position += flask.Mouth.position + Vector3.up * .15f - oldOrigin;
            Physics.SyncTransforms();
            double before = bottle.Liquid.VolumeMl;
            Assert.That(bottle.PreviewPour(out var origin, out var target), Is.EqualTo(flask));
            Assert.That(target.y, Is.EqualTo(flask.Mouth.position.y).Within(.001f));
            Assert.That(target.x, Is.EqualTo(origin.x));
            Assert.That(target.z, Is.EqualTo(origin.z));
            var obstruction = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                obstruction.transform.position = (origin + target) * .5f;
                obstruction.transform.localScale = new Vector3(.15f, .015f, .15f);
                Physics.SyncTransforms();
                Assert.That(bottle.PreviewPour(out _, out var blockedPoint), Is.Null);
                Assert.That(blockedPoint.y, Is.GreaterThan(target.y));
                Assert.That(bottle.Liquid.VolumeMl, Is.EqualTo(before));
                Hold(bottle);
                var guide = bottle.GetComponent<LabPourGuide>();
                guide.SendMessage("LateUpdate");
                Assert.That(guide.Visible, Is.True);
                bottle.ResetContents();
                guide.SendMessage("LateUpdate");
                Assert.That(guide.Visible, Is.False);
            }
            finally { Object.Destroy(obstruction); }
            yield return null;
        }

        [UnityTest]
        public IEnumerator ActualPipetteUseAndMeasuredDoses_CompleteThreeConcordantTrials()
        {
            for (int trial = 0; trial < 3; trial++)
            {
                if (trial == 0) Prepare();
                else
                {
                    var naoh = Get(LabVesselRole.Bottle, LabReagent.NaOH); Open(naoh);
                    var b = Get(LabVesselRole.Burette);
                    station.Transfer(naoh, b, 5); station.Transfer(b, Get(LabVesselRole.Waste), 5); station.Transfer(naoh, b, 50);
                }
                var vinegar = Get(LabVesselRole.Bottle, LabReagent.DilutedVinegar); Open(vinegar);
                var indicator = Get(LabVesselRole.Bottle, LabReagent.Indicator); Open(indicator);
                var pipette = Get(LabVesselRole.Pipette); var flask = Get(LabVesselRole.Flask);
                Hold(pipette);
                pipette.transform.rotation = Quaternion.identity;
                pipette.transform.position = vinegar.Mouth.position - Vector3.up * .01f;
                pipette.Use(); Assert.That(pipette.Liquid.VolumeMl, Is.EqualTo(10));
                pipette.transform.position = flask.Mouth.position + Vector3.up * .04f;
                Physics.SyncTransforms(); pipette.Use();
                Assert.That(flask.Liquid.VolumeMl, Is.EqualTo(10)); Release(pipette);
                // Park the pipette clear of the burette flow after dispensing.
                pipette.transform.position = new Vector3(-.5f, 1.1f, .2f);
                Assert.That(station.Transfer(indicator, flask, .05), Is.EqualTo(.05));
                Assert.That(lesson.Experiment.CurrentStep, Is.EqualTo(TitrationStep.AddIndicator));
                station.Transfer(indicator, flask, .05);
                var burette = Get(LabVesselRole.Burette); Physics.SyncTransforms();
                for (int i = 0; i < 833; i++) burette.Pour(.01, burette.transform.Find("Outlet").position);
                Assert.That(lesson.Experiment.DeliveredVolumeMl, Is.EqualTo(8.33).Within(.001));
                Assert.That(burette.Liquid.VolumeMl, Is.EqualTo(41.67).Within(.001));
                lesson.RecordResult(); Assert.That(lesson.LastActionAccepted, Is.True);
                yield return null;
            }
            Assert.That(lesson.Experiment.IsComplete, Is.True);
            lesson.ResetChemicalAmounts();
            Assert.That(lesson.Experiment.Observations.Count, Is.EqualTo(3), "Chemical reset must preserve recorded results.");
            Assert.That(lesson.Experiment.MassVolumePercent, Is.EqualTo(5.002).Within(.02));
        }
        [UnityTest]
        public IEnumerator ClosedLidWrongReagentAndBlockedPour_DoNotAdvanceLesson()
        {
            var naoh = Get(LabVesselRole.Bottle, LabReagent.NaOH); var burette = Get(LabVesselRole.Burette);
            Assert.That(station.Transfer(naoh, burette, 5), Is.Zero);
            lesson.PerformSafety(); Open(naoh);
            var vinegar = Get(LabVesselRole.Bottle, LabReagent.DilutedVinegar); Open(vinegar);
            Assert.That(station.Transfer(vinegar, burette, 5), Is.Zero);
            var obstruction = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstruction.transform.position = burette.Mouth.position + Vector3.up * .10f;
            obstruction.transform.localScale = Vector3.one * .08f;
            Physics.SyncTransforms();
            Assert.That(naoh.Pour(1, burette.Mouth.position + Vector3.up * .20f), Is.Zero);
            Assert.That(burette.Liquid.VolumeMl, Is.Zero);
            Assert.That(naoh.Liquid.VolumeMl, Is.EqualTo(199));
            Object.Destroy(obstruction);
            yield return null;
        }
        [UnityTest]
        public IEnumerator ControllerSignals_DriveGripTriggerAndTrackingLoss()
        {
            InputSystem.RegisterLayout<XRSimulatedController>();
            var leftDevice = InputSystem.AddDevice<XRSimulatedController>();
            InputSystem.SetDeviceUsage(leftDevice, UnityEngine.InputSystem.CommonUsages.LeftHand);
            System.Action queueTrigger = null;
            try
            {
                var state = new XRSimulatedControllerState { isTracked = true, trackingState = 3, deviceRotation = Quaternion.identity, devicePosition = new Vector3(0, 1.3f, 0) };
                InputSystem.QueueStateEvent(leftDevice, state); InputSystem.Update();
                Object.FindAnyObjectByType<ChemistryLabModeController>().ApplyPresentationMode(VLabPresentationMode.OpenXRHardware);
                yield return null;
                var direct = Object.FindObjectsByType<XRDirectInteractor>(FindObjectsInactive.Include)
                    .First(i => i.transform.parent != null && i.transform.parent.name == "Left Controller");
                Assert.That(direct.enabled, Is.True, "Input System simulated tracking must keep the hand active.");
                state = state.WithButton(ControllerButton.GripButton); state.grip = 1;
                InputSystem.QueueStateEvent(leftDevice, state); InputSystem.Update();
                Assert.That(direct.selectInput.ReadIsPerformed(), Is.True, "Grip must reach the actual scene interactor.");
                var bottle = Get(LabVesselRole.Bottle, LabReagent.NaOH);
                var grab = bottle.GetComponent<XRGrabInteractable>();
                int activationCount = 0;
                int inputEdges = 0;
                direct.activateInput.inputActionPerformed.performed += _ => inputEdges++;
                grab.activated.AddListener(_ => activationCount++);
                direct.interactionManager.SelectEnter((IXRSelectInteractor)direct, grab);
                yield return null;
                Assert.That(grab.isSelected, Is.True, "Grip must retain the selected bottle before Trigger.");
                state = state.WithButton(ControllerButton.TriggerButton); state.trigger = 1;
                // XR devices also update BeforeRender. Deliver the button edge in Dynamic,
                // where XRI processes activation, rather than letting a render-only update consume it.
                queueTrigger = () =>
                {
                    if (UnityEngine.InputSystem.LowLevel.InputState.currentUpdateType != UnityEngine.InputSystem.LowLevel.InputUpdateType.Dynamic) return;
                    InputSystem.QueueStateEvent(leftDevice, state);
                    InputSystem.onBeforeUpdate -= queueTrigger;
                };
                InputSystem.onBeforeUpdate += queueTrigger;
                yield return new WaitForSeconds(.1f);
                Assert.That(direct.activateInput.ReadIsPerformed(), Is.True);
#pragma warning disable CS0618
                Assert.That(bottle.IsOpen, Is.True, $"Trigger must open the lid: events={activationCount}, edges={inputEdges}, logical={direct.logicalActivateState.active}, legacy={direct.forceDeprecatedInput}, enabled={direct.isActiveAndEnabled}, selected={direct.IsSelecting(grab)}, managerActive={direct.interactionManager.isActiveAndEnabled}, allowActivate={direct.allowActivate}.");
#pragma warning restore CS0618
                state.isTracked = false;
                InputSystem.QueueStateEvent(leftDevice, state); InputSystem.Update();
                yield return new WaitForSeconds(.1f);
                Assert.That(direct.enabled, Is.False, "Tracking loss must suspend the physical interactor.");
            }
            finally { if (queueTrigger != null) InputSystem.onBeforeUpdate -= queueTrigger; InputSystem.RemoveDevice(leftDevice); }
        }
        [UnityTest]
        public IEnumerator TiltedOpenBottle_PoursThroughMouthAndUprightStopsFlow()
        {
            lesson.PerformSafety();
            var bottle = Get(LabVesselRole.Bottle, LabReagent.NaOH); Open(bottle);
            var burette = Get(LabVesselRole.Burette);
            // This test isolates pour geometry; do not let Rigidbody interpolation rewind
            // its deliberately teleported, kinematic fixture during the next render frame.
            bottle.GetComponent<Rigidbody>().interpolation = RigidbodyInterpolation.None;
            bottle.transform.rotation = Quaternion.Euler(0, 0, 110);
            bottle.transform.position += burette.Mouth.position + Vector3.up * .08f - bottle.Mouth.position;
            Physics.SyncTransforms();
            yield return new WaitForSeconds(.15f);
            Assert.That(burette.Liquid.VolumeMl, Is.GreaterThan(0));
            Assert.That(bottle.Liquid.VolumeMl + burette.Liquid.VolumeMl, Is.EqualTo(200).Within(.001));
            bottle.transform.rotation = Quaternion.identity;
            double before = bottle.Liquid.VolumeMl;
            yield return new WaitForSeconds(.1f);
            Assert.That(bottle.Liquid.VolumeMl, Is.EqualTo(before));
        }
        [UnityTest]
        public IEnumerator SpillInvalidatesRecordingAndResetRestoresApparatus()
        {
            Prepare();
            var burette = Get(LabVesselRole.Burette);
            station.Spill(burette, 1);
            Assert.That(station.RequiresReset, Is.True);
            lesson.RecordResult(); Assert.That(lesson.LastActionAccepted, Is.False);
            lesson.ResetTrial();
            Assert.That(station.RequiresReset, Is.False);
            Assert.That(burette.Liquid.VolumeMl, Is.Zero);
            Assert.That(Get(LabVesselRole.Bottle, LabReagent.NaOH).Liquid.VolumeMl, Is.EqualTo(200));
            yield return null;
        }
    }
}
