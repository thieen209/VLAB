using UnityEngine;

namespace VLAB.Core.Input
{
    /// <summary>Optional pose boundary for a tracked or calibrated 3DoF controller.</summary>
    public interface IVLabRayProvider
    {
        bool TryGetRay(Camera view, out Ray ray);
    }
}
