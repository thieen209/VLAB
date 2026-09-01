using System;
using UnityEngine;
using VLAB.PhysicsLab.Common;

namespace VLAB.PhysicsLab.Oscillation
{
    public sealed class PhysicsCoilSpring : LabMeasurementSource
    {
        [SerializeField] private Transform topAnchor;
        [SerializeField] private Transform bottomAnchor;
        [SerializeField] private Rigidbody attachedBody;
        [SerializeField] private Transform springVisual;
        [SerializeField, Min(0.01f)] private float naturalLength = 0.37f;
        [SerializeField, Min(0f)] private float springConstant = 18f;
        [SerializeField, Min(0f)] private float damping = 0.8f;
        [SerializeField, Min(1f)] private float maximumForce = 200f;
        private Vector3 visualInitialScale;
        private float previousExtension;
        private float previousSlope;
        private double previousPeakTime = double.NaN;
        private bool hasExtensionSample;

        public event Action<double> PeriodMeasured;

        public float NaturalLength => naturalLength;
        public float SpringConstant => springConstant;
        public float Damping => damping;
        public float Extension { get; private set; }
        public double MeasuredPeriod { get; private set; }
        public float AttachedMassKilograms => attachedBody != null ? attachedBody.mass : 0f;

        protected override void Awake()
        {
            base.Awake();
            ConfigureMeasurement("spring extension", "m");
            if (springVisual != null)
            {
                visualInitialScale = springVisual.localScale;
            }
        }

        public void Configure(Transform top, Transform bottom, Rigidbody body, Transform visual, float k, float restLength, float dampingCoefficient)
        {
            topAnchor = top;
            bottomAnchor = bottom;
            attachedBody = body;
            springVisual = visual;
            springConstant = Mathf.Max(0f, k);
            naturalLength = Mathf.Max(0.01f, restLength);
            damping = Mathf.Max(0f, dampingCoefficient);
            visualInitialScale = springVisual != null ? springVisual.localScale : Vector3.one;
        }

        public void SetSpringConstant(float value) => springConstant = Mathf.Max(0f, value);
        public void SetNaturalLength(float value) => naturalLength = Mathf.Max(0.01f, value);

        public void AttachBody(Rigidbody body, Transform bottom)
        {
            attachedBody = body;
            if (bottom != null)
            {
                bottomAnchor = bottom;
            }
        }

        public Rigidbody DetachBody()
        {
            var detached = attachedBody;
            attachedBody = null;
            return detached;
        }

        public void ReleaseAttachedBody()
        {
            if (attachedBody != null)
            {
                attachedBody.isKinematic = false;
                attachedBody.WakeUp();
            }
        }

        public void ProcessExtensionSample(float extension, double timestampSeconds)
        {
            if (!hasExtensionSample)
            {
                previousExtension = extension;
                hasExtensionSample = true;
                return;
            }

            var slope = extension - previousExtension;
            if (previousSlope > 0f && slope <= 0f)
            {
                if (!double.IsNaN(previousPeakTime))
                {
                    MeasuredPeriod = timestampSeconds - previousPeakTime;
                    PeriodMeasured?.Invoke(MeasuredPeriod);
                }
                previousPeakTime = timestampSeconds;
            }
            previousExtension = extension;
            previousSlope = slope;
        }

        private void FixedUpdate()
        {
            if (topAnchor == null || bottomAnchor == null)
            {
                return;
            }

            var delta = bottomAnchor.position - topAnchor.position;
            var distance = delta.magnitude;
            if (distance < 0.0001f)
            {
                return;
            }

            var direction = delta / distance;
            Extension = distance - naturalLength;
            PublishMeasurement(Extension);
            ProcessExtensionSample(Extension, Time.timeAsDouble);
            if (attachedBody != null && !attachedBody.isKinematic)
            {
                var relativeSpeed = Vector3.Dot(attachedBody.GetPointVelocity(bottomAnchor.position), direction);
                var scalarForce = Mathf.Clamp(SpringMath.CalculateScalarForce(Extension, relativeSpeed, springConstant, damping), -maximumForce, maximumForce);
                attachedBody.AddForceAtPosition(direction * scalarForce, bottomAnchor.position, ForceMode.Force);
            }

            UpdateVisual(delta, distance);
        }

        protected override void OnResetLabObject()
        {
            base.OnResetLabObject();
            Extension = 0f;
            MeasuredPeriod = 0d;
            previousExtension = 0f;
            previousSlope = 0f;
            previousPeakTime = double.NaN;
            hasExtensionSample = false;
        }

        private void UpdateVisual(Vector3 delta, float distance)
        {
            if (springVisual == null)
            {
                return;
            }

            springVisual.position = topAnchor.position + delta * 0.5f;
            springVisual.rotation = Quaternion.FromToRotation(Vector3.up, delta.normalized);
            var scale = visualInitialScale;
            scale.y = visualInitialScale.y * distance / naturalLength;
            springVisual.localScale = scale;
        }
    }
}
