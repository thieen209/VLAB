using System;

namespace VLAB.ChemistryLab.Interaction
{
    public enum LabReagent { None, NaOH, DilutedVinegar, Indicator, Mixture }
    public enum LabVesselRole { Bottle, Burette, Pipette, Flask, Waste }

    /// <summary>Finite-volume storage. All successful transfers conserve volume, including waste.</summary>
    public sealed class LabLiquidState
    {
        public double CapacityMl { get; }
        public double VolumeMl { get; private set; }
        public LabReagent Reagent { get; private set; }
        public LabLiquidState(double capacityMl, LabReagent reagent = LabReagent.None, double volumeMl = 0)
        {
            if (!Finite(capacityMl) || capacityMl <= 0 || !Finite(volumeMl) || volumeMl < 0 || volumeMl > capacityMl)
                throw new ArgumentOutOfRangeException(nameof(capacityMl));
            if (volumeMl > 0 && reagent == LabReagent.None) throw new ArgumentException("Liquid requires an identity.");
            CapacityMl = capacityMl;
            VolumeMl = volumeMl;
            Reagent = volumeMl > 0 ? reagent : LabReagent.None;
        }
        public double TransferTo(LabLiquidState target, double requestedMl)
        {
            if (target == null || target == this || !Finite(requestedMl) || requestedMl <= 0) return 0;
            double amount = Math.Min(requestedMl, Math.Min(VolumeMl, target.CapacityMl - target.VolumeMl));
            if (amount <= 0) return 0;
            target.Reagent = target.VolumeMl <= 0 ? Reagent : target.Reagent == Reagent ? Reagent : LabReagent.Mixture;
            target.VolumeMl += amount;
            Remove(amount);
            return amount;
        }
        public double Remove(double requestedMl)
        {
            if (!Finite(requestedMl) || requestedMl <= 0) return 0;
            double amount = Math.Min(VolumeMl, requestedMl);
            VolumeMl -= amount;
            if (VolumeMl < 1e-8) { VolumeMl = 0; Reagent = LabReagent.None; }
            return amount;
        }
        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
