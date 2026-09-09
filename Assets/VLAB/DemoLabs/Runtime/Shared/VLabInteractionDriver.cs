using UnityEngine;
using UnityEngine.EventSystems;
using VLAB.Core.Input;

namespace VLAB.DemoLabs
{
    public sealed class VLabInteractionDriver : MonoBehaviour
    {
        public InputManager Input;
        public VLabDemoInputProvider PointerProvider;
        public Camera ViewCamera;
        public VLabHud Hud;
        public VLabSnapZone[] Zones;
        public VLabGrabInteractable[] Items;
        public VLabExperimentController Experiment;
        public bool ViewLocked;
        public VLabGrabInteractable Held { get; private set; }
        public VLabInteractable Hovered { get; private set; }
        private readonly RaycastHit[] hits = new RaycastHit[48];
        private float nextRecovery;
        private float pitch, yaw;
        private Vector3 startPosition;
        private Quaternion startRotation;
        private Ray currentRay;
        private Vector2 pressPointer;
        private bool pickedThisPress;
        private bool initialized, contextInitialized;
        private VLabInteractable contextHover;
        private VLabGrabInteractable contextHeld;
        private int previousFrameRate;
        private PointerEventData uiPointer;
        private EventSystem uiEventSystem;
        private readonly System.Collections.Generic.List<RaycastResult> uiHits = new System.Collections.Generic.List<RaycastResult>(16);
#if UNITY_EDITOR && ENABLE_VR
        private GameObject suspendedSimulator;
        private System.Collections.IEnumerator SuspendEditorSimulator()
        {
            yield return null; // The package loader creates its persistent simulator after scene load.
            var simulator = UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation.XRInteractionSimulator.instance;
            if (simulator != null && simulator.gameObject.activeSelf)
            { suspendedSimulator = simulator.gameObject; suspendedSimulator.SetActive(false); }
        }
#endif

