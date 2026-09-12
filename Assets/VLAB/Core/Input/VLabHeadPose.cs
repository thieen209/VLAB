using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace VLAB.Core.Input
{
    /// <summary>One rotational head pose owner. Cardboard owns sensor fusion and stereo.</summary>
    [DefaultExecutionOrder(1000)]
    public sealed class VLabHeadPose : MonoBehaviour
    {
        public bool SimulateInEditor;
        public bool SimulationEnabled = true;
        private Quaternion forwardReference;
        private float yaw, pitch;
        public const string ViewerPreferenceKey = "VLAB.MobileVR.Enabled";
        private static bool viewerRequested = true;
        private static bool preferenceLoaded;
        private bool touchDragAllowed;
        private Vector2 touchStart;
        private readonly List<RaycastResult> touchUiHits = new List<RaycastResult>();
        private PointerEventData touchPointer;
        private EventSystem touchEventSystem;
        public static bool ViewerRequested => viewerRequested;
        public static bool PhoneViewer => Application.platform == RuntimePlatform.Android && viewerRequested;
        public bool OwnsRotation => Application.platform == RuntimePlatform.Android || (Application.isEditor && SimulateInEditor && SimulationEnabled);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void LoadViewerPreference()
        { if (!preferenceLoaded) SetViewerRequested(PlayerPrefs.GetInt(ViewerPreferenceKey, 1) != 0); }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCachedPreference() { viewerRequested = true; preferenceLoaded = false; }
        public static void SetViewerRequested(bool requested) { viewerRequested = requested; preferenceLoaded = true; }

        private void Awake()
        {
            // A desktop camera's staged downward pitch must not tilt the viewer's neutral horizon.
            forwardReference = PhoneViewer ? Quaternion.Euler(0,transform.localEulerAngles.y,0) : transform.localRotation;
        }
        private void OnEnable() { Application.onBeforeRender += ApplyDevicePose; }
        private void OnDisable() { Application.onBeforeRender -= ApplyDevicePose; }
        private void Update()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!PhoneViewer) UpdateTouchLook();
#endif
#if UNITY_EDITOR
            if (!SimulateInEditor) return;
            if (Keyboard.current?.f8Key.wasPressedThisFrame == true) SimulationEnabled = !SimulationEnabled;
            if (Keyboard.current?.homeKey.wasPressedThisFrame == true) Recenter();
            if (SimulationEnabled && Mouse.current?.rightButton.isPressed == true)
            {
                var delta = Mouse.current.delta.ReadValue();
                Simulate(delta.x * .12f, -delta.y * .12f);
            }
#endif
        }
        public void Simulate(float yawDelta, float pitchDelta)
        {
            if (!Application.isEditor || !SimulationEnabled || !SimulateInEditor) return;
            if (!IsFinite(yawDelta) || !IsFinite(pitchDelta)) return;
            yaw += yawDelta; pitch = Mathf.Clamp(pitch + pitchDelta, -80, 80);
            transform.localRotation = forwardReference * Quaternion.Euler(pitch, yaw, 0);
        }
        private void LateUpdate() { ApplyDevicePose(); }
        private void ApplyDevicePose()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!PhoneViewer) return;
            var head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            if ((head.TryGetFeatureValue(UnityEngine.XR.CommonUsages.centerEyeRotation, out Quaternion pose) ||
                head.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceRotation, out pose)) && TryNormalizePose(pose, out var normalized))
                transform.localRotation = forwardReference * normalized;
#endif
        }

        private void UpdateTouchLook()
        {
            var touch = Touchscreen.current?.primaryTouch;
            if (touch == null || !touch.press.isPressed) { touchDragAllowed = false; return; }
            if (touch.press.wasPressedThisFrame)
            {
                touchStart = touch.position.ReadValue();
                touchDragAllowed = !StartsOverUi(touchStart);
            }
            if (!touchDragAllowed || (touch.position.ReadValue() - touchStart).sqrMagnitude < 144) return;
            var delta = touch.delta.ReadValue() * (1080f / Mathf.Max(540, Screen.height)) * .12f;
            if (!IsFinite(delta.x) || !IsFinite(delta.y)) return;
            yaw = Mathf.Repeat(yaw + delta.x + 180, 360) - 180;
            pitch = Mathf.Clamp(pitch - delta.y, -75, 75);
            transform.localRotation = forwardReference * Quaternion.Euler(pitch, yaw, 0);
        }

        private bool StartsOverUi(Vector2 screenPosition)
        {
            var current = EventSystem.current;
            if (current == null) return false;
            if (touchPointer == null || touchEventSystem != current)
            { touchEventSystem = current; touchPointer = new PointerEventData(current); }
            touchPointer.Reset(); touchPointer.position = screenPosition;
            touchUiHits.Clear(); current.RaycastAll(touchPointer, touchUiHits);
            var hit = false;
            foreach (var result in touchUiHits)
                if (result.gameObject != null && result.gameObject.GetComponentInParent<Canvas>() != null) { hit = true; break; }
            touchUiHits.Clear(); return hit;
        }

        public static bool TryNormalizePose(Quaternion pose, out Quaternion normalized)
        {
            normalized = Quaternion.identity;
            if (!IsFinite(pose.x) || !IsFinite(pose.y) || !IsFinite(pose.z) || !IsFinite(pose.w)) return false;
            var lengthSquared = pose.x * pose.x + pose.y * pose.y + pose.z * pose.z + pose.w * pose.w;
            if (!IsFinite(lengthSquared) || lengthSquared < .000001f) return false;
            var inverseLength = 1 / Mathf.Sqrt(lengthSquared);
            normalized = new Quaternion(pose.x * inverseLength, pose.y * inverseLength, pose.z * inverseLength, pose.w * inverseLength);
            return true;
        }
        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        public void Recenter()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (PhoneViewer) { Google.XR.Cardboard.Api.Recenter(); return; }
#endif
            yaw = pitch = 0;
            transform.localRotation = forwardReference;
        }
    }
}
