using System;
using NUnit.Framework;

namespace VLAB.ChemistryLab.Tests
{
    public class ChemistryCalculationTests
    {
        private const double Tolerance = 0.0001d;

        [Test]
        public void CalculateOriginalAcidConcentration_AppliesTenFoldVinegarDilution()
        {
            double concentration = ChemistryCalculation.CalculateOriginalAcidConcentration(
                naohConcentrationMolar: 0.1000d,
                naohVolumeMl: 8.33d,
                dilutedVinegarAliquotMl: 10.00d,
                dilutionFactor: 10d);

            Assert.That(concentration, Is.EqualTo(0.833d).Within(Tolerance));
        }

        [Test]
        public void CalculateMassVolumePercent_ConvertsMolarityUsingAceticAcidMolarMass()
        {
            double percent = ChemistryCalculation.CalculateMassVolumePercent(
                acidConcentrationMolar: 0.833d);

            Assert.That(percent, Is.EqualTo(5.0023316d).Within(Tolerance));
        }

        [Test]
        public void CalculateOriginalAcidConcentration_RejectsZeroAliquotVolume()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ChemistryCalculation.CalculateOriginalAcidConcentration(
                    naohConcentrationMolar: 0.1000d,
                    naohVolumeMl: 8.33d,
                    dilutedVinegarAliquotMl: 0d,
                    dilutionFactor: 10d));
        }

        [TestCase(8.24d, TitrationEndpointState.BeforeEndpoint)]
        [TestCase(8.28d, TitrationEndpointState.Endpoint)]
        [TestCase(8.38d, TitrationEndpointState.Overshot)]
        public void ClassifyEndpoint_UsesEndpointWindowAndDetectsOvershoot(
            double deliveredVolumeMl,
            TitrationEndpointState expected)
        {
            TitrationEndpointState state = ChemistryCalculation.ClassifyEndpoint(
                deliveredVolumeMl,
                expectedEndpointVolumeMl: 8.33d,
                endpointToleranceMl: 0.05d);

            Assert.That(state, Is.EqualTo(expected));
        }

        [Test]
        public void AreConcordant_AcceptsThreeTrialsWithinPointOneMillilitreSpread()
        {
            bool concordant = ChemistryCalculation.AreConcordant(
                firstTitreMl: 8.29d,
                secondTitreMl: 8.34d,
                thirdTitreMl: 8.39d,
                maximumSpreadMl: 0.10d);

            Assert.That(concordant, Is.True);
        }

        [Test]
        public void AreConcordant_RejectsTrialsOutsidePointOneMillilitreSpread()
        {
            bool concordant = ChemistryCalculation.AreConcordant(
                firstTitreMl: 8.20d,
                secondTitreMl: 8.33d,
                thirdTitreMl: 8.41d,
                maximumSpreadMl: 0.10d);

            Assert.That(concordant, Is.False);
        }

        [Test]
        public void CalculateMeanTitre_ReturnsMeanForConcordantTrials()
        {
            double mean = ChemistryCalculation.CalculateMeanTitre(
                firstTitreMl: 8.29d,
                secondTitreMl: 8.34d,
                thirdTitreMl: 8.39d,
                maximumSpreadMl: 0.10d);

            Assert.That(mean, Is.EqualTo(8.34d).Within(Tolerance));
        }

        [Test]
        public void CalculateMeanTitre_RejectsDiscordantTrials()
        {
            Assert.Throws<InvalidOperationException>(() =>
                ChemistryCalculation.CalculateMeanTitre(
                    firstTitreMl: 8.20d,
                    secondTitreMl: 8.33d,
                    thirdTitreMl: 8.41d,
                    maximumSpreadMl: 0.10d));
        }
    }
}
