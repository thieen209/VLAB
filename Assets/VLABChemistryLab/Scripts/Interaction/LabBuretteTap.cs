using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace VLAB.ChemistryLab.Interaction
{
    [RequireComponent(typeof(XRSimpleInteractable))]
    public sealed class LabBuretteTap : MonoBehaviour, ILabCommandTarget
    {
        [SerializeField] private LabLiquidVessel burette;
        [SerializeField] private Transform outlet;
        private XRSimpleInteractable interactable;
        private bool flowing;
        private bool desktopHeld;
        private float desktopHoldStarted;
        public bool IsDesktopHeld => desktopHeld;
        private double pendingMl;
        private readonly LabPressGate gate = new LabPressGate();
        public void Configure(LabLiquidVessel source, Transform tip) { burette = source; outlet = tip; }
        private void Awake() { interactable = GetComponent<XRSimpleInteractable>(); }
        private void OnEnable()
        {
            interactable.selectEntered.AddListener(Selected);
            interactable.selectExited.AddListener(Released);
            interactable.activated.AddListener(Activated);
            interactable.deactivated.AddListener(Deactivated);
        }
        private void OnDisable()
        {
            flowing = false; EndDesktopHold();
            interactable.selectEntered.RemoveListener(Selected);
            interactable.selectExited.RemoveListener(Released);
            interactable.activated.RemoveListener(Activated);
            interactable.deactivated.RemoveListener(Deactivated);
        }
        private void Selected(SelectEnterEventArgs _) => TryActivate();
        private void Released(SelectExitEventArgs _) { flowing = false; pendingMl = 0; }
        private void Activated(ActivateEventArgs _) { flowing = true; }
        private void Deactivated(DeactivateEventArgs _) { flowing = false; pendingMl = 0; }
        public void TryActivate()
        {
            if (gate.TryPress(Time.unscaledTime, isActiveAndEnabled)) burette.Pour(.01, outlet.position);
        }
        public void BeginDesktopHold()
        {
            if (!isActiveAndEnabled || desktopHeld) return;
            desktopHeld = true;
            desktopHoldStarted = Time.unscaledTime;
            TryActivate();
        }
        public void EndDesktopHold() { desktopHeld = false; pendingMl = 0; }
        public void StopFlow() { flowing = false; EndDesktopHold(); }
        private void Update()
        {
            // A short click remains one drop; a deliberate hold opens continuous flow.
            if ((flowing && interactable.isSelected) || (desktopHeld && Time.unscaledTime - desktopHoldStarted >= .25f))
            {
                // Quantize to measured drops: per-frame rounding in the lesson must not cause titre drift.
                pendingMl += Time.deltaTime * (burette.Station.IsRinsing ? 5f : .5f);
                double amount = System.Math.Floor(pendingMl * 100) / 100;
                if (amount >= .01) { pendingMl -= amount; burette.Pour(amount, outlet.position); }
            }
        }
    }
}
