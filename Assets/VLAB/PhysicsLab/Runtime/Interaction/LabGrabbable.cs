using UnityEngine;
using VLAB.PhysicsLab.Common;

namespace VLAB.PhysicsLab.Interaction
{
    public enum LabReleaseMode { RestoreStagedState, Dynamic, DynamicNoGravity, KeepKinematic }

    [RequireComponent(typeof(Rigidbody), typeof(LabInteractable))]
    public sealed class LabGrabbable : MonoBehaviour, IGrabbable
    {
        [SerializeField] private Rigidbody body;
        [SerializeField] private LabInteractable interactable;
        [SerializeField] private bool grabEnabled = true;
        [SerializeField] private LabReleaseMode releaseMode = LabReleaseMode.RestoreStagedState;

        private bool previousKinematic;
        private bool previousUseGravity;

        public bool CanGrab => grabEnabled && CanInteract;
        public bool CanInteract => interactable != null && interactable.CanInteract;
        public Transform InteractionTransform => transform;
        public Rigidbody Body => body;
        public bool IsHeld { get; private set; }

        public event System.Action<LabGrabbable> Grabbed;
        public event System.Action<LabGrabbable> Released;

        private void Awake()
        {
            body = body != null ? body : GetComponent<Rigidbody>();
            interactable = interactable != null ? interactable : GetComponent<LabInteractable>();
        }

        public void Configure(bool enabled, bool stagedKinematic)
        {
            Configure(enabled, stagedKinematic, LabReleaseMode.RestoreStagedState);
        }

        public void Configure(bool enabled, bool stagedKinematic, LabReleaseMode mode)
        {
            grabEnabled = enabled;
            releaseMode = mode;
            body = body != null ? body : GetComponent<Rigidbody>();
            if (body != null && stagedKinematic)
            {
                body.isKinematic = true;
                body.useGravity = false;
            }
        }

        public void SetHighlighted(bool highlighted) => interactable?.SetHighlighted(highlighted);
        public void BeginInteraction() => interactable?.BeginInteraction();
        public void EndInteraction() => interactable?.EndInteraction();

        public void OnGrabbed(Transform grabAnchor)
        {
            body = body != null ? body : GetComponent<Rigidbody>();
            interactable = interactable != null ? interactable : GetComponent<LabInteractable>();
            if (body == null)
            {
                return;
            }

            previousKinematic = body.isKinematic;
            previousUseGravity = body.useGravity;
            if (!body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
            body.useGravity = false;
            body.isKinematic = true;
            IsHeld = true;
            BeginInteraction();
            Grabbed?.Invoke(this);
        }

        public void NotifyExternalGrab(Transform grabAnchor)
        {
            IsHeld = true;
            BeginInteraction();
            Grabbed?.Invoke(this);
        }

        public void OnReleased()
        {
            IsHeld = false;
            if (body != null)
            {
                var dynamicRelease = releaseMode == LabReleaseMode.Dynamic || releaseMode == LabReleaseMode.DynamicNoGravity;
                body.isKinematic = dynamicRelease ? false
                    : releaseMode == LabReleaseMode.KeepKinematic ? true
                    : previousKinematic;
                body.useGravity = releaseMode == LabReleaseMode.Dynamic ? true
                    : releaseMode == LabReleaseMode.DynamicNoGravity ? false
                    : releaseMode == LabReleaseMode.KeepKinematic ? false
                    : previousUseGravity;
                if (!dynamicRelease && !body.isKinematic)
                {
                    body.linearVelocity = Vector3.zero;
                    body.angularVelocity = Vector3.zero;
                }
                else if (dynamicRelease)
                {
                    body.linearVelocity = Vector3.ClampMagnitude(body.linearVelocity, 3f);
                    body.angularVelocity = Vector3.ClampMagnitude(body.angularVelocity, 8f);
                }
            }
            EndInteraction();
            Released?.Invoke(this);
        }

        public void NotifyExternalRelease() => OnReleased();
    }
}
