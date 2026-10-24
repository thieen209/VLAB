using System.Linq;
using UnityEngine;
using TMPro;

namespace Vlab.Product
{
    public sealed partial class VlabApp
    {
        bool quickStartActive;int quickStartStep;RectTransform quickStartCard;TourSpotlight quickStartCue;
        TextMeshProUGUI quickStartText;string quickStartRoute,quickStartTask;
        void QuickStartChoice()
        {
            if(quickStartActive||Profile.quickStartSeen)return;
            var box=ui.Panel(page,"Quick start choice",210,900,1180,88);
            ui.Text(box,L("Lần đầu vào VLAB?","New to VLAB?"),24,22,380,48,28);
            Button(box,L("Xem hướng dẫn nhanh","Quick tour"),420,12,370,64,StartQuickStart,Accent).name="Start quick tour";
            Button(box,L("Tự khám phá","Explore on my own"),808,12,350,64,()=>{Profile.quickStartSeen=true;Save();ShowHome();}).name="Skip quick tour";
        }
        void StartQuickStart(){Profile.quickStartSeen=true;Save();quickStartActive=true;quickStartStep=0;quickStartRoute=null;ShowHome();}
        void EndQuickStart(){quickStartActive=false;if(compactAction)((RectTransform)compactAction.transform).anchoredPosition=new Vector2(560,-858);if(quickStartCard){quickStartCard.gameObject.SetActive(false);Destroy(quickStartCard.gameObject);}if(quickStartCue)Destroy(quickStartCue.gameObject);quickStartRoute=null;if(Route=="browser")ShowBrowser();}
        void UpdateQuickStart()
        {
            if(!quickStartActive||!page)return;
            int before=quickStartStep;
            if(quickStartStep==0&&Route=="browser")quickStartStep=1;
            if(quickStartStep<=1&&Route=="activity")quickStartStep=2;
            if(quickStartStep==2&&modal&&modal.Find("Workbench equipment picker"))quickStartStep=3;
            if(quickStartStep==3&&workbenchItem){quickStartStep=4;quickStartTask=GuideStep(CurrentGuide());}
            if(quickStartStep==4&&Route=="activity"&&GuideStep(CurrentGuide())!=quickStartTask)quickStartStep=5;
            if(quickStartStep>=7){EndQuickStart();return;}
            if(quickStartRoute!=Route||before!=quickStartStep||!quickStartCard)
            {
                if(quickStartCard){quickStartCard.gameObject.SetActive(false);Destroy(quickStartCard.gameObject);}if(quickStartCue)Destroy(quickStartCue.gameObject);
                quickStartRoute=Route;quickStartCard=ui.Panel(page,"Quick tour",120,900,1360,88);
                quickStartText=ui.Text(quickStartCard,"",20,14,1010,64,24);
                Button(quickStartCard,L("Tiếp","Next"),1044,12,126,64,()=>{quickStartStep++;quickStartRoute=null;}).name="Quick tour next";
                Button(quickStartCard,L("Bỏ qua","Skip tour"),1182,12,158,64,EndQuickStart).name="Quick tour exit";
                string select=Input.VrMode||Input.SimulatorMode?L("Nhìn hoặc trỏ để chọn.","Look or point to select."):L("Chạm để chọn.","Tap to select.");
                string[] vi={"Chọn một phòng bạn muốn khám phá. "+select,"Chọn bài, xem mục tiêu rồi bắt đầu. Bạn có thể quay lại bất cứ lúc nào.","Mở Dụng cụ và trợ giúp → Dụng cụ. Ảnh cho biết đúng món đang có trên bàn.","Chọn một dụng cụ. Bảng sẽ đóng để bạn nhìn lại bàn.","Thử thao tác trên dụng cụ hoặc nút của nó. Gợi ý chỉ dẫn; bạn tự thực hiện.","Gợi ý → đúng vị trí; Bảng bài học → công thức và phác thảo; Sổ tay → lưu và ôn lại kết quả.","Quay lại để tạm dừng / làm lại. Ở Trang chủ, nút VR bật hoặc thoát kính; Căn lại đặt hướng nhìn trước mặt."};
                string[] en={"Choose a laboratory to explore. "+select,"Select a lesson, read its goal, then begin. You can always go back.","Open Equipment & help → Equipment. Previews show the actual apparatus.","Choose an item. The picker closes so you can see the bench.","Try the instrument or one of its controls. Hints point; you perform the action.","Equipment & help: Hint locates the target; Lesson board has formulas and sketches; Notebook keeps results.","Back pauses or restarts. On Home, VR enters or exits the viewer; Recenter sets your forward direction."};
                quickStartText.text=(quickStartStep+1)+"/7 · "+(Vi?vi:en)[quickStartStep];
                string target=quickStartStep==0?"Subject P":quickStartStep==1?"Browser start":quickStartStep==2?"Station tools":quickStartStep==4?"Selected equipment":"";
                var found=page.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name==target);
                if(found)quickStartCue=TourSpotlight.Attach(page,(RectTransform)found,Accent);
            }
            quickStartCard.gameObject.SetActive(!modal&&!hudHidden);if(quickStartCue)quickStartCue.gameObject.SetActive(!modal&&!hudHidden);quickStartCard.SetAsLastSibling();if(compactAction)((RectTransform)compactAction.transform).anchoredPosition=new Vector2(560,-810);
        }
    }
}
