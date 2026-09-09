using System;
using UnityEngine;

namespace VLAB.DemoLabs
{
    public sealed class VLabGrabInteractable : VLabInteractable, IVLabResettable
    {
        public string Kind;
        public int Value;
        public bool CanFlip;
        public bool Flipped { get; private set; }
        public bool IsHeld { get; internal set; }
        public VLabSnapZone Zone { get; internal set; }
        public event Action Changed;
        private Vector3 homePosition;
        private Quaternion homeRotation;
        private Transform homeParent;
        private bool captured;

        private void Awake() => CaptureHome();
        public void CaptureHome()
        {
            if (captured) return;
            homePosition = transform.position;
            homeRotation = transform.rotation;
            homeParent = transform.parent;
            captured = true;
        }
        public void NotifyChanged() => Changed?.Invoke();
        public void Detach()
        {
            if (Zone != null) Zone.Detach(this);
            if (captured) transform.SetParent(homeParent, true);
            Changed?.Invoke();
        }
        public override void Rotate(float amount)
        {
            if (!CanFlip || Mathf.Abs(amount) < .001f) return;
            Flipped = !Flipped;
            transform.Rotate(Vector3.up, 180f, Space.Self);
            Changed?.Invoke();
        }
        public void ResetState()
        {
            CaptureHome();
            Detach();
            IsHeld = false;
            Flipped = false;
            transform.SetPositionAndRotation(homePosition, homeRotation);
            SetFocus(false);
            Changed?.Invoke();
        }
        public bool RecoverIfLost()
        {
            if (!IsHeld && (transform.position.y < .3f || Vector3.Distance(transform.position, homePosition) > 8f))
            { ResetState(); return true; }
            return false;
        }
    }
}
