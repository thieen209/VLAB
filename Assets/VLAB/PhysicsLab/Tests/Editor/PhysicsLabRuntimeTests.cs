using NUnit.Framework;
using UnityEngine;
using VLAB.PhysicsLab.AirTrack;
using VLAB.PhysicsLab.Common;
using VLAB.PhysicsLab.Measurement;
using VLAB.PhysicsLab.Mechanics;
using VLAB.PhysicsLab.Oscillation;
using VLAB.PhysicsLab.Projectile;

namespace VLAB.PhysicsLab.Tests
{
    public sealed class PhysicsLabRuntimeTests
    {
        [Test]
        public void PhysicsParameter_ClampsAndPublishesSIValue()
        {
            var parameter = ScriptableObject.CreateInstance<PhysicsParameter>();
            parameter.Configure("mass", "kg", 0.1f, 2f, 0.5f);

            parameter.SetValue(5f);

            Assert.That(parameter.Value, Is.EqualTo(2f));
            Assert.That(parameter.Unit, Is.EqualTo("kg"));
            Object.DestroyImmediate(parameter);
        }

        [Test]
        public void AttachmentPoint_AttachesAndDetachesCompatibleObject()
        {
            var pointObject = new GameObject("Point");
            var payload = new GameObject("Payload");
            var point = pointObject.AddComponent<LabAttachmentPoint>();
            point.Configure("mass", true, true);
            var attachment = payload.AddComponent<LabAttachment>();
            attachment.Configure("mass");

            Assert.That(point.TryAttach(attachment), Is.True);
            Assert.That(payload.transform.parent, Is.EqualTo(point.transform));
            Assert.That(point.Detach(), Is.EqualTo(attachment));

            Object.DestroyImmediate(pointObject);
            Object.DestroyImmediate(payload);
        }

        [Test]
        public void ResetManager_RestoresRegisteredObjectTransform()
        {
            var managerObject = new GameObject("ResetManager");
            var target = new GameObject("Target");
            target.transform.position = new Vector3(1f, 2f, 3f);
            var resettable = target.AddComponent<ExperimentObject>();
            resettable.CaptureResetState();
            var manager = managerObject.AddComponent<ExperimentResetManager>();
            manager.Register(resettable);
            target.transform.position = Vector3.one * 10f;

            manager.ResetAll();

            Assert.That(target.transform.position, Is.EqualTo(new Vector3(1f, 2f, 3f)));
            Object.DestroyImmediate(managerObject);
            Object.DestroyImmediate(target);
        }

        [Test]
        public void Timer_ModeAPlusB_MeasuresBetweenTwoPorts()
        {
            var timerObject = new GameObject("Timer");
            var timer = timerObject.AddComponent<DigitalTimerMC964>();
            timer.SetMode(TimerMode.APlusB);
            timer.SetResolution(TimerResolution.Milliseconds);

            timer.ProcessGateEvent(TimerPort.A, true, 10.0);
            timer.ProcessGateEvent(TimerPort.B, true, 10.1236);

            Assert.That(timer.MeasuredTime, Is.EqualTo(0.124).Within(0.0001));
            Assert.That(timer.DisplayText, Is.EqualTo("0.124"));
            Object.DestroyImmediate(timerObject);
        }

        [Test]
        public void Timer_ModeT_MeasuresPeriodBetweenSuccessiveBlocks()
        {
            var timerObject = new GameObject("Timer");
            var timer = timerObject.AddComponent<DigitalTimerMC964>();
            timer.SetMode(TimerMode.T);

            timer.ProcessGateEvent(TimerPort.A, true, 2.0);
            timer.ProcessGateEvent(TimerPort.A, true, 3.75);

            Assert.That(timer.MeasuredTime, Is.EqualTo(1.75).Within(0.001));
            Object.DestroyImmediate(timerObject);
        }

