using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Vlab.Product
{
    // Fade presentation only: panel geometry, raycast areas and camera pose stay fixed.
    public sealed class UiEntrance:MonoBehaviour
    {
        const float Duration=.18f;
        CanvasGroup group;float elapsed;bool reduced,ready;
        public void Initialize(bool reducedMotion)
        {
            group=GetComponent<CanvasGroup>();if(!group)group=gameObject.AddComponent<CanvasGroup>();
            ready=true;SetReducedMotion(reducedMotion);Begin();
        }
        public void SetReducedMotion(bool value){reduced=value;if(value&&group){elapsed=Duration;group.alpha=1;}}
        void Begin(){elapsed=reduced?Duration:0;if(group)group.alpha=reduced?1:0;}
        void OnEnable(){if(ready)Begin();}
        void OnDisable(){if(group)group.alpha=1;elapsed=Duration;}
        void Update()
        {
            if(!group||elapsed>=Duration)return;
            elapsed=Mathf.Min(Duration,elapsed+Time.unscaledDeltaTime);float t=elapsed/Duration;
            group.alpha=1-(1-t)*(1-t)*(1-t);
        }
    }

    // Only the caption moves; the Button's physical target never grows or drifts.
    public sealed class UiButtonMotion:MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,IPointerDownHandler,IPointerUpHandler,ISelectHandler,IDeselectHandler
    {
        const float Duration=.1f;
        Button button;RectTransform caption;Vector3 restScale;Vector2 restPosition;
        bool reduced,hover,selected,pressed,ready,custom;float amount=1,from=1,target=1,elapsed=Duration;
        public void Initialize(Button control,RectTransform text,bool reducedMotion)
        {button=control;caption=text;SetReducedMotion(reducedMotion);}
        void Start()
        {
            if(!caption)return;
            restScale=caption.localScale;restPosition=caption.anchoredPosition;ready=true;
            custom=GetComponent<PortalFocus>()||string.IsNullOrEmpty(caption.GetComponent<TMPro.TMP_Text>().text);
        }
        public void SetReducedMotion(bool value)
        {
            reduced=value;
            if(button){var colors=button.colors;colors.fadeDuration=value?0:.08f;button.colors=colors;}
            if(value)Restore();
        }
        public void OnPointerEnter(PointerEventData data){hover=true;Retarget();}
        public void OnPointerExit(PointerEventData data){hover=false;Retarget();}
        public void OnSelect(BaseEventData data){selected=true;Retarget();}
        public void OnDeselect(BaseEventData data){selected=false;Retarget();}
        public void OnPointerDown(PointerEventData data){if(data!=null&&data.button!=PointerEventData.InputButton.Left)return;pressed=true;Retarget();}
        public void OnPointerUp(PointerEventData data){if(data!=null&&data.button!=PointerEventData.InputButton.Left)return;pressed=false;Retarget();}
        void Retarget()
        {
            float next=!reduced&&button&&button.IsInteractable()?(pressed?.975f:hover||selected?1.012f:1):1;
            if(Mathf.Approximately(next,target))return;
            from=amount;target=next;elapsed=0;
        }
        void Update()
        {
            if(!ready||custom)return;
            if(reduced||!button||!button.IsInteractable()){Restore();return;}
            if(elapsed>=Duration)return;
            elapsed=Mathf.Min(Duration,elapsed+Time.unscaledDeltaTime);float t=elapsed/Duration;
            amount=Mathf.LerpUnclamped(from,target,1-(1-t)*(1-t)*(1-t));
            caption.localScale=restScale*amount;
            var size=caption.rect.size;var pivot=caption.pivot;
            caption.anchoredPosition=restPosition+new Vector2(size.x*(.5f-pivot.x),size.y*(.5f-pivot.y))*(1-amount);
        }
        void Restore()
        {
            amount=from=target=1;elapsed=Duration;
            if(ready&&caption){caption.localScale=restScale;caption.anchoredPosition=restPosition;}
        }
        void OnDisable(){hover=selected=pressed=false;Restore();}
    }
}
