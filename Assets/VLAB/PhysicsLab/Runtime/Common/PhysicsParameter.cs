using System;
using UnityEngine;

namespace VLAB.PhysicsLab.Common
{
    [CreateAssetMenu(menuName = "VLAB/Physics Lab/Physics Parameter", fileName = "PhysicsParameter")]
    public sealed class PhysicsParameter : ScriptableObject
    {
        [SerializeField] private string parameterId = "parameter";
        [SerializeField] private string unit = "SI";
        [SerializeField] private float minimum;
        [SerializeField] private float maximum = 1f;
        [SerializeField] private float value;

        public event Action<float> ValueChanged;

        public string ParameterId => parameterId;
        public string Unit => unit;
        public float Minimum => minimum;
        public float Maximum => maximum;
        public float Value => value;

        public void Configure(string id, string siUnit, float min, float max, float initialValue)
        {
            parameterId = string.IsNullOrWhiteSpace(id) ? "parameter" : id;
            unit = string.IsNullOrWhiteSpace(siUnit) ? "SI" : siUnit;
            minimum = Mathf.Min(min, max);
            maximum = Mathf.Max(min, max);
            SetValue(initialValue);
        }

        public void SetValue(float nextValue)
        {
            var clamped = Mathf.Clamp(nextValue, minimum, maximum);
            if (Mathf.Approximately(value, clamped))
            {
                return;
            }

            value = clamped;
            ValueChanged?.Invoke(value);
        }

        private void OnValidate()
        {
            if (maximum < minimum)
            {
                maximum = minimum;
            }

            value = Mathf.Clamp(value, minimum, maximum);
        }
    }
}
