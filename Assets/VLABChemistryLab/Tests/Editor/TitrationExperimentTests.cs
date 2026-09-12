using NUnit.Framework;

namespace VLAB.ChemistryLab.Tests
{
    public class TitrationExperimentTests
    {
        [Test]
        public void NewExperiment_StartsByRequiringSafetyEquipment()
        {
            var experiment = new TitrationExperiment();

            Assert.That(experiment.CurrentStep, Is.EqualTo(TitrationStep.WearSafetyEquipment));
        }

        [Test]
        public void TryCompleteStep_AdvancesWhenRequiredStepIsPerformed()
        {
            var experiment = new TitrationExperiment();

            bool accepted = experiment.TryCompleteStep(TitrationStep.WearSafetyEquipment);

            Assert.That(accepted, Is.True);
            Assert.That(experiment.CurrentStep, Is.EqualTo(TitrationStep.RinseAndFillBurette));
        }

        [Test]
        public void TryCompleteStep_RejectsOutOfOrderActionWithoutAdvancing()
        {
            var experiment = new TitrationExperiment();

            bool accepted = experiment.TryCompleteStep(TitrationStep.AddIndicator);

            Assert.That(accepted, Is.False);
            Assert.That(experiment.CurrentStep, Is.EqualTo(TitrationStep.WearSafetyEquipment));
        }

        [Test]
        public void RequiredProcedure_CompletesOnlyInApprovedOrder()
        {
            var experiment = new TitrationExperiment();

            CompleteSafetyAndRecordTrial(experiment, 8.31d);
            CompleteTrial(experiment, 8.34d);
            CompleteTrial(experiment, 8.37d);

            Assert.That(experiment.IsComplete, Is.True);
            Assert.That(experiment.Observations.Count, Is.EqualTo(3));
        }

        [Test]
        public void RecordTitre_AcceptsEndpointResult()
        {
            var experiment = CreateExperimentReadyToTitrate();

            bool accepted = experiment.RecordTitre(
                deliveredVolumeMl: 8.33d,
                expectedEndpointVolumeMl: 8.33d,
                endpointToleranceMl: 0.05d);

            Assert.That(accepted, Is.True);
            Assert.That(experiment.EndpointState, Is.EqualTo(TitrationEndpointState.Endpoint));
            Assert.That(experiment.CurrentStep, Is.EqualTo(TitrationStep.RecordResult));
        }

        [Test]
        public void RecordTitre_RejectsOvershootAndKeepsExperimentAtTitrationStep()
        {
            var experiment = CreateExperimentReadyToTitrate();

            bool accepted = experiment.RecordTitre(
                deliveredVolumeMl: 8.50d,
                expectedEndpointVolumeMl: 8.33d,
                endpointToleranceMl: 0.05d);

            Assert.That(accepted, Is.False);
            Assert.That(experiment.EndpointState, Is.EqualTo(TitrationEndpointState.Overshot));
            Assert.That(experiment.CurrentStep, Is.EqualTo(TitrationStep.TitrateToEndpoint));
        }

        [Test]
        public void AddTitrant_AccumulatesDoseAndUpdatesEndpointState()
        {
            var experiment = CreateExperimentReadyToTitrate();

            Assert.That(experiment.AddTitrant(8.28d), Is.True);
            Assert.That(experiment.DeliveredVolumeMl, Is.EqualTo(8.28d).Within(0.000001d));
            Assert.That(experiment.EndpointState, Is.EqualTo(TitrationEndpointState.Endpoint));
        }

        [Test]
        public void RecordCurrentTitre_AcceptsEndpointAndStartsNextTrial()
        {
            var experiment = CreateExperimentReadyToTitrate();
            experiment.AddTitrant(8.32d);

            Assert.That(experiment.RecordCurrentTitre(), Is.True);
            Assert.That(experiment.Observations.Count, Is.EqualTo(1));
            Assert.That(experiment.CurrentStep, Is.EqualTo(TitrationStep.RinseAndFillBurette));
            Assert.That(experiment.DeliveredVolumeMl, Is.Zero);
        }

        [Test]
        public void DesktopDosePlan_ReachesTheTargetWithCoarseMediumAndFineControls()
        {
            var experiment = CreateExperimentReadyToTitrate();

            for (int i = 0; i < 8; i++) Assert.That(experiment.AddTitrant(1.00d), Is.True);
            for (int i = 0; i < 3; i++) Assert.That(experiment.AddTitrant(0.10d), Is.True);
            for (int i = 0; i < 3; i++) Assert.That(experiment.AddTitrant(0.01d), Is.True);

            Assert.That(experiment.DeliveredVolumeMl, Is.EqualTo(8.33d).Within(0.000001d));
            Assert.That(experiment.EndpointState, Is.EqualTo(TitrationEndpointState.Endpoint));
            Assert.That(experiment.RecordCurrentTitre(), Is.True);
        }

