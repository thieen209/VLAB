using UnityEngine;
using VLAB.PhysicsLab.Common;

namespace VLAB.PhysicsLab.AirTrack
{
    public sealed class MomentumCollisionAnalyzer : LabMeasurementSource
    {
        public float MomentumBefore { get; private set; }
        public float MomentumAfter { get; private set; }
        public float MomentumDifference => MomentumAfter - MomentumBefore;

        protected override void Awake()
        {
            base.Awake();
            ConfigureMeasurement("momentum difference", "kg m/s");
        }

        public void RecordBefore(AirTrackGlider first, AirTrackGlider second)
        {
            MomentumBefore = Calculate(first, second);
        }

        public void RecordAfter(AirTrackGlider first, AirTrackGlider second)
        {
            MomentumAfter = Calculate(first, second);
            PublishMeasurement(MomentumDifference);
        }

        protected override void OnResetLabObject()
        {
            base.OnResetLabObject();
            MomentumBefore = MomentumAfter = 0f;
        }

        private static float Calculate(AirTrackGlider first, AirTrackGlider second)
        {
            if (first == null || second == null)
            {
                return 0f;
            }
            return MomentumMath.TotalMomentum(first.MassKilograms, first.SignedVelocity, second.MassKilograms, second.SignedVelocity);
        }
    }
}
