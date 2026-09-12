using System;
using UnityEngine;

namespace VLAB.ChemistryLab.Interaction
{
    public interface ILabCommandTarget { void TryActivate(); }

    public sealed class LabPressGate
    {
        private double lastPress = double.NegativeInfinity;
        public bool TryPress(double time, bool enabled)
        {
            if (!enabled || double.IsNaN(time) || double.IsInfinity(time) || time - lastPress < .2) return false;
            lastPress = time;
            return true;
        }
    }

    public static class LabInteractionSafety
    {
        public static bool ShouldRecover(Vector3 position, bool held) => !held &&
            (position.y < -.3f || Mathf.Abs(position.x) > 7.55f || Mathf.Abs(position.z) > 7.55f);

        public static Vector3 ClampThrow(Vector3 velocity, float maximum)
        {
            if (!float.IsFinite(velocity.x) || !float.IsFinite(velocity.y) || !float.IsFinite(velocity.z)) return Vector3.zero;
            return Vector3.ClampMagnitude(velocity, Mathf.Max(0, maximum));
        }
    }
}
