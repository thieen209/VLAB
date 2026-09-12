using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using VLAB.Core.Input;

namespace VLAB.MainMenu
{
    /// <summary>Scene-owned viewer support, independent of lab simulation and controller transport.</summary>
    public sealed class VLabViewerRuntime : MonoBehaviour
    {
        public VLabHeadPose Head { get; private set; }
        private VLABApplicationUI app;
#if UNITY_EDITOR && ENABLE_VR
        private GameObject suspendedMenuSimulator;
        private void ConfigureEditorMenu(Camera camera)
        {
            if(gameObject.scene.name!=VLABMenuBootstrap.MenuScene || UnityEngine.XR.XRSettings.isDeviceActive)return;
            var simulator=UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation.XRInteractionSimulator.instance;
            if(simulator!=null && simulator.gameObject.activeSelf)
            { suspendedMenuSimulator=simulator.gameObject;suspendedMenuSimulator.SetActive(false); }
            var origin=camera.GetComponentInParent<Unity.XR.CoreUtils.XROrigin>();
            if(origin!=null)
            {
                foreach(var renderer in origin.GetComponentsInChildren<Renderer>(true))renderer.enabled=false;
                foreach(var interactor in origin.GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor>(true))interactor.enabled=false;
            }
            if(EventSystem.current!=null)
                foreach(var module in EventSystem.current.GetComponents<UnityEngine.XR.Interaction.Toolkit.UI.XRUIInputModule>())
                { module.enableMouseInput=true;module.enableTouchInput=true; }
        }
        private void OnDestroy()
        {
            if(suspendedMenuSimulator!=null)suspendedMenuSimulator.SetActive(true);
        }
#endif
        private IEnumerator Start()
        {
            app = GetComponent<VLABApplicationUI>();
            yield return null;
            var camera = Camera.main;
            if (camera == null) yield break;
#if UNITY_EDITOR && ENABLE_VR
            ConfigureEditorMenu(camera);
#endif
            Head = camera.GetComponent<VLabHeadPose>() ?? camera.gameObject.AddComponent<VLabHeadPose>();
            Head.SimulateInEditor = gameObject.scene.name == VLABMenuBootstrap.MenuScene && !UnityEngine.XR.XRSettings.isDeviceActive;
            bool mobileInput = VLabHeadPose.PhoneViewer;
#if UNITY_ANDROID && !UNITY_EDITOR
            mobileInput = true;
#endif
            foreach (var behaviour in camera.GetComponents<Behaviour>())
                if ((Head.SimulateInEditor || mobileInput) && behaviour.GetType().Name == "TrackedPoseDriver") behaviour.enabled = false;
            if (mobileInput)
                foreach (var input in FindObjectsByType<VLAB.Core.Input.InputManager>())
                    if (!input.HasProvider || input.ActiveProviderName.ToLowerInvariant().Contains("simulator") || input.ActiveProviderName.ToLowerInvariant().Contains("desktop") || input.ActiveProviderName.ToLowerInvariant().Contains("keyboard"))
                        input.SetProvider(input.GetComponent<PhoneInputProvider>() ?? input.gameObject.AddComponent<PhoneInputProvider>());
            if (!mobileInput)
            {
                var comfort = GetComponent<VLabNativeComfort>() ?? gameObject.AddComponent<VLabNativeComfort>();
                comfort.Configure(camera, FindAnyObjectByType<VLAB.Core.Input.InputManager>());
            }
            // Legacy lesson HUDs use the same world-space pointer route in every mode.
            // Their position is established once, so they remain at the workstation.
            foreach (var canvas in FindObjectsByType<Canvas>())
            {
                if(canvas.name=="FadeCanvas")
                {
                    // A transition veil must cover both eyes; it is not a workstation menu.
                    if(mobileInput){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=camera.nearClipPlane+.05f;}
                    continue;
                }
                if (canvas.renderMode == RenderMode.WorldSpace) continue;
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = camera;
                var rect = (RectTransform)canvas.transform;
                rect.sizeDelta = new Vector2(1440, 900);
                canvas.transform.localScale = Vector3.one * .0018f;
                canvas.transform.SetPositionAndRotation(camera.transform.position + camera.transform.forward * 2.8f, camera.transform.rotation);
                if(canvas.GetComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>()==null)
                    canvas.gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>();
            }
            if (!VLabHeadPose.PhoneViewer) yield break;
            camera.stereoTargetEye = StereoTargetEyeMask.Both;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Application.targetFrameRate = 60;
            foreach (var behaviour in FindObjectsByType<MonoBehaviour>())
                if (behaviour.GetType().Name == "DynamicMoveProvider" || behaviour.GetType().Name == "ContinuousMoveProvider" || behaviour.GetType().Name == "SnapTurnProvider") behaviour.enabled = false;
            if (gameObject.scene.name == "PhysicsLab_Base") ConfigurePhonePhysics(camera);
            if (EventSystem.current != null)
            {
                foreach (var module in EventSystem.current.GetComponents<BaseInputModule>()) module.enabled = false;
                EventSystem.current.gameObject.AddComponent<VLabGazeInputModule>();
            }
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!Google.XR.Cardboard.Api.HasDeviceParams()) Google.XR.Cardboard.Api.ScanDeviceParams();
#endif
        }
        private static void ConfigurePhonePhysics(Camera camera)
        {
            var input = FindAnyObjectByType<VLAB.Core.Input.InputManager>();
            if (input == null) return;
            // Use the existing upright locomotion root, never the tracked head transform.
            var body = camera.GetComponentInParent<CharacterController>();
            if (body == null) return;
            var grab = body.GetComponent<VLAB.PhysicsLab.Interaction.GrabController>() ?? body.gameObject.AddComponent<VLAB.PhysicsLab.Interaction.GrabController>();
            grab.enabled=false;grab.Configure(input,camera);grab.enabled=true;
            var rig = body.GetComponent<VLAB.PhysicsLab.Interaction.DesktopPlayerRig>() ?? body.gameObject.AddComponent<VLAB.PhysicsLab.Interaction.DesktopPlayerRig>();
            rig.enabled=false;rig.Configure(input,camera,grab);rig.enabled=true;
            var ray = body.GetComponent<VLAB.PhysicsLab.Interaction.InteractionRaycaster>() ?? body.gameObject.AddComponent<VLAB.PhysicsLab.Interaction.InteractionRaycaster>();
            ray.enabled=false;ray.Configure(input,rig,grab,camera);ray.enabled=true;
        }
        private void Update()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!VLabHeadPose.PhoneViewer) return;
            if (Google.XR.Cardboard.Api.IsGearButtonPressed) Google.XR.Cardboard.Api.ScanDeviceParams();
            if (Google.XR.Cardboard.Api.IsCloseButtonPressed) app?.NavigateBack();
            if (Google.XR.Cardboard.Api.IsTriggerHeldPressed) Head?.Recenter();
            if (Google.XR.Cardboard.Api.HasNewDeviceParams()) Google.XR.Cardboard.Api.ReloadDeviceParams();
            Google.XR.Cardboard.Api.UpdateScreenParams();
