using System.Collections.Generic;
using UnityEngine;
using VLAB.PhysicsLab.Common;
using VLAB.PhysicsLab.Interaction;

namespace VLAB.PhysicsLab.Education
{
    public sealed class ExperimentSafetyBoundary : MonoBehaviour
    {
        [SerializeField] private ExperimentResetManager resetManager;
        [SerializeField] private Vector3 centerOffset = new Vector3(0f, 1.2f, 0f);
        [SerializeField] private Vector3 halfExtents = new Vector3(2.8f, 2.2f, 2.2f);
        [SerializeField, Min(0.05f)] private float checkInterval = 0.25f;
        [SerializeField, Min(0f)] private float recoveryDelay = 1.5f;

        private readonly List<LabResettable> trackedObjects = new List<LabResettable>();
        private readonly Dictionary<LabResettable, float> outsideSince = new Dictionary<LabResettable, float>();
        private float nextCheckTime;

        public void Configure(ExperimentResetManager manager, Vector3 localCenter, Vector3 boundsHalfExtents)
        {
            resetManager = manager;
            centerOffset = localCenter;
            halfExtents = new Vector3(
                Mathf.Max(0.1f, boundsHalfExtents.x),
                Mathf.Max(0.1f, boundsHalfExtents.y),
                Mathf.Max(0.1f, boundsHalfExtents.z));
        }

        private void Start()
        {
            RefreshTrackedObjects();
        }

        private void Update()
        {
            if (Time.unscaledTime < nextCheckTime)
            {
                return;
            }
            nextCheckTime = Time.unscaledTime + checkInterval;
            RecoverExpiredObjects(Time.unscaledTime);
        }

        public void RefreshTrackedObjects()
        {
            trackedObjects.Clear();
            outsideSince.Clear();
            trackedObjects.AddRange(GetComponentsInChildren<LabResettable>(true));
            if (trackedObjects.Count == 0 && resetManager != null)
            {
                foreach (var resettable in resetManager.Resettables)
                {
                    if (resettable != null)
                    {
                        trackedObjects.Add(resettable);
                    }
                }
            }
        }

        public void RecoverOutOfBoundsNow()
        {
            if (!PhysicsLabPreferences.AutoReturnTools)
            {
                return;
            }
            var center = transform.TransformPoint(centerOffset);
            var worldHalfExtents = Vector3.Scale(halfExtents, Abs(transform.lossyScale));
            foreach (var resettable in trackedObjects)
            {
                if (resettable != null
                    && !IsHeld(resettable)
                    && !Contains(resettable.transform.position, center, worldHalfExtents))
                {
                    resettable.ResetLabObject();
                }
            }
        }

        private void RecoverExpiredObjects(float now)
        {
            if (!PhysicsLabPreferences.AutoReturnTools)
            {
                outsideSince.Clear();
                return;
            }

            var center = transform.TransformPoint(centerOffset);
            var worldHalfExtents = Vector3.Scale(halfExtents, Abs(transform.lossyScale));
            foreach (var resettable in trackedObjects)
            {
                if (resettable == null || IsHeld(resettable) || Contains(resettable.transform.position, center, worldHalfExtents))
                {
                    if (resettable != null) outsideSince.Remove(resettable);
                    continue;
                }

                if (!outsideSince.TryGetValue(resettable, out var firstSeen))
                {
                    outsideSince[resettable] = now;
                    continue;
                }

                if (now - firstSeen >= recoveryDelay)
                {
                    resettable.ResetLabObject();
                    outsideSince.Remove(resettable);
                }
            }
        }

        private static bool IsHeld(LabResettable resettable)
        {
            var grabbable = resettable.GetComponent<LabGrabbable>();
            return grabbable != null && grabbable.IsHeld;
        }

        public static bool Contains(Vector3 point, Vector3 center, Vector3 boundsHalfExtents)
        {
            var delta = point - center;
            return Mathf.Abs(delta.x) <= Mathf.Abs(boundsHalfExtents.x)
                && Mathf.Abs(delta.y) <= Mathf.Abs(boundsHalfExtents.y)
                && Mathf.Abs(delta.z) <= Mathf.Abs(boundsHalfExtents.z);
        }

        private static Vector3 Abs(Vector3 value) => new Vector3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));
    }
}
