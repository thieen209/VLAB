using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using VLAB.ChemistryLab.Interaction;
using VLAB.ChemistryLab.Mode;

namespace VLAB.ChemistryLab.Tests.PlayMode
{
    public class LabTeleportTests
    {
        [UnityTest]
        public IEnumerator TeleportRay_SelectAndReleaseOnFloor_TeleportsThroughInteractable()
        {
            yield return SceneManager.LoadSceneAsync("ChemistryLab"); yield return null;
            var mode = Object.FindAnyObjectByType<ChemistryLabModeController>();
            mode.ApplyPresentationMode(VLabPresentationMode.XRSimulator);
            yield return null;
            var area = Object.FindAnyObjectByType<LabTeleportArea>();
            var rayObject = new GameObject("Teleport test ray");
            try
            {
                rayObject.transform.SetPositionAndRotation(new Vector3(0, 1, -2), Quaternion.LookRotation(Vector3.down));
                var ray = rayObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor>();
                ray.interactionManager = area.interactionManager;
                ray.interactionLayers = area.interactionLayers;
                ray.enableUIInteraction = false;
                Physics.SyncTransforms();
                yield return null; yield return null;
                Assert.That(ray.TryGetCurrent3DRaycastHit(out var hit), Is.True);
                Assert.That(hit.collider.gameObject, Is.EqualTo(area.gameObject));
                Assert.That(area.IsSelectableBy((UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor)ray), Is.True);
                ray.StartManualInteraction((UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable)area);
                Assert.That(ray.hasSelection, Is.True);
                yield return null;
                ray.EndManualInteraction();
                yield return new WaitForSeconds(.3f);
                Assert.That(Camera.main.transform.position.z, Is.EqualTo(-2).Within(.05f));
            }
            finally
            {
                Object.Destroy(rayObject);
                mode.ApplyPresentationMode(VLabPresentationMode.Desktop);
            }
        }

        [UnityTest]
        public IEnumerator FloorOnlyTeleport_RejectsFurnitureAndMovesRigWithoutChangingLesson()
        {
            yield return SceneManager.LoadSceneAsync("ChemistryLab"); yield return null;
            var mode = Object.FindAnyObjectByType<ChemistryLabModeController>();
            var area = Object.FindAnyObjectByType<LabTeleportArea>(FindObjectsInactive.Include);
            Assert.That(area, Is.Not.Null);
            Assert.That(area.enabled, Is.False, "Desktop must not activate teleport selection.");
            mode.ApplyPresentationMode(VLabPresentationMode.XRSimulator);
            yield return null;
            Physics.SyncTransforms();
            var rays = Object.FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int teleportRays = 0;
            foreach (var ray in rays)
            {
                if (!ray.name.Contains("Teleport")) continue;
                teleportRays++;
                Assert.That(ray.interactionLayers.value & area.interactionLayers.value, Is.Not.Zero, "Teleport ray must share the floor interaction layer.");
            }
            Assert.That(teleportRays, Is.GreaterThanOrEqualTo(2));
            Assert.That(area.CanStandAt(new Vector3(0, 0, -2)), Is.True, "Clear main aisle.");
            Assert.That(area.CanStandAt(new Vector3(0, .95f, .5f)), Is.False, "Tabletop.");
            Assert.That(area.CanStandAt(new Vector3(0, 0, .5f)), Is.False, "Under the main bench.");
            Assert.That(area.CanStandAt(new Vector3(8, 0, 0)), Is.False, "Outside floor safety margin.");
            Assert.That(area.CanStandAt(new Vector3(float.NaN, 0, 0)), Is.False);
            var lesson = Object.FindAnyObjectByType<TitrationLessonController>();
            var step = lesson.Experiment.CurrentStep;
            var snap = Object.FindAnyObjectByType<SnapTurnProvider>();
            Assert.That(snap.turnAmount, Is.EqualTo(30));
            var request = new TeleportRequest { destinationPosition = new Vector3(0, 0, -2),
                destinationRotation = Quaternion.identity, matchOrientation = MatchOrientation.WorldSpaceUp };
            Assert.That(area.teleportationProvider.QueueTeleportRequest(request), Is.True);
            yield return new WaitForSeconds(.3f);
            Assert.That(Camera.main.transform.position.z, Is.EqualTo(-2).Within(.05f));
            float yawBefore = Camera.main.transform.eulerAngles.y;
            snap.leftHandTurnInput.inputSourceMode = UnityEngine.XR.Interaction.Toolkit.Inputs.Readers.XRInputValueReader.InputSourceMode.ManualValue;
            snap.rightHandTurnInput.inputSourceMode = UnityEngine.XR.Interaction.Toolkit.Inputs.Readers.XRInputValueReader.InputSourceMode.ManualValue;
            snap.leftHandTurnInput.manualValue = Vector2.zero;
            snap.rightHandTurnInput.manualValue = Vector2.right;
            yield return null;
            yield return null;
            snap.rightHandTurnInput.manualValue = Vector2.zero;
            yield return null;
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(yawBefore, Camera.main.transform.eulerAngles.y)), Is.EqualTo(30).Within(.5f), "Snap input must rotate the rig by 30 degrees.");
            Assert.That(lesson.Experiment.CurrentStep, Is.EqualTo(step));
            mode.ApplyPresentationMode(VLabPresentationMode.Desktop);
            Assert.That(area.enabled, Is.False);
        }
    }
}
