using UnityEngine;

namespace VLAB.Core.Input
{
    /// <summary>Transport-independent replay of decoded controller state, not an invented BLE wire protocol.</summary>
    public sealed class VLabControllerReplayProvider : MonoBehaviour, IVLABInputProvider, IVLabRayProvider
    {
        [SerializeField, Range(0, .4f)] private float deadzone = .12f;
        [SerializeField, Min(.05f)] private float timeout = .5f;
        private VLABInputState state;
        private Quaternion rotation = Quaternion.identity, calibration = Quaternion.identity;
        private float receivedAt = float.NegativeInfinity;
        private uint sequence;
        private bool received;
        private bool hasHeading;
        private Quaternion referenceHeading = Quaternion.identity;
        private Camera lastView;
        public string ProviderName => "VLAB decoded controller / replay";
        public bool Connected => received && Time.unscaledTime - receivedAt <= timeout;
        public bool InteractionPressed => Connected && state.PrimaryPressed;
        public bool ResetPressed { get; private set; }
        public bool Submit(VLABInputState input, Quaternion orientation, uint packetSequence, bool reset = false)
        {
            if (Connected && unchecked((int)(packetSequence - sequence)) <= 0) return false;
            var norm = orientation.x * orientation.x + orientation.y * orientation.y + orientation.z * orientation.z + orientation.w * orientation.w;
            if (float.IsNaN(norm) || float.IsInfinity(norm) || norm < .00001f) return false;
            var scale = 1 / Mathf.Sqrt(norm);
            rotation = new Quaternion(orientation.x * scale, orientation.y * scale, orientation.z * scale, orientation.w * scale);
            state = input; sequence = packetSequence; received = true; receivedAt = Time.unscaledTime; ResetPressed = reset;
            return true;
        }
        public VLABInputState ReadState()
        {
            if (!Connected) { ResetPressed = false; return default; }
            var result = state;
            if (result.Move.magnitude <= deadzone) result.Move = Vector2.zero;
            else result.Move = result.Move.normalized * Mathf.Clamp01((result.Move.magnitude - deadzone) / (1 - deadzone));
            return result;
        }
        public void Calibrate()
        {
            calibration = Quaternion.Inverse(rotation);
            hasHeading = lastView != null;
            if (hasHeading) referenceHeading = Quaternion.Euler(0, lastView.transform.eulerAngles.y, 0);
        }
        public void Disconnect() { received = false; state = default; ResetPressed = false; }
        private void OnApplicationPause(bool paused) { if (paused) Disconnect(); }
        private void OnDisable() => Disconnect();
        public bool TryGetRay(Camera view, out Ray ray)
        {
            ray = default;
            if (!Connected || view == null) return false;
            // The arm anchor is an explicit approximation; no IMU-derived position is claimed.
            var heading = Quaternion.Euler(0, view.transform.eulerAngles.y, 0);
            lastView = view;
            if (!hasHeading) { referenceHeading = heading; hasHeading = true; }
            ray = new Ray(view.transform.TransformPoint(new Vector3(VLabComfortSettings.Current.leftHanded?-.20f:.20f, -.17f, .60f)), referenceHeading * calibration * rotation * Vector3.forward);
            return true;
        }
    }
}
