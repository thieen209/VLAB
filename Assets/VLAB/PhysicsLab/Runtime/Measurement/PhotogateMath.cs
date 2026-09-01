using UnityEngine;

namespace VLAB.PhysicsLab.Measurement
{
    public static class PhotogateMath
    {
        public static float SpeedFromFlag(float flagWidthMetres, float blockedDurationSeconds)
        {
            return flagWidthMetres > 0f && blockedDurationSeconds > 0f
                ? flagWidthMetres / blockedDurationSeconds
                : 0f;
        }

        public static float Acceleration(float initialSpeed, float finalSpeed, float distanceMetres)
        {
            return distanceMetres > 0f
                ? (finalSpeed * finalSpeed - initialSpeed * initialSpeed) / (2f * distanceMetres)
                : 0f;
        }
    }
}
