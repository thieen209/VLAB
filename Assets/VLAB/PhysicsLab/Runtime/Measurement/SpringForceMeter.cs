using UnityEngine;
using VLAB.PhysicsLab.Common;

namespace VLAB.PhysicsLab.Measurement
{
    public sealed class SpringForceMeter : LabMeasurementSource
    {
        [SerializeField, Min(0.1f)] private float capacityNewtons = 10f;
        [SerializeField] private Transform indicator;
        [SerializeField] private Vector3 indicatorTravel = new Vector3(0f, 0f, -0.18f);
        private Vector3 indicatorRestPosition;
        private float tensionNewtons;

        public float TensionNewtons => tensionNewtons;

        protected override void Awake()
        {
            base.Awake();
            ConfigureMeasurement("tension", "N");
            if (indicator != null)
            {
                indicatorRestPosition = indicator.localPosition;
            }
        }

        public void ConfigureIndicator(Transform indicatorTransform)
        {
            indicator = indicatorTransform;
            indicatorRestPosition = indicator != null ? indicator.localPosition : Vector3.zero;
        }

        public void SetTension(float valueNewtons)
        {
            tensionNewtons = Mathf.Clamp(valueNewtons, 0f, capacityNewtons);
            PublishMeasurement(tensionNewtons);
            if (indicator != null)
            {
                indicator.localPosition = indicatorRestPosition + indicatorTravel * (tensionNewtons / capacityNewtons);
            }
        }

        protected override void OnResetLabObject()
        {
            base.OnResetLabObject();
            SetTension(0f);
        }
    }
}
