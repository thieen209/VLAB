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
            if(gameObject.scene.name!=VLABMenuBootstrap.MenuScene)return;
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
            Head.SimulateInEditor = gameObject.scene.name == VLABMenuBootstrap.MenuScene;
            foreach (var behaviour in camera.GetComponents<Behaviour>())
                if ((Head.SimulateInEditor || VLabHeadPose.PhoneViewer) && behaviour.GetType().Name == "TrackedPoseDriver") behaviour.enabled = false;
            if (!VLabHeadPose.PhoneViewer) yield break;
            camera.stereoTargetEye = StereoTargetEyeMask.Both;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Application.targetFrameRate = 60;
            foreach (var input in FindObjectsByType<VLAB.Core.Input.InputManager>())
                if (!input.HasProvider || input.ActiveProviderName.ToLowerInvariant().Contains("simulator") || input.ActiveProviderName.ToLowerInvariant().Contains("desktop") || input.ActiveProviderName.ToLowerInvariant().Contains("keyboard"))
                    input.SetProvider(input.gameObject.AddComponent<PhoneInputProvider>());
            foreach (var behaviour in FindObjectsByType<MonoBehaviour>())
                if (behaviour.GetType().Name == "DynamicMoveProvider" || behaviour.GetType().Name == "ContinuousMoveProvider" || behaviour.GetType().Name == "SnapTurnProvider") behaviour.enabled = false;
            if (gameObject.scene.name == "PhysicsLab_Base") ConfigurePhonePhysics(camera);
            if (EventSystem.current != null)
            {
                foreach (var module in EventSystem.current.GetComponents<BaseInputModule>()) module.enabled = false;
                EventSystem.current.gameObject.AddComponent<VLabGazeInputModule>();
            }
            foreach (var canvas in FindObjectsByType<Canvas>())
            {
                if (canvas.renderMode == RenderMode.WorldSpace) continue;
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = camera;
                var rect = (RectTransform)canvas.transform;
                rect.sizeDelta = new Vector2(1440, 900);
                canvas.transform.localScale = Vector3.one * .0018f;
                canvas.transform.SetPositionAndRotation(camera.transform.position + camera.transform.forward * 2.8f, camera.transform.rotation);
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
        private PointerEventData pointer;
        private bool held;
        private GameObject reticle;
        private Material reticleMaterial;
        public override void Process()
        {
            if (Camera.main == null) return;
            if(reticle==null)
            {
                reticle=GameObject.CreatePrimitive(PrimitiveType.Sphere);reticle.name="Viewer gaze cursor";
                Destroy(reticle.GetComponent<Collider>());
                reticle.transform.SetParent(Camera.main.transform,false);
                reticle.transform.localPosition=Vector3.forward*2;reticle.transform.localScale=Vector3.one*.009f;
                reticleMaterial=new Material(Shader.Find("Unlit/Color")){color=new Color(.2f,.85f,1)};
                var renderer=reticle.GetComponent<Renderer>();renderer.sharedMaterial=reticleMaterial;
                renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
            }
            if (pointer == null) pointer = new PointerEventData(eventSystem) { pointerId = -10, button = PointerEventData.InputButton.Left };
            pointer.position = new Vector2(Screen.width * .5f, Screen.height * .5f);
            eventSystem.RaycastAll(pointer, m_RaycastResultCache);
            pointer.pointerCurrentRaycast = FindFirstRaycast(m_RaycastResultCache);
            m_RaycastResultCache.Clear();
            var target = pointer.pointerCurrentRaycast.gameObject;
            HandlePointerExitAndEnter(pointer, target);
            var pressed = PhoneInputProvider.ViewerTouchHeld || Gamepad.current?.buttonSouth.isPressed == true;
            if (pressed && !held)
            {
                pointer.pressPosition = pointer.position;
                pointer.pointerPressRaycast = pointer.pointerCurrentRaycast;
                pointer.pointerPress = ExecuteEvents.ExecuteHierarchy(target, pointer, ExecuteEvents.pointerDownHandler) ?? ExecuteEvents.GetEventHandler<IPointerClickHandler>(target);
            }
            if (!pressed && held && pointer.pointerPress != null)
            {
                ExecuteEvents.Execute(pointer.pointerPress, pointer, ExecuteEvents.pointerUpHandler);
                if (ExecuteEvents.GetEventHandler<IPointerClickHandler>(target) == pointer.pointerPress)
                    ExecuteEvents.Execute(pointer.pointerPress, pointer, ExecuteEvents.pointerClickHandler);
                pointer.pointerPress = null;
            }
            held = pressed;
            // Selection is explicit: looking at a consent or exit button cannot activate it.
            pointer.scrollDelta = Gamepad.current?.rightStick.ReadValue() ?? Vector2.zero;
            if (pointer.scrollDelta.sqrMagnitude > .01f) ExecuteEvents.ExecuteHierarchy(target, pointer, ExecuteEvents.scrollHandler);
        }
        public override bool IsPointerOverGameObject(int pointerId) => pointer?.pointerEnter != null;
        protected override void OnDestroy()
        {
            if(reticle!=null)Destroy(reticle);if(reticleMaterial!=null)Destroy(reticleMaterial);
            base.OnDestroy();
        }
        public override void DeactivateModule()
        {
            if (pointer != null)
            {
                if (pointer.pointerPress != null) ExecuteEvents.Execute(pointer.pointerPress, pointer, ExecuteEvents.pointerUpHandler);
                HandlePointerExitAndEnter(pointer, null);
            }
            pointer = null; held = false;
            base.DeactivateModule();
        }
    }
}
