using System;
using System.Linq;
using TMPro;
using UnityEngine;

namespace Vlab.Product
{
    public sealed partial class VlabApp
    {
        void ShowMicroscopeView(EquipmentHandle item,BiologyDriver driver)
        {
            var box=Modal(L("Qua thị kính","Through the eyepiece"),1440,900,true);inspectedItem=item;
            box.name="Microscope eyepiece";box.GetComponent<PhoneDialogLayout>().enabled=false;
            box.GetComponent<UnityEngine.UI.Image>().color=Color.black;box.GetComponent<VlabPanelOpacity>().Solid=true;
            modal.GetComponent<UnityEngine.UI.Image>().color=Color.black;
            var title=box.GetComponentInChildren<TextMeshProUGUI>();title.color=Color.white;
            var layout=box.gameObject.AddComponent<MicroscopeEyepieceLayout>();
            var optics=ui.Rect(box,"Microscope optics controls",32,126,320,668);
            var answers=ui.Rect(box,"Microscope specimen controls",1088,126,320,668);
            var field=ui.Rect(box,"Microscope circular field",420,140,600,600);
            var empty=ui.Text(field,L("Đặt lam lên bàn kính và đóng hai kẹp để quan sát.","Mount the slide and close both stage clips to observe."),80,240,440,130,30,Color.white,TextAlignmentOptions.Center);
            inspectionImage=ui.Picture(field,driver.Inspection(item.assetId),0,0,600,600);inspectionImage.name="Microscope source image";inspectionImage.raycastTarget=true;
            var selection=inspectionImage.gameObject.AddComponent<InstrumentInspection>();selection.Circular=true;selection.MarkerColor=VlabUi.Hex("F0A34D");
            var presentation=inspectionImage.gameObject.AddComponent<MicroscopePresentation>();presentation.Bind(inspectionImage,driver);
            var readout=ui.Text(box,"",390,96,660,40,28,Color.white,TextAlignmentOptions.Center);readout.name="Microscope optical readout";
            var notice=ui.Text(box,MicroscopePresentation.Notice(Vi),370,750,700,60,24,Color.white,TextAlignmentOptions.Center);notice.name="Microscope schematic notice";
            var toggle=Button(box,L("Ẩn điều khiển","Hide controls"),980,22,300,68,()=>layout.SetControlsVisible(!layout.ControlsVisible));toggle.name="Microscope toggle controls";
            layout.Initialize(ui,field,optics,answers,readout,notice,title.rectTransform,toggle,box.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b=>b.name=="Close dialog"),Vi);
            selectedCellSummary=ui.Text(answers,"",0,0,320,86,27,Color.white);selectedCellSummary.name="Microscope selected cell";
            ui.Text(optics,L("VẬT KÍNH","OBJECTIVE"),0,0,320,44,26,Color.white);
            string[] objectiveIds={"obj4","obj10","obj40"};
            for(int i=0;i<3;i++){int n=i;AddControl(optics,objectiveIds[i],i*108,48,104,72,(n==0?"4":n==1?"10":"40")+"×");}
            ui.Text(optics,L("LẤY NÉT THÔ · 0,5 mm","COARSE FOCUS · 0.5 mm"),0,138,320,42,24,Color.white);
            AddControl(optics,"focus-",0,182,154,76,"−");AddControl(optics,"focus+",166,182,154,76,"+");
            ui.Text(optics,L("LẤY NÉT TINH · 0,05 mm","FINE FOCUS · 0.05 mm"),0,278,320,42,24,Color.white);
            AddControl(optics,"focusFine-",0,322,154,76,"−");AddControl(optics,"focusFine+",166,322,154,76,"+");
            ui.Text(optics,L("DỊCH VÙNG QUAN SÁT","MOVE OBSERVATION FIELD"),0,418,320,42,23,Color.white);
            AddPan("left","←",Vector2.left,0,462);AddPan("up","↑",Vector2.up,108,462);AddPan("right","→",Vector2.right,216,462);
            AddPan("center",L("Giữa","Center"),Vector2.zero,54,548);AddPan("down","↓",Vector2.down,162,548);
            var labels=driver.Controls(item.assetId).Where(c=>IsClassificationControl(c.id)).ToArray();
            for(int i=0;i<labels.Length;i++)AddControl(answers,labels[i].id,i%2*166,100+i/2*94,154,84);
            var extra=driver.Controls(item.assetId).Where(c=>!IsClassificationControl(c.id)&&!objectiveIds.Contains(c.id)&&!c.id.StartsWith("focus",StringComparison.Ordinal)).ToArray();
            int extraPage=0;RectTransform extras=ui.Rect(answers,"Microscope additional controls",0,398,320,252);
            void ExtraControls()
            {
                foreach(Transform child in extras){child.gameObject.SetActive(false);Destroy(child.gameObject);}
                for(int i=0;i<3&&extraPage*3+i<extra.Length;i++)AddControl(extras,extra[extraPage*3+i].id,0,i*78,320,68);
            }
            ExtraControls();
            if(extra.Length>3)Button(answers,L("Thao tác khác ›","More actions ›"),0,650,320,64,()=>{extraPage=(extraPage+1)%((extra.Length+2)/3);ExtraControls();}).name="Microscope more actions";
            inspectionStatus=ui.Text(box,L("Chạm vùng mẫu, rồi tự chọn nhãn.","Select a specimen region, then choose its label."),32,800,1376,40,24,Color.white);inspectionStatus.name="Microscope action status";
            var readings=Button(box,L("Số đo trực tiếp","Live readings"),32,844,300,52,()=>{inspectionTab="readings";ShowInstrument(item);});readings.name="Instrument tab readings";
            var explanation=Button(box,L("Hiểu kết quả","Interpretation"),348,844,300,52,()=>{inspectionTab="explain";ShowInstrument(item);});explanation.name="Instrument tab explain";
            var guidance=Button(box,L("Hướng dẫn","Guidance"),664,844,300,52,()=>{guidePage=-1;ShowGuidance();});
            var back=Button(box,L("Trở lại trạm","Return to station"),980,844,428,52,()=>{CloseModal();RefreshActivity();},Accent);
            layout.SetFooter(inspectionStatus.rectTransform,new[]{readings,explanation,guidance,back});
            selection.Selected=sourceUv=>
            {
                if(Session.spec.id=="B01"&&Session.Phase==ExperimentPhase.Preparing&&!BeginMicroscopeObservation())return;
                if(!driver.PointOnInspection(item.assetId,sourceUv,out var message)){inspectionStatus.text=LessonGuide.ExplainFailure(message,Vi);return;}
                selectedInspectionUv=sourceUv;selectedInspectionSession=Session;Save();selection.SetSelection(sourceUv);RefreshInstrument();
                inspectionStatus.text=L("Đã chọn vị trí. Tự chọn nhãn ở bảng bên cạnh.","Location selected. Choose its label in the side panel.");
            };
            layout.RefreshView=()=>
            {
                if(!inspectionImage)return;presentation.RefreshNow();inspectionImage.enabled=inspectionImage.texture;inspectionImage.raycastTarget=inspectionImage.texture;empty.gameObject.SetActive(!inspectionImage.texture);
                int objective=driver.MicroscopeObjectiveIndex==0?4:driver.MicroscopeObjectiveIndex==1?10:40;
                readout.text=L("Vật kính ","Objective ")+objective+"×   ·   "+L("Lấy nét ","Focus ")+driver.MicroscopeFocusMm.ToString("0.00",Vi?System.Globalization.CultureInfo.GetCultureInfo("vi-VN"):System.Globalization.CultureInfo.InvariantCulture)+" mm";
                if(ReferenceEquals(selectedInspectionSession,Session))selection.SetSelection(selectedInspectionUv);
            };
            foreach(var text in box.GetComponentsInChildren<TextMeshProUGUI>())if(!text.GetComponentInParent<UnityEngine.UI.Button>())text.color=Color.white;
            RefreshInstrument();layout.RefreshNow();

