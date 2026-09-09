using UnityEngine;
using UnityEngine.EventSystems;

namespace VLAB.DemoLabs
{
    // Screen dials manipulate the same physical rotary component while the eye is at the eyepiece.
    public sealed class VLabDialDrag : MonoBehaviour, IDragHandler, IScrollHandler
    {
        public VLabRotaryControl Control;
        public RectTransform Indicator;
        private float lastValue = -1;
        private Canvas canvas;
        private void Awake() => canvas = GetComponentInParent<Canvas>();
        public void OnDrag(PointerEventData data) => Control.Rotate((data.delta.x - data.delta.y) * .075f / (canvas != null ? canvas.scaleFactor : 1));
        public void OnScroll(PointerEventData data) => Control.Rotate(data.scrollDelta.y);
        private void Update()
        {
            if (Indicator != null && lastValue != Control.Value)
            { lastValue = Control.Value; Indicator.localRotation = Quaternion.Euler(0, 0, -Control.Value * 300); }
        }
    }
}
