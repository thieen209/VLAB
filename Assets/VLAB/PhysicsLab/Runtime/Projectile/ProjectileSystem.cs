using UnityEngine;

namespace VLAB.PhysicsLab.Projectile
{
    public readonly struct ProjectileTrajectorySample
    {
        public ProjectileTrajectorySample(float flightTimeSeconds, float rangeMetres, float maximumHeightMetres)
        {
            FlightTimeSeconds = flightTimeSeconds;
            RangeMetres = rangeMetres;
            MaximumHeightMetres = maximumHeightMetres;
        }

        public float FlightTimeSeconds { get; }
        public float RangeMetres { get; }
        public float MaximumHeightMetres { get; }
    }

    public static class ProjectileMath
    {
        public static Vector3 CalculateVelocity(float angleDegrees, float speedMetresPerSecond, Vector3 horizontalForward, Vector3 up)
        {
            var upDirection = up.sqrMagnitude > 0f ? up.normalized : Vector3.up;
            var horizontal = Vector3.ProjectOnPlane(horizontalForward, upDirection).normalized;
            if (horizontal.sqrMagnitude < 0.001f)
            {
                horizontal = Vector3.forward;
            }
            var radians = Mathf.Clamp(angleDegrees, 0f, 90f) * Mathf.Deg2Rad;
            return (horizontal * Mathf.Cos(radians) + upDirection * Mathf.Sin(radians)) * Mathf.Max(0f, speedMetresPerSecond);
        }

        public static ProjectileTrajectorySample CalculateTrajectory(float speedMetresPerSecond, float angleDegrees, float gravity = 9.80665f)
        {
            if (speedMetresPerSecond <= 0f || gravity <= 0f)
            {
                return new ProjectileTrajectorySample(0f, 0f, 0f);
            }
            var radians = Mathf.Clamp(angleDegrees, 0f, 90f) * Mathf.Deg2Rad;
            var verticalSpeed = speedMetresPerSecond * Mathf.Sin(radians);
            var horizontalSpeed = speedMetresPerSecond * Mathf.Cos(radians);
            var flightTime = 2f * verticalSpeed / gravity;
            return new ProjectileTrajectorySample(
                flightTime,
                horizontalSpeed * flightTime,
                verticalSpeed * verticalSpeed / (2f * gravity));
        }
    }
}
