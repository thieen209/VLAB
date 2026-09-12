using UnityEngine;

namespace VLAB.ChemistryLab
{
    /// <summary>Keeps physical control labels readable while the player explores the room.</summary>
    public sealed class FloatingLabelBillboard : MonoBehaviour
    {
        private Camera targetCamera;

        private void LateUpdate()
        {
            if (targetCamera == null) targetCamera = Camera.main;
            if (targetCamera == null) return;
            Vector3 direction = transform.position - targetCamera.transform.position;
            if (direction.sqrMagnitude > .0001f)
                transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }
    }
}
