using System;
using System.Collections.Generic;

namespace VLAB.ChemistryLab
{
    public enum TitrationStep
    {
        WearSafetyEquipment, RinseAndFillBurette, PipetteDilutedVinegar,
        AddIndicator, TitrateToEndpoint, RecordResult, Completed
    }

    public readonly struct TitrationObservation
    {
        public TitrationObservation(int trialNumber, double titreMl)
        {
            TrialNumber = trialNumber;
            TitreMl = titreMl;
        }
        public int TrialNumber { get; }
        public double TitreMl { get; }
    }

    /// <summary>Unity-independent lesson state and assessment for vinegar titration.</summary>
    public sealed class TitrationExperiment
    {
        private readonly List<TitrationObservation> observations = new List<TitrationObservation>();
        private readonly double expectedEndpointVolumeMl;
        private readonly double endpointToleranceMl;
        private readonly double maximumConcordanceSpreadMl;
        private double pendingTitreMl;

        public TitrationExperiment(double expectedEndpointVolumeMl = 8.33d,
            double endpointToleranceMl = 0.05d, double maximumConcordanceSpreadMl = 0.10d)
        {
            if (expectedEndpointVolumeMl <= 0d) throw new ArgumentOutOfRangeException(nameof(expectedEndpointVolumeMl));
            if (endpointToleranceMl < 0d) throw new ArgumentOutOfRangeException(nameof(endpointToleranceMl));
            if (maximumConcordanceSpreadMl < 0d) throw new ArgumentOutOfRangeException(nameof(maximumConcordanceSpreadMl));
            this.expectedEndpointVolumeMl = expectedEndpointVolumeMl;
            this.endpointToleranceMl = endpointToleranceMl;
            this.maximumConcordanceSpreadMl = maximumConcordanceSpreadMl;
        }

        public TitrationStep CurrentStep { get; private set; } = TitrationStep.WearSafetyEquipment;
        public TitrationEndpointState EndpointState { get; private set; } = TitrationEndpointState.BeforeEndpoint;
        public IReadOnlyList<TitrationObservation> Observations => observations;
        public double DeliveredVolumeMl { get; private set; }
        public bool IsComplete => CurrentStep == TitrationStep.Completed;
        public bool HasConcordantResults => observations.Count >= 3 && LatestThreeAreConcordant();
        public double MeanTitreMl => HasConcordantResults ? LatestThreeMean() : 0d;
        public double AcidConcentrationMolar => HasConcordantResults
            ? ChemistryCalculation.CalculateOriginalAcidConcentration(0.1000d, MeanTitreMl, 10.00d, 10d) : 0d;
        public double MassVolumePercent => HasConcordantResults
            ? ChemistryCalculation.CalculateMassVolumePercent(AcidConcentrationMolar) : 0d;
        public string RubricMessage => IsComplete
            ? "Đạt: 3 kết quả đồng quy trong 0.10 mL."
            : observations.Count < 3
                ? $"Cần thêm {3 - observations.Count} kết quả hợp lệ."
                : "Chưa đạt: ba kết quả gần nhất phải chênh không quá 0.10 mL.";

        public bool TryCompleteStep(TitrationStep step)
        {
            if (IsComplete || step != CurrentStep || step == TitrationStep.TitrateToEndpoint) return false;
            if (step == TitrationStep.RecordResult)
            {
                AcceptPendingTitre();
                return true;
            }
            CurrentStep = GetNextPreparationStep(step);
            return true;
        }

        public bool AddTitrant(double volumeMl)
        {
            if (CurrentStep != TitrationStep.TitrateToEndpoint || volumeMl <= 0d ||
                double.IsNaN(volumeMl) || double.IsInfinity(volumeMl)) return false;
            DeliveredVolumeMl = Math.Round(DeliveredVolumeMl + volumeMl, 4, MidpointRounding.AwayFromZero);
            EndpointState = ChemistryCalculation.ClassifyEndpoint(
                DeliveredVolumeMl, expectedEndpointVolumeMl, endpointToleranceMl);
            return true;
        }

        public bool RecordCurrentTitre()
        {
            if (!RecordTitre(DeliveredVolumeMl, expectedEndpointVolumeMl, endpointToleranceMl)) return false;
            return TryCompleteStep(TitrationStep.RecordResult);
        }

        public bool RecordTitre(double deliveredVolumeMl, double expectedEndpointVolumeMl, double endpointToleranceMl)
        {
            if (CurrentStep != TitrationStep.TitrateToEndpoint) return false;
            EndpointState = ChemistryCalculation.ClassifyEndpoint(deliveredVolumeMl, expectedEndpointVolumeMl, endpointToleranceMl);
            if (EndpointState != TitrationEndpointState.Endpoint) return false;
            DeliveredVolumeMl = deliveredVolumeMl;
            pendingTitreMl = deliveredVolumeMl;
            CurrentStep = TitrationStep.RecordResult;
            return true;
        }

        public void ResetTrial()
        {
            DeliveredVolumeMl = 0d;
            pendingTitreMl = 0d;
            EndpointState = TitrationEndpointState.BeforeEndpoint;
            CurrentStep = CurrentStep == TitrationStep.WearSafetyEquipment
                ? TitrationStep.WearSafetyEquipment : TitrationStep.RinseAndFillBurette;
        }

        public void ClearResults()
        {
            observations.Clear();
            DeliveredVolumeMl = 0d;
            pendingTitreMl = 0d;
            EndpointState = TitrationEndpointState.BeforeEndpoint;
            CurrentStep = TitrationStep.WearSafetyEquipment;
        }

        private void AcceptPendingTitre()
        {
            observations.Add(new TitrationObservation(observations.Count + 1, pendingTitreMl));
            DeliveredVolumeMl = 0d;
            pendingTitreMl = 0d;
            EndpointState = TitrationEndpointState.BeforeEndpoint;
            CurrentStep = LatestThreeAreConcordant() ? TitrationStep.Completed : TitrationStep.RinseAndFillBurette;
        }

        private bool LatestThreeAreConcordant()
        {
            if (observations.Count < 3) return false;
            int last = observations.Count - 1;
            return ChemistryCalculation.AreConcordant(observations[last - 2].TitreMl,
                observations[last - 1].TitreMl, observations[last].TitreMl, maximumConcordanceSpreadMl);
        }

        private double LatestThreeMean()
        {
            int last = observations.Count - 1;
            return (observations[last - 2].TitreMl + observations[last - 1].TitreMl + observations[last].TitreMl) / 3d;
        }

        private static TitrationStep GetNextPreparationStep(TitrationStep step)
        {
            switch (step)
            {
                case TitrationStep.WearSafetyEquipment: return TitrationStep.RinseAndFillBurette;
                case TitrationStep.RinseAndFillBurette: return TitrationStep.PipetteDilutedVinegar;
                case TitrationStep.PipetteDilutedVinegar: return TitrationStep.AddIndicator;
                case TitrationStep.AddIndicator: return TitrationStep.TitrateToEndpoint;
                default: return step;
            }
        }
    }
}
