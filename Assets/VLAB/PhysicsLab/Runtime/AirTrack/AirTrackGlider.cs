using UnityEngine;
using VLAB.PhysicsLab.Common;

namespace VLAB.PhysicsLab.AirTrack
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class AirTrackGlider : LabMeasurementSource
    {
        [SerializeField] private Rigidbody body;
        [SerializeField] private PhysicsAirTrack track;
        [SerializeField, Min(0.01f)] private float massKilograms = 0.25f;

        public Rigidbody Body => body != null ? body : body = GetComponent<Rigidbody>();
        public float MassKilograms => massKilograms;
        public Vector3 TrackAxis => track != null ? track.MovementAxis.normalized : transform.parent != null ? transform.parent.right : Vector3.right;
        public float SignedVelocity => Vector3.Dot(Body.linearVelocity, TrackAxis);

        protected override void Awake()
        {
            base.Awake();
            ConfigureMeasurement("glider velocity", "m/s");
            body = GetComponent<Rigidbody>();
            ConfigureBody();
        }

        public void Configure(PhysicsAirTrack targetTrack, float mass)
        {
            track = targetTrack;
            massKilograms = Mathf.Max(0.01f, mass);
            ConfigureBody();
        }

        public void SetMass(float value)
        {
            massKilograms = Mathf.Max(0.01f, value);
            ConfigureBody();
        }

        private void FixedUpdate()
        {
            PublishMeasurement(SignedVelocity);
        }

        private void ConfigureBody()
        {
            if (body == null)
            {
                return;
            }
            body.mass = massKilograms;
            body.useGravity = false;
            body.linearDamping = track != null ? track.EffectiveLinearDamping : 0.002f;
            body.angularDamping = 0.05f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            body.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
        }
    }
}
