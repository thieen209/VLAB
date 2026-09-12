using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using VLAB.ChemistryLab.Input;

namespace VLAB.ChemistryLab.Interaction
{
    /// <summary>Desktop adapter uses XRI selection too, so lids, recovery and ownership stay consistent.</summary>
    [RequireComponent(typeof(Camera))]
    public sealed class DesktopLabGrabber : MonoBehaviour
    {
        private XRDirectInteractor hand;
        private Camera view;
        private XRGrabInteractable held;
        private float distance = 1f;
        private float tilt;
        private GUIStyle hintStyle;
        private HandsOnTitration station;
        private DesktopTitrationInterface panel;
        private readonly System.Collections.Generic.Dictionary<XRGrabInteractable, XRInteractionManager> previousManagers = new System.Collections.Generic.Dictionary<XRGrabInteractable, XRInteractionManager>();
        public XRGrabInteractable Held => held;
        public VLAB.Core.Input.InputManager SharedInput { get; set; }
        public bool IsHolding => held != null && hand != null && hand.IsSelecting(held);

        private void Awake()
        {
            view = GetComponent<Camera>();
            station = Object.FindAnyObjectByType<HandsOnTitration>();
            panel = Object.FindAnyObjectByType<DesktopTitrationInterface>();
            var handObject = new GameObject("Desktop physical hand");
            handObject.transform.SetParent(transform, false);
            handObject.AddComponent<SphereCollider>().isTrigger = true;
            handObject.GetComponent<SphereCollider>().radius = .025f;
            var manager = handObject.AddComponent<XRInteractionManager>();
            hand = handObject.AddComponent<XRDirectInteractor>();
            hand.interactionManager = manager;
            hand.selectInput.inputSourceMode = XRInputButtonReader.InputSourceMode.ManualValue;
            hand.activateInput.inputSourceMode = XRInputButtonReader.InputSourceMode.ManualValue;
            hand.keepSelectedTargetValid = true;
            hand.selectActionTrigger = XRBaseInputInteractor.InputTriggerType.State;
            hand.attachTransform = handObject.transform;
            hand.selectExited.AddListener(OnReleased);
        }
        public bool TryGrab(Collider collider)
        {
            if (!isActiveAndEnabled || IsHolding || collider == null) return false;
            var target = collider.GetComponentInParent<XRGrabInteractable>();
            if (target == null || !target.isActiveAndEnabled || target.isSelected ||
                Vector3.Distance(transform.position, target.transform.position) > 3f) return false;
            var manager = hand.interactionManager;
            previousManagers[target] = target.interactionManager;
            target.interactionManager = manager;
            distance = Mathf.Clamp(Vector3.Distance(transform.position, target.transform.position), .45f, 2.5f);
            tilt = 0;
            hand.transform.SetPositionAndRotation(target.transform.position, target.transform.rotation);
            held = target;
            hand.selectInput.manualPerformed = true;
            hand.selectInput.manualValue = 1;
            manager.SelectEnter((IXRSelectInteractor)hand, target);
            if (!hand.IsSelecting(target)) { Release(); return false; }
            return true;
        }
        public void Release()
        {
            if (hand == null) return;
            hand.selectInput.manualPerformed = false;
            hand.selectInput.manualValue = 0;
            var released = held;
            if (released != null && hand.IsSelecting(released)) hand.interactionManager.SelectExit((IXRSelectInteractor)hand, released);
            if (released != null && previousManagers.TryGetValue(released, out var previous))
            {
                released.interactionManager = previous;
                previousManagers.Remove(released);
            }
            held = null;
        }
        private void OnReleased(SelectExitEventArgs _) { held = null; hand.selectInput.manualPerformed = false; }
        public void ProcessInput(VLabDesktopInputFrame frame)
        {
            if (!IsHolding) return;
            if (frame.CancelPressed) { Release(); return; }
            if (frame.PointerOverScrollableUi) return;
            if (frame.UsePressed) held.GetComponent<LabLiquidVessel>()?.Use();
            if (Mathf.Abs(frame.ZoomDelta) > .01f) distance = Mathf.Clamp(distance + Mathf.Sign(frame.ZoomDelta) * .12f, .45f, 2.5f);
            tilt = Mathf.MoveTowards(tilt, frame.TiltHeld ? 115f : 0f, Time.deltaTime * 160f);
            var point=Mouse.current != null ? Mouse.current.position.ReadValue() : new Vector2(Screen.width / 2f, Screen.height / 2f);
            Ray ray = SharedInput!=null ? SharedInput.PointerRay(view,point) : VLAB.Core.Input.VLabHeadPose.PhoneViewer ? new Ray(view.transform.position,view.transform.forward) : view.ScreenPointToRay(point);
            hand.transform.SetPositionAndRotation(ray.GetPoint(distance), Quaternion.Euler(0, transform.eulerAngles.y, tilt));
        }
        private void OnDisable()
        {
            Release();
            foreach (var entry in previousManagers) if (entry.Key != null) entry.Key.interactionManager = entry.Value;
            previousManagers.Clear();
        }
        private void OnDestroy() { if (hand != null) hand.selectExited.RemoveListener(OnReleased); }
        private void OnGUI()
        {
            if (VLAB.Core.Input.VLabHeadPose.PhoneViewer || SharedInput?.HasRayProvider==true) return;
            if (hintStyle == null) hintStyle = new GUIStyle(GUI.skin.box) { fontSize = 16, alignment = TextAnchor.MiddleCenter, wordWrap = true, normal = { textColor = Color.white } };
            var vessel = IsHolding ? held.GetComponent<LabLiquidVessel>() : null;
            string item = vessel != null && vessel.Liquid != null ? vessel.DisplayName + "  |  " + vessel.Liquid.VolumeMl.ToString("F2") + " mL  |  " + (vessel.IsOpen ? "Đang mở" : "Đã đóng nắp") + "\n" : "";
            var guide = vessel != null ? vessel.GetComponent<LabPourGuide>() : null;
            if (guide != null && guide.Visible) item = item.TrimEnd('\n') + "  |  " + guide.Hint + "\n";
            GUI.Box(new Rect(24, Screen.height - 80, Mathf.Max(100, Screen.width - 48), 64),
                IsHolding ? item + "Click: thả • F: nắp/pipette • Giữ R: nghiêng • Cuộn: xa/gần • Esc: thả"
                          : "Click: cầm dụng cụ • Vòi burette: click một giọt, giữ để rót • Esc: bảng hướng dẫn", hintStyle);
            if (station != null && panel != null && !panel.IsPanelVisible)
                GUI.Box(new Rect(Screen.width * .2f, 16, Screen.width * .6f, 72), station.Feedback ?? "", hintStyle);
        }
    }
}
