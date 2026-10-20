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
            yield return Steppers(ui,tap,check);
            yield return Compact(ui,tap,check);
        }
        finally{root.SetActive(false);UnityEngine.Object.Destroy(root);}
    }
    static IEnumerator Compact(VlabUi ui,Func<Button,IEnumerator> tap,Action<string,bool> check)
    {
        var page=ui.Page("Compact station tools fixture");var box=ui.Panel(page,"Dialog",440,205,720,590);
        ui.Text(box,"Dụng cụ & hướng dẫn",40,25,540,70,32);ui.Button(box,"×",612,20,80,72,()=>{}).name="Close dialog";
        int selected=-1;var controls=new List<Button>();
        for(int i=0;i<4;i++){int index=i;controls.Add(ui.Button(box,"Thao tác "+i,40+i%2*336,120+i/2*120,304,88,()=>selected=index));}
        ui.Button(box,"Vào VR",40,360,640,70,()=>{});
        var back=ui.Button(box,"Trở lại trạm",280,470,400,72,()=>selected=4);back.name="Station return";
        ui.ConfigurePhoneDialog(box);box.GetComponent<PhoneDialogLayout>().CompactControls=true;
        yield return null;yield return null;Canvas.ForceUpdateCanvases();
        check("Station tools compact height bounded",box.rect.height<=490);
        check("Station return remains pinned outside scrolling content",!back.GetComponentInParent<ScrollRect>());
        for(int i=0;i<4;i++)
        {
            var rect=(RectTransform)controls[i].transform;
            if(i%2==1)check("Station controls remain paired",Mathf.Abs(rect.anchoredPosition.y-((RectTransform)controls[i-1].transform).anchoredPosition.y)<1);
            yield return tap(controls[i]);check("Station control receives actual tap",selected==i);
        }
        yield return tap(back);check("Pinned station return receives actual tap",selected==4);
        page.gameObject.SetActive(false);UnityEngine.Object.Destroy(page.gameObject);yield return null;
    }
    static IEnumerator Steppers(VlabUi ui,Func<Button,IEnumerator> tap,Action<string,bool> check)
    {
        var page=ui.Page("Stepper layout fixture");var box=ui.Panel(page,"Dialog",250,50,1100,900);
        ui.Text(box,"Thông số thí nghiệm",40,25,900,70,32);ui.Button(box,"×",992,20,80,72,()=>{}).name="Close dialog";
        int value=0;var pairs=new List<(RectTransform label,Button minus,Button plus)>();
        for(int i=0;i<3;i++)
        {
            float top=110+i*115;bool settings=i==1;
            var label=ui.Text(box,settings?"Tốc độ: 1,0 m/s":"Nồng độ mẫu: 0,1 mol/L",40,top+(settings?14:0),settings?500:650,64,26);
            var minus=ui.Button(box,"−",settings?572:730,top,100,64,()=>value--);
            var plus=ui.Button(box,"+",settings?924:850,top,100,64,()=>value++);
            if(settings){ui.Rule(box,716,top+34,192,12,VlabUi.Stroke).name="Settings value track";ui.Rule(box,716,top+34,96,12,VlabUi.Stroke).name="Settings value fill";}
            pairs.Add((label.rectTransform,minus,plus));
        }
        var fixedLabel=ui.Text(box,"Thể tích mẫu 10 mL",40,455,650,64,26);
        var fixedValue=ui.Text(box,"Cố định",730,465,220,55,22);fixedValue.name="Parameter fixed sampleVolume";
        ui.Button(box,"Quay lại",40,820,300,64,()=>{});ui.ConfigurePhoneDialog(box);box.GetComponent<PhoneDialogLayout>().ParameterPanel=true;
        yield return null;yield return null;Canvas.ForceUpdateCanvases();
        check("Fixed parameter status retains its label row",Mathf.Abs(fixedLabel.rectTransform.anchoredPosition.y-fixedValue.rectTransform.anchoredPosition.y)<1);
        if(ui.PhoneViewport.SafePixels.width>ui.PhoneViewport.SafePixels.height)
            check("Parameter sheet leaves at least half of landscape for apparatus",box.rect.width*box.localScale.x*ui.PhoneViewport.PixelsPerLogicalUnit<=ui.PhoneViewport.SafePixels.width*.5f);
        foreach(var pair in pairs)
        {
            var minus=(RectTransform)pair.minus.transform;var plus=(RectTransform)pair.plus.transform;
            check("Stepper label and both controls retain one row",Mathf.Abs(pair.label.anchoredPosition.y-minus.anchoredPosition.y)<1&&Mathf.Abs(minus.anchoredPosition.y-plus.anchoredPosition.y)<1);
            check("Stepper controls remain adjacent and tappable size",minus.rect.width>=80&&plus.rect.width>=80&&Mathf.Abs(plus.anchoredPosition.x-minus.anchoredPosition.x-minus.rect.width-14)<1);
            yield return tap(pair.plus);check("Real plus tap changes associated value",value==1);
            yield return tap(pair.minus);check("Real minus tap reverses associated value",value==0);
        }
        page.gameObject.SetActive(false);UnityEngine.Object.Destroy(page.gameObject);yield return null;
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
