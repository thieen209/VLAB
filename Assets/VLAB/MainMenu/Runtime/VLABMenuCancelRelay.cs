using UnityEngine;
using UnityEngine.EventSystems;

namespace VLAB.MainMenu
{
    // The existing XRUIInputModule sends Cancel to the selected control itself.
    // Forward it to the scene's existing navigation owner, without another input stack.
    public sealed class VLABMenuCancelRelay : MonoBehaviour, ICancelHandler
    {
        public void OnCancel(BaseEventData eventData)
        {
            GetComponentInParent<VLABApplicationUI>()?.NavigateBack();
            eventData.Use();
        }
    }
}
