using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Vlab.Product
{
    // A tap keeps the existing single adjustment; holding repeats without rebuilding the dialog.
    public sealed class HoldToAdjust : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        Button button;bool held;float next;
        void Awake(){button=GetComponent<Button>();}
        public void OnPointerDown(PointerEventData data){if(data.button!=PointerEventData.InputButton.Left||!button.IsInteractable())return;held=true;next=Time.unscaledTime+.45f;}
        public void OnPointerUp(PointerEventData data){held=false;}
        public void OnPointerExit(PointerEventData data){held=false;}
        void OnDisable(){held=false;}
        void OnApplicationPause(bool paused){if(paused)held=false;}
        void OnApplicationFocus(bool focused){if(!focused)held=false;}
        void Update(){if(!held)return;if(!button.IsInteractable()){held=false;return;}if(Time.unscaledTime<next)return;next=Time.unscaledTime+.1f;button.onClick.Invoke();}
    }
}
