#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Vlab.Product;

// Root supplies its real touchscreen-tap coroutine. No direct onClick or lesson-driver invocation.
public static class PhoneDocumentUiChecks
{
    public static IEnumerator Run(Transform parent,Camera camera,Func<Button,IEnumerator> tap,Action<string,bool> check)
    {
        var root=new GameObject("Phone document UI fixtures");root.transform.SetParent(parent,false);
        var ui=new VlabUi(root.transform);ui.canvas.sortingOrder=100;ui.SetReducedMotion(true);
        try
        {
            yield return Fixture(ui,camera,false,tap,check);
            yield return Fixture(ui,camera,true,tap,check);
        }
        finally{root.SetActive(false);UnityEngine.Object.Destroy(root);}
    }
    static IEnumerator Fixture(VlabUi ui,Camera camera,bool instrument,Func<Button,IEnumerator> tap,Action<string,bool> check)
    {
        var page=ui.Page(instrument?"Explain fixture":"Legal fixture");
        var box=ui.Panel(page,"Dialog",80,50,instrument?1440:1120,instrument?900:860);
        ui.Text(box,instrument?"Giải thích kết quả":"Chính sách bảo mật",40,25,900,70,32);
        if(instrument)
        {
            ui.Button(box,"Đóng",1332,20,80,72,()=>{}).name="Close dialog";
            ui.Button(box,"Giải thích",40,116,288,64,()=>{}).name="Instrument tab explain";
            ui.Text(box,"Dùng trợ giúp khi cần.",40,746,1360,54,24);
            ui.Button(box,"Trở lại",920,820,480,64,()=>{});
        }
        else{ui.Text(box,"Tài liệu ngoại tuyến",48,98,1024,42,24);ui.Button(box,"Quay lại",48,754,300,72,()=>{});}
        Transform hostParent=instrument?ui.Rect(box,"Instrument observation output",40,205,920,515):box;
        string body=string.Join("\n\n",Enumerable.Range(1,35).Select(i=>i+". Đọc hướng dẫn, tự thực hiện thao tác và kiểm tra kết quả trước khi ghi vào sổ. Công thức và số liệu cần giữ đúng đơn vị."));
        var host=ui.Scroll(hostParent,body,instrument?0:48,instrument?0:158,instrument?920:1024,instrument?515:566,28);
        if(instrument)ui.Button(box,"Thao tác",1000,205,400,66,()=>{}).name="Instrument control fixture";
        var document=host.GetComponent<VlabScrollContent>();
        var geometry=new[]{host,document.Scroll.viewport,document.Body.rectTransform,(RectTransform)document.Up.transform,(RectTransform)document.Down.transform};
        var sizes=geometry.Select(x=>x.sizeDelta).ToArray();var positions=geometry.Select(x=>x.anchoredPosition).ToArray();float font=document.Body.fontSize;
        ui.ConfigurePhoneDialog(box);yield return null;yield return null;Canvas.ForceUpdateCanvases();
        var layout=box.GetComponent<PhoneDialogLayout>();
        check((instrument?"Explain":"Legal")+" native layout valid",layout.AssertPhoneLayout(out _));
        check("Document keeps authored font size",Mathf.Approximately(document.Body.fontSize,font));
        check("Document text stays at dialog scale",Mathf.Abs(document.Body.rectTransform.lossyScale.x/box.lossyScale.x-1)<.01f);
        check("Document has scrollable overflow",document.Scroll.content.rect.height>document.Scroll.viewport.rect.height+100);
        float before=document.Scroll.verticalNormalizedPosition;
        yield return tap(document.Down);yield return null;
        check("Real UI Page down moves document",document.Scroll.verticalNormalizedPosition<before-.001f);
        float lower=document.Scroll.verticalNormalizedPosition;
        yield return tap(document.Up);yield return null;
        check("Real UI Page up moves document back",document.Scroll.verticalNormalizedPosition>lower+.001f);
        // This verifies canvas geometry restoration only; it is not Cardboard/device acceptance.
        ui.SetWorld(true,camera);yield return null;yield return null;
        bool restored=true;for(int i=0;i<geometry.Length;i++)restored&=(geometry[i].sizeDelta-sizes[i]).sqrMagnitude<.1f&&(geometry[i].anchoredPosition-positions[i]).sqrMagnitude<.1f;
        check("VR document geometry restored",restored&&Mathf.Approximately(document.Body.fontSize,font));
        ui.SetWorld(false,camera);page.gameObject.SetActive(false);UnityEngine.Object.Destroy(page.gameObject);yield return null;
    }
}
#endif