        private void Start()
        {
            previousFrameRate = Application.targetFrameRate;
            Application.targetFrameRate = 60;
            startPosition = ViewCamera.transform.position;
            startRotation = ViewCamera.transform.rotation;
            initialized = true;
            SyncAngles();
            Input.PrimaryPressed += Select;
            Input.PrimaryReleased += ReleaseDrag;
            Input.DropPressed += ReturnHeld;
            Input.ResetPressed += ResetExperiment;
            Input.PausePressed += Escape;
#if UNITY_EDITOR && ENABLE_VR
            StartCoroutine(SuspendEditorSimulator());
#endif
        }
        private void OnDestroy()
        {
            if (initialized) Application.targetFrameRate = previousFrameRate;
#if UNITY_EDITOR && ENABLE_VR
            // Restore the user's simulator when leaving these desktop demo scenes.
            if (suspendedSimulator != null) suspendedSimulator.SetActive(true);
#endif
            if (Input == null) return;
            Input.PrimaryPressed -= Select; Input.PrimaryReleased -= ReleaseDrag;
            Input.DropPressed -= ReturnHeld; Input.ResetPressed -= ResetExperiment; Input.PausePressed -= Escape;
        }
        private void SyncAngles() { pitch = ViewCamera.transform.eulerAngles.x; yaw = ViewCamera.transform.eulerAngles.y; }
        private void Escape() { if (Experiment != null) Experiment.ExitInspection(); ReturnHeld(); }
        private void ResetExperiment() { Experiment.ResetExperiment(); ResetView(); }
        public void ResetView()
        {
            ReturnHeld(); ViewLocked = false;
            if (!initialized) return;
            ViewCamera.transform.SetPositionAndRotation(startPosition, startRotation); SyncAngles();
        }
        private bool OverUi => IsOverUi(PointerProvider != null ? PointerProvider.Pointer : Vector2.zero);
        private bool IsOverUi(Vector2 position)
        {
                var events = EventSystem.current;
                if (events == null || PointerProvider == null) return false;
                if (uiEventSystem != events) { uiEventSystem = events; uiPointer = new PointerEventData(events); }
                uiPointer.Reset(); uiPointer.position = position;
                uiHits.Clear(); events.RaycastAll(uiPointer, uiHits);
                return uiHits.Count != 0;
        }
        private void Update()
        {
            var state = Input.CurrentState;
            if (!ViewLocked && !Hud.ModalOpen)
            {
                if (state.SecondaryPressed && !OverUi)
                {
                    yaw += state.Look.x * .13f; pitch = Mathf.Clamp(pitch - state.Look.y * .13f, -55, 75);
                    ViewCamera.transform.rotation = Quaternion.Euler(pitch, yaw, 0);
                }
                var forward = Vector3.ProjectOnPlane(ViewCamera.transform.forward, Vector3.up).normalized;
                var move = (forward * state.Move.y + ViewCamera.transform.right * state.Move.x) * (Time.deltaTime * 1.8f);
                var p = ViewCamera.transform.position + move;
                // Keep the viewing rig on the accessible side of the workstation and inside the room.
                p.x = Mathf.Clamp(p.x, -3.7f, 3.7f); p.z = Mathf.Clamp(p.z, -4.2f, -1.45f);
                ViewCamera.transform.position = p;
            }
            currentRay = ViewCamera.ScreenPointToRay(PointerProvider != null ? PointerProvider.Pointer : new Vector2(Screen.width / 2f, Screen.height / 2f));
            RefreshRay(currentRay, OverUi || Hud.ModalOpen || ViewLocked);
            if (state.ScrollDelta != 0 && !OverUi && !Hud.ModalOpen)
            {
                if (ViewLocked) Experiment.RotateInspection(state.ScrollDelta, state.SprintPressed);
                else if (Held != null) Held.Rotate(state.ScrollDelta);
                else Hovered?.Rotate(state.ScrollDelta);
                Experiment.MarkActivity();
            }
            if (Time.unscaledTime >= nextRecovery)
            {
                nextRecovery = Time.unscaledTime + .5f;
                foreach (var item in Items) if (item.RecoverIfLost()) Hud.Feedback("Dụng cụ đã được đưa về khay. Bạn có thể thử lại.", true);
            }
        }
        public void RefreshRay(Ray ray, bool blocked = false)
        {
            currentRay = ray;
            VLabInteractable next = null;
            var nearest = 7f;
            var count = blocked ? 0 : Physics.RaycastNonAlloc(ray, hits, 7, ~0, QueryTriggerInteraction.Ignore);
            for (var i = 0; i < count; i++)
            {
                var hit = hits[i];
                if (Held != null && hit.collider.transform.IsChildOf(Held.transform)) continue;
                if (hit.distance >= nearest) continue;
                nearest = hit.distance;
                next = hit.collider.GetComponentInParent<VLabInteractable>();
                // A fitted item remains selectable even when its dock collider surrounds it.
                if (Held == null && next is VLabSnapZone dock && dock.Occupant != null) next = dock.Occupant;
                else if (Held == null && next is VLabSnapZone nestedZone && nestedZone.GetComponentInParent<VLabGrabInteractable>() is VLabGrabInteractable host) next = host;
            }
            if (Held != null)
            {
                var matchDistance = nearest + .08f;
                for (var i = 0; i < count; i++)
                {
                    if (hits[i].collider.transform.IsChildOf(Held.transform)) continue;
                    var zone = hits[i].collider.GetComponentInParent<VLabSnapZone>();
                    if (zone == null || zone.AcceptedKind != Held.Kind || hits[i].distance >= matchDistance) continue;
                    next = zone; matchDistance = hits[i].distance;
                }
            }
            if (Held == null && next is VLabGrabInteractable)
            {
                // Forgiving grab bounds must not bury adjacent precision controls (e.g. slide clips).
                var controlDistance = nearest + .16f;
                for (var i = 0; i < count; i++)
                {
                    var control = hits[i].collider.GetComponentInParent<VLabInteractable>();
                    if (control == null || control is VLabGrabInteractable || control is VLabSnapZone || hits[i].distance >= controlDistance) continue;
                    next = control; controlDistance = hits[i].distance;
                }
            }
            if (Held != null && !blocked)
            {
                var zone = next as VLabSnapZone;
                var plane = new Plane(Vector3.up, new Vector3(0, 1.19f, 0));
                var destination = Held.transform.position;
                if (zone != null) destination = (zone.Anchor != null ? zone.Anchor.position : zone.transform.position) + Vector3.up * .08f;
                else if (plane.Raycast(ray, out var distance)) destination = ray.GetPoint(Mathf.Clamp(distance, .5f, 5f));
                Held.transform.position = Vector3.Lerp(Held.transform.position, destination, 1 - Mathf.Exp(-Time.unscaledDeltaTime * 28));
            }
            if (next != Hovered) { Hovered?.SetFocus(false); Hovered = next; Hovered?.SetFocus(true); }
            if (!contextInitialized || contextHover != Hovered || contextHeld != Held)
            {
                contextInitialized = true; contextHover = Hovered; contextHeld = Held;
                Hud.Context(Held != null ? $"Đang cầm: {Held.ContextLabel}  •  Chọn vị trí để đặt  •  Q: trả về khay" : Hovered != null ? Hovered.ContextLabel : "Chọn dụng cụ để thao tác  •  Giữ chuột phải: nhìn  •  WASD: di chuyển");
            }
        }
        private void Select()
        {
            pressPointer = PointerProvider.PressPointer;
            if (IsOverUi(pressPointer) || Hud.ModalOpen || ViewLocked || !Experiment.Started) return;
            var wasEmpty = Held == null;
            SelectRay(ViewCamera.ScreenPointToRay(pressPointer));
            pickedThisPress = wasEmpty && Held != null;
        }
        private void ReleaseDrag()
        {
            if (pickedThisPress && Held != null && !OverUi && Vector2.Distance(pressPointer, PointerProvider.Pointer) > 14f) SelectRay(ViewCamera.ScreenPointToRay(PointerProvider.Pointer));
            pickedThisPress = false;
        }
        // Controller and test adapters submit world rays through the same selection path.
        public bool SelectRay(Ray ray)
        {
            if (!Experiment.Started || Hud.ModalOpen || ViewLocked) return false;
            RefreshRay(ray);
            Experiment.MarkActivity();
            if (Held != null)
            {
                if (Hovered is VLabSnapZone zone)
                {
                    var item = Held;
                    if (!zone.TryPlace(item, out var reason)) { Experiment.Mistake(reason); return false; }
                    Held = null; item.SetFocus(false); Experiment.FeedbackSounds?.Snap(); return true;
                }
                Experiment.Mistake("Hãy chọn vùng đặt dụng cụ được ghi nhãn; nhấn Q để trả về khay."); return false;
            }
            if (Hovered is VLabGrabInteractable grab)
            {
                grab.CaptureHome(); grab.Detach(); Held = grab; grab.IsHeld = true; Experiment.FeedbackSounds?.Pick(); return true;
            }
            if (Hovered != null) { Hovered.Activate(); Experiment.FeedbackSounds?.Pick(); return true; }
            return false;
        }
        public void ReturnHeld() { if (Held != null) Held.ResetState(); Held = null; }
    }
}
