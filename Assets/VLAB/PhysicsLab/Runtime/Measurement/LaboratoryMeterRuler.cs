using UnityEngine;
using VLAB.PhysicsLab.Common;

namespace VLAB.PhysicsLab.Measurement
{
    public sealed class LaboratoryMeterRuler : LabMeasurementSource
    {
        [SerializeField] private Transform measurementOrigin;
        [SerializeField] private Vector3 localMeasurementDirection = Vector3.right;

        protected override void Awake()
        {
            base.Awake();
            ConfigureMeasurement("distance", "m");
            if (measurementOrigin == null)
            {
                measurementOrigin = transform;
            }
        }

        public void Configure(Transform origin, Vector3 localDirection)
        {
            measurementOrigin = origin != null ? origin : transform;
            localMeasurementDirection = localDirection.sqrMagnitude > 0f ? localDirection.normalized : Vector3.right;
        }

        public float MeasureWorldPoint(Vector3 worldPoint)
        {
            var origin = measurementOrigin != null ? measurementOrigin : transform;
            var direction = origin.TransformDirection(localMeasurementDirection).normalized;
            var distance = Vector3.Dot(worldPoint - origin.position, direction);
            PublishMeasurement(distance);
            return distance;
        }
    }
}
