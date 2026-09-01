using UnityEngine;
using VLAB.PhysicsLab.Common;

namespace VLAB.PhysicsLab.Measurement
{
    public sealed class PhotogateVelocityMeter : LabMeasurementSource
    {
        [SerializeField] private PhysicsPhotogate photogate;
        [SerializeField, Min(0.001f)] private float flagWidthMetres = 0.05f;
        private double blockedAt = double.NaN;

        public float VelocityMetresPerSecond { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            ConfigureMeasurement("photogate velocity", "m/s");
        }

        private void OnEnable()
        {
            if (photogate != null) photogate.GateStateChanged += Process;
        }

        private void OnDisable()
        {
            if (photogate != null) photogate.GateStateChanged -= Process;
        }

        public void Configure(PhysicsPhotogate gate, float flagWidth)
        {
            if (isActiveAndEnabled && photogate != null) photogate.GateStateChanged -= Process;
            photogate = gate;
            flagWidthMetres = Mathf.Max(0.001f, flagWidth);
            if (isActiveAndEnabled && photogate != null) photogate.GateStateChanged += Process;
        }

        public void Process(PhotogateEvent gateEvent)
        {
            if (gateEvent.Blocked)
            {
                blockedAt = gateEvent.TimestampSeconds;
                return;
            }
            if (double.IsNaN(blockedAt))
            {
                return;
            }
            var duration = gateEvent.TimestampSeconds - blockedAt;
            if (duration > 0d)
            {
                VelocityMetresPerSecond = (float)(flagWidthMetres / duration);
                PublishMeasurement(VelocityMetresPerSecond);
            }
            blockedAt = double.NaN;
        }

        protected override void OnResetLabObject()
        {
            base.OnResetLabObject();
            blockedAt = double.NaN;
            VelocityMetresPerSecond = 0f;
        }
    }
}
