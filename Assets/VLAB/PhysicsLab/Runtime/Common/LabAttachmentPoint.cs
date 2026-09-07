using System;
using UnityEngine;

namespace VLAB.PhysicsLab.Common
{
    public sealed class LabAttachmentPoint : MonoBehaviour
    {
        [SerializeField] private string acceptedType = "generic";
        [SerializeField] private bool snapToPoint = true;
        [SerializeField] private bool freezeAttachedRigidbody = true;
        [SerializeField] private LabAttachment current;

        private Rigidbody attachedRigidbody;
        private bool previousKinematic;

        public event Action<LabAttachment> Attached;
        public event Action<LabAttachment> Detached;

        public LabAttachment Current => current;
        public bool IsOccupied => current != null;
        public string AcceptedType => acceptedType;

        public bool CanAccept(LabAttachment attachment) => attachment != null
            && (current == null || current == attachment)
            && string.Equals(attachment.AttachmentType, acceptedType, StringComparison.OrdinalIgnoreCase);

        public void Configure(string type, bool snap, bool freezeRigidbody)
        {
            acceptedType = string.IsNullOrWhiteSpace(type) ? "generic" : type;
            snapToPoint = snap;
            freezeAttachedRigidbody = freezeRigidbody;
        }

        public bool TryAttach(LabAttachment attachment)
        {
            if (!CanAccept(attachment) || current == attachment)
            {
                return false;
            }

            current = attachment;
            attachedRigidbody = attachment.GetComponent<Rigidbody>();
            if (attachedRigidbody != null && freezeAttachedRigidbody)
            {
                previousKinematic = attachedRigidbody.isKinematic;
                if (!attachedRigidbody.isKinematic)
                {
                    attachedRigidbody.linearVelocity = Vector3.zero;
                    attachedRigidbody.angularVelocity = Vector3.zero;
                }
                attachedRigidbody.isKinematic = true;
            }

            attachment.transform.SetParent(transform, true);
            if (snapToPoint)
            {
                attachment.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            }

            Attached?.Invoke(attachment);
            return true;
        }

        public LabAttachment Detach()
        {
            if (current == null)
            {
                return null;
            }

            var detached = current;
            detached.transform.SetParent(null, true);
            if (attachedRigidbody != null && freezeAttachedRigidbody)
            {
                attachedRigidbody.isKinematic = previousKinematic;
            }

            current = null;
            attachedRigidbody = null;
            Detached?.Invoke(detached);
            return detached;
        }
    }
}
