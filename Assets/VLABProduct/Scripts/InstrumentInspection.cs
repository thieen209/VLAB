using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Vlab.Product
{
    // Maps the displayed/cropped RawImage back into the source texture's UV coordinates.
    public sealed class InstrumentInspection:MonoBehaviour,IPointerClickHandler
    {
        public Action<Vector2> Selected;
        public bool Circular;
        public static bool InsideEyepiece(Vector2 normalized)=>(normalized-Vector2.one*.5f).sqrMagnitude<=.25f;
        public Color MarkerColor=Color.cyan;RectTransform marker;
        public void SetSelection(Vector2? sourceUv)
        {
            var image=GetComponent<RawImage>();if(!image||!sourceUv.HasValue)return;
            Rect uv=image.uvRect;float x=(sourceUv.Value.x-uv.x)/uv.width,y=(sourceUv.Value.y-uv.y)/uv.height;
            if(x<0||x>1||y<0||y>1||(Circular&&!InsideEyepiece(new Vector2(x,y)))){if(marker)marker.gameObject.SetActive(false);return;}
            if(!marker)
            {
                var go=new GameObject("Selected image location",typeof(RectTransform),typeof(UnityEngine.UI.Image));go.transform.SetParent(transform,false);marker=go.GetComponent<RectTransform>();
                marker.anchorMin=marker.anchorMax=Vector2.zero;marker.pivot=Vector2.one*.5f;marker.sizeDelta=new Vector2(30,30);
                var graphic=go.GetComponent<UnityEngine.UI.Image>();graphic.sprite=VlabUi.PointerRing();graphic.color=MarkerColor;graphic.raycastTarget=false;
            }
            marker.gameObject.SetActive(true);Rect rect=image.rectTransform.rect;marker.anchoredPosition=new Vector2(x*rect.width,y*rect.height);
        }
        public void OnPointerClick(PointerEventData data)
        {
            var image=GetComponent<RawImage>();if(!image||!image.texture)return;
            var canvas=GetComponentInParent<Canvas>();Camera camera=canvas&&canvas.renderMode!=RenderMode.ScreenSpaceOverlay?canvas.worldCamera:null;
            if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(image.rectTransform,data.position,camera,out var local))return;
            Rect rect=image.rectTransform.rect;if(!rect.Contains(local)||rect.width<=0||rect.height<=0)return;
            float x=(local.x-rect.xMin)/rect.width,y=(local.y-rect.yMin)/rect.height;
            if(Circular&&!InsideEyepiece(new Vector2(x,y)))return;
            Rect uv=image.uvRect;Selected?.Invoke(new Vector2(uv.x+x*uv.width,uv.y+y*uv.height));
        }
    }
}
