using System;
using System.Collections.Generic;
using UnityEngine;
using VLAB.PhysicsLab.Common;

namespace VLAB.PhysicsLab.Measurement
{
    public readonly struct PhotogateEvent
    {
        public PhotogateEvent(PhysicsPhotogate gate, bool blocked, double timestampSeconds)
        {
            Gate = gate;
            Blocked = blocked;
            TimestampSeconds = timestampSeconds;
        }

        public PhysicsPhotogate Gate { get; }
        public bool Blocked { get; }
        public double TimestampSeconds { get; }
    }

    public sealed class PhysicsPhotogate : LabSensor
    {
        [SerializeField] private LayerMask detectableLayers = ~0;
        [SerializeField, Min(0.001f)] private float beamWidth = 0.05f;
        private readonly HashSet<Collider> blockers = new HashSet<Collider>();

        public event Action<PhotogateEvent> GateStateChanged;
        public event Action Blocked;
        public event Action Unblocked;

        public bool IsBlocked => blockers.Count > 0;
        public float BeamWidth => beamWidth;

        protected override void Awake()
        {
            base.Awake();
            ConfigureMeasurement("beam state", "bool");
        }

        public bool IsValidTarget(Collider candidate)
        {
            if (candidate == null || !SensorEnabled)
            {
                return false;
            }
            if (candidate.transform == transform || candidate.transform.IsChildOf(transform))
            {
                return false;
            }
            return (detectableLayers.value & (1 << candidate.gameObject.layer)) != 0;
        }

        public void ProcessEnter(Collider candidate, double timestampSeconds)
        {
            if (!IsValidTarget(candidate))
            {
                return;
            }
            var wasBlocked = IsBlocked;
            blockers.Add(candidate);
            if (!wasBlocked && IsBlocked)
            {
                PublishMeasurement(1d);
                Blocked?.Invoke();
                GateStateChanged?.Invoke(new PhotogateEvent(this, true, timestampSeconds));
            }
        }

        public void ProcessExit(Collider candidate, double timestampSeconds)
        {
            if (candidate == null)
            {
                return;
            }
            var wasBlocked = IsBlocked;
            blockers.Remove(candidate);
            if (wasBlocked && !IsBlocked)
            {
                PublishMeasurement(0d);
                Unblocked?.Invoke();
                GateStateChanged?.Invoke(new PhotogateEvent(this, false, timestampSeconds));
            }
        }

        protected override void OnResetLabObject()
        {
            base.OnResetLabObject();
            blockers.Clear();
        }

        private void OnTriggerEnter(Collider other) => ProcessEnter(other, Time.timeAsDouble);
        private void OnTriggerExit(Collider other) => ProcessExit(other, Time.timeAsDouble);
    }
}