#endif
        }
    }

    public sealed class VLabGazeInputModule : PointerInputModule
    {
        public VLAB.Core.Input.InputManager Input;
        public Camera ViewCamera;
        private PointerEventData pointer;
        private bool held;
        private bool providerObserved, suppressUntilRelease=true;
        private IVLABInputProvider previousProvider;
        public override void Process()
        {
            var camera = ViewCamera != null ? ViewCamera : Camera.main;
            if (camera == null || (Input != null && !Input.isActiveAndEnabled))
            {
                CancelPointer();
                suppressUntilRelease = true;
                return;
            }
            var provider = Input != null ? Input.Provider : null;
            if (providerObserved && !ReferenceEquals(previousProvider, provider))
            {
                CancelPointer();
                suppressUntilRelease = true;
            }
            previousProvider = provider;
            providerObserved = true;
            var controller = provider as IVLabRayProvider;
            var pressed = controller != null ? Input.RawState.PrimaryPressed
                : PhoneInputProvider.ViewerTouchHeld || Gamepad.current?.buttonSouth.isPressed == true;
            Ray ray;
            if (controller != null)
            {
                if (!controller.TryGetRay(camera, out ray))
                {
                    CancelPointer();
                    suppressUntilRelease = true;
                    return;
                }
            }
            else ray = new Ray(camera.transform.position, camera.transform.forward);
            if (pointer == null) pointer = new PointerEventData(eventSystem) { pointerId = -10, button = PointerEventData.InputButton.Left };
            var previousPosition = pointer.position;
            pointer.position = new Vector2(Screen.width * .5f, Screen.height * .5f);
            pointer.pointerCurrentRaycast=VLabPointerUi.Raycast(ray,pointer,m_RaycastResultCache);
            pointer.delta = pointer.position - previousPosition;
            var target = pointer.pointerCurrentRaycast.gameObject;
            HandlePointerExitAndEnter(pointer, target);
            if (suppressUntilRelease)
            {
                if (pressed) return;
                suppressUntilRelease = false;
            }
            if (pressed && !held)
            {
                pointer.eligibleForClick = true;
                pointer.dragging = false;
                pointer.useDragThreshold = true;
                pointer.delta = Vector2.zero;
                pointer.pressPosition = pointer.position;
                pointer.pointerPressRaycast = pointer.pointerCurrentRaycast;
                DeselectIfSelectionChanged(target, pointer);
                pointer.pointerPress = ExecuteEvents.ExecuteHierarchy(target, pointer, ExecuteEvents.pointerDownHandler) ?? ExecuteEvents.GetEventHandler<IPointerClickHandler>(target);
                pointer.rawPointerPress = target;
                pointer.pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(target);
                if (pointer.pointerDrag != null) ExecuteEvents.Execute(pointer.pointerDrag, pointer, ExecuteEvents.initializePotentialDrag);
            }
            if (pressed && held && pointer.pointerDrag != null && pointer.delta.sqrMagnitude > 0)
            {
                if (!pointer.dragging && (!pointer.useDragThreshold || (pointer.position - pointer.pressPosition).sqrMagnitude >= eventSystem.pixelDragThreshold * eventSystem.pixelDragThreshold))
                {
                    ExecuteEvents.Execute(pointer.pointerDrag, pointer, ExecuteEvents.beginDragHandler);
                    pointer.dragging = true;
                    pointer.eligibleForClick = false;
                    if (pointer.pointerPress != pointer.pointerDrag)
                    {
                        ExecuteEvents.Execute(pointer.pointerPress, pointer, ExecuteEvents.pointerUpHandler);
                        pointer.pointerPress = null;
                        pointer.rawPointerPress = null;
                    }
                }
                if (pointer.dragging) ExecuteEvents.Execute(pointer.pointerDrag, pointer, ExecuteEvents.dragHandler);
            }
            if (!pressed && held)
            {
                if (pointer.pointerPress != null) ExecuteEvents.Execute(pointer.pointerPress, pointer, ExecuteEvents.pointerUpHandler);
                if (pointer.eligibleForClick && pointer.pointerPress != null && ExecuteEvents.GetEventHandler<IPointerClickHandler>(target) == pointer.pointerPress)
                    ExecuteEvents.Execute(pointer.pointerPress, pointer, ExecuteEvents.pointerClickHandler);
                else if (pointer.dragging) ExecuteEvents.ExecuteHierarchy(target, pointer, ExecuteEvents.dropHandler);
                if (pointer.dragging && pointer.pointerDrag != null) ExecuteEvents.Execute(pointer.pointerDrag, pointer, ExecuteEvents.endDragHandler);
                pointer.eligibleForClick = false;
                pointer.pointerPress = null;
                pointer.rawPointerPress = null;
                pointer.pointerDrag = null;
                pointer.dragging = false;
            }
            held = pressed;
            // Selection is explicit: looking at a consent or exit button cannot activate it.
            pointer.scrollDelta = controller != null ? new Vector2(0, Input.RawState.ScrollDelta)
                : Gamepad.current?.rightStick.ReadValue() ?? Vector2.zero;
            if (pointer.scrollDelta.sqrMagnitude > .01f) ExecuteEvents.ExecuteHierarchy(target, pointer, ExecuteEvents.scrollHandler);
        }
        public override bool IsPointerOverGameObject(int pointerId) => pointer?.pointerEnter != null;
        private void CancelPointer()
        {
            if (pointer != null)
            {
                if (pointer.pointerPress != null) ExecuteEvents.Execute(pointer.pointerPress, pointer, ExecuteEvents.pointerUpHandler);
                if (pointer.dragging && pointer.pointerDrag != null) ExecuteEvents.Execute(pointer.pointerDrag, pointer, ExecuteEvents.endDragHandler);
                HandlePointerExitAndEnter(pointer, null);
            }
            pointer = null; held = false;
        }
        protected override void OnDisable()
        {
            CancelPointer();
            suppressUntilRelease = true;
            base.OnDisable();
        }
        public override void DeactivateModule()
        {
            CancelPointer();
            suppressUntilRelease = true;
            base.DeactivateModule();
        }
    }
}
