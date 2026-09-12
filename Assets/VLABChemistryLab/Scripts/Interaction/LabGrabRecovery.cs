using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace VLAB.ChemistryLab.Interaction
{
    [RequireComponent(typeof(Rigidbody), typeof(XRGrabInteractable))]
    public sealed class LabGrabRecovery : MonoBehaviour
    {
        [SerializeField, Min(.1f)] private float recoveryDelay = .75f;
        [SerializeField, Min(.1f)] private float maximumThrowSpeed = 3f;
        private Rigidbody body;
        private XRGrabInteractable grab;
        private Vector3 spawnPosition;
        private Quaternion spawnRotation;
        private float lostSince = -1;
        public bool IsHeld => grab != null && grab.isSelected;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            grab = GetComponent<XRGrabInteractable>();
            spawnPosition = body.position;
            spawnRotation = body.rotation;
        }

        private void FixedUpdate()
        {
            if (IsHeld) { lostSince = -1; return; }
            if (!body.isKinematic)
            {
                body.linearVelocity = LabInteractionSafety.ClampThrow(body.linearVelocity, maximumThrowSpeed);
                body.angularVelocity = LabInteractionSafety.ClampThrow(body.angularVelocity, 12f);
            }
            if (!LabInteractionSafety.ShouldRecover(body.position, false)) { lostSince = -1; return; }
            if (lostSince < 0) lostSince = Time.unscaledTime;
            if (Time.unscaledTime - lostSince >= recoveryDelay) RecoverToSpawn();
        }

        public bool RecoverToSpawn()
        {
            if (IsHeld || body == null) return false;
            if (!body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
            body.position = spawnPosition;
            body.rotation = spawnRotation;
            body.Sleep();
            lostSince = -1;
            return true;
        }
    }
}
