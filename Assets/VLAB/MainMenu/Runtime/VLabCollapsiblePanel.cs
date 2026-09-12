using UnityEngine;
using UnityEngine.UI;

namespace VLAB.MainMenu
{
    public static class VLabCollapsiblePanel
    {
        public static void Configure(Canvas canvas)
        {
            var ui=new VLABUI(Resources.Load<VLABMenuAssets>("VLABMenuAssets").font);
            var children=new Transform[canvas.transform.childCount];
            for(int i=0;i<children.Length;i++)children[i]=canvas.transform.GetChild(i);
            var content=ui.Rect(canvas.transform,"Guidance content",0,0,1,1);
            foreach(var child in children)child.SetParent(content,false);
            Button toggle=null;
            toggle=ui.Button(canvas.transform,"ToggleGuidance","×",.9f,.94f,.10f,.075f,()=>
            {
                content.gameObject.SetActive(!content.gameObject.activeSelf);
                var shown=content.gameObject.activeSelf;
                toggle.GetComponentInChildren<Text>().text=shown?"×":"Hiện hướng dẫn";
                var rect=(RectTransform)toggle.transform;rect.anchorMin=new Vector2(shown?.9f:.59f,.94f);
            });
        }
    }
}
