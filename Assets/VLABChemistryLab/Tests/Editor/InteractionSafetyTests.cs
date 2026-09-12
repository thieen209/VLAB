using NUnit.Framework;
using UnityEngine;
using VLAB.ChemistryLab.Interaction;

namespace VLAB.ChemistryLab.Tests
{
    public sealed class InteractionSafetyTests
    {
        [Test]
        public void OnePressCannotDispatchTwiceOrWhileDisabled()
        {
            var gate = new LabPressGate();
            Assert.That(gate.TryPress(1, true), Is.True);
            Assert.That(gate.TryPress(1, true), Is.False);
            Assert.That(gate.TryPress(1.1, true), Is.False);
            Assert.That(gate.TryPress(1.3, false), Is.False);
            Assert.That(gate.TryPress(1.3, true), Is.True);
            Assert.That(gate.TryPress(.5, true), Is.False);
            Assert.That(gate.TryPress(double.NaN, true), Is.False);
        }

        [Test]
        public void RecoveryNeverMovesHeldObjectsAndDetectsFloorAndRoomLoss()
        {
            Assert.That(LabInteractionSafety.ShouldRecover(new Vector3(20, -2, 0), true), Is.False);
            Assert.That(LabInteractionSafety.ShouldRecover(new Vector3(0, 1, 0), false), Is.False);
            Assert.That(LabInteractionSafety.ShouldRecover(new Vector3(8, 1, 0), false), Is.True);
            Assert.That(LabInteractionSafety.ShouldRecover(new Vector3(0, -.5f, 0), false), Is.True);
            Assert.That(LabInteractionSafety.ShouldRecover(new Vector3(0, 1, -8), false), Is.True);
        }

        [Test]
        public void UnsafeThrowsAreLimitedWithoutChangingDirection()
        {
            var velocity = LabInteractionSafety.ClampThrow(new Vector3(30, 40, 0), 3);
            Assert.That(velocity.magnitude, Is.EqualTo(3).Within(.0001f));
            Assert.That(velocity.normalized, Is.EqualTo(new Vector3(.6f, .8f, 0)));
            Assert.That(LabInteractionSafety.ClampThrow(new Vector3(float.NaN, 0, 0), 3), Is.EqualTo(Vector3.zero));
        }
    }
}
