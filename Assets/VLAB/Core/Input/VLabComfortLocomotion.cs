using UnityEngine;

namespace VLAB.Core.Input
{
    /// <summary>Per-rig stick latch. Mouse pixel deltas do not pass through this analog policy.</summary>
    public sealed class VLabComfortTurn
    {
        private bool armed = true;
        public void Reset() => armed = true;

        public float Step(float axis, float deltaTime, VLabComfortSettings settings = null)
        {
            if (!VLabComfortLocomotion.IsFinite(axis) || !VLabComfortLocomotion.IsFinite(deltaTime) || deltaTime <= 0) return 0;
            settings = settings ?? VLabComfortSettings.Current;
            axis = Mathf.Clamp(axis, -1, 1);
            var magnitude = Mathf.Abs(axis);
            if (magnitude <= .2f) { armed = true; return 0; }
            if (settings.smoothTurn)
                return Mathf.Sign(axis) * ((magnitude - .2f) / .8f) * settings.turnSpeed * deltaTime;
            if (!armed || magnitude < .6f) return 0;
            armed = false;
            return Mathf.Sign(axis) * settings.snapAngle;
        }
    }

    public static class VLabComfortLocomotion
    {
        public static bool UsesAnalogLook(IVLABInputProvider provider) => provider is PhoneInputProvider || provider is IVLabRayProvider;
        internal static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        public static Vector3 PlanarVelocity(Vector3 facing, Vector2 move, bool sprint, VLabComfortSettings settings = null)
        {
            if (!IsFinite(move.x) || !IsFinite(move.y) || !IsFinite(facing.x) || !IsFinite(facing.y) || !IsFinite(facing.z)) return Vector3.zero;
            settings = settings ?? VLabComfortSettings.Current;
            var forward = Vector3.ProjectOnPlane(facing, Vector3.up).normalized;
            if (forward.sqrMagnitude < .001f) forward = Vector3.forward;
            move = Vector2.ClampMagnitude(move, 1);
            return (forward * move.y + Vector3.Cross(Vector3.up, forward) * move.x)
                * settings.movementSpeed * (sprint ? 1.65f : 1);
        }

        public static Transform CreateViewRoot(Camera view, string name)
        {
            var root = new GameObject(name).transform;
            root.SetParent(view.transform.parent, false);
            root.SetPositionAndRotation(view.transform.position, Quaternion.identity);
            view.transform.SetParent(root, true);
            return root;
        }

        public static void TurnRoot(Transform root, Transform head, float angle)
        {
            if (root == null || head == null || root == head || !IsFinite(angle) || Mathf.Abs(angle) < .00001f) return;
            root.RotateAround(head.position, Vector3.up, angle);
        }

        public static void MoveRootToViewPosition(Transform root, Transform head, Vector3 position)
        {
            if (root == null || head == null || root == head) return;
            root.position += position - head.position;
        }
    }
}
