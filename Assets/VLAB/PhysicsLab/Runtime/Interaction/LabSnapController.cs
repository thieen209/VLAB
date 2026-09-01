using System;
using System.Collections.Generic;
using UnityEngine;
using VLAB.PhysicsLab.Common;

namespace VLAB.PhysicsLab.Interaction
{
    public sealed class LabSnapController : MonoBehaviour
    {
        [SerializeField, Min(0.02f)] private float snapRadius = 0.24f;

        private readonly List<LabAttachmentPoint> points = new List<LabAttachmentPoint>();
        private readonly List<LabGrabbable> grabbables = new List<LabGrabbable>();

        public event Action<LabAttachmentPoint, LabAttachment> Snapped;

        private void Start()
        {
            Refresh();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        public void Configure(float radius)
        {
            snapRadius = Mathf.Max(0.02f, radius);
        }

        public void Refresh()
        {
            Unsubscribe();
            points.Clear();
            grabbables.Clear();
            points.AddRange(GetComponentsInChildren<LabAttachmentPoint>(true));
            grabbables.AddRange(GetComponentsInChildren<LabGrabbable>(true));
            if (Application.isPlaying)
            {
                Subscribe();
            }
        }

        public bool TrySnap(LabGrabbable grabbable)
        {
            if (grabbable == null)
            {
                return false;
            }
            var attachment = grabbable.GetComponent<LabAttachment>();
            if (attachment == null)
            {
                return false;
            }

            LabAttachmentPoint closest = null;
            var closestDistance = snapRadius;
            foreach (var point in points)
            {
                if (point == null || !point.CanAccept(attachment))
                {
                    continue;
                }
                var distance = Vector3.Distance(point.transform.position, attachment.transform.position);
                if (distance <= closestDistance)
                {
                    closest = point;
                    closestDistance = distance;
                }
            }
            if (closest == null || !closest.TryAttach(attachment))
            {
                return false;
            }
            Snapped?.Invoke(closest, attachment);
            return true;
        }

        private void Subscribe()
        {
            foreach (var grabbable in grabbables)
            {
                if (grabbable == null) continue;
                grabbable.Grabbed += HandleGrabbed;
                grabbable.Released += HandleReleased;
            }
        }

        private void Unsubscribe()
        {
            foreach (var grabbable in grabbables)
            {
                if (grabbable == null) continue;
                grabbable.Grabbed -= HandleGrabbed;
                grabbable.Released -= HandleReleased;
            }
        }

        private void HandleGrabbed(LabGrabbable grabbable)
        {
            var attachment = grabbable != null ? grabbable.GetComponent<LabAttachment>() : null;
            if (attachment == null) return;
            foreach (var point in points)
            {
                if (point != null && point.Current == attachment)
                {
                    point.Detach();
                    break;
                }
            }
        }

        private void HandleReleased(LabGrabbable grabbable) => TrySnap(grabbable);
    }
}
