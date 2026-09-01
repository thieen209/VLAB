using System;
using UnityEngine;
using UnityEngine.InputSystem;
using VLAB.PhysicsLab.Common;

namespace VLAB.PhysicsLab.Projectile
{
    [RequireComponent(typeof(LabInteractable))]
    public sealed class LauncherAngleManipulator : MonoBehaviour
    {
        [SerializeField] private ProjectileLauncher launcher;
        [SerializeField, Min(0.01f)] private float mouseDegreesPerPixel = 0.12f;
        [SerializeField, Min(0.1f)] private float scrollDegrees = 2f;
        private LabInteractable interactable;

        public event Action<float> AngleChanged;

        public void Configure(ProjectileLauncher target) => launcher = target;

        private void Awake() => interactable = GetComponent<LabInteractable>();

        private void Update()
        {
            if (launcher == null || interactable == null || !interactable.IsInteracting || Mouse.current == null)
            {
                return;
            }
            var delta = -Mouse.current.delta.ReadValue().y * mouseDegreesPerPixel
                + Mouse.current.scroll.ReadValue().y / 120f * scrollDegrees;
            if (Mathf.Abs(delta) < 0.01f)
            {
                return;
            }
            launcher.SetLaunchAngle(launcher.LaunchAngle + delta);
            AngleChanged?.Invoke(launcher.LaunchAngle);
        }
    }
}
