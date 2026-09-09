using UnityEngine;
using UnityEngine.EventSystems;
namespace VLAB.DemoLabs
{
    public sealed class SpecimenPointer : MonoBehaviour, IPointerClickHandler
    {
        public MicroscopeView View;
        public void OnPointerClick(PointerEventData eventData) => View.OnPointerClick(eventData);
    }
}
