using System;
using UnityEngine;

namespace VLAB.PhysicsLab.Common
{
    public abstract class LabMeasurementSource : ExperimentObject
    {
        [SerializeField] private string measurementName = "measurement";
        [SerializeField] private string siUnit = "SI";
        [SerializeField] private double latestValue;

        public event Action<double> MeasurementUpdated;

        public string MeasurementName => measurementName;
        public string Unit => siUnit;
        public double LatestValue => latestValue;

        public void ConfigureMeasurement(string name, string unit)
        {
            measurementName = string.IsNullOrWhiteSpace(name) ? "measurement" : name;
            siUnit = string.IsNullOrWhiteSpace(unit) ? "SI" : unit;
        }

        protected void PublishMeasurement(double value)
        {
            latestValue = value;
            MeasurementUpdated?.Invoke(latestValue);
        }

        protected override void OnResetLabObject()
        {
            latestValue = 0d;
            base.OnResetLabObject();
        }
    }

    public abstract class LabSensor : LabMeasurementSource
    {
        [SerializeField] private bool sensorEnabled = true;
        public bool SensorEnabled => sensorEnabled;
        public void SetSensorEnabled(bool value) => sensorEnabled = value;
    }
}
