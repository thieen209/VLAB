using UnityEngine;

namespace VLAB.PhysicsLab.Common
{
    public class ExperimentObject : LabResettable
    {
        [SerializeField] private string objectId = "experiment-object";
        [SerializeField] private bool available = true;

        public string ObjectId => objectId;
        public bool Available => available;

        public void ConfigureIdentity(string id)
        {
            objectId = string.IsNullOrWhiteSpace(id) ? gameObject.name : id;
        }

        public void SetAvailable(bool value)
        {
            available = value;
        }
    }
}
