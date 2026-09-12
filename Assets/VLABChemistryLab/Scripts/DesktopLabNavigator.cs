using UnityEngine;
using VLAB.ChemistryLab.Input;
using UnityEngine.InputSystem;
using VLAB.ChemistryLab.Interaction;
using VLAB.Core.Input;

namespace VLAB.ChemistryLab
{
    /// <summary>Grounded mouse-and-keyboard movement for desktop and simulated-VR play.</summary>
    public sealed class DesktopLabNavigator : MonoBehaviour
    {
        [SerializeField] private float gravity = -20f;
        [SerializeField] private float floorEyeHeight = 1.70f;
        [SerializeField] private float seatedEyeHeight = 1.08f;
        [SerializeField] private float zoomSpeed = 9f;
        [SerializeField] private float minFieldOfView = 32f;
        [SerializeField] private float maxFieldOfView = 64f;
        [SerializeField] private float roomHalfExtent = 7.55f;
        private float yaw;
        private float pitch;
        private CharacterController body;
        private Camera playerCamera;
        private float verticalVelocity;
        private float currentEyeHeight;
        private IVLabDesktopInputSource inputSource;
        private DesktopTitrationInterface desktopInterface;
        private GameObject bodyObject;
        private DesktopLabGrabber grabber;
        private LabBuretteTap activeTap;
        private Transform locomotionRoot;
        private readonly VLabComfortTurn comfortTurn = new VLabComfortTurn();
        public VLAB.Core.Input.InputManager SharedInput { get; set; }
        public bool ExperimentInputSuspended { get; set; }
        private bool previousSharedPress;
        private bool previousSharedUse;
        private VLAB.Core.Input.IVLABInputProvider previousSharedProvider;
        private UnityEngine.EventSystems.PointerEventData sharedPointer;
        private readonly System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult> sharedUiHits=new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>(16);

        private void Awake()
        {
            CharacterController legacyBody = GetComponent<CharacterController>();
            if (legacyBody != null)
            {
                // The view can pitch; a player capsule must remain upright in world space.
                bodyObject = new GameObject("VLAB Desktop Upright Body");
                bodyObject.transform.position = transform.position;
                bodyObject.layer = gameObject.layer;
                body = bodyObject.AddComponent<CharacterController>();
                body.radius = legacyBody.radius;
                body.height = legacyBody.height;
                body.center = legacyBody.center;
                body.stepOffset = legacyBody.stepOffset;
                body.skinWidth = legacyBody.skinWidth;
                body.slopeLimit = legacyBody.slopeLimit;
                legacyBody.enabled = false;
            }
            playerCamera = GetComponent<Camera>();
            grabber = GetComponent<DesktopLabGrabber>() ?? gameObject.AddComponent<DesktopLabGrabber>();
            currentEyeHeight = floorEyeHeight;
            Vector3 angles = transform.eulerAngles;
            yaw = angles.y;
            pitch = angles.x > 180f ? angles.x - 360f : angles.x;
            DesktopInputActionSource actionSource = GetComponent<DesktopInputActionSource>();
            if (actionSource == null)
                actionSource = gameObject.AddComponent<DesktopInputActionSource>();
            inputSource = actionSource;
            desktopInterface = Object.FindAnyObjectByType<DesktopTitrationInterface>();
            locomotionRoot = VLabComfortLocomotion.CreateViewRoot(playerCamera, "VLAB Chemistry Locomotion Root");
        }

        private void OnEnable()
        {
            if (bodyObject != null) bodyObject.SetActive(true);
        }

        private void OnDisable()
        {
            StopTap();
            previousSharedPress = previousSharedUse = false;
            previousSharedProvider = null;
            comfortTurn.Reset();
            if (bodyObject != null) bodyObject.SetActive(false);
        }

        private void StopTap() { if (activeTap != null) activeTap.EndDesktopHold(); activeTap = null; }
        private void OnApplicationFocus(bool focused) { if (!focused) StopTap(); }

        private void OnDestroy()
        {
            if (bodyObject != null) Destroy(bodyObject);
        }

