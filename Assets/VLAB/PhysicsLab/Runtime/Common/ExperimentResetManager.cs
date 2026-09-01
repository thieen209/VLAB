using System.Collections.Generic;
using UnityEngine;

namespace VLAB.PhysicsLab.Common
{
    public sealed class ExperimentResetManager : MonoBehaviour
    {
        [SerializeField] private List<LabResettable> resettables = new List<LabResettable>();

        public IReadOnlyList<LabResettable> Resettables => resettables;

        public void Register(LabResettable resettable)
        {
            if (resettable != null && !resettables.Contains(resettable))
            {
                resettables.Add(resettable);
            }
        }

        public void Unregister(LabResettable resettable)
        {
            if (resettable != null)
            {
                resettables.Remove(resettable);
            }
        }

        [ContextMenu("Capture Reset States")]
        public void CaptureAll()
        {
            CollectChildrenIfEmpty();
            foreach (var resettable in resettables)
            {
                resettable?.CaptureResetState();
            }
        }

        [ContextMenu("Reset All")]
        public void ResetAll()
        {
            CollectChildrenIfEmpty();
            foreach (var point in GetComponentsInChildren<LabAttachmentPoint>(true))
            {
                point?.Detach();
            }
            foreach (var resettable in resettables)
            {
                resettable?.ResetLabObject();
            }
        }

        private void CollectChildrenIfEmpty()
        {
            if (resettables.Count == 0)
            {
                GetComponentsInChildren(true, resettables);
            }
        }
    }
}
