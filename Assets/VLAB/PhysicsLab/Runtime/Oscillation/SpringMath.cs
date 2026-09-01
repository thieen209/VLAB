using UnityEngine;

namespace VLAB.PhysicsLab.Oscillation
{
    public static class SpringMath
    {
        public static float CalculateScalarForce(float displacementMetres, float relativeVelocityMetresPerSecond, float springConstant, float damping)
        {
            return -springConstant * displacementMetres - damping * relativeVelocityMetresPerSecond;
        }

        public static float StaticExtension(float massKilograms, float springConstant, float gravity = 9.80665f)
        {
            return massKilograms > 0f && springConstant > 0f
                ? massKilograms * Mathf.Max(0f, gravity) / springConstant
                : 0f;
        }

        public static float TheoreticalPeriod(float massKilograms, float springConstant)
        {
            return massKilograms > 0f && springConstant > 0f
                ? 2f * Mathf.PI * Mathf.Sqrt(massKilograms / springConstant)
                : 0f;
        }
    }
}
