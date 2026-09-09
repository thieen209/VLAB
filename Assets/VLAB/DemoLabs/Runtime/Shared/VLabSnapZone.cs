using System;
using UnityEngine;

namespace VLAB.DemoLabs
{
    public sealed class VLabSnapZone : VLabInteractable, IVLabResettable
    {
        public string AcceptedKind;
        public Transform Anchor;
        public Renderer PreviewRenderer;
        public bool ConsumeAndReturn;
        public bool AllowReplacement;
        public float CaptureRadius = .14f;
        public VLabGrabInteractable Occupant { get; private set; }
        public Func<VLabGrabInteractable, string> Validate;
        public event Action<VLabGrabInteractable> Placed;
        public event Action<VLabGrabInteractable> Removed;
        public void ShowGuide(bool visible) { if (PreviewRenderer != null) PreviewRenderer.enabled = visible; }

        public bool TryPlace(VLabGrabInteractable item, out string reason)
        {
            reason = "";
            if (item == null) { reason = "Hãy cầm dụng cụ trước."; return false; }
            if (item.Kind != AcceptedKind) { reason = "Dụng cụ này không phù hợp với vị trí đang chọn."; return false; }
            if (Occupant != null && Occupant != item && !AllowReplacement)
            { reason = "Vị trí này đã có dụng cụ. Hãy lấy ra trước."; return false; }
            reason = Validate?.Invoke(item) ?? "";
            if (reason.Length > 0) return false;
            if (Occupant != null && Occupant != item) Occupant.ResetState();
            item.Detach();
            item.IsHeld = false;
            if (!ConsumeAndReturn)
            {
                Occupant = item;
                item.Zone = this;
                var anchor = Anchor != null ? Anchor : transform;
                item.transform.SetParent(anchor, true);
                item.transform.SetPositionAndRotation(anchor.position,
                    anchor.rotation * Quaternion.Euler(0, item.Flipped ? 180 : 0, 0));
            }
            Placed?.Invoke(item);
            if (ConsumeAndReturn) item.ResetState();
            item.NotifyChanged();
            return true;
        }
        public void Detach(VLabGrabInteractable item)
        {
            if (Occupant != item) return;
            Occupant = null;
            item.Zone = null;
            Removed?.Invoke(item);
        }
        public void ResetState()
        {
            if (Occupant != null) Occupant.ResetState();
            SetFocus(false);
        }
    }
}
