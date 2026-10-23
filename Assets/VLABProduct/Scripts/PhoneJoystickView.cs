using UnityEngine;
using UnityEngine.UI;

namespace Vlab.Product
{
    // Display only. VlabInput owns touch claims/deadzone/movement; these graphics never intercept UI.
    public sealed class PhoneJoystickView:MonoBehaviour
    {
        Canvas canvas;PhoneUiViewport viewport;RectTransform host,thumb;Image ring,knob;
        static Sprite dot;
        public void Initialize(Canvas value,PhoneUiViewport fit)
        {
            canvas=value;viewport=fit;
            host=new GameObject("Phone movement joystick",typeof(RectTransform)).GetComponent<RectTransform>();host.SetParent(transform,false);host.anchorMin=host.anchorMax=Vector2.one*.5f;host.pivot=Vector2.one*.5f;
            ring=host.gameObject.AddComponent<Image>();ring.sprite=VlabUi.PointerRing();ring.raycastTarget=false;
            thumb=new GameObject("Phone joystick thumb",typeof(RectTransform)).GetComponent<RectTransform>();thumb.SetParent(host,false);thumb.anchorMin=thumb.anchorMax=Vector2.one*.5f;thumb.pivot=Vector2.one*.5f;
            knob=thumb.gameObject.AddComponent<Image>();knob.sprite=Dot();knob.raycastTarget=false;host.gameObject.SetActive(false);
        }
        static Sprite Dot()
        {
            if(dot)return dot;const int size=64;var texture=new Texture2D(size,size,TextureFormat.RGBA32,false);
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)texture.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(27.5f-Vector2.Distance(new Vector2(x+.5f,y+.5f),Vector2.one*32))));
            texture.Apply(false,true);dot=Sprite.Create(texture,new Rect(0,0,size,size),Vector2.one*.5f);return dot;
        }
        void LateUpdate(){Refresh();}
        public void Refresh()
        {
            if(!host||!canvas)return;var input=VlabApp.Instance?VlabApp.Instance.Input:null;
            bool visible=canvas.renderMode!=RenderMode.WorldSpace&&input&&input.PhoneControlsVisible;
            if(host.gameObject.activeSelf!=visible)host.gameObject.SetActive(visible);if(!visible)return;
            viewport.Refresh();var safe=viewport.SafePixels;var centre=input.PhoneJoystickCenter;
            Vector2 pixels=canvas.renderMode==RenderMode.ScreenSpaceCamera&&canvas.worldCamera&&canvas.worldCamera.targetTexture?new Vector2(canvas.worldCamera.targetTexture.width,canvas.worldCamera.targetTexture.height):new Vector2(Screen.width,Screen.height);
            float radius=safe.height*input.PhoneJoystickRadius;host.anchoredPosition=new Vector2(safe.x+safe.width*centre.x,safe.y+safe.height*centre.y)-pixels*.5f;host.sizeDelta=Vector2.one*radius*2;
            thumb.sizeDelta=Vector2.one*radius*.7f;// Keep the knob inside its disc even at full input near a safe-area edge.
            thumb.anchoredPosition=Vector2.ClampMagnitude(input.PhoneJoystickOffset,1)*(radius*.65f);
            ring.color=new Color(.82f,.9f,.93f,input.PhoneJoystickActive?.95f:.65f);knob.color=input.PhoneJoystickActive?VlabUi.Hex("3B82F6"):UiTheme.Current.secondary;
            host.SetAsLastSibling();
        }
    }
}
