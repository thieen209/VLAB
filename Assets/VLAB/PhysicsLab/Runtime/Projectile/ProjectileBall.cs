using System;
using UnityEngine;
using VLAB.PhysicsLab.Common;

namespace VLAB.PhysicsLab.Projectile
{
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public sealed class ProjectileBall : LabMeasurementSource
    {
        [SerializeField] private Rigidbody body;
        private Vector3 launchPosition;
        private double launchTime;

        public bool InFlight { get; private set; }
        public float RangeMetres { get; private set; }
        public float MaximumHeightMetres { get; private set; }
        public double FlightTimeSeconds { get; private set; }
        public Rigidbody Body => body != null ? body : body = GetComponent<Rigidbody>();
        public event Action<ProjectileBall> Landed;

        protected override void Awake()
        {
            base.Awake();
            ConfigureMeasurement("projectile range", "m");
            body = GetComponent<Rigidbody>();
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        public void BeginFlight(Vector3 position, Vector3 velocity, double timestamp)
        {
            launchPosition = position;
            launchTime = timestamp;
            RangeMetres = 0f;
            MaximumHeightMetres = 0f;
            FlightTimeSeconds = 0d;
            body.position = position;
            body.isKinematic = false;
            body.linearVelocity = velocity;
            body.angularVelocity = Vector3.zero;
            body.WakeUp();
            InFlight = true;
        }

        private void FixedUpdate()
        {
            if (!InFlight)
            {
                return;
            }
            var offset = body.position - launchPosition;
            RangeMetres = Vector3.ProjectOnPlane(offset, Vector3.up).magnitude;
            MaximumHeightMetres = Mathf.Max(MaximumHeightMetres, offset.y);
            FlightTimeSeconds = Time.timeAsDouble - launchTime;
            PublishMeasurement(RangeMetres);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (InFlight && Time.timeAsDouble - launchTime > 0.02d)
            {
                InFlight = false;
                FlightTimeSeconds = Time.timeAsDouble - launchTime;
                PublishMeasurement(RangeMetres);
                Landed?.Invoke(this);
            }
        }

        protected override void OnResetLabObject()
        {
            base.OnResetLabObject();
            InFlight = false;
            RangeMetres = MaximumHeightMetres = 0f;
            FlightTimeSeconds = 0d;
        }
    }
}
