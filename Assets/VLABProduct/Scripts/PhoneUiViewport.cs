using UnityEngine;
using UnityEngine.UI;

namespace Vlab.Product
{
    // Phone layout is fitted to safe pixels; VR retains its authored world-space plane.
    // This is bounds correctness, not a claim that portrait letterboxing supplies 44dp targets.
    public sealed class PhoneUiViewport:MonoBehaviour
    {
        public static readonly Vector2 LogicalSize=new Vector2(1600,1000);
        Canvas canvas;RectTransform surface;CanvasScaler scaler;
        Vector2 lastSize;Rect lastSafe;RenderMode lastMode;bool ready,hasOverride;
        Vector2 overrideSize;Rect overrideSafe;
        public Rect SafePixels{get;private set;}
        public Rect ContentPixels{get;private set;}
        public float PixelsPerLogicalUnit{get;private set;}=1;
        public bool Portrait=>SafePixels.height>SafePixels.width;
        public void Initialize(Canvas value,RectTransform content)
        {canvas=value;surface=content;scaler=canvas.GetComponent<CanvasScaler>();Refresh();}
        public static Rect Fit(Vector2 screen,Rect requested)
        {
            float sw=Mathf.Max(1,screen.x),sh=Mathf.Max(1,screen.y);
            if(requested.width<1||requested.height<1)requested=new Rect(0,0,sw,sh);
            float left=Mathf.Clamp(requested.xMin,0,sw-1),bottom=Mathf.Clamp(requested.yMin,0,sh-1);
            float right=Mathf.Clamp(requested.xMax,left+1,sw),top=Mathf.Clamp(requested.yMax,bottom+1,sh);
            float scale=Mathf.Min((right-left)/LogicalSize.x,(top-bottom)/LogicalSize.y);
            return new Rect((left+right-LogicalSize.x*scale)*.5f,(bottom+top-LogicalSize.y*scale)*.5f,LogicalSize.x*scale,LogicalSize.y*scale);
        }
        // Native screenshot fixture can supply a notch without changing OS/device settings.
        public void SetViewportOverride(Vector2 size,Rect safe){hasOverride=true;overrideSize=size;overrideSafe=safe;ready=false;Refresh();}
        public void ClearViewportOverride(){hasOverride=false;ready=false;Refresh();}
        void LateUpdate(){Refresh();}
        public void Refresh()
        {
            if(!canvas||!surface)return;
            Vector2 size=new Vector2(Mathf.Max(1,Screen.width),Mathf.Max(1,Screen.height));Rect safe=Screen.safeArea;
            // Offscreen native captures are real alternate phone aspect ratios.
            if(canvas.renderMode==RenderMode.ScreenSpaceCamera&&canvas.worldCamera&&canvas.worldCamera.targetTexture)
            {var target=canvas.worldCamera.targetTexture;size=new Vector2(target.width,target.height);safe=new Rect(Vector2.zero,size);}
            if(hasOverride){size=overrideSize;safe=overrideSafe;}
            // Flat layout already converts logical units to pixels; a second Canvas scale clips it.
            if(canvas.renderMode!=RenderMode.WorldSpace)
            {scaler.uiScaleMode=CanvasScaler.ScaleMode.ConstantPixelSize;scaler.scaleFactor=1;canvas.scaleFactor=1;}
            if(ready&&lastSize==size&&lastSafe==safe&&lastMode==canvas.renderMode)return;
            ready=true;lastSize=size;lastSafe=safe;lastMode=canvas.renderMode;
            if(canvas.renderMode==RenderMode.WorldSpace)
            {
                surface.anchorMin=Vector2.zero;surface.anchorMax=Vector2.one;surface.pivot=Vector2.one*.5f;
                surface.offsetMin=surface.offsetMax=Vector2.zero;surface.localScale=Vector3.one;
                PixelsPerLogicalUnit=1;return;
            }
            scaler.uiScaleMode=CanvasScaler.ScaleMode.ConstantPixelSize;scaler.scaleFactor=1;
            ContentPixels=Fit(size,safe);PixelsPerLogicalUnit=ContentPixels.width/LogicalSize.x;
            SafePixels=safe.width>=1&&safe.height>=1?safe:new Rect(Vector2.zero,size);
            surface.anchorMin=surface.anchorMax=Vector2.one*.5f;surface.pivot=Vector2.one*.5f;
            surface.sizeDelta=LogicalSize;surface.anchoredPosition=ContentPixels.center-size*.5f;
            surface.localScale=Vector3.one*PixelsPerLogicalUnit;
        }
    }
}
