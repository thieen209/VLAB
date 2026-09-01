using System;
using UnityEngine;
using VLAB.PhysicsLab.Common;

namespace VLAB.PhysicsLab.Projectile
{
    public sealed class ProjectileLauncher : ExperimentObject
    {
        [SerializeField] private Transform barrelPivot;
        [SerializeField] private Transform launchOrigin;
        [SerializeField] private Transform projectileSocket;
        [SerializeField, Range(0f, 80f)] private float launchAngle = 30f;
        [SerializeField, Range(0f, 30f)] private float initialVelocity = 8f;
        [SerializeField] private ProjectileBall loadedProjectile;

        public event Action<ProjectileBall, Vector3> ProjectileLaunched;

        public float LaunchAngle => launchAngle;
        public float InitialVelocity => initialVelocity;
        public bool IsLoaded => loadedProjectile != null;
        public event Action<ProjectileBall> ProjectileLoaded;

        public void Configure(Transform pivot, Transform origin, Transform socket)
        {
            barrelPivot = pivot;
            launchOrigin = origin;
            projectileSocket = socket;
            ApplyAngleVisual();
        }

        public void SetLaunchAngle(float degrees)
        {
            launchAngle = Mathf.Clamp(degrees, 0f, 80f);
            ApplyAngleVisual();
        }

        public void SetInitialVelocity(float metresPerSecond)
        {
            initialVelocity = Mathf.Clamp(metresPerSecond, 0f, 30f);
        }

        public bool LoadProjectile(ProjectileBall projectile)
        {
            if (projectile == null || loadedProjectile != null)
            {
                return false;
            }
            loadedProjectile = projectile;
            var socket = projectileSocket != null ? projectileSocket : transform;
            projectile.transform.SetParent(socket, false);
            projectile.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            projectile.Body.isKinematic = true;
            projectile.Body.linearVelocity = Vector3.zero;
            projectile.Body.angularVelocity = Vector3.zero;
            ProjectileLoaded?.Invoke(projectile);
            return true;
        }

        public ProjectileBall Launch()
        {
            if (loadedProjectile == null)
            {
                return null;
            }
            var projectile = loadedProjectile;
            loadedProjectile = null;
            projectile.transform.SetParent(null, true);
            var origin = launchOrigin != null ? launchOrigin : transform;
            var velocity = ProjectileMath.CalculateVelocity(launchAngle, initialVelocity, origin.forward, origin.up);
            projectile.BeginFlight(origin.position, velocity, Time.timeAsDouble);
            ProjectileLaunched?.Invoke(projectile, velocity);
            return projectile;
        }

        protected override void OnResetLabObject()
        {
            base.OnResetLabObject();
            if (loadedProjectile != null)
            {
                loadedProjectile.ResetLabObject();
            }
        }

        private void ApplyAngleVisual()
        {
            if (barrelPivot != null)
            {
                barrelPivot.localRotation = Quaternion.Euler(-launchAngle, 0f, 0f);
            }
        }
    }
}
