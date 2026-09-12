using System;
using NUnit.Framework;
using VLAB.ChemistryLab.Interaction;

namespace VLAB.ChemistryLab.Tests
{
    public class LabLiquidStateTests
    {
        [Test]
        public void Transfer_ConservesVolumeAndStopsAtCapacity()
        {
            var source = new LabLiquidState(250, LabReagent.NaOH, 200);
            var target = new LabLiquidState(50);
            Assert.That(source.TransferTo(target, 70), Is.EqualTo(50));
            Assert.That(source.VolumeMl + target.VolumeMl, Is.EqualTo(200));
            Assert.That(source.TransferTo(target, 1), Is.Zero);
            Assert.That(target.Reagent, Is.EqualTo(LabReagent.NaOH));
        }
        [TestCase(double.NaN)] [TestCase(double.PositiveInfinity)] [TestCase(-1)] [TestCase(0)]
        public void InvalidTransfer_DoesNotMutate(double requested)
        {
            var source = new LabLiquidState(10, LabReagent.DilutedVinegar, 10);
            var target = new LabLiquidState(10);
            Assert.That(source.TransferTo(target, requested), Is.Zero);
            Assert.That(source.VolumeMl, Is.EqualTo(10));
            Assert.That(target.VolumeMl, Is.Zero);
        }
        [Test]
        public void MixedLiquid_IsTrackedAndEmptyVesselLosesIdentity()
        {
            var a = new LabLiquidState(10, LabReagent.DilutedVinegar, 5);
            var b = new LabLiquidState(10, LabReagent.Indicator, 1);
            b.TransferTo(a, 1);
            Assert.That(a.Reagent, Is.EqualTo(LabReagent.Mixture));
            Assert.That(b.Reagent, Is.EqualTo(LabReagent.None));
            Assert.That(a.TransferTo(a, 1), Is.Zero);
        }
        [Test]
        public void RepeatedFractionalTransfers_ConserveVolume()
        {
            var source = new LabLiquidState(50, LabReagent.NaOH, 50);
            var target = new LabLiquidState(100);
            for (int i = 0; i < 5000; i++) source.TransferTo(target, .01);
            Assert.That(target.VolumeMl, Is.EqualTo(50).Within(1e-7));
            Assert.That(source.VolumeMl, Is.Zero);
        }
        [Test]
        public void InvalidStorage_IsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new LabLiquidState(double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => new LabLiquidState(1, LabReagent.NaOH, 2));
            Assert.Throws<ArgumentException>(() => new LabLiquidState(10, LabReagent.None, 5));
        }
    }
}
