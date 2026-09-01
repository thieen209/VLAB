using UnityEngine;
using VLAB.PhysicsLab.Common;

namespace VLAB.PhysicsLab.AirTrack
{
    public sealed class PhysicsAirTrack : ExperimentObject
    {
        [SerializeField] private Transform axisReference;
        [SerializeField, Min(0f)] private float effectiveLinearDamping = 0.002f;

        public Vector3 MovementAxis => axisReference != null ? axisReference.right : transform.right;
        public float EffectiveLinearDamping => effectiveLinearDamping;

        public void Configure(Transform reference, float damping)
        {
            axisReference = reference != null ? reference : transform;
            effectiveLinearDamping = Mathf.Max(0f, damping);
        }
    }
}