        [Test]
        public void Photogate_RejectsItsOwnColliders()
        {
            var gateObject = new GameObject("Gate");
            var gate = gateObject.AddComponent<PhysicsPhotogate>();
            var child = new GameObject("SensorHousing");
            child.transform.SetParent(gateObject.transform);
            var ownCollider = child.AddComponent<BoxCollider>();
            var target = new GameObject("Flag");
            var targetCollider = target.AddComponent<BoxCollider>();

            Assert.That(gate.IsValidTarget(ownCollider), Is.False);
            Assert.That(gate.IsValidTarget(targetCollider), Is.True);
            Object.DestroyImmediate(gateObject);
            Object.DestroyImmediate(target);
        }

        [Test]
        public void PhotogateVelocity_UsesFlagWidthAndBeamBlockDuration()
        {
            var gateObject = new GameObject("Gate");
            var gate = gateObject.AddComponent<PhysicsPhotogate>();
            var meter = gateObject.AddComponent<PhotogateVelocityMeter>();
            meter.Configure(gate, 0.05f);

            meter.Process(new PhotogateEvent(gate, true, 1.0));
            meter.Process(new PhotogateEvent(gate, false, 1.025));

            Assert.That(meter.VelocityMetresPerSecond, Is.EqualTo(2f).Within(0.001f));
            Object.DestroyImmediate(gateObject);
        }

        [Test]
        public void SpringMath_ReturnsHookeAndDampingForce()
        {
            var force = SpringMath.CalculateScalarForce(0.1f, 0.2f, 20f, 0.5f);
            Assert.That(force, Is.EqualTo(-2.1f).Within(0.0001f));
        }

        [Test]
        public void CoilSpring_MeasuresPeriodBetweenSuccessiveExtensionPeaks()
        {
            var springObject = new GameObject("Spring");
            var spring = springObject.AddComponent<PhysicsCoilSpring>();

            spring.ProcessExtensionSample(0f, 0.0);
            spring.ProcessExtensionSample(0.1f, 0.25);
            spring.ProcessExtensionSample(0f, 0.50);
            spring.ProcessExtensionSample(0.1f, 1.25);
            spring.ProcessExtensionSample(0f, 1.50);

            Assert.That(spring.MeasuredPeriod, Is.EqualTo(1.0).Within(0.0001));
            Object.DestroyImmediate(springObject);
        }

        [Test]
        public void PendulumMath_ReturnsExpectedSmallAnglePeriod()
        {
            Assert.That(PendulumMath.TheoreticalPeriod(1f), Is.EqualTo(2.006f).Within(0.005f));
        }

        [Test]
        public void FrictionMath_UsesStaticThenKineticLimit()
        {
            Assert.That(FrictionMath.CalculateMagnitude(10f, 3f, 0f, 0.4f, 0.25f), Is.EqualTo(3f));
            Assert.That(FrictionMath.CalculateMagnitude(10f, 8f, 1f, 0.4f, 0.25f), Is.EqualTo(2.5f));
        }

        [Test]
        public void ProjectileMath_ProducesRequestedSpeedAndAngle()
        {
            var velocity = ProjectileMath.CalculateVelocity(30f, 8f, Vector3.forward, Vector3.up);
            Assert.That(velocity.magnitude, Is.EqualTo(8f).Within(0.001f));
            Assert.That(Vector3.Angle(Vector3.ProjectOnPlane(velocity, Vector3.up), velocity), Is.EqualTo(30f).Within(0.01f));
        }

        [Test]
        public void MeterRuler_ProjectsDistanceOnMeasurementAxis()
        {
            var rulerObject = new GameObject("Ruler");
            var ruler = rulerObject.AddComponent<LaboratoryMeterRuler>();
            ruler.Configure(rulerObject.transform, Vector3.right);

            Assert.That(ruler.MeasureWorldPoint(new Vector3(0.42f, 2f, 0f)), Is.EqualTo(0.42f).Within(0.0001f));
            Object.DestroyImmediate(rulerObject);
        }

        [Test]
        public void MomentumMath_SumsSignedMomentumAlongTrack()
        {
            var momentum = MomentumMath.TotalMomentum(0.25f, 1.2f, 0.40f, -0.3f);
            Assert.That(momentum, Is.EqualTo(0.18f).Within(0.0001f));
        }
    }
}
