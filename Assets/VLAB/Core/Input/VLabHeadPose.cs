using UnityEngine;
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
        public static bool PhoneViewer => Application.platform == RuntimePlatform.Android;
        public bool OwnsRotation => PhoneViewer || (Application.isEditor && SimulateInEditor && SimulationEnabled);

        private void Awake()
        {
            // A desktop camera's staged downward pitch must not tilt the viewer's neutral horizon.
            forwardReference = PhoneViewer ? Quaternion.Euler(0,transform.localEulerAngles.y,0) : transform.localRotation;
        }
        private void OnEnable() { Application.onBeforeRender += ApplyDevicePose; }
        private void OnDisable() { Application.onBeforeRender -= ApplyDevicePose; }
        private void Update()
        {
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
            yaw += yawDelta; pitch = Mathf.Clamp(pitch + pitchDelta, -80, 80);
            transform.localRotation = forwardReference * Quaternion.Euler(pitch, yaw, 0);
        }
        private void LateUpdate() { ApplyDevicePose(); }
        private void ApplyDevicePose()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            var head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            if (head.TryGetFeatureValue(UnityEngine.XR.CommonUsages.centerEyeRotation, out Quaternion pose) ||
                head.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceRotation, out pose))
                transform.localRotation = forwardReference * pose;
#endif
        }
        public void Recenter()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            Google.XR.Cardboard.Api.Recenter();
#else
            yaw = pitch = 0;
            transform.localRotation = forwardReference;
#endif
        }
    }
}
