using System;
using UnityEngine;
using VLAB.PhysicsLab.Common;

namespace VLAB.PhysicsLab.Oscillation
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PendulumBob : LabMeasurementSource
    {
        [SerializeField] private Rigidbody body;
        [SerializeField] private Transform pivot;
        [SerializeField, Range(0.1f, 2f)] private float length = 0.75f;
        [SerializeField, Min(0.01f)] private float bobMass = 0.20f;
        private ConfigurableJoint joint;
        private float previousSide;
        private double previousSameDirectionCrossing = double.NaN;

        public event Action<double> PeriodMeasured;

        public float Length => length;
        public float BobMass => bobMass;
        public double MeasuredPeriod { get; private set; }
        public Rigidbody Body => body != null ? body : body = GetComponent<Rigidbody>();

        protected override void Awake()
        {
            base.Awake();
            ConfigureMeasurement("pendulum period", "s");
            body = body != null ? body : GetComponent<Rigidbody>();
            ConfigureBody();
            ConfigureJoint();
        }

        public void Configure(Transform anchor, float pendulumLength, float massKilograms)
        {
            pivot = anchor;
            length = Mathf.Clamp(pendulumLength, 0.1f, 2f);
            bobMass = Mathf.Max(0.01f, massKilograms);
            body = body != null ? body : GetComponent<Rigidbody>();
            ConfigureBody();
            ConfigureJoint();
        }

        public void SetLength(float value)
        {
            length = Mathf.Clamp(value, 0.1f, 2f);
            ConfigureJoint();
        }

        public void SetMass(float value)
        {
            bobMass = Mathf.Max(0.01f, value);
            ConfigureBody();
        }

        public void Release()
        {
            if (body != null)
            {
                body.isKinematic = false;
                body.WakeUp();
            }
        }

        private void FixedUpdate()
        {
            if (pivot == null || body == null)
            {
                return;
            }

            var offset = body.worldCenterOfMass - pivot.position;
            var side = Vector3.Dot(offset, pivot.right);
            if (previousSide < 0f && side >= 0f && Vector3.Dot(body.linearVelocity, pivot.right) > 0f)
            {
                var now = Time.timeAsDouble;
                if (!double.IsNaN(previousSameDirectionCrossing))
                {
                    MeasuredPeriod = now - previousSameDirectionCrossing;
                    PublishMeasurement(MeasuredPeriod);
                    PeriodMeasured?.Invoke(MeasuredPeriod);
                }
                previousSameDirectionCrossing = now;
            }
            previousSide = side;
        }

        protected override void OnResetLabObject()
        {
            base.OnResetLabObject();
            previousSide = 0f;
            previousSameDirectionCrossing = double.NaN;
            MeasuredPeriod = 0d;
        }

        private void ConfigureBody()
        {
            if (body == null)
            {
                return;
            }
            body.mass = bobMass;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }

        private void ConfigureJoint()
        {
            if (body == null || pivot == null)
            {
                return;
            }
            joint = joint != null ? joint : GetComponent<ConfigurableJoint>();
            if (joint == null)
            {
                joint = gameObject.AddComponent<ConfigurableJoint>();
            }
            joint.autoConfigureConnectedAnchor = false;
            joint.connectedBody = null;
            joint.connectedAnchor = pivot.position;
            joint.anchor = Vector3.zero;
            joint.xMotion = ConfigurableJointMotion.Limited;
            joint.yMotion = ConfigurableJointMotion.Limited;
            joint.zMotion = ConfigurableJointMotion.Limited;
            joint.angularXMotion = ConfigurableJointMotion.Free;
            joint.angularYMotion = ConfigurableJointMotion.Free;
            joint.angularZMotion = ConfigurableJointMotion.Free;
            var limit = joint.linearLimit;
            limit.limit = length;
            limit.bounciness = 0f;
            limit.contactDistance = 0.005f;
            joint.linearLimit = limit;
            joint.projectionMode = JointProjectionMode.PositionAndRotation;
            joint.projectionDistance = 0.005f;
        }
    }
}
