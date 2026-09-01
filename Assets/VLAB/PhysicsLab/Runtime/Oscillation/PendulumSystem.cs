using UnityEngine;

namespace VLAB.PhysicsLab.Oscillation
{
    public static class PendulumMath
    {
        public static float TheoreticalPeriod(float lengthMetres, float gravity = 9.80665f)
        {
            return lengthMetres > 0f && gravity > 0f ? 2f * Mathf.PI * Mathf.Sqrt(lengthMetres / gravity) : 0f;
        }

        public static float EstimateGravity(float lengthMetres, float periodSeconds)
        {
            if (lengthMetres <= 0f || periodSeconds <= 0f)
            {
                return 0f;
            }
            return 4f * Mathf.PI * Mathf.PI * lengthMetres / (periodSeconds * periodSeconds);
        }
    }
}
