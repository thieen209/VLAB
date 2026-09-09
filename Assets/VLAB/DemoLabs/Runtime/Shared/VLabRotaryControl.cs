using System;
using System.Collections;
using UnityEngine;

namespace VLAB.DemoLabs
{
    public sealed class VLabRotaryControl : VLabInteractable, IVLabResettable
    {
        public Transform MovingPart;
        public Vector3 Axis = Vector3.right;
        public float Sensitivity = .04f;
        public float InitialValue;
        public bool Detented;
        public float Value { get; private set; }
        public event Action<float> ValueChanged;
        public Func<bool> CanAdjust;
        private Quaternion initialRotation;
        private Coroutine motion;
        private void Awake() => initialRotation = (MovingPart != null ? MovingPart : transform).localRotation;
        public override void Activate() => Rotate(1);
        public override void Rotate(float amount)
        {
            if (CanAdjust != null && !CanAdjust()) return;
            if (Detented) { if (amount != 0) SetValue(Value < .5f ? 1 : 0); }
            else SetValue(Value + amount * Sensitivity);
        }
        public void SetValue(float value)
        {
            Value = Mathf.Clamp01(value);
            var part = MovingPart != null ? MovingPart : transform;
            var target = initialRotation * Quaternion.AngleAxis(Value * (Detented ? 180 : 300), Axis);
            if (motion != null) StopCoroutine(motion);
            if (Application.isPlaying && isActiveAndEnabled) motion = StartCoroutine(Animate(part, target));
            else part.localRotation = target;
            ValueChanged?.Invoke(Value);
        }
        private IEnumerator Animate(Transform part, Quaternion target)
        {
            var from = part.localRotation;
            for (var t = 0f; t < 1; t += Time.unscaledDeltaTime / .18f)
            { part.localRotation = Quaternion.Slerp(from, target, Mathf.SmoothStep(0, 1, t)); yield return null; }
            part.localRotation = target; motion = null;
        }
        public void ResetState()
        {
            SetValue(InitialValue);
            if (motion != null) StopCoroutine(motion); motion = null;
            (MovingPart != null ? MovingPart : transform).localRotation = initialRotation * Quaternion.AngleAxis(Value * (Detented ? 180 : 300), Axis);
        }
    }
}
