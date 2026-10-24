using UnityEngine;

namespace Vlab.Product
{
    // One authored column; fit to safe pixels without shrinking individual captions.
    public sealed class WorkbenchPanelLayout:MonoBehaviour
    {
        VlabUi ui;RectTransform panel;Vector2 size;bool modal,center;
        public void Initialize(VlabUi owner,Vector2 authoredSize,bool modalPanel=false,bool centered=false){ui=owner;panel=(RectTransform)transform;size=authoredSize;modal=modalPanel;center=centered;LateUpdate();}
        void LateUpdate()
        {
            if(ui==null||!panel)return;
            panel.anchorMin=panel.anchorMax=panel.pivot=new Vector2(0,1);panel.sizeDelta=size;
            if(ui.canvas.renderMode==UnityEngine.RenderMode.WorldSpace)
            {
                if(modal){var root=(RectTransform)panel.parent;root.localScale=Vector3.one*.54f;root.localPosition=new Vector3(0,0,-843.75f);}
                panel.localScale=Vector3.one;panel.anchoredPosition=center?new Vector2((1600-size.x)/2,-(1000-size.y)/2):new Vector2(40,-80);return;
            }
            float unit=Mathf.Max(.001f,ui.PhoneViewport.PixelsPerLogicalUnit);var safe=ui.PhoneViewport.SafePixels;var fitted=ui.PhoneViewport.ContentPixels;
            float w=safe.width/unit,h=safe.height/unit;
            float scale=center?Mathf.Min((h-64)/size.y,(w-96)/size.x):Mathf.Min((h-(modal?100:300))/size.y,(w*.36f-32)/size.x);
            if(h>w&&!center)scale=Mathf.Min((w-48)/size.x,(h*.48f-32)/size.y);
            scale=Mathf.Max(.1f,scale);panel.localScale=Vector3.one*scale;
            panel.anchoredPosition=new Vector2((safe.xMin-fitted.xMin)/unit+(center?(w-size.x*scale)/2:24),-(fitted.yMax-safe.yMax)/unit-(center?(h-size.y*scale)/2:32));
        }
    }
}
