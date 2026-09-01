using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using VLAB.PhysicsLab.Education;
using VLAB.PhysicsLab.Interaction;
using VLAB.PhysicsLab.Common;

namespace VLAB.PhysicsLab.Tests
{
    public sealed class PhysicalLabArchitectureTests
    {
        [Test]
        public void PhysicalProgress_AdvancesOnlyFromSourcedApparatusEvents()
        {
            var definition = PhysicalExperimentDefinitionCatalog.Get("PHY_01");
            var progress = new PhysicalExperimentProgress(definition);
            var bob = new GameObject("Pendulum_Bob");

            Assert.That(progress.Observe(new PhysicalLabEvent(null, "Pendulum_Bob", PhysicalActionKind.Grabbed)), Is.False);
            Assert.That(progress.StepIndex, Is.Zero);
            Assert.That(progress.Observe(new PhysicalLabEvent(bob.transform, "Pendulum_Bob", PhysicalActionKind.Grabbed)), Is.True);
            Assert.That(progress.StepIndex, Is.EqualTo(1));

            Object.DestroyImmediate(bob);
        }

        [Test]
        public void TrialRecorder_RejectsPresetDataAndAcceptsLiveComponentMeasurements()
        {
            var recorder = new PhysicalTrialRecorder(3);
            var sourceObject = new GameObject("LiveSensor");
            var source = sourceObject.AddComponent<TestLiveSource>();

            Assert.That(recorder.TryRecord(new LiveMeasurementSample(null, "T", 1.8d, "s", "L=0.8")), Is.False);
            Assert.That(recorder.TryRecord(new LiveMeasurementSample(source, "T", 1.8d, "s", "L=0.8")), Is.True);
            Assert.That(recorder.TrialCount, Is.EqualTo(1));

            Object.DestroyImmediate(sourceObject);
        }

        [Test]
        public void ResultSheet_RemainsLockedUntilPhysicalSequenceAndThreeLiveMeasurements()
        {
            var station = new GameObject("PendulumStation");
            var source = station.AddComponent<TestLiveSource>();
            var controller = station.AddComponent<ExperimentPhysicalController>();
            controller.Configure("PHY_01", null, null);

            Assert.That(controller.RecordLiveMeasurement(source, "Pendulum_Bob", "Chu kỳ", 2d, "s", "L=1m;m=0.2kg"), Is.False);
            controller.PublishPhysicalAction(source, "Pendulum_Bob", PhysicalActionKind.Grabbed);
            controller.PublishPhysicalAction(source, "Pendulum_Bob", PhysicalActionKind.Released);
            Assert.That(controller.RecordLiveMeasurement(source, "Pendulum_Bob", "Chu kỳ", 2.01d, "s", "L=1m;m=0.2kg"), Is.True);
            Assert.That(controller.RecordLiveMeasurement(source, "Pendulum_Bob", "Chu kỳ", 2.02d, "s", "L=1m;m=0.2kg"), Is.True);
            Assert.That(controller.HasEnoughTrials, Is.False);
            Assert.That(controller.RecordLiveMeasurement(source, "Pendulum_Bob", "Chu kỳ", 2.03d, "s", "L=1m;m=0.2kg"), Is.True);
            Assert.That(controller.HasEnoughTrials, Is.True);
            Assert.That(controller.ResultSheetText, Does.Contain("Trung bình"));
            Assert.That(controller.ResultSheetText, Does.Contain("Giá trị lý thuyết"));
            Object.DestroyImmediate(station);
        }

        [Test]
        public void LearningController_HasNoUiMethodThatPerformsPhysicalTrial()
        {
            Assert.That(typeof(ExperimentPhysicalController).GetMethod("PrimaryAction", BindingFlags.Instance | BindingFlags.Public), Is.Null);
            Assert.That(typeof(ExperimentPhysicalController).GetMethod("IncreaseParameter", BindingFlags.Instance | BindingFlags.Public), Is.Null);
            Assert.That(typeof(ExperimentPhysicalController).GetMethod("PerformTrial", BindingFlags.Instance | BindingFlags.Public), Is.Null);
        }

        [Test]
        public void Grabbable_CanReleaseAsDynamicForPhysicalExperimentMotion()
        {
            var target = new GameObject("PhysicalObject");
            var body = target.AddComponent<Rigidbody>();
            target.AddComponent<Common.LabInteractable>();
            var grabbable = target.AddComponent<LabGrabbable>();
            grabbable.Configure(true, true, LabReleaseMode.Dynamic);

            grabbable.OnGrabbed(target.transform);
            grabbable.OnReleased();

            Assert.That(body.isKinematic, Is.False);
            Assert.That(body.useGravity, Is.True);
            Object.DestroyImmediate(target);
        }

        [Test]
        public void Grabbable_ReportsHeldStateSoSafetyRecoveryCannotInterruptAHand()
        {
            var target = new GameObject("HeldPhysicalObject");
            target.AddComponent<Rigidbody>();
            target.AddComponent<Common.LabInteractable>();
            var grabbable = target.AddComponent<LabGrabbable>();

            grabbable.OnGrabbed(target.transform);
            Assert.That(grabbable.IsHeld, Is.True);

            grabbable.OnReleased();
            Assert.That(grabbable.IsHeld, Is.False);
            Object.DestroyImmediate(target);
        }

        [Test]
        public void SnapController_AttachesOnlyCompatibleReleasedApparatus()
        {
            var station = new GameObject("Station");
            var targetObject = new GameObject("MassTarget");
            targetObject.transform.SetParent(station.transform);
            var point = targetObject.AddComponent<LabAttachmentPoint>();
            point.Configure("mass", true, true);

            var payload = new GameObject("Mass");
            payload.transform.SetParent(station.transform);
            payload.transform.position = targetObject.transform.position + Vector3.right * 0.05f;
            payload.AddComponent<Rigidbody>();
            payload.AddComponent<Common.LabInteractable>();
            payload.AddComponent<LabAttachment>().Configure("mass");
            var grabbable = payload.AddComponent<LabGrabbable>();
            var snap = station.AddComponent<LabSnapController>();
            snap.Refresh();

            Assert.That(snap.TrySnap(grabbable), Is.True);
            Assert.That(point.Current, Is.EqualTo(payload.GetComponent<LabAttachment>()));

            Object.DestroyImmediate(station);
        }

        [Test]
        public void ResetManager_DetachesSnappedObjectAndRestoresItsOriginalParent()
        {
            var station = new GameObject("Station");
            var pointObject = new GameObject("MassMount");
            pointObject.transform.SetParent(station.transform);
            var point = pointObject.AddComponent<LabAttachmentPoint>();
            point.Configure("mass", true, true);

            var payload = new GameObject("Mass");
            payload.transform.SetParent(station.transform);
            payload.AddComponent<Rigidbody>();
            var resettable = payload.AddComponent<ExperimentObject>();
            var attachment = payload.AddComponent<LabAttachment>();
            attachment.Configure("mass");
            var manager = station.AddComponent<ExperimentResetManager>();
            manager.Register(resettable);
            manager.CaptureAll();

            Assert.That(point.TryAttach(attachment), Is.True);
            Assert.That(payload.transform.parent, Is.EqualTo(point.transform));

            manager.ResetAll();
            Assert.That(point.Current, Is.Null);
            Assert.That(payload.transform.parent, Is.EqualTo(station.transform));
            Object.DestroyImmediate(station);
        }

        private sealed class TestLiveSource : MonoBehaviour { }
    }
}
