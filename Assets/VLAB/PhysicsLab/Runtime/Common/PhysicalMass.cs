using UnityEngine;

namespace VLAB.PhysicsLab.Common
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PhysicalMass : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float massKilograms = 0.10f;
        public float MassKilograms => massKilograms;

        public void Configure(float value)
        {
            massKilograms = Mathf.Max(0.01f, value);
            GetComponent<Rigidbody>().mass = massKilograms;
        }
    }
}
