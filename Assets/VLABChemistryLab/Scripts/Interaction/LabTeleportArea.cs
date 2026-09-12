using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

namespace VLAB.ChemistryLab.Interaction
{
    /// <summary>Only the floor may be targeted; a clear standing capsule is required at landing.</summary>
    public sealed class LabTeleportArea : TeleportationArea
    {
        private Collider floor;
        private Transform playerRoot;
        private readonly Collider[] overlaps = new Collider[64];
        public void Configure(Collider surface, Transform rig) { floor = surface; playerRoot = rig; }

        public bool CanStandAt(Vector3 point)
        {
            if (floor == null || !floor.enabled || float.IsNaN(point.x) || float.IsNaN(point.y) || float.IsNaN(point.z) ||
                float.IsInfinity(point.x) || float.IsInfinity(point.y) || float.IsInfinity(point.z)) return false;
            Bounds bounds = floor.bounds;
            const float radius = .28f;
            if (Mathf.Abs(point.y - bounds.max.y) > .04f ||
                point.x < bounds.min.x + radius || point.x > bounds.max.x - radius ||
                point.z < bounds.min.z + radius || point.z > bounds.max.z - radius) return false;
            int count = Physics.OverlapCapsuleNonAlloc(point + Vector3.up * (radius + .05f),
                point + Vector3.up * (1.8f - radius), radius, overlaps, ~0, QueryTriggerInteraction.Ignore);
            if (count == overlaps.Length) return false;
            for (int i = 0; i < count; i++)
            {
                Collider obstacle = overlaps[i];
                if (obstacle == floor || (playerRoot != null && obstacle.transform.IsChildOf(playerRoot))) continue;
                if (obstacle.bounds.max.y <= bounds.max.y + .04f) continue; // floor markings
                return false;
            }
            return true;
        }

        public override bool IsSelectableBy(IXRSelectInteractor interactor)
        {
            return interactor is XRRayInteractor ray && ray.name.Contains("Teleport") &&
                ray.TryGetCurrent3DRaycastHit(out var hit) && hit.collider == floor && CanStandAt(hit.point) &&
                base.IsSelectableBy(interactor);
        }

        protected override bool GenerateTeleportRequest(IXRInteractor interactor, RaycastHit hit, ref TeleportRequest request)
        {
            return hit.collider == floor && CanStandAt(hit.point) && base.GenerateTeleportRequest(interactor, hit, ref request);
        }
    }
}