        private void Update()
        {
            VLabDesktopInputFrame frame = inputSource == null ? default : inputSource.ReadFrame();
            bool phone = VLAB.Core.Input.VLabHeadPose.PhoneViewer;
            if (phone)
            {
                var gamepad = Gamepad.current;
                frame = new VLabDesktopInputFrame
                {
                    Move = gamepad?.leftStick.ReadValue() ?? Vector2.zero,
                    Look = gamepad?.rightStick.ReadValue() ?? Vector2.zero,
                    PrimaryHeld = VLAB.Core.Input.PhoneInputProvider.ViewerTouchHeld || gamepad?.buttonSouth.isPressed == true,
                    PrimaryPressed = VLAB.Core.Input.PhoneInputProvider.ViewerTouchPressed || gamepad?.buttonSouth.wasPressedThisFrame == true,
                    UsePressed = gamepad?.buttonWest.wasPressedThisFrame == true,
                    TiltHeld = gamepad?.rightShoulder.isPressed == true
                };
                frame.PointerOverScrollableUi = UnityEngine.EventSystems.EventSystem.current?.IsPointerOverGameObject() == true;
            }
            bool controller=SharedInput!=null && SharedInput.HasRayProvider;
            bool mobile = phone || (SharedInput != null && SharedInput.Provider is PhoneInputProvider);
            bool analog = controller || mobile;
            if(SharedInput != null && analog)
            {
                frame = ReadSharedControllerFrame();
            }
            else { previousSharedPress = previousSharedUse = false; previousSharedProvider = null; }
            if (frame.CancelPressed)
            {
                desktopInterface?.TogglePanel();
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            if (!phone) frame.PointerOverScrollableUi = desktopInterface != null && desktopInterface.IsPointerOverScrollableUi;
            if(UnityEngine.EventSystems.EventSystem.current!=null)
            {
                if(sharedPointer==null)sharedPointer=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
                var point = PointerPosition();
                var uiRay=SharedInput!=null?SharedInput.PointerRay(playerCamera,point):playerCamera.ScreenPointToRay(point);
                frame.PointerOverScrollableUi |= VLAB.Core.Input.VLabPointerUi.Raycast(uiRay,sharedPointer,sharedUiHits).gameObject!=null;
            }
            if (!frame.PrimaryHeld || frame.CancelPressed || frame.PointerOverScrollableUi) StopTap();
            bool wasHolding = grabber != null && grabber.IsHolding;
            if (!ExperimentInputSuspended && frame.PrimaryPressed && !frame.CancelPressed && !frame.PointerOverScrollableUi && playerCamera != null && (analog || Mouse.current != null))
            {
                var point = PointerPosition();
                Ray ray = SharedInput!=null ? SharedInput.PointerRay(playerCamera,point) : phone ? new Ray(transform.position, transform.forward) : playerCamera.ScreenPointToRay(point);
                if (wasHolding) grabber.Release();
                else if (Physics.Raycast(ray, out RaycastHit hit, 8f, ~0, QueryTriggerInteraction.Ignore) && !grabber.TryGrab(hit.collider))
                {
                    activeTap = hit.collider.GetComponentInParent<LabBuretteTap>();
                    if (activeTap != null) activeTap.BeginDesktopHold();
                    else hit.collider.GetComponentInParent<ILabCommandTarget>()?.TryActivate();
                }
            }
            if(!ExperimentInputSuspended)grabber?.ProcessInput(frame);
            if (!phone && !wasHolding && (grabber == null || !grabber.IsHolding)) UpdateZoom(frame);
            UpdateSeatedHeight(frame);

            var head = playerCamera.GetComponent<VLabHeadPose>();
            if (analog)
            {
                VLabComfortLocomotion.TurnRoot(locomotionRoot, transform, comfortTurn.Step(frame.Look.x, Time.deltaTime));
            }
            else if (frame.LookHeld && (head == null || !head.OwnsRotation))
            {
                comfortTurn.Reset();
                yaw = transform.eulerAngles.y;
                pitch = transform.eulerAngles.x;
                if (pitch > 180) pitch -= 360;
                yaw += frame.Look.x * LabPreferences.Current.lookSensitivity;
                pitch = Mathf.Clamp(pitch - frame.Look.y * LabPreferences.Current.lookSensitivity, -80f, 80f);
                transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            }

            Vector3 horizontalMove = VLabComfortLocomotion.PlanarVelocity(transform.forward, frame.Move, frame.RunHeld);

            if (body != null)
            {
                bool onFloor = body.isGrounded || transform.position.y <= currentEyeHeight + .01f;
                if (onFloor && verticalVelocity < 0f) verticalVelocity = -2f;
                verticalVelocity += gravity * Time.deltaTime;
                Vector3 motion = horizontalMove + Vector3.up * verticalVelocity;
                body.Move(motion * Time.deltaTime);
                VLabComfortLocomotion.MoveRootToViewPosition(locomotionRoot, transform, body.transform.position);
                // The lab has one flat floor. This fallback prevents any physics/import
                // mismatch from ever letting the player fall below it.
                if (transform.position.y < currentEyeHeight)
                {
                    Vector3 safePosition = transform.position;
                    safePosition.y = currentEyeHeight;
                    VLabComfortLocomotion.MoveRootToViewPosition(locomotionRoot, transform, safePosition);
                    verticalVelocity = 0f;
                }
            }

            // Keep the player on the enlarged laboratory floor even where a
            // wall is intentionally open for the desktop camera's starting view.
            Vector3 boundedPosition = transform.position;
            boundedPosition.x = Mathf.Clamp(boundedPosition.x, -roomHalfExtent, roomHalfExtent);
            boundedPosition.z = Mathf.Clamp(boundedPosition.z, -roomHalfExtent, roomHalfExtent);
            VLabComfortLocomotion.MoveRootToViewPosition(locomotionRoot, transform, boundedPosition);
            if (body != null) body.transform.position = boundedPosition;
        }

        private Vector2 PointerPosition()
        {
            var touch = Touchscreen.current?.primaryTouch;
            if (!VLabHeadPose.PhoneViewer && touch != null && (touch.press.isPressed || touch.press.wasReleasedThisFrame)) return touch.position.ReadValue();
            return Mouse.current?.position.ReadValue() ?? new Vector2(Screen.width * .5f, Screen.height * .5f);
        }

        private VLabDesktopInputFrame ReadSharedControllerFrame()
        {
            if (!ReferenceEquals(previousSharedProvider, SharedInput.Provider))
            {
                previousSharedPress = previousSharedUse = false;
                previousSharedProvider = SharedInput.Provider;
            }
            var state = SharedInput.CurrentState;
            var frame = new VLabDesktopInputFrame
            {
                Move = state.Move, Look = state.Look, RunHeld = state.SprintPressed, PrimaryHeld = state.PrimaryPressed,
                PrimaryPressed = state.PrimaryPressed && !previousSharedPress,
                UsePressed = state.SecondaryPressed && !previousSharedUse,
                TiltHeld = state.RightGrabPressed, ZoomDelta = state.ScrollDelta
            };
            previousSharedPress = state.PrimaryPressed;
            previousSharedUse = state.SecondaryPressed;
            return frame;
        }

        private void UpdateZoom(VLabDesktopInputFrame frame)
        {
            if (playerCamera == null) return;
            float wheel = VLabDesktopInputPolicy.FilterZoom(frame);
            if (Mathf.Abs(wheel) > .001f)
                playerCamera.fieldOfView = Mathf.Clamp(playerCamera.fieldOfView - wheel * zoomSpeed, minFieldOfView, maxFieldOfView);
        }

        private void UpdateSeatedHeight(VLabDesktopInputFrame frame)
        {
            // Hold C to sit/crouch. The capsule bottom remains on the floor so
            // this never reintroduces the falling-through-floor issue.
            float targetHeight = frame.CrouchHeld ? seatedEyeHeight : floorEyeHeight;
            currentEyeHeight = Mathf.MoveTowards(currentEyeHeight, targetHeight, 3.8f * Time.deltaTime);
            if (body != null)
            {
                body.height = currentEyeHeight;
                body.center = new Vector3(0f, -currentEyeHeight * .5f, 0f);
            }
            Vector3 seatedPosition = transform.position;
            seatedPosition.y = currentEyeHeight;
            VLabComfortLocomotion.MoveRootToViewPosition(locomotionRoot, transform, seatedPosition);
            if (body != null) body.transform.position = seatedPosition;
            // Sync the resized capsule and its eye-origin transform before CharacterController.Move.
            // Otherwise its sweep can use the previous standing geometry while crouching.
            if (body != null) Physics.SyncTransforms();
        }
    }
}
