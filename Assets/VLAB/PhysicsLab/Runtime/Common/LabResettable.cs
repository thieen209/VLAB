using UnityEngine;
using VLAB.PhysicsLab.Interaction;

namespace VLAB.PhysicsLab.Common
{
    public class LabResettable : MonoBehaviour, IResettable
    {
        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private Vector3 initialScale;
        private bool initialActive;
        private Transform initialParent;
        private bool captured;
        private Rigidbody cachedRigidbody;
        private bool initialKinematic;

        protected virtual void Awake()
        {
            CaptureResetState();
        }

        public virtual void CaptureResetState()
        {
            initialPosition = transform.position;
            initialParent = transform.parent;
            initialRotation = transform.rotation;
            initialScale = transform.localScale;
            initialActive = gameObject.activeSelf;
            cachedRigidbody = GetComponent<Rigidbody>();
            initialKinematic = cachedRigidbody != null && cachedRigidbody.isKinematic;
            captured = true;
            OnCaptureResetState();
        }

        public virtual void ResetLabObject()
        {
            if (!captured)
            {
                CaptureResetState();
            }

            gameObject.SetActive(initialActive);
            transform.SetParent(initialParent, true);
            transform.SetPositionAndRotation(initialPosition, initialRotation);
            transform.localScale = initialScale;
            if (cachedRigidbody != null)
            {
                cachedRigidbody.isKinematic = initialKinematic;
                cachedRigidbody.linearVelocity = Vector3.zero;
                cachedRigidbody.angularVelocity = Vector3.zero;
                cachedRigidbody.position = initialPosition;
                cachedRigidbody.rotation = initialRotation;
                cachedRigidbody.Sleep();
            }

            OnResetLabObject();
        }

        protected virtual void OnCaptureResetState() { }
        protected virtual void OnResetLabObject() { }
    }
}
