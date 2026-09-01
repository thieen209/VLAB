using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR;
using VLAB.Core.Input;
using VLAB.PhysicsLab.Education;

namespace VLAB.PhysicsLab.Interaction
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class DesktopPlayerRig : MonoBehaviour
    {
        [SerializeField] private InputManager inputManager;
        [SerializeField] private Camera viewCamera;
        [SerializeField] private GrabController grabController;
        [SerializeField, Min(0.5f)] private float walkSpeed = 2.8f;
        [SerializeField, Min(0.5f)] private float sprintSpeed = 4.5f;
        [SerializeField, Range(45f, 89f)] private float pitchLimit = 82f;
        [SerializeField] private float gravity = -9.81f;

        private CharacterController controller;
        private float verticalVelocity;
        private float pitch;

        public bool IsCursorLocked { get; private set; }
        public Camera ViewCamera => viewCamera;

        public void Configure(InputManager input, Camera camera, GrabController grab)
        {
            inputManager = input;
            viewCamera = camera;
            grabController = grab;
        }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            SetCursorLocked(!XRSettings.isDeviceActive);
        }

        private void OnEnable()
        {
            if (inputManager != null)
            {
                inputManager.PausePressed += ReleaseCursor;
            }
        }

        private void OnDisable()
        {
            if (inputManager != null)
            {
                inputManager.PausePressed -= ReleaseCursor;
            }
        }

        private void Update()
        {
            if (inputManager == null || viewCamera == null || XRSettings.isDeviceActive)
            {
                return;
            }

            var state = inputManager.CurrentState;
            if (!IsCursorLocked && state.SecondaryPressed && !IsPointerOverUi())
            {
                SetCursorLocked(true);
            }

            if (IsCursorLocked)
            {
                if (grabController == null || !grabController.IsHolding || !state.SecondaryPressed)
                {
                    ApplyLook(state.Look);
                }
                ApplyMovement(state);
            }
        }

        public void SetCursorLocked(bool locked)
        {
            IsCursorLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        public void ReleaseCursor()
        {
            SetCursorLocked(false);
        }

        private void ApplyLook(Vector2 delta)
        {
            var sensitivity = PhysicsLabPreferences.MouseSensitivity;
            transform.Rotate(Vector3.up, delta.x * sensitivity, Space.World);
            pitch = Mathf.Clamp(pitch - delta.y * sensitivity, -pitchLimit, pitchLimit);
            viewCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        private void ApplyMovement(VLABInputState state)
        {
            if (controller == null)
            {
                return;
            }

            var speed = state.SprintPressed ? sprintSpeed : walkSpeed;
            var planar = transform.right * state.Move.x + transform.forward * state.Move.y;
            if (controller.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -1.5f;
            }
            verticalVelocity += gravity * Time.deltaTime;
            controller.Move((planar * speed + Vector3.up * verticalVelocity) * Time.deltaTime);
        }

        private static bool IsPointerOverUi() =>
            EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}
