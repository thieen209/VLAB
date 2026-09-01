using UnityEngine;

namespace VLAB.PhysicsLab.Measurement
{
    public sealed class PhotogateTimerLink : MonoBehaviour
    {
        [SerializeField] private PhysicsPhotogate photogate;
        [SerializeField] private DigitalTimerMC964 timer;
        [SerializeField] private TimerPort port;

        private void OnEnable()
        {
            if (photogate != null)
            {
                photogate.GateStateChanged += Forward;
            }
        }

        private void OnDisable()
        {
            if (photogate != null)
            {
                photogate.GateStateChanged -= Forward;
            }
        }

        public void Configure(PhysicsPhotogate source, DigitalTimerMC964 destination, TimerPort destinationPort)
        {
            if (isActiveAndEnabled && photogate != null)
            {
                photogate.GateStateChanged -= Forward;
            }
            photogate = source;
            timer = destination;
            port = destinationPort;
            if (isActiveAndEnabled && photogate != null)
            {
                photogate.GateStateChanged += Forward;
            }
        }

        private void Forward(PhotogateEvent gateEvent)
        {
            timer?.ReceivePhotogateEvent(port, gateEvent);
        }
    }
}
