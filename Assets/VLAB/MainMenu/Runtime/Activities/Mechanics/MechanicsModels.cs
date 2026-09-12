namespace VLAB.MainMenu
{
    public enum GearAssemblyState { Incomplete, SpeedIncrease, Reduction }

    /// <summary>Two external spur gears of the same module on two shafts.</summary>
    public sealed class GearTrainModel
    {
        public int DriverTeeth { get; private set; }
        public int OutputTeeth { get; private set; }
        public float SpeedRatio => DriverTeeth == 0 || OutputTeeth == 0 ? 0 : -(float)DriverTeeth / OutputTeeth;

        public bool Place(int teeth, int shaft)
        {
            if ((teeth != 12 && teeth != 24) || shaft < 0 || shaft > 1) return false;
            // Placing an occupied component moves it; the displaced gear returns to its tray.
            if (DriverTeeth == teeth) DriverTeeth = 0;
            if (OutputTeeth == teeth) OutputTeeth = 0;
            if (shaft == 0) DriverTeeth = teeth; else OutputTeeth = teeth;
            return true;
        }

        public GearAssemblyState Check() => DriverTeeth == 0 || OutputTeeth == 0
            ? GearAssemblyState.Incomplete : DriverTeeth < OutputTeeth ? GearAssemblyState.Reduction : GearAssemblyState.SpeedIncrease;
        public void Reset() { DriverTeeth = 0; OutputTeeth = 0; }
    }

    /// <summary>Massless beam, frictionless pivot, two downward forces; each division is 0.1 m.</summary>
    public sealed class LeverBalanceModel
    {
        public const float LoadForce = 10f, EffortForce = 5f, MetresPerDivision = .1f;
        public int LoadPosition { get; private set; } = -2;
        public int PivotPosition { get; private set; }
        public int EffortPosition { get; private set; } = 2;
        public float LoadTorque => LoadForce * (PivotPosition - LoadPosition) * MetresPerDivision;
        public float EffortTorque => EffortForce * (EffortPosition - PivotPosition) * MetresPerDivision;
        public float NetTorque => LoadTorque - EffortTorque;
        public bool Balanced => System.Math.Abs(NetTorque) < .001f;

        public bool SetPositions(int load, int pivot, int effort)
        {
            if (load < -4 || effort > 4 || pivot < -1 || pivot > 1 || load >= pivot || effort <= pivot) return false;
            LoadPosition = load; PivotPosition = pivot; EffortPosition = effort;
            return true;
        }
        public void Reset() { LoadPosition = -2; PivotPosition = 0; EffortPosition = 2; }
    }
}
