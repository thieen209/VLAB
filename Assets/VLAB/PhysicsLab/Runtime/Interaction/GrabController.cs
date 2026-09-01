using UnityEngine;
using VLAB.Core.Input;

namespace VLAB.PhysicsLab.Interaction
{
    public sealed class GrabController : MonoBehaviour
    {
        [SerializeField] private InputManager inputManager;
        [SerializeField] private Camera viewCamera;
        [SerializeField, Min(0.25f)] private float minimumDistance = 0.5f;
        [SerializeField, Min(0.5f)] private float maximumDistance = 4f;
        [SerializeField, Min(0.1f)] private float distanceStep = 0.25f;
        [SerializeField, Min(10f)] private float rotationSpeed = 90f;

        private IGrabbable held;
        private float holdDistance = 1.5f;
        private Quaternion holdRotation;

        public bool IsHolding => held != null;
        public IGrabbable Held => held;

        public void Configure(InputManager input, Camera camera)
        {
            inputManager = input;
            viewCamera = camera;
        }

        private void OnEnable()
        {
            if (inputManager != null)
            {
                inputManager.DropPressed += Release;
            }
        }

        private void OnDisable()
        {
            if (inputManager != null)
            {
                inputManager.DropPressed -= Release;
            }
            Release();
        }

        private void Update()
        {
            if (held == null || inputManager == null)
            {
                return;
            }

            var state = inputManager.CurrentState;
            if (Mathf.Abs(state.ScrollDelta) > 0.01f)
            {
                holdDistance = Mathf.Clamp(holdDistance + state.ScrollDelta * distanceStep, minimumDistance, maximumDistance);
            }

            if (state.SecondaryPressed)
            {
                var yaw = Quaternion.AngleAxis(state.Look.x * rotationSpeed * 0.01f, Vector3.up);
                var pitch = Quaternion.AngleAxis(-state.Look.y * rotationSpeed * 0.01f, viewCamera != null ? viewCamera.transform.right : Vector3.right);
                holdRotation = yaw * pitch * holdRotation;
            }
        }

        private void FixedUpdate()
        {
            if (held == null || held.Body == null || viewCamera == null)
            {
                return;
            }

            var targetPosition = viewCamera.transform.position + viewCamera.transform.forward * holdDistance;
            held.Body.MovePosition(targetPosition);
            held.Body.MoveRotation(holdRotation);
        }

        public bool TryGrab(IGrabbable candidate, Vector3 hitPoint)
        {
            if (candidate == null || !candidate.CanGrab || candidate.Body == null)
            {
                return false;
            }

            Release();
            held = candidate;
            holdDistance = viewCamera != null
                ? Mathf.Clamp(Vector3.Distance(viewCamera.transform.position, hitPoint), minimumDistance, maximumDistance)
                : 1.5f;
            holdRotation = candidate.InteractionTransform.rotation;
            candidate.OnGrabbed(transform);
            return true;
        }

        public void Release()
        {
            if (held == null)
            {
                return;
            }

            var released = held;
            held = null;
            released.OnReleased();
        }
    }
}