            void AddControl(Transform parent,string id,float x,float y,float width,float height,string caption=null)
            {
                var control=driver.Controls(item.assetId).FirstOrDefault(c=>c.id==id);if(control==null)return;
                var button=Button(parent,caption??(Vi?control.vi:control.en),x,y,width,height,()=>
                {
                    if(!driver.Controls(item.assetId).Any(c=>c.id==id&&c.enabled))return;
                    bool okay=driver.Operate(id,out var message);if(okay){Guide().Applied(id);VlabAudio.Action(id,item.transform.position);Save();}
                    RefreshInstrument();layout.RefreshNow();inspectionStatus.text=LessonGuide.ExplainAction(message,okay,Vi);
                });
                button.name="Instrument control "+id;button.interactable=control.enabled;button.GetComponentInChildren<TextMeshProUGUI>().fontSize=26;inspectionControls[id]=button;
                if(id.EndsWith("+",StringComparison.Ordinal)||id.EndsWith("-",StringComparison.Ordinal))button.gameObject.AddComponent<HoldToAdjust>();
            }
            void AddPan(string id,string label,Vector2 direction,float x,float y)
            {var button=Button(optics,label,x,y,104,76,()=>{driver.PanMicroscopeView(direction);layout.RefreshNow();});button.name="Microscope pan "+id;button.GetComponentInChildren<TextMeshProUGUI>().fontSize=32;}
        }
    }

    // Fits the eyepiece and its controls together, so phone focus adjustments never scroll the specimen away.
    public sealed class MicroscopeEyepieceLayout:MonoBehaviour
    {
        VlabUi ui;RectTransform field,optics,answers,readout,notice,title,status;UnityEngine.UI.Button toggle,close;UnityEngine.UI.Button[] footer;
        bool vi;public bool ControlsVisible{get;private set;}=true;public Action RefreshView;
        public void Initialize(VlabUi owner,RectTransform view,RectTransform left,RectTransform right,TextMeshProUGUI values,TextMeshProUGUI sourceNotice,RectTransform heading,UnityEngine.UI.Button controls,UnityEngine.UI.Button closeButton,bool vietnamese)
        {ui=owner;field=view;optics=left;answers=right;readout=values.rectTransform;notice=sourceNotice.rectTransform;title=heading;toggle=controls;close=closeButton;vi=vietnamese;}
        public void SetFooter(RectTransform message,UnityEngine.UI.Button[] buttons){status=message;footer=buttons;}
        public void SetControlsVisible(bool value)
        {ControlsVisible=value;optics.gameObject.SetActive(value);answers.gameObject.SetActive(value);toggle.GetComponentInChildren<TextMeshProUGUI>().text=vi?(value?"Ẩn điều khiển":"Hiện điều khiển"):(value?"Hide controls":"Show controls");RefreshNow();}
        void LateUpdate(){RefreshNow();}
        public void RefreshNow()
        {
            if(ui==null||footer==null)return;ui.PhoneViewport.Refresh();bool flat=ui.canvas.renderMode!=RenderMode.WorldSpace,portrait=flat&&ui.PhoneViewport.Portrait;
            float width=portrait?900:1440,height=portrait?1740:1000;var root=(RectTransform)transform;
            root.anchorMin=root.anchorMax=root.pivot=Vector2.one*.5f;root.sizeDelta=new Vector2(width,height);
            if(flat)
            {
                Rect safe=ui.PhoneViewport.SafePixels;float units=Mathf.Max(.001f,ui.PhoneViewport.PixelsPerLogicalUnit),scale=Mathf.Min(safe.width/width,safe.height/height);
                root.localScale=Vector3.one*(scale/units);root.anchoredPosition=(safe.center-ui.PhoneViewport.ContentPixels.center)/units;
            }
            else{root.localScale=Vector3.one;root.anchoredPosition=Vector2.zero;}
            Place(title,32,20,width-490,68);Place((RectTransform)toggle.transform,width-440,22,300,68);Place((RectTransform)close.transform,width-108,20,80,72);
            float diameter=portrait?(ControlsVisible?620:820):(ControlsVisible?600:650),fieldY=150;
            Place(field,(width-diameter)/2,fieldY,diameter,diameter);
            var image=field.GetComponentInChildren<UnityEngine.UI.RawImage>();Place(image.rectTransform,0,0,diameter,diameter);
            Place(readout,(width-720)/2,98,720,44);Place(notice,(width-760)/2,portrait?790:ControlsVisible?750:800,760,60);
            if(portrait){Place(optics,60,870,320,668);Place(answers,520,870,320,668);}
            else{Place(optics,32,140,320,668);Place(answers,1088,140,320,668);}
            if(portrait&&!ControlsVisible)Place(notice,70,980,760,70);
            float footerY=height-78;Place(status,32,footerY-58,width-64,52);
            for(int i=0;i<footer.Length;i++){float w=(width-64-36)/4;Place((RectTransform)footer[i].transform,32+i*(w+12),footerY,w,68);}
            RefreshView?.Invoke();
        }
        static void Place(RectTransform rect,float x,float y,float width,float height)
        {rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(width,height);}
    }
}
