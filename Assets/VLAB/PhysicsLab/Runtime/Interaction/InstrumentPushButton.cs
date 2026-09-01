using System;
using UnityEngine;
using VLAB.PhysicsLab.Common;
using VLAB.PhysicsLab.Measurement;
using VLAB.PhysicsLab.Projectile;

namespace VLAB.PhysicsLab.Interaction
{
    public enum InstrumentButtonAction { ProjectileTrigger, TimerReset, TimerMode, TimerResolution }

    [RequireComponent(typeof(LabInteractable), typeof(Collider))]
    public sealed class InstrumentPushButton : MonoBehaviour
    {
        [SerializeField] private InstrumentButtonAction action;
        [SerializeField] private ProjectileLauncher launcher;
        [SerializeField] private DigitalTimerMC964 timer;
        private LabInteractable interactable;

        public event Action<InstrumentPushButton> Pressed;
        public InstrumentButtonAction Action => action;

        public void Configure(InstrumentButtonAction buttonAction, ProjectileLauncher targetLauncher = null, DigitalTimerMC964 targetTimer = null)
        {
            action = buttonAction;
            launcher = targetLauncher;
            timer = targetTimer;
        }

        private void Awake() => interactable = GetComponent<LabInteractable>();
        private void OnEnable()
        {
            if (interactable == null) interactable = GetComponent<LabInteractable>();
            interactable.Activated += Press;
        }
        private void OnDisable()
        {
            if (interactable != null) interactable.Activated -= Press;
        }

        public void Press()
        {
            switch (action)
            {
                case InstrumentButtonAction.ProjectileTrigger: launcher?.Launch(); break;
                case InstrumentButtonAction.TimerReset: timer?.ResetTimer(); break;
                case InstrumentButtonAction.TimerMode: timer?.CycleMode(); break;
                case InstrumentButtonAction.TimerResolution:
                    if (timer != null) timer.SetResolution(timer.Resolution == TimerResolution.Milliseconds ? TimerResolution.Centiseconds : TimerResolution.Milliseconds);
                    break;
            }
            Pressed?.Invoke(this);
        }
    }
}