        [Test]
        public void ThreeDiscordantTitres_DoNotCompleteUntilLatestThreeAreConcordant()
        {
            var experiment = new TitrationExperiment();

            CompleteSafetyAndRecordTrial(experiment, 8.28d);
            CompleteTrial(experiment, 8.33d);
            CompleteTrial(experiment, 8.37d);
            Assert.That(experiment.IsComplete, Is.True);

            experiment.ClearResults();
            CompleteSafetyAndRecordTrial(experiment, 8.28d);
            CompleteTrial(experiment, 8.37d);
            CompleteTrial(experiment, 8.33d);
            Assert.That(experiment.IsComplete, Is.True);
        }

        [Test]
        public void DiscordantAcceptedResults_KeepLessonOpenForAnotherTrial()
        {
            var experiment = new TitrationExperiment(expectedEndpointVolumeMl: 8.33d, endpointToleranceMl: 0.20d);

            CompleteSafetyAndRecordTrial(experiment, 8.20d);
            CompleteTrial(experiment, 8.33d);
            CompleteTrial(experiment, 8.45d);

            Assert.That(experiment.IsComplete, Is.False);
            Assert.That(experiment.CurrentStep, Is.EqualTo(TitrationStep.RinseAndFillBurette));
            Assert.That(experiment.RubricMessage, Does.Contain("0.10"));
        }

        [Test]
        public void CompletedExperiment_ExposesCalculatedVinegarResults()
        {
            var experiment = new TitrationExperiment();

            CompleteSafetyAndRecordTrial(experiment, 8.31d);
            CompleteTrial(experiment, 8.34d);
            CompleteTrial(experiment, 8.37d);

            Assert.That(experiment.MeanTitreMl, Is.EqualTo(8.34d).Within(0.000001d));
            Assert.That(experiment.AcidConcentrationMolar, Is.EqualTo(0.834d).Within(0.000001d));
            Assert.That(experiment.MassVolumePercent, Is.EqualTo(5.0083368d).Within(0.000001d));
        }

        [Test]
        public void ResetTrial_PreservesObservationsButReturnsToSetup()
        {
            var experiment = new TitrationExperiment();
            CompleteSafetyAndRecordTrial(experiment, 8.32d);
            Assert.That(experiment.TryCompleteStep(TitrationStep.RinseAndFillBurette), Is.True);
            Assert.That(experiment.TryCompleteStep(TitrationStep.PipetteDilutedVinegar), Is.True);
            Assert.That(experiment.TryCompleteStep(TitrationStep.AddIndicator), Is.True);
            experiment.AddTitrant(3d);

            experiment.ResetTrial();

            Assert.That(experiment.Observations.Count, Is.EqualTo(1));
            Assert.That(experiment.CurrentStep, Is.EqualTo(TitrationStep.RinseAndFillBurette));
            Assert.That(experiment.DeliveredVolumeMl, Is.Zero);
        }

        [Test]
        public void ClearResults_ReturnsToSafetyAndRemovesAllObservations()
        {
            var experiment = new TitrationExperiment();
            CompleteSafetyAndRecordTrial(experiment, 8.32d);

            experiment.ClearResults();

            Assert.That(experiment.Observations, Is.Empty);
            Assert.That(experiment.CurrentStep, Is.EqualTo(TitrationStep.WearSafetyEquipment));
        }

        private static TitrationExperiment CreateExperimentReadyToTitrate()
        {
            var experiment = new TitrationExperiment();
            Assert.That(experiment.TryCompleteStep(TitrationStep.WearSafetyEquipment), Is.True);
            Assert.That(experiment.TryCompleteStep(TitrationStep.RinseAndFillBurette), Is.True);
            Assert.That(experiment.TryCompleteStep(TitrationStep.PipetteDilutedVinegar), Is.True);
            Assert.That(experiment.TryCompleteStep(TitrationStep.AddIndicator), Is.True);
            return experiment;
        }

        private static void CompleteSafetyAndRecordTrial(TitrationExperiment experiment, double titreMl)
        {
            Assert.That(experiment.TryCompleteStep(TitrationStep.WearSafetyEquipment), Is.True);
            CompleteTrial(experiment, titreMl);
        }

        private static void CompleteTrial(TitrationExperiment experiment, double titreMl)
        {
            Assert.That(experiment.TryCompleteStep(TitrationStep.RinseAndFillBurette), Is.True);
            Assert.That(experiment.TryCompleteStep(TitrationStep.PipetteDilutedVinegar), Is.True);
            Assert.That(experiment.TryCompleteStep(TitrationStep.AddIndicator), Is.True);
            Assert.That(experiment.AddTitrant(titreMl), Is.True);
            Assert.That(experiment.RecordCurrentTitre(), Is.True);
        }
    }
}
