using System;
using UnityEngine;
using VLAB.PhysicsLab.Common;

namespace VLAB.PhysicsLab.Measurement
{
    public enum TimerMode { A, B, APlusB, AMinusB, T }
    public enum TimerPort { A, B, C }
    public enum TimerResolution { Milliseconds, Centiseconds }

    public sealed class DigitalTimerMC964 : LabMeasurementSource
    {
        [SerializeField] private TimerMode mode;
        [SerializeField] private TimerResolution resolution = TimerResolution.Milliseconds;
        [SerializeField] private double measuredTime;

        private double aStart = double.NaN;
        private double bStart = double.NaN;
        private double lastAPulse = double.NaN;
        private double lastBPulse = double.NaN;

        public event Action<double> DisplayChanged;

        public TimerMode Mode => mode;
        public TimerResolution Resolution => resolution;
        public double MeasuredTime => measuredTime;
        public string DisplayText => measuredTime.ToString(resolution == TimerResolution.Milliseconds ? "0.000" : "0.00");

        protected override void Awake()
        {
            base.Awake();
            ConfigureMeasurement("time", "s");
        }

        public void SetMode(TimerMode nextMode)
        {
            mode = nextMode;
            ResetTimer();
        }

        public void SetResolution(TimerResolution nextResolution)
        {
            resolution = nextResolution;
            SetMeasuredTime(measuredTime);
        }

        public void CycleMode()
        {
            SetMode((TimerMode)(((int)mode + 1) % Enum.GetValues(typeof(TimerMode)).Length));
        }

        public void ProcessGateEvent(TimerPort port, bool blocked, double timestampSeconds)
        {
            switch (mode)
            {
                case TimerMode.A:
                    ProcessSinglePort(TimerPort.A, port, blocked, timestampSeconds, ref aStart);
                    break;
                case TimerMode.B:
                    ProcessSinglePort(TimerPort.B, port, blocked, timestampSeconds, ref bStart);
                    break;
                case TimerMode.APlusB:
                    if (port == TimerPort.A && blocked)
                    {
                        aStart = timestampSeconds;
                    }
                    else if (port == TimerPort.B && blocked && !double.IsNaN(aStart))
                    {
                        SetMeasuredTime(timestampSeconds - aStart);
                    }
                    break;
                case TimerMode.AMinusB:
                    if (!blocked)
                    {
                        return;
                    }
                    if (port == TimerPort.A)
                    {
                        lastAPulse = timestampSeconds;
                    }
                    else if (port == TimerPort.B)
                    {
                        lastBPulse = timestampSeconds;
                    }
                    if (!double.IsNaN(lastAPulse) && !double.IsNaN(lastBPulse))
                    {
                        SetMeasuredTime(Math.Abs(lastAPulse - lastBPulse));
                    }
                    break;
                case TimerMode.T:
                    if (port == TimerPort.A && blocked)
                    {
                        if (!double.IsNaN(lastAPulse))
                        {
                            SetMeasuredTime(timestampSeconds - lastAPulse);
                        }
                        lastAPulse = timestampSeconds;
                    }
                    break;
            }
        }

        public void ReceivePhotogateEvent(TimerPort port, PhotogateEvent gateEvent)
        {
            ProcessGateEvent(port, gateEvent.Blocked, gateEvent.TimestampSeconds);
        }

        [ContextMenu("Reset Timer")]
        public void ResetTimer()
        {
            aStart = bStart = lastAPulse = lastBPulse = double.NaN;
            SetMeasuredTime(0d);
        }

        protected override void OnResetLabObject()
        {
            base.OnResetLabObject();
            ResetTimer();
        }

        private void ProcessSinglePort(TimerPort required, TimerPort received, bool blocked, double timestamp, ref double start)
        {
            if (required != received)
            {
                return;
            }
            if (blocked)
            {
                start = timestamp;
            }
            else if (!double.IsNaN(start))
            {
                SetMeasuredTime(timestamp - start);
                start = double.NaN;
            }
        }

        private void SetMeasuredTime(double seconds)
        {
            var step = resolution == TimerResolution.Milliseconds ? 0.001d : 0.01d;
            measuredTime = Math.Max(0d, Math.Round(seconds / step, MidpointRounding.AwayFromZero) * step);
            PublishMeasurement(measuredTime);
            DisplayChanged?.Invoke(measuredTime);
        }
    }
}
