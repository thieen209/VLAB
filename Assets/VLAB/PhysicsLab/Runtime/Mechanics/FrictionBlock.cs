using System;
using UnityEngine;
using VLAB.PhysicsLab.Common;

namespace VLAB.PhysicsLab.Mechanics
{
    public readonly struct FrictionExperimentSample
    {
        public FrictionExperimentSample(float maximumStaticForceNewtons, float kineticForceNewtons)
        {
            MaximumStaticForceNewtons = maximumStaticForceNewtons;
            KineticForceNewtons = kineticForceNewtons;
        }

        public float MaximumStaticForceNewtons { get; }
        public float KineticForceNewtons { get; }
    }

    public static class FrictionMath
    {
        public static float CalculateMagnitude(float normalForceNewtons, float appliedForceMagnitude, float speed, float staticCoefficient, float kineticCoefficient)
        {
            var normal = Mathf.Max(0f, normalForceNewtons);
            if (Mathf.Abs(speed) < 0.001f)
            {
                return Mathf.Min(Mathf.Abs(appliedForceMagnitude), Mathf.Max(0f, staticCoefficient) * normal);
            }
            return Mathf.Max(0f, kineticCoefficient) * normal;
        }

        public static FrictionExperimentSample CalculateExperiment(float massKilograms, float staticCoefficient, float kineticCoefficient, float gravity = 9.80665f)
        {
            var normalForce = Mathf.Max(0f, massKilograms) * Mathf.Max(0f, gravity);
            return new FrictionExperimentSample(
                Mathf.Max(0f, staticCoefficient) * normalForce,
                Mathf.Max(0f, kineticCoefficient) * normalForce);
        }
    }

    [RequireComponent(typeof(Rigidbody))]
    public sealed class FrictionBlock : LabMeasurementSource
    {
        [SerializeField] private Rigidbody body;
        [SerializeField, Min(0.01f)] private float massKilograms = 0.5f;
        [SerializeField, Range(0f, 2f)] private float staticFriction = 0.45f;
        [SerializeField, Range(0f, 2f)] private float kineticFriction = 0.30f;
        [SerializeField] private Vector3 surfaceNormal = Vector3.up;
        private Vector3 requestedPullForce;
        private bool wasSliding;

        public event Action<float> SlidingStarted;
        public event Action<float> FrictionForceMeasured;

        public float MassKilograms => massKilograms;
        public float StaticFriction => staticFriction;
        public float KineticFriction => kineticFriction;
        public float Speed => body != null ? Vector3.ProjectOnPlane(body.linearVelocity, surfaceNormal).magnitude : 0f;

        protected override void Awake()
        {
            base.Awake();
            ConfigureMeasurement("block speed", "m/s");
            body = body != null ? body : GetComponent<Rigidbody>();
            ConfigureBody();
        }

        public void SetParameters(float mass, float staticCoefficient, float kineticCoefficient)
        {
            massKilograms = Mathf.Max(0.01f, mass);
            staticFriction = Mathf.Max(0f, staticCoefficient);
            kineticFriction = Mathf.Max(0f, kineticCoefficient);
            ConfigureBody();
        }

        public void ApplyPullForce(Vector3 forceNewtons)
        {
            requestedPullForce = Vector3.ProjectOnPlane(forceNewtons, surfaceNormal);
        }

        private void FixedUpdate()
        {
            if (body == null || body.isKinematic)
            {
                return;
            }

            var planarVelocity = Vector3.ProjectOnPlane(body.linearVelocity, surfaceNormal);
            var speed = planarVelocity.magnitude;
            var pullMagnitude = requestedPullForce.magnitude;
            var direction = pullMagnitude > 0.0001f ? requestedPullForce / pullMagnitude : (speed > 0.0001f ? planarVelocity / speed : Vector3.zero);
            var normalForce = massKilograms * Physics.gravity.magnitude;
            var frictionMagnitude = FrictionMath.CalculateMagnitude(normalForce, pullMagnitude, speed, staticFriction, kineticFriction);
            if (pullMagnitude > 0f)
            {
                body.AddForce(requestedPullForce, ForceMode.Force);
            }
            if (direction.sqrMagnitude > 0f)
            {
                body.AddForce(-direction * frictionMagnitude, ForceMode.Force);
            }

            PublishMeasurement(speed);
            FrictionForceMeasured?.Invoke(frictionMagnitude);
            var sliding = speed > 0.015f;
            if (sliding && !wasSliding)
            {
                SlidingStarted?.Invoke(frictionMagnitude);
            }
            wasSliding = sliding;
            requestedPullForce = Vector3.zero;
        }

        protected override void OnResetLabObject()
        {
            base.OnResetLabObject();
            requestedPullForce = Vector3.zero;
            wasSliding = false;
        }

        private void ConfigureBody()
        {
            if (body == null)
            {
                return;
            }
            body.mass = massKilograms;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        }
    }
}
