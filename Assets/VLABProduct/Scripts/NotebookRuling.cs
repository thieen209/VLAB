using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vlab.Product
{
    // Lines belong to the writing viewport, below its ink. Their positions follow
    // actual TMP baselines, including line wrapping, large text and input scrolling.
    [DefaultExecutionOrder(100)]
    public sealed class NotebookRuling:MonoBehaviour
    {
        TMP_InputField field;RectTransform viewport;readonly List<RectTransform> rules=new List<RectTransform>();readonly List<float> visible=new List<float>();
        TMP_Text measuredSource;TMP_FontAsset measuredFont;string measuredText;float measuredSize,measuredSpacing;Vector2 measuredPosition,measuredRect,measuredView;
        public void Bind(TMP_InputField input){field=input;viewport=(RectTransform)transform;Refresh();}
        void LateUpdate(){Refresh();}
        public void Refresh()
        {
            if(!field||!viewport)return;
            var source=string.IsNullOrEmpty(field.text)?field.placeholder as TMP_Text:field.textComponent;if(!source)return;
            var rect=source.rectTransform;Vector2 position=rect.anchoredPosition,size=rect.rect.size,view=viewport.rect.size;
            if(source==measuredSource&&source.font==measuredFont&&source.text==measuredText&&source.fontSize==measuredSize&&source.lineSpacing==measuredSpacing&&position==measuredPosition&&size==measuredRect&&view==measuredView)return;
            source.ForceMeshUpdate(true,true);visible.Clear();int count=source.textInfo.lineCount;
            if(count>0)
            {
                float first=Baseline(source,0),last=Baseline(source,count-1);
                float advance=count>1?first-Baseline(source,1):source.GetPreferredValues("Ag\nAg",Mathf.Max(100,size.x),float.PositiveInfinity).y-source.GetPreferredValues("Ag",Mathf.Max(100,size.x),float.PositiveInfinity).y;
                // Preferred-size probes may reuse TMP's internal text buffers.
                if(count==1)source.ForceMeshUpdate(true,true);
                if(!float.IsFinite(advance)||advance<1)advance=source.fontSize*1.2f;
                for(int i=0;i<count;i++)Add(Baseline(source,i));
                for(float y=first+advance;y<viewport.rect.yMax;y+=advance)Add(y);
                for(float y=last-advance;y>viewport.rect.yMin;y-=advance)Add(y);
            }
            while(rules.Count<visible.Count)
            {
                var line=new GameObject("Writing baseline rule",typeof(RectTransform),typeof(Image));var rule=line.GetComponent<RectTransform>();rule.SetParent(viewport,false);rule.anchorMin=rule.anchorMax=new Vector2(0,1);rule.pivot=new Vector2(0,1);rule.SetAsFirstSibling();var image=line.GetComponent<Image>();image.color=VlabUi.Hex("CBD8DE");image.raycastTarget=false;rules.Add(rule);
            }
            for(int i=0;i<rules.Count;i++)
            {
                rules[i].gameObject.SetActive(i<visible.Count);if(i>=visible.Count)continue;
                rules[i].anchoredPosition=new Vector2(0,visible[i]-viewport.rect.yMax);rules[i].sizeDelta=new Vector2(view.x,2);
            }
            measuredSource=source;measuredFont=source.font;measuredText=source.text;measuredSize=source.fontSize;measuredSpacing=source.lineSpacing;measuredPosition=position;measuredRect=size;measuredView=view;
        }
        float Baseline(TMP_Text source,int index)=>viewport.InverseTransformPoint(source.rectTransform.TransformPoint(new Vector3(0,source.textInfo.lineInfo[index].baseline,0))).y;
        // Two logical pixels survive minification in the world-space canvas.
        // With a top pivot, baseline-1 places the stroke midpoint at baseline-2.
        void Add(float baseline){float y=baseline-1;if(y>viewport.rect.yMin&&y<viewport.rect.yMax)visible.Add(y);}
    }
}
