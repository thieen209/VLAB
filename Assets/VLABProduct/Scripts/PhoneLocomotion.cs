using UnityEngine;

namespace Vlab.Product
{
    // The origin is the learner's eye, not the capsule's feet.
    public static class PhoneLocomotion
    {
        public static Vector3 Direction(Quaternion view, Vector2 input)
        {
            var forward = Quaternion.Euler(0, view.eulerAngles.y, 0) * Vector3.forward;
            return Vector3.ClampMagnitude(forward * input.y + Vector3.Cross(Vector3.up, forward) * input.x, 1);
        }

        public static Vector3 Move(Transform origin, Vector3 delta, out Collider blockedBy)
        {
            blockedBy = null;
            var position = origin.position;
            for (int pass = 0; pass < 2 && delta.sqrMagnitude > .000001f; pass++)
            {
                var lower = position - Vector3.up * 1.25f;
                var upper = position - Vector3.up * .12f;
                // Bench inspection lowers the eye; keep the body above the actual floor instead of embedding its feet.
                foreach (var ground in Physics.RaycastAll(position, Vector3.down, 3, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    if (ground.normal.y < .6f || ground.collider.transform.IsChildOf(origin.root) || ground.collider.GetComponentInParent<EquipmentHandle>()) continue;
                    lower.y = Mathf.Max(lower.y, ground.point.y + .2f);
                }
                upper.y = Mathf.Max(upper.y, lower.y);
                float distance = delta.magnitude;
                var direction = delta / distance;
                RaycastHit? nearest = null;
                foreach (var hit in Physics.CapsuleCastAll(lower, upper, .18f, direction, distance + .02f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    if (hit.collider.transform.IsChildOf(origin.root) || hit.collider.GetComponentInParent<EquipmentHandle>()) continue;
                    // Interaction-only handles must not become invisible walls. Room furniture retains its physical mesh colliders.
                    if (hit.collider.GetComponent<LabFixture>() && !hit.collider.GetComponent<Renderer>() && hit.collider.GetComponentsInChildren<Renderer>().Length == 0) continue;
                    if (!nearest.HasValue || hit.distance < nearest.Value.distance) nearest = hit;
                }
                if (!nearest.HasValue) { position += delta; break; }
                var obstacle = nearest.Value;
                blockedBy = obstacle.collider;
                float travel = Mathf.Max(0, obstacle.distance - .02f);
                position += direction * travel;
                delta = Vector3.ProjectOnPlane(delta - direction * travel, obstacle.normal);
                delta.y = 0;
            }
            return position;
        }
    }
}
