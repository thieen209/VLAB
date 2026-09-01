using System;
using UnityEngine;

namespace VLAB.PhysicsLab.AirTrack
{
    [RequireComponent(typeof(AirTrackGlider))]
    public sealed class GliderCollisionAttachment : MonoBehaviour
    {
        [SerializeField] private GliderCollisionMode mode;
        private AirTrackGlider glider;
        private FixedJoint latch;

        public GliderCollisionMode Mode => mode;
        public event Action<AirTrackGlider, AirTrackGlider> CollisionOccurred;
        public void SetMode(GliderCollisionMode value) => mode = value;

        private void Awake() => glider = GetComponent<AirTrackGlider>();

        private void OnCollisionEnter(Collision collision)
        {
            var other = collision.rigidbody != null ? collision.rigidbody.GetComponent<AirTrackGlider>() : null;
            if (other == null || glider == null || glider.GetHashCode() > other.GetHashCode())
            {
                return;
            }

            CollisionOccurred?.Invoke(glider, other);

            if (mode == GliderCollisionMode.Inelastic)
            {
                var commonVelocity = MomentumMath.TotalMomentum(glider.MassKilograms, glider.SignedVelocity, other.MassKilograms, other.SignedVelocity) /
                                     (glider.MassKilograms + other.MassKilograms);
                var velocity = glider.TrackAxis * commonVelocity;
                glider.Body.linearVelocity = velocity;
                other.Body.linearVelocity = velocity;
                latch = gameObject.AddComponent<FixedJoint>();
                latch.connectedBody = other.Body;
                latch.breakForce = Mathf.Infinity;
                return;
            }

            MomentumMath.ElasticVelocities(glider.MassKilograms, glider.SignedVelocity, other.MassKilograms, other.SignedVelocity, out var velocityA, out var velocityB);
            glider.Body.linearVelocity = glider.TrackAxis * velocityA;
            other.Body.linearVelocity = other.TrackAxis * velocityB;
        }

        public void ReleaseLatch()
        {
            if (latch != null)
            {
                Destroy(latch);
                latch = null;
            }
        }
    }
}
