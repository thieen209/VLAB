using System;

namespace VLAB.ChemistryLab
{
    public enum TitrationEndpointState
    {
        BeforeEndpoint,
        Endpoint,
        Overshot
    }

    public static class ChemistryCalculation
    {
        private const double AceticAcidMolarMassGramsPerMole = 60.052d;

        public static double CalculateOriginalAcidConcentration(
            double naohConcentrationMolar,
            double naohVolumeMl,
            double dilutedVinegarAliquotMl,
            double dilutionFactor)
        {
            RequirePositiveFinite(naohConcentrationMolar, nameof(naohConcentrationMolar));
            RequirePositiveFinite(naohVolumeMl, nameof(naohVolumeMl));
            RequirePositiveFinite(dilutedVinegarAliquotMl, nameof(dilutedVinegarAliquotMl));
            RequirePositiveFinite(dilutionFactor, nameof(dilutionFactor));

            return naohConcentrationMolar * naohVolumeMl / dilutedVinegarAliquotMl * dilutionFactor;
        }

        public static double CalculateMassVolumePercent(double acidConcentrationMolar)
        {
            RequirePositiveFinite(acidConcentrationMolar, nameof(acidConcentrationMolar));
            return acidConcentrationMolar * AceticAcidMolarMassGramsPerMole / 10d;
        }

        public static TitrationEndpointState ClassifyEndpoint(
            double deliveredVolumeMl,
            double expectedEndpointVolumeMl,
            double endpointToleranceMl)
        {
            RequireNonNegativeFinite(deliveredVolumeMl, nameof(deliveredVolumeMl));
            RequirePositiveFinite(expectedEndpointVolumeMl, nameof(expectedEndpointVolumeMl));
            RequireNonNegativeFinite(endpointToleranceMl, nameof(endpointToleranceMl));

            double lowerEndpointLimit = expectedEndpointVolumeMl - endpointToleranceMl;
            double upperEndpointLimit = expectedEndpointVolumeMl + endpointToleranceMl;

            if (deliveredVolumeMl < lowerEndpointLimit)
            {
                return TitrationEndpointState.BeforeEndpoint;
            }

            return deliveredVolumeMl < upperEndpointLimit
                ? TitrationEndpointState.Endpoint
                : TitrationEndpointState.Overshot;
        }

        public static bool AreConcordant(
            double firstTitreMl,
            double secondTitreMl,
            double thirdTitreMl,
            double maximumSpreadMl)
        {
            RequireNonNegativeFinite(firstTitreMl, nameof(firstTitreMl));
            RequireNonNegativeFinite(secondTitreMl, nameof(secondTitreMl));
            RequireNonNegativeFinite(thirdTitreMl, nameof(thirdTitreMl));
            RequireNonNegativeFinite(maximumSpreadMl, nameof(maximumSpreadMl));

            double minimum = Math.Min(firstTitreMl, Math.Min(secondTitreMl, thirdTitreMl));
            double maximum = Math.Max(firstTitreMl, Math.Max(secondTitreMl, thirdTitreMl));
            return maximum - minimum <= maximumSpreadMl + 1e-9d;
        }

        public static double CalculateMeanTitre(
            double firstTitreMl,
            double secondTitreMl,
            double thirdTitreMl,
            double maximumSpreadMl)
        {
            if (!AreConcordant(firstTitreMl, secondTitreMl, thirdTitreMl, maximumSpreadMl))
            {
                throw new InvalidOperationException("Titre results are not concordant.");
            }

            return (firstTitreMl + secondTitreMl + thirdTitreMl) / 3d;
        }

        private static void RequirePositiveFinite(double value, string parameterName)
        {
            if (value <= 0d || double.IsNaN(value) || double.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(parameterName, "Value must be positive and finite.");
            }
        }

        private static void RequireNonNegativeFinite(double value, string parameterName)
        {
            if (value < 0d || double.IsNaN(value) || double.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(parameterName, "Value must be non-negative and finite.");
            }
        }
    }
}
