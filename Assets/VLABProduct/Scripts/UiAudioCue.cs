using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Vlab.Product
{
    public sealed class UiAudioCue : MonoBehaviour,IPointerEnterHandler,ISelectHandler
    {
        Button button;
        public void Initialize(Button target)
        {
            button=target;button.onClick.AddListener(Click);
        }
        void Click()
        {
            if(!button||!button.IsInteractable())return;
            string label=button.name.ToLowerInvariant();
            VlabAudio.Play(label.Contains("back")||label.Contains("quay lại")?VlabSound.Back:VlabSound.Click);
        }
        public void OnPointerEnter(PointerEventData data){Hover();}
        public void OnSelect(BaseEventData data){Hover();}
        void Hover(){if(button&&button.IsInteractable())VlabAudio.Play(VlabSound.Hover);}
        void OnDestroy(){if(button)button.onClick.RemoveListener(Click);}
    }
}
