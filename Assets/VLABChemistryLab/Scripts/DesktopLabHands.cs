using UnityEngine;

namespace VLAB.ChemistryLab
{
    /// <summary>Compatibility stub for saved scenes. Decorative hands have been retired.</summary>
    public sealed class DesktopLabHands : MonoBehaviour
    {
        private void Awake() => Destroy(this);
    }
}
