using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vlab.Product
{
    public sealed partial class VlabApp
    {
        RectTransform toolCard;EquipmentHandle workbenchItem;int toolPage;
        TextMeshProUGUI toolState;readonly Dictionary<string,Button> toolControls=new Dictionary<string,Button>();
        string workbenchStep;

        string EquipmentPurpose(EquipmentHandle item)
        {
            if(Flagship(Session.spec.id)&&Session.spec.id!="P01")return FlagshipPurpose(item);
            var task=CurrentGuide();if(task?.key==item.assetId)return ShortInstruction(task.action);
            for(int n=0;n<Session.spec.operationsEn.Length;n++)if(Guide().Preparation(Apparatus,Vi,n).key==item.assetId)return (Vi?Session.spec.operationsVi:Session.spec.operationsEn)[n];
            var controls=Apparatus.Driver?.Controls(item.assetId);
            if(controls?.Length>0)return string.Join(" · ",controls.Take(2).Select(c=>Vi?c.vi:c.en));
            var supported=Apparatus.items.FirstOrDefault(x=>Apparatus.InstallSupport(x)==item);
            return supported?L("Đỡ / giữ ","Supports ")+EquipmentName(supported):L("Dụng cụ trong bài này","Equipment for this lesson");
        }
        static string ShortInstruction(string text)
        {
            if(string.IsNullOrEmpty(text))return "";
            return text.Replace(". Nhấn Thực hiện bước này.",".").Replace(". Press Perform this step.",".");
        }
        void ShowWorkbenchPicker()
        {
            if(!Apparatus)return;CloseWorkbench();
            var box=Modal(L("Chọn dụng cụ","Choose equipment"),520,790,true);box.name="Workbench equipment picker";
            box.GetComponent<PhoneDialogLayout>().enabled=false;modal.GetComponent<Image>().color=Color.clear;
            box.gameObject.AddComponent<WorkbenchPanelLayout>().Initialize(ui,new Vector2(520,790),true);
            ui.Text(box,L("Dụng cụ của bài · Chọn để thao tác","This lesson's kit · Select to use"),28,98,464,44,24,VlabUi.Secondary);
            var preview=Apparatus.GetComponent<EquipmentPreview>()??Apparatus.gameObject.AddComponent<EquipmentPreview>();
            int count=Apparatus.items.Count,pages=Math.Max(1,(count+3)/4);equipmentPage=Mathf.Clamp(equipmentPage,0,pages-1);
            for(int n=0;n<4;n++)
            {
                int index=equipmentPage*4+n;if(index>=count)break;var item=Apparatus.items[index];
                var row=Button(box,"",24,156+n*132,472,120,()=>ChooseWorkbenchItem(item));row.name="Equipment item "+item.assetId;
                var image=ui.Picture(row.transform,preview.Get(item),10,10,100,100);image.raycastTarget=false;
                var name=ui.Text(row.transform,EquipmentName(item),124,8,332,50,26);name.overflowMode=TextOverflowModes.Ellipsis;name.maxVisibleLines=2;
                var purpose=ui.Text(row.transform,EquipmentPurpose(item),124,58,332,60,22,VlabUi.Secondary);purpose.overflowMode=TextOverflowModes.Ellipsis;purpose.maxVisibleLines=2;
            }
            var previous=Button(box,"‹",24,704,90,62,()=>{equipmentPage--;ShowWorkbenchPicker();});previous.name="Equipment previous";previous.interactable=equipmentPage>0;
            ui.Text(box,(equipmentPage+1)+" / "+pages,128,714,264,44,24,VlabUi.Secondary,TextAlignmentOptions.Center);
            var next=Button(box,"›",406,704,90,62,()=>{equipmentPage++;ShowWorkbenchPicker();});next.name="Equipment next";next.interactable=equipmentPage+1<pages;
        }
        void ChooseWorkbenchItem(EquipmentHandle item,bool take=false)
        {
            CloseModal();Apparatus.Select(item);string error="";
            if(take&&!item.installed)
            {
                if(Apparatus.TakeSelected(View.ViewportPointToRay(new Vector3(.58f,.48f,0)),out var why))PointAtEquipment(item);
                else error=LessonGuide.ExplainFailure(why,Vi);
            }
            SelectWorkbenchTool(item);if(error!=""&&toolState)toolState.text=error;RefreshActivity();
        }
        void CloseWorkbench()
        {
            if(toolCard){toolCard.gameObject.SetActive(false);Destroy(toolCard.gameObject);}toolCard=null;workbenchItem=null;toolControls.Clear();
            if(activityCard)activityCard.gameObject.SetActive(!compactActivity&&!hudHidden);RefreshPracticeSummary();
        }
        void SelectWorkbenchTool(EquipmentHandle item,string focusControl="")
        {
            if(!item||!Apparatus||Route!="activity")return;
            if(workbenchItem!=item)toolPage=0;
            CloseWorkbench();workbenchItem=item;Apparatus.Select(item);
            if(activityCard)activityCard.gameObject.SetActive(false);
            var controls=Apparatus.Driver?.Controls(item.assetId)??Array.Empty<LessonControl>();var task=CurrentGuide();
            if(!string.IsNullOrEmpty(focusControl)){int index=Array.FindIndex(controls,c=>c.id==focusControl);if(index>=0)toolPage=index/4;}
            bool preparation=item.installed&&!Apparatus.InspectingInstalled&&task?.kind=="prepare"&&task.key==item.assetId;
            if(preparation)controls=Array.Empty<LessonControl>();
            int pages=Math.Max(1,(controls.Length+3)/4);toolPage=Mathf.Clamp(toolPage,0,pages-1);
            float end=!item.installed||Apparatus.InspectingInstalled?392:preparation?346:224+Mathf.Min(4,Mathf.Max(0,controls.Length-toolPage*4))*76;
            bool extra=pages>1&&!Apparatus.InspectingInstalled||Apparatus.CanLiftForInspection(item);float footer=end+(extra?76:8),height=footer+80;
            toolCard=ui.Panel(page,"Selected equipment",40,80,480,height);toolCard.gameObject.AddComponent<WorkbenchPanelLayout>().Initialize(ui,new Vector2(480,height));
            ui.Text(toolCard,EquipmentName(item),24,22,354,68,32);
            Button(toolCard,"×",396,20,62,62,()=>{Apparatus.CancelGrab();CloseWorkbench();RefreshActivity();}).name="Close equipment controls";
            toolState=ui.Text(toolCard,EquipmentPurpose(item),24,104,432,106,24,VlabUi.Secondary);
            if(!item.installed)
            {
                Button(toolCard,Apparatus.HasLatchedCarry?L("Trả về khay","Return to tray"):L("Lấy dụng cụ","Take equipment"),24,224,432,68,()=>{if(Apparatus.HasLatchedCarry){Apparatus.CancelGrab();SelectWorkbenchTool(item);}else if(Apparatus.TakeSelected(View.ViewportPointToRay(new Vector3(.58f,.48f,0)),out var why))SelectWorkbenchTool(item);else toolState.text=LessonGuide.ExplainFailure(why,Vi);}).name="Equipment take selected";
                Button(toolCard,L("Đặt vào vị trí được đánh dấu","Place at marked position"),24,304,432,68,()=>{if(Apparatus.InstallSelected()){Save();SelectWorkbenchTool(item);}else{Apparatus.CanInstallSelected(out var why);toolState.text=LessonGuide.ExplainFailure(why,Vi);}}).name="Equipment place selected";
            }
            else if(Apparatus.InspectingInstalled)
            {
                toolState.text=L("Di chuyển con trỏ để nhìn gần. Đặt lại trả về đúng chỗ cũ, không thay đổi phép đo.","Move the pointer to inspect. Return restores its original position without altering measurements.");
                Button(toolCard,"↶",24,224,208,68,()=>Apparatus.RotateInspected(-15)).name="Rotate equipment left";
                Button(toolCard,"↷",248,224,208,68,()=>Apparatus.RotateInspected(15)).name="Rotate equipment right";
                Button(toolCard,L("Đặt lại trên bàn","Return to bench"),24,312,432,68,()=>{Apparatus.CancelGrab();SelectWorkbenchTool(item);}).name="Return inspected equipment";
            }
            else for(int n=0;n<4;n++)
            {
                int index=toolPage*4+n;if(index>=controls.Length)break;var control=controls[index];
                var button=Button(toolCard,Vi?control.vi:control.en,24,224+n*76,432,64,()=>ApplyWorkbenchControl(item,control.id));
                button.name="Instrument control "+control.id;button.interactable=control.enabled;toolControls[control.id]=button;
                if(control.id.EndsWith("+",StringComparison.Ordinal)||control.id.EndsWith("-",StringComparison.Ordinal))button.gameObject.AddComponent<HoldToAdjust>();
            }
            if(pages>1&&!Apparatus.InspectingInstalled){Button(toolCard,"‹",24,end+12,64,54,()=>{toolPage--;SelectWorkbenchTool(item);}).interactable=toolPage>0;ui.Text(toolCard,(toolPage+1)+" / "+pages,96,end+21,288,38,22,VlabUi.Secondary,TextAlignmentOptions.Center);Button(toolCard,"›",392,end+12,64,54,()=>{toolPage++;SelectWorkbenchTool(item);}).interactable=toolPage+1<pages;}
            else if(Apparatus.CanLiftForInspection(item))Button(toolCard,L("Nhấc lên quan sát","Lift to inspect"),24,end+12,432,54,()=>{if(Apparatus.LiftForInspection(item,View.ViewportPointToRay(new Vector3(.58f,.48f,0))))SelectWorkbenchTool(item);}).name="Lift equipment";
            if(preparation)
                Button(toolCard,ShortInstruction(task.action),24,224,432,110,()=>{PrepareAction();CloseWorkbench();RefreshActivity();}).name="Equipment preparation";
            Button(toolCard,L("Xem chi tiết","Inspect"),24,footer,208,60,()=>{CloseWorkbench();instrumentPage=0;ShowInstrument(item);}).name="Equipment information";
            Button(toolCard,L("Gợi ý","Hint"),248,footer,208,60,()=>{if(Profile.settings.hints)ShowEquipmentCue(item,true);else toolState.text=L("Bật Gợi ý trong Cài đặt để xem chỉ dẫn.","Enable hints in Settings to show cues.");}).name="Equipment hint";
            if(Apparatus.CanReturnToTray(item))
            {
                var remove=toolCard.GetComponentsInChildren<UnityEngine.UI.Button>().First(b=>b.name=="Equipment hint");remove.onClick.RemoveAllListeners();remove.onClick.AddListener(()=>{if(Apparatus.ReturnToTray(item)){Save();SelectWorkbenchTool(item);RefreshActivity();}});remove.name="Equipment remove";remove.GetComponentInChildren<TextMeshProUGUI>().text=L("Tháo về khay","Remove to tray");
            }
            if(item.installed&&!Apparatus.Ready)
            {
                var next=Apparatus.NextInstallableItem();toolState.text=L("Đã lắp. Tiếp theo: ","Fitted. Next: ")+(next?EquipmentName(next):"");
                var info=toolCard.GetComponentsInChildren<UnityEngine.UI.Button>().First(b=>b.name=="Equipment information");
                info.onClick.RemoveAllListeners();info.onClick.AddListener(NextPracticeTool);info.name="Equipment next required";info.GetComponentInChildren<TextMeshProUGUI>().text=L("Lấy món tiếp","Take next");
            }
            workbenchStep=GuideStep(CurrentGuide());if(Profile.settings.hints)ShowEquipmentCue(item,false);
        }
        void ApplyWorkbenchControl(EquipmentHandle item,string control)
        {
            var driver=Apparatus?.Driver;if(driver==null||item.Owner!=EquipmentGrabOwner.None)return;
            if(!driver.Controls(item.assetId).Any(c=>c.id==control&&c.enabled))return;
            bool okay=driver.Operate(control,out var message);if(okay){Guide().Applied(control);VlabAudio.Action(control,item.transform.position);ClearGuideCue();Save();}else guideMistakes++;
            if(toolState)toolState.text=LessonGuide.ExplainAction(message,okay,Vi);RefreshWorkbench();
        }
        void RefreshWorkbench()
        {
            if(!toolCard||!workbenchItem)return;
            if(activityCard)activityCard.gameObject.SetActive(false);
            foreach(var control in Apparatus.Driver?.Controls(workbenchItem.assetId)??Array.Empty<LessonControl>())
                if(toolControls.TryGetValue(control.id,out var button))button.interactable=control.enabled&&workbenchItem.Owner==EquipmentGrabOwner.None;
            string step=GuideStep(CurrentGuide());if(step!=workbenchStep){workbenchStep=step;if(toolState)toolState.text=EquipmentPurpose(workbenchItem);}
        }
    }
}
